using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using System.Text;
using Microsoft.Data.Sqlite;
using TinyOrm.Attributes;
using TinyOrm.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace TinyOrm.DataProvider
{
    public class SqliteProvider : IDataProvider
	{
        private readonly string connectionString;

		public SqliteProvider(string connectionString)
		{
            this.connectionString = connectionString;
        }

        public T CreateOrUpdate<T>(T entity) where T: EntityBase
        {
            var entityType = typeof(T);
            var tableName = GetTableName(entityType);
            var properties = entityType.GetProperties();
            var columnNames = new List<string>();
            var parameters = new List<SqliteParameter>();
            long? currentId = null;
            var columnNameToValue = new Dictionary<string, object>();

            foreach (var property in properties)
            {
                var columnAttribute = property.GetCustomAttribute<ColumnAttribute>();

                if (columnAttribute?.Name is not null)
                {
                    if (columnAttribute.Name != "id")
                    {
                        columnNames.Add(columnAttribute.Name);
                        parameters.Add(new SqliteParameter()
                        {
                            ParameterName = "@" + columnAttribute.Name,
                            Value = property.GetValue(entity)
                        });
                        columnNameToValue.Add(columnAttribute.Name, property.GetValue(entity)!);
                    }
                    else
                    {
                        var id = property.GetValue(entity!);
                        if (id is not null)
                        {
                            currentId = (long)id;
                        }
                    }
                }
            }

            long lastInsertRowId;

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                var parameterTags = string.Join(",", columnNames.Select(column => "@" + column));

                string commandText = currentId is null ?
                    $@"INSERT INTO {tableName} ({string.Join(",", columnNames)})
                        VALUES ({parameterTags})" :
                    GenerateUpdateQuery(tableName!, currentId, columnNameToValue);

                using (var command = new SqliteCommand(commandText, connection))
                {
                    command.Parameters.AddRange(parameters);
                    command.ExecuteNonQuery();
                    using (var cmd = new SqliteCommand("SELECT last_insert_rowid()", connection))
                    {
                        lastInsertRowId = (long)cmd.ExecuteScalar()!;
                    }
                }
            }

            if(entity.Id is null)
            {
                entity.Id = lastInsertRowId;
            }  

            InsertOrUpdateOneToOne(entity);
            InsertOrUpdateOneToMany(entity);
            InsertOrUpdateManyToMany(entity);

            return entity;
        }

        public string GenerateUpdateQuery(string tableName, long? rowId, Dictionary<string, object> columnNameToValue)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append($"UPDATE {tableName} SET ");

            // Iterate through the dictionary and append each column name and value to the SET clause.
            int count = 0;
            foreach (KeyValuePair<string, object> kvp in columnNameToValue)
            {
                sb.Append($"{kvp.Key} = '{kvp.Value}'");
                if (++count < columnNameToValue.Count)
                {
                    sb.Append(", ");
                }
            }

            sb.Append($" WHERE id = {rowId}");

            return sb.ToString();
        }

        private void InsertOrUpdateOneToOne<T>(T entity) where T : EntityBase
        {
            var entityType = entity.GetType();
            var oneToOneProperties = entityType.GetProperties().Where(property =>
            {
                return property.GetCustomAttribute<OneToOneAttribute>() is not null;
            }).ToList();

            foreach(var property in oneToOneProperties)
            {
                var nestedEntity = property.GetValue(entity);
                var navigationProperty = nestedEntity?.GetType().GetProperties()
                    .First(property => property?.GetCustomAttribute<ColumnAttribute>()?.Name == entityType.Name.ToLower() + "_id");
                navigationProperty?.SetValue(nestedEntity, entity.Id);
                MethodInfo? method = GetType().GetMethod(nameof(this.CreateOrUpdate), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                Type[] genericTypes = new Type[] { nestedEntity!.GetType() };
                MethodInfo? closedMethod = method?.MakeGenericMethod(genericTypes);
                closedMethod!.Invoke(this, new object[] { nestedEntity! });
            };
        }

        private void InsertOrUpdateOneToMany<T>(T entity) where T : EntityBase
        {
            var entityType = entity.GetType();
            var oneToManyProperties = entityType.GetProperties().Where(property =>
            {
                return property.GetCustomAttribute<OneToManyAttribute>() is not null;
            }).ToList();

            foreach(var property in oneToManyProperties)
            {
                var nestedEntities = property.GetValue(entity) as IEnumerable<EntityBase>;
                if (nestedEntities is not null)
                {
                    foreach(var nestedEntity in nestedEntities)
                    {
                        var navigationProperty = nestedEntity?.GetType().GetProperties()
                            .First(property => property?.GetCustomAttribute<ColumnAttribute>()?.Name == entityType.Name.ToLower() + "_id");
                        navigationProperty?.SetValue(nestedEntity, entity.Id);
                        MethodInfo? method = GetType().GetMethod(nameof(this.CreateOrUpdate), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        Type[] genericTypes = new Type[] { nestedEntity!.GetType() };
                        MethodInfo? closedMethod = method?.MakeGenericMethod(genericTypes);
                        closedMethod?.Invoke(this, new object[] { nestedEntity! });
                    }
                }
            };
        }

        private void InsertOrUpdateManyToMany<T>(T entity) where T : EntityBase
        {
            var entityType = entity.GetType();
            var manyToManyProperties = entityType.GetProperties().Where(property =>
            {
                return property.GetCustomAttribute<ManyToManyAttribute>() is not null;
            }).ToList();

            foreach (var property in manyToManyProperties)
            {
                var nestedEntities = property.GetValue(entity) as IEnumerable<EntityBase>;
                if (nestedEntities is not null)
                {
                    foreach (var nestedEntity in nestedEntities)
                    {
                        // insert or update related entity
                        MethodInfo? method = GetType().GetMethod(nameof(this.CreateOrUpdate), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        Type[] genericTypes = new Type[] { nestedEntity!.GetType() };
                        MethodInfo? closedMethod = method?.MakeGenericMethod(genericTypes);
                        var nestedEntityWithId = closedMethod?.Invoke(this, new object[] { nestedEntity! });

                        // add entry to join table if not exists
                        var joinTableName = property.GetCustomAttribute<JoinTableAttribute>()!.Name;
                        var entityKeyColumn = entityType.Name.ToLower() + "_id";
                        var nestedEntityKeyColumn = property.GetCustomAttribute<ManyToManyAttribute>()!.NavigationProperty;
                        var nestedEntityId = ((EntityBase)nestedEntityWithId!).Id;
                        var commandText = @$"SELECT * FROM {joinTableName}
                                       WHERE {entityKeyColumn} = {entity.Id}
                                       AND {nestedEntityKeyColumn} = {nestedEntityId}";

                        bool relationExists = false;
                        using (var connection = new SqliteConnection(connectionString))
                        {
                            connection.Open();
                            using (var command = new SqliteCommand(commandText, connection))
                            {
                                var reader = command.ExecuteReader();
                                if (reader.HasRows)
                                {
                                    relationExists = true;
                                }
                            }
                            if (!relationExists)
                            {
                                commandText = $@"INSERT INTO {joinTableName} ({entityKeyColumn},{nestedEntityKeyColumn})
                                                VALUES ({entity.Id},{nestedEntityId})";
                                using (var command = new SqliteCommand(commandText, connection))
                                {
                                    command.ExecuteNonQuery();
                                }
                            }
                        }
                    }
                }
            };
        }

        public IEnumerable<T> ReadAll<T>() where T: EntityBase, new()
        {
            return GetAll<T>();
        }

        public bool Remove<T>(T entity) where T: EntityBase
        {
            var tableName = entity!.GetType().GetCustomAttribute<TableAttribute>()!.Name;
            var commandText = @$"DELETE FROM {tableName} WHERE id = {entity.Id}";

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqliteCommand(commandText, connection))
                {
                    try
                    {
                        int result = command.ExecuteNonQuery();

                        return result > 0;
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine(ex.Message);

                        return false;
                    }
                }
            }
        }

        private IEnumerable<T> GetAll<T>(Specification specification = null!) where T: EntityBase, new()
        {
            var result = new List<T>();
            var entityType = typeof(T);
            var properties = entityType.GetProperties();
            string tableName;
            if (specification?.TableName is not null)
            {
                tableName = specification.TableName;
            }
            else
            {
                tableName = GetTableName(entityType)!;
            }

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = $"SELECT * FROM {tableName}";
                if (specification?.DesiredValues is not null)
                {
                    command.CommandText += $" WHERE {specification.ColumnName} IN (@ids)";
                    command.Parameters.AddWithValue("@ids", string.Join(",", specification.DesiredValues));
                }

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        T row = new();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            var columnName = reader.GetName(i);
                            var value = reader.GetValue(i);
                            PropertyInfo? propertyInfo = properties.FirstOrDefault(property =>
                            {
                                return property.GetCustomAttribute<ColumnAttribute>()?.Name == columnName;
                            });
                            propertyInfo?.SetValue(row, value == DBNull.Value ? null : value);
                        }
                        MapOneToOne(row);
                        MapOneToMany(row);
                        MapManyToMany(row, specification?.ExcludedTypes);
                        result.Add(row);
                    }
                }
            }

            return result;
        }

        private string? GetTableName(Type entityType)
        {
            var tableAttribute = entityType.GetCustomAttribute<TableAttribute>();

            return tableAttribute?.Name;
        }

        private IEnumerable<object>? GetNestedValues(Type? type, Specification specification)
        {
            MethodInfo? method = GetType().GetMethod(nameof(this.GetAll), BindingFlags.NonPublic | BindingFlags.Instance);
            Type[] genericTypes = new Type[] { type! };
            MethodInfo? closedMethod = method?.MakeGenericMethod(genericTypes);

            return closedMethod?.Invoke(this, new object[] { specification }) as IEnumerable<object>;
        }

        private void MapOneToOne<T>(T row) where T : EntityBase, new()
        {
            var properties = row.GetType()
                .GetProperties()
                .Where(property => property.GetCustomAttribute<OneToOneAttribute>() is not null);

            properties.ToList().ForEach(property =>
            {
                var relatedTableName = property.PropertyType
                    .GetCustomAttribute<TableAttribute>()?.Name;
                var navigationColumnName = property
                    .GetCustomAttribute<OneToOneAttribute>()?.NavigationProperty;
                var ids = new long?[] { row.Id };
                var specification = new Specification()
                {
                    TableName = relatedTableName,
                    ColumnName = navigationColumnName,
                    DesiredValues = ids,
                };
                var nestedValues = GetNestedValues(property.PropertyType, specification );
                property.SetValue(row, nestedValues?.FirstOrDefault() );
            });
        }

        private void MapOneToMany<T>(T row) where T : EntityBase, new()
        {
            var properties = row.GetType()
                .GetProperties()
                .Where(property => property.GetCustomAttribute<OneToManyAttribute>() is not null);

            properties.ToList().ForEach(property =>
            {
                var relatedTableName = property.PropertyType
                    .GenericTypeArguments.First().GetCustomAttribute<TableAttribute>()?.Name;
                var navigationColumnName = property
                    .GetCustomAttribute<OneToManyAttribute>()?.NavigationProperty;
                var specification = new Specification()
                {
                    TableName = relatedTableName,
                    ColumnName = navigationColumnName,
                    DesiredValues = new long?[] { row.Id },
                };
                var nestedValues = GetNestedValues(
                    property.PropertyType.GenericTypeArguments.First(), specification);
                property.SetValue(row, nestedValues);
            });
        }

        private void MapManyToMany<T>(T row, Type[]? excluded) where T : EntityBase, new()
        {
            var properties = row.GetType()
                .GetProperties()
                .Where(property => property.GetCustomAttribute<ManyToManyAttribute>() is not null);

            foreach(var property in properties)
            {
                var navigationColumnName = property
                    .GetCustomAttribute<ManyToManyAttribute>()!.NavigationProperty;
                var joinTableName = property
                    .GetCustomAttribute<JoinTableAttribute>()!.Name;
                var nestedClassType = property.PropertyType
                    .GenericTypeArguments.First();
                var relatedTableName = nestedClassType
                    .GetCustomAttribute<TableAttribute>()!.Name;
                var entityColumnName = GetTableName(typeof(T));
                var allItemsFromJoinTable = GetNestedValues(
                    GetTypeByClassName(joinTableName!),
                    new Specification()
                    {
                        TableName = joinTableName,
                        ColumnName = row.GetType().Name.ToLower() + "_id",
                        DesiredValues = new long?[] { row.Id }
                    });
                var ids = new List<long?>();
                allItemsFromJoinTable?.ToList().ForEach(item =>
                {
                    var joinTableProperty =
                        GetTypeByClassName(joinTableName)!.GetProperties().First(property =>
                    {
                        return property.GetCustomAttribute<ColumnAttribute>()?.Name == navigationColumnName;
                    });
                    long? id = long.Parse(joinTableProperty!.GetValue(item)!.ToString()!);
                    if (id is not null)
                    {
                        ids.Add(id);
                    }
                });
                if (!excluded?.Any(type => property.PropertyType.GenericTypeArguments.First() == type) ?? true)
                {
                    var specification = new Specification()
                    {
                        TableName = relatedTableName,
                        ColumnName = "id",
                        DesiredValues = ids.ToArray(),
                        ExcludedTypes = new Type[] { typeof(T) }
                    };
                    var nestedItems = GetNestedValues(nestedClassType, specification);
                    property.SetValue(row, nestedItems);
                }
            };
        }

        private Type? GetTypeByClassName(string className)
        {
            Type[] types = Assembly.GetExecutingAssembly().GetTypes();

            return types.FirstOrDefault(type => type.Name == char.ToUpper(className[0]) + className[1..]);
        }
    }
}
