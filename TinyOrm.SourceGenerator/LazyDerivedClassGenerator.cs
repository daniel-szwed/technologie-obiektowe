using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using TinyOrm.Abstraction.Attributes;

namespace TinyOrm.SourceGenerators;

[Generator]
public class LazyDerivedClassGenerator : ISourceGenerator
{
    public void Execute(GeneratorExecutionContext context)
    {
        var classesToGenerate = context.Compilation
            .GetSymbolsWithName(symbolName => true, SymbolFilter.Type)
            .OfType<INamedTypeSymbol>()
            .Where(symbol =>
                symbol.GetAttributes().Any(attr => attr.AttributeClass?.Name == nameof(GenerateLazyDerivedClassAttribute)));

        // Generate the LazyWrapper class for each marked class
        foreach (var classToGenerate in classesToGenerate)
        {
            var generatedSourceCode = GenerateLazyClassSource(classToGenerate);
            var outputFileName = $"Lazy{classToGenerate.Name}.g.cs";
            context.AddSource(outputFileName, SourceText.From(generatedSourceCode, Encoding.UTF8));
        }
    }

    public void Initialize(GeneratorInitializationContext context)
    {
        // No initialization required for this one
    }

    private string GenerateLazyClassSource(INamedTypeSymbol typeSymbol)
    {
        var builder = new StringBuilder();

        var entityRelationAttributes = new[]
        {
            nameof(OneToOneAttribute),
            nameof(OneToManyAttribute),
            nameof(ManyToManyAttribute)
        };

        var propertiesWithRelationAttribute = typeSymbol
            .GetMembers()
            .OfType<IPropertySymbol>()
            .Where(prop =>
                prop.GetAttributes().Any(attr => entityRelationAttributes.Any(a => a == attr.AttributeClass?.Name)));

        builder.AppendLine("using TinyOrm.Abstraction.Attributes;");
        builder.AppendLine("using TinyOrm.Abstraction.Data;");
        builder.AppendLine("using TinyOrm.Models;");
        builder.AppendLine();

        builder.AppendLine("namespace TinyOrm");
        // open namespace scope
        builder.AppendLine("{");

        builder.AppendLine($"    [Table(\"{GetTableName(typeSymbol)}\")]");
        builder.AppendLine($"    public class Lazy{typeSymbol.Name} : {typeSymbol.Name}, ILazy");
        // open class scope
        builder.AppendLine("    {");

        foreach (var property in propertiesWithRelationAttribute)
        {
            builder.AppendLine($"        private {property.Type} {GetPropertyCamelCaseName(property)};");
        }

        builder.AppendLine("        private IDataProvider dataProvider;");
        builder.AppendLine();
        builder.AppendLine("        public void SetProvider(IDataProvider provider)");
        builder.AppendLine("        {");
        builder.AppendLine("            this.dataProvider = provider;");
        builder.AppendLine("        }");
        // builder.AppendLine();

        foreach (var property in propertiesWithRelationAttribute)
        {
            builder.AppendLine();
            var propertySourceCode =
                $$"""
                        {{GetAttributes(property)}}
                        public override {{property.Type}} {{property.Name}}
                        {
                            get => this.{{GetPropertyCamelCaseName(property)}} ??= dataProvider.GetNestedEntity<{{property.Type}}>(this, nameof({{property.Name}}));
                            set => {{GetPropertyCamelCaseName(property)}} = value;
                        }
                """;

            builder.AppendLine(propertySourceCode);
        }

        // close class scope
        builder.AppendLine("    }");
        // close namespace scope
        builder.AppendLine("}");

        return builder.ToString();
    }

    private string GetPropertyCamelCaseName(IPropertySymbol property)
    {
        return $"{char.ToLower(property.Name[0])}{property.Name.Substring(1)}";
    }

    private string GetAttributes(IPropertySymbol property)
    {
        var entityRelationAttributes = new[]
        {
            nameof(OneToOneAttribute),
            nameof(OneToManyAttribute),
            nameof(ManyToManyAttribute),
            nameof(JoinTableAttribute)
        };

        var attributes = property.GetAttributes()
            .Where(attribute => entityRelationAttributes.Any(name => attribute.AttributeClass?.Name == name));

        var sb = new StringBuilder();
        var counter = 0;
        foreach (var attribute in attributes)
        {
            var attributeName = attribute.AttributeClass.Name
                .Replace("Attribute", string.Empty);
            var attributeConstructorParameter = attribute.ConstructorArguments
                .First()
                .Value
                .ToString();
            var prefix = counter == 0 ? string.Empty : "        ";
            var attributeSourceCode = $"{prefix}[{attributeName}(\"{attributeConstructorParameter}\")]"; 
            sb.AppendLine(attributeSourceCode);
            counter++;
        }

        sb.Remove(sb.Length - 1, 1);
        
        return sb.ToString();
    }

    private string GetTableName(INamedTypeSymbol typeSymbol)
    {
        return typeSymbol
            .GetAttributes()
            .First(attribute => attribute.AttributeClass.Name == nameof(TableAttribute))
            .ConstructorArguments
            .First()
            .Value.ToString();
    }
}