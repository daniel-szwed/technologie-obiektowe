//using System;
//using Microsoft.CodeAnalysis;
//using Microsoft.CodeAnalysis.Text;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace TinyOrm.Generators
//{


//    [Generator]
//    public class EnrollmentGenerator : ISourceGenerator
//    {
//        public void Execute(GeneratorExecutionContext context)
//        {
//            // Retrieve the syntax trees for the classes we want to generate code for.
//            SyntaxTree studentSyntaxTree = context.Compilation.SyntaxTrees
//                .FirstOrDefault(tree => tree.FilePath.EndsWith("Student.cs"));

//            SyntaxTree classSyntaxTree = context.Compilation.SyntaxTrees
//                .FirstOrDefault(tree => tree.FilePath.EndsWith("Class.cs"));

//            if (studentSyntaxTree == null || classSyntaxTree == null)
//            {
//                return;
//            }

//            // Retrieve the semantic models for the syntax trees.
//            SemanticModel studentSemanticModel = context.Compilation.GetSemanticModel(studentSyntaxTree);
//            SemanticModel classSemanticModel = context.Compilation.GetSemanticModel(classSyntaxTree);

//            // Retrieve the symbol for the Student class.
//            INamedTypeSymbol studentType = studentSemanticModel.GetDeclaredSymbol(
//                studentSyntaxTree.GetRoot()
//                    .DescendantNodes()
//                    .OfType<ClassDeclarationSyntax>()
//                    .FirstOrDefault(node => node.Identifier.ValueText == "Student"));

//            // Retrieve the symbol for the Class class.
//            INamedTypeSymbol classType = classSemanticModel.GetDeclaredSymbol(
//                classSyntaxTree.GetRoot()
//                    .DescendantNodes()
//                    .OfType<ClassDeclarationSyntax>()
//                    .FirstOrDefault(node => node.Identifier.ValueText == "Class"));

//            // Generate the code for the Enrollment class.
//            string enrollmentCode = GenerateEnrollmentCode(studentType, classType);

//            // Add the generated code to the context.
//            context.AddSource("Enrollment.cs", SourceText.From(enrollmentCode, Encoding.UTF8));
//        }

//        public void Initialize(GeneratorInitializationContext context)
//        {
//        }

//        private string GenerateEnrollmentCode(INamedTypeSymbol studentType, INamedTypeSymbol classType)
//        {
//            // Retrieve the property symbols for the ManyToMany attributes.
//            IPropertySymbol studentClassesProperty = studentType.GetMembers()
//                .OfType<IPropertySymbol>()
//                .FirstOrDefault(property =>
//                    property.GetAttributes()
//                        .Any(attribute => attribute.AttributeClass?.Name == "ManyToManyAttribute"));

//            IPropertySymbol classStudentsProperty = classType.GetMembers()
//                .OfType<IPropertySymbol>()
//                .FirstOrDefault(property =>
//                    property.GetAttributes()
//                        .Any(attribute => attribute.AttributeClass?.Name == "ManyToManyAttribute"));

//            // Retrieve the type symbols for the primary key properties.
//            ITypeSymbol studentIdType = studentType.GetMembers()
//                .OfType<IPropertySymbol>()
//                .FirstOrDefault(property =>
//                    property.GetAttributes()
//                        .Any(attribute => attribute.AttributeClass?.Name == "ColumnAttribute" &&
//                                           attribute.NamedArguments.Any(argument =>
//                                               argument.Key == "Name" &&
//                                               (string)argument.Value.Value == "id")))?.Type;

//            ITypeSymbol classIdType = classType.GetMembers()
//                .OfType<IPropertySymbol>()
//                .FirstOrDefault(property =>
//                    property.GetAttributes()
//                        .Any(attribute => attribute.AttributeClass?.Name == "ColumnAttribute" &&
//                                           attribute.NamedArguments.Any(argument =>
//                                               argument.Key == "Name" &&
//                                               (string)argument.Value.Value == "id")))?.Type;

//            // Generate the code for the Enrollment class.
//            StringBuilder code
    
//}

