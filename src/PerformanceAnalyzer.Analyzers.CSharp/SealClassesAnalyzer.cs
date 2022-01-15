using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace PerformanceAnalyzer.Analyzers.CSharp
{
    /// <summary>
    /// Providing a class with the sealed modifier can provide performance improvements, since in certain cases the compiler does not have to search for classes that are affected by the
    /// This <see cref="DiagnosticAnalyzer"/> takes care of that.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class SealClassesAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "PERF0002";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Resources.SealClassesTitle, Resources.SealClassesMessageFormat, Category.JitAssistanceCategory,
            DiagnosticSeverity.Warning, true, Resources.SealClassesDescription);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);


        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            
            context.RegisterSyntaxTreeAction(HandleSyntaxTree);
        }

        private static void HandleSyntaxTree(SyntaxTreeAnalysisContext context)
        {
            var syntaxTree = context.Tree;
            var root = syntaxTree.GetRoot();
            var childClasses = GetApplicableChildClasses(root);
            foreach (var classDeclarationSyntax in childClasses)
            {
                var methodDeclarations = classDeclarationSyntax.Members
                    .Where(member => member.IsKind(SyntaxKind.MethodDeclaration))
                    .Cast<MethodDeclarationSyntax>();
                var propertyDeclarations = classDeclarationSyntax.Members
                    .Where(member => member.IsKind(SyntaxKind.PropertyDeclaration))
                    .Cast<PropertyDeclarationSyntax>();

                if (methodDeclarations.Any(method =>
                        method.Modifiers.Any(mod => mod.IsKind(SyntaxKind.VirtualKeyword)))) continue;
                if (propertyDeclarations.Any(method =>
                        method.Modifiers.Any(mod => mod.IsKind(SyntaxKind.VirtualKeyword)))) continue;

                var diagnostic = Diagnostic.Create(
                    Rule,
                    classDeclarationSyntax.Keyword.GetLocation());
                context.ReportDiagnostic(diagnostic);

            }
        }

        private static IEnumerable<ClassDeclarationSyntax> GetApplicableChildClasses(SyntaxNode node)
        {
            foreach (var subNode in node.ChildNodes())
            {
                foreach (var classDeclarationSyntax in GetApplicableChildClasses(subNode))
                {
                    yield return classDeclarationSyntax;
                }
            }

            if (!node.IsKind(SyntaxKind.ClassDeclaration)) yield break;
            var classDeclaration = (ClassDeclarationSyntax)node;

            if (classDeclaration.Modifiers.Any(mod => mod.IsKind(SyntaxKind.StaticKeyword) || mod.IsKind(SyntaxKind.SealedKeyword))) yield break;
            {
                yield return classDeclaration;
            }
        }
    }
}
