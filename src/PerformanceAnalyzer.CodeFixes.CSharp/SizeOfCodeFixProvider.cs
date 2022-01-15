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
    /// <see cref="CodeFixProvider"/> of <see cref="SizeOfAnalyzer"/>
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SizeOfCodeFixProvider)), Shared]
    public sealed class SizeOfCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(SizeOfAnalyzer.DiagnosticId);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var declaration = (InvocationExpressionSyntax)root.FindNode(diagnosticSpan);

            context.RegisterCodeFix(
                CodeAction.Create(
                    CodeFixResources.SizeOfTitle,
                    ct => UseSizeOfAsync(context.Document, declaration, ct),
                    nameof(CodeFixResources.SizeOfTitle)),
                diagnostic);
        }

        private static async Task<Solution> UseSizeOfAsync(Document document, SyntaxNode invocationExpressionSyntax,
            CancellationToken cancellationToken)
        {
            var childNodes = invocationExpressionSyntax.ChildNodes().ToImmutableArray();
            var methodAccessExpression = (MemberAccessExpressionSyntax)childNodes[0];

            var methodChildNodes = methodAccessExpression.ChildNodes().ToImmutableArray();
            var genericMethodName = (GenericNameSyntax)methodChildNodes[1];
            var genericTypes = genericMethodName.TypeArgumentList.Arguments;
            var genericType = genericTypes.First();

            var sizeOfExpression = SyntaxFactory.SizeOfExpression(genericType);

            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var newRoot = root.ReplaceNode(invocationExpressionSyntax, sizeOfExpression);
            var newDocument = document.WithSyntaxRoot(newRoot);

            return newDocument.Project.Solution;
        }
    }
}
