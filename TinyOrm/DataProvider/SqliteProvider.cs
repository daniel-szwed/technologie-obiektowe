using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using Microsoft.Data.Sqlite;
using TinyOrm.Attributes;
using TinyOrm.Models;

namespace TinyOrm.DataProvider
{
    public class SqliteProvider : IDataProvider
	{
        private readonly string connectionString;

		public SqliteProvider(string connectionString)
		{
            this.connectionString = connectionString;
        }

        public bool CreateOrUpdate<T>(T entity)
        {
            var entityType = typeof(T);
            var tableName = GetTableName(entityType);
            var properties = entityType.GetProperties();
            var columnNames = new List<string>();
            var parameters = new List<SqliteParameter>();

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
                    }
                }
            }

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                var parameterTags = string.Join(",", columnNames.Select(column => "@" + column));

                using (var command = new SqliteCommand(
                    @$"INSERT INTO {tableName} ({string.Join(",", columnNames)})
                        VALUES ({parameterTags})",
                    connection))
                {
                    command.Parameters.AddRange(parameters);
                    command.ExecuteNonQuery();
                    using (var cmd = new SqliteCommand("SELECT last_insert_rowid()", connection))
                    {
                        long lastInsertRowId = (long)cmd.ExecuteScalar();

                        // print the ID
                        Console.WriteLine("Last inserted row ID: {0}", lastInsertRowId);
                    }
                }
            }

            return true;
        }

        public IEnumerable<T> ReadAll<T>() where T: EntityBase, new()
        {
            return GetAll<T>();
        }

        public bool Remove<T>(T entity)
        {
            throw new NotImplementedException();
        }

        private IEnumerable<T> GetAll<T>(
            string? tableName = null,
            string? navigationColumn = null,
            long?[]? navigationIds = null,
            Type[]? excluded = null) where T: EntityBase, new()
        {
            var result = new List<T>();
            var entityType = typeof(T);
            var properties = entityType.GetProperties();
            if (tableName is null)
            {
                tableName = GetTableName(entityType);
            }

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = $"SELECT * FROM {tableName}";
                if (navigationIds is not null)
                {
                    command.CommandText += $" WHERE {navigationColumn} IN (@ids)";
                    command.Parameters.AddWithValue("@ids", string.Join(",", navigationIds));
                }

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        T row = new T();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            var columnName = reader.GetName(i);
                            var value = reader.GetValue(i);
                            PropertyInfo? propertyInfo = properties.FirstOrDefault(property =>
                            {
                                return property.GetCustomAttribute<ColumnAttribute>()?.Name == columnName;
                            });
                            var dbType = reader.GetDataTypeName(i);
                            if (dbType == "INTEGER")
                            {
                                propertyInfo?.SetValue(row, value == DBNull.Value ? null : long.Parse(value.ToString()));
                            }
                            else
                            {
                                propertyInfo?.SetValue(row, value == DBNull.Value ? null : value);
                            }
                        }
                        MapOneToOne(row);
                        MapOneToMany(row);
                        MapManyToMany(row, excluded);
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

        private IEnumerable<object>? GetNestedValues(
            Type? type,
            string? tableName = null,
            string? idColumnName = null,
            long?[]? ids = null,
            Type[]? excluded = null)
        {
            MethodInfo? method = GetType().GetMethod(nameof(this.GetAll), BindingFlags.NonPublic | BindingFlags.Instance);
            Type[] genericTypes = new Type[] { type };
            MethodInfo? closedMethod = method?.MakeGenericMethod(genericTypes);
            return closedMethod?.Invoke(this, new object[] { tableName, idColumnName, ids, excluded }) as IEnumerable<object>;
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
                var nestedValues = GetNestedValues(property.PropertyType, relatedTableName, navigationColumnName, ids);
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
                var ids = new long?[] { row.Id };
                var nestedValues = GetNestedValues(
                    property.PropertyType.GenericTypeArguments.First(),
                    relatedTableName,
                    navigationColumnName,
                    ids);
                property.SetValue(row, nestedValues);
            });
        }

        private void MapManyToMany<T>(T row, Type[]? excluded) where T : EntityBase, new()
        {
            var properties = row.GetType()
                .GetProperties()
                .Where(property => property.GetCustomAttribute<ManyToManyAttribute>() is not null);

            properties.ToList().ForEach(property =>
            {
                var navigationColumnName = property
                    .GetCustomAttribute<ManyToManyAttribute>()?.NavigationProperty;
                var joinTableName = property
                    .GetCustomAttribute<JoinTableAttribute>()?.Name;
                var nestedClassType = property.PropertyType
                    .GenericTypeArguments.First();
                var relatedTableName = nestedClassType
                    .GetCustomAttribute<TableAttribute>()?.Name;
                var allItemsFromJoinTable =
                    GetNestedValues(GetTypeByClassName(joinTableName), joinTableName);
                var ids = new List<long?>();
                allItemsFromJoinTable?.ToList().ForEach(item =>
                {
                    var joinTableProperty =
                        GetTypeByClassName(joinTableName)?.GetProperties().First(property =>
                    {
                        return property.GetCustomAttribute<ColumnAttribute>()?.Name == navigationColumnName;
                    });
                    long? id = long.Parse(joinTableProperty?.GetValue(item)?.ToString());
                    if (id is not null)
                    {
                        ids.Add(id);
                    }
                });
                if (!excluded?.Any(type => property.PropertyType.GenericTypeArguments.First() == type) ?? true)
                {
                    var nestedItems = GetNestedValues(
                        nestedClassType,
                        relatedTableName,
                        "id",
                        ids.ToArray(),
                        new Type[] { typeof(T) });
                    property.SetValue(row, nestedItems);
                }
            });
        }

        private Type? GetTypeByClassName(string className)
        {
            Type[] types = Assembly.GetExecutingAssembly().GetTypes();

            return types.FirstOrDefault(type => type.Name == char.ToUpper(className[0]) + className[1..]);
        }
    }
}
