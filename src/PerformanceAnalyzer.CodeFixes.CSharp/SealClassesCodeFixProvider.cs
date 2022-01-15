using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using PerformanceAnalyzer.Analyzers.CSharp;

namespace PerformanceAnalyzer.CodeFixes.CSharp
{
    /// <summary>
    /// <see cref="CodeFixProvider"/> of <see cref="SealClassesAnalyzer"/>
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SealClassesCodeFixProvider)), Shared]
    public sealed class SealClassesCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(SealClassesAnalyzer.DiagnosticId);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
            
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the type declaration identified by the diagnostic.
            var declaration = (ClassDeclarationSyntax) root.FindNode(diagnosticSpan);

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    string.Format(CodeFixResources.SealClassesTitle, declaration.Identifier.Text),
                    ct => SealClassAsync(context.Document, declaration, ct),
                    nameof(CodeFixResources.SealClassesTitle)),
                diagnostic);
        }

        private static async Task<Solution> SealClassAsync(Document document, ClassDeclarationSyntax classDeclarationSyntax,
            CancellationToken cancellationToken)
        {
            var newClassDeclarationSyntax = classDeclarationSyntax
                .WithModifiers(classDeclarationSyntax.Modifiers.Add(SyntaxFactory.Token(SyntaxKind.SealedKeyword)));

            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var newRoot = root.ReplaceNode(classDeclarationSyntax, newClassDeclarationSyntax);
            var newDocument = document.WithSyntaxRoot(newRoot);

            return newDocument.Project.Solution;
        }
    }
}
