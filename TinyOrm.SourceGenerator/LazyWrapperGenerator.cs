using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using TinyOrm.Abstraction.Attributes;

namespace TinyOrm.SourceGenerators;

[Generator]
public class LazyWrapperGenerator : ISourceGenerator
{
    public void Execute(GeneratorExecutionContext context)
    {
        // Get the syntax receiver to access the generated source code
        //if (context.SyntaxReceiver is not ISyntaxReceiver receiver)
        //{
        //    return;
        //}

        var classesToGenerate = context.Compilation
            .GetSymbolsWithName(symbolName => true, SymbolFilter.Type)
            .OfType<INamedTypeSymbol>()
            .Where(symbol =>
                symbol.GetAttributes().Any(attr => attr.AttributeClass?.Name == nameof(GenerateLazyWrapperAttribute)));

        // Get the compilation and semantic model
        var compilation = context.Compilation;
        //var semanticModel = compilation.GetSemanticModel(receiver.TargetUnit);

        var outputDirectory =
            context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.CompilerGeneratedFilesOutputPath",
                out var outputPath)
                ? outputPath
                : Path.GetDirectoryName(context.Compilation.SyntaxTrees.First().FilePath);

        //Directory.CreateDirectory(outputDirectory);

        // Generate the LazyWrapper class for each marked class
        foreach (var classToGenerate in classesToGenerate)
        {
            var generatedSourceCode = GenerateLazyClassSource(classToGenerate);
            var outputFileName = $"Lazy{classToGenerate.Name}.g.cs";
            //File.WriteAllText(Path.Combine(outputDirectory, outputFileName), generatedSourceCode);
            context.AddSource(outputFileName, SourceText.From(generatedSourceCode, Encoding.UTF8));
        }
    }

    public void Initialize(GeneratorInitializationContext context)
    {
        //throw new NotImplementedException();
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

        var propertiesWithColumnAttribute = typeSymbol
            .GetMembers()
            .OfType<IPropertySymbol>()
            .Where(prop => prop.GetAttributes().Any(attr => attr.AttributeClass?.Name == nameof(ColumnAttribute)));


        builder.AppendLine("using TinyOrm.Abstraction.Attributes;");
        builder.AppendLine("using TinyOrm.Abstraction.Data;");
        builder.AppendLine("using TinyOrm.Models;");
        builder.AppendLine();

        builder.AppendLine("namespace TinyOrm");
        // open namespace scope
        builder.AppendLine("{");

        builder.AppendLine($"    [Table(\"{GetTableName(typeSymbol)}\")]");
        builder.AppendLine($"    public class Lazy{typeSymbol.Name} : {typeSymbol.Name}");
        // open class scope
        builder.AppendLine("    {");

        foreach (var property in propertiesWithRelationAttribute)
            builder.AppendLine(
                $"        private {property.Type} {char.ToLower(property.Name[0])}{property.Name.Substring(1)};");

        builder.AppendLine("        private IDataProvider dataProvider;");
        // builder.AppendLine("        private long? id;");
        builder.AppendLine();

        builder.AppendLine($"        public Lazy{typeSymbol.Name}(IDataProvider provider, {typeSymbol.Name} source) : base(source.Id)");
        builder.AppendLine("        {");
        builder.AppendLine("            this.dataProvider = provider;");

        builder.AppendLine();

        foreach (var property in propertiesWithColumnAttribute)
            builder.AppendLine(
                $"            {char.ToUpper(property.Name[0])}{property.Name.Substring(1)} = source.{property.Name};");

        builder.AppendLine("        }");
        builder.AppendLine();

        // builder.AppendLine("        public long? Id => this.id;");
        // builder.AppendLine();

        foreach (var property in propertiesWithRelationAttribute)
        {
            var propertySourceCode =
                $$"""
                        {{GetAttributes(property)}}
                        public {{property.Type}} {{property.Name}}
                        {
                            get
                            {
                                return this.{{char.ToLower(property.Name[0])}}{{property.Name.Substring(1)}} ??= dataProvider.GetNestedEntity<{{property.Type}}>(this, nameof({{property.Name}}));;
                            }
                            set
                            {
                                {{char.ToLower(property.Name[0])}}{{property.Name.Substring(1)}} = value;
                            }
                        }
                """;

            builder.AppendLine(propertySourceCode);
            builder.AppendLine();
        }

        // close class scope
        builder.AppendLine("    }");
        // close namespace scope
        builder.AppendLine("}");

        return builder.ToString();
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