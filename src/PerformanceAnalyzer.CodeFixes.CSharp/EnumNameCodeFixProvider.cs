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
    /// <see cref="CodeFixProvider"/> of <see cref="EnumNameAnalyzer"/>
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(EnumNameCodeFixProvider)), Shared]
    public sealed class EnumNameCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(EnumNameAnalyzer.DiagnosticId);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            
            var declaration = (InvocationExpressionSyntax) root.FindNode(diagnosticSpan);
            
            context.RegisterCodeFix(
                CodeAction.Create(
                    CodeFixResources.EnumNameTitle,
                    ct => UseNameOfAsync(context.Document, declaration, ct),
                    nameof(CodeFixResources.EnumNameTitle)),
                diagnostic);
        }

        private static async Task<Solution> UseNameOfAsync(Document document, InvocationExpressionSyntax invocationExpressionSyntax,
            CancellationToken cancellationToken)
        {
            var childNodes = invocationExpressionSyntax.ChildNodes().ToImmutableArray();
            var methodAccessExpression = (MemberAccessExpressionSyntax)childNodes[0];

            var methodChildNodes = methodAccessExpression.ChildNodes().ToImmutableArray();
            var enumAccess = (MemberAccessExpressionSyntax)methodChildNodes[0];

            var newInvocationExpressionSyntax = invocationExpressionSyntax.ReplaceNode(methodAccessExpression,
                SyntaxFactory.IdentifierName("nameof"))
                .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(new []{ SyntaxFactory.Argument(enumAccess) })));

            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var newRoot = root.ReplaceNode(invocationExpressionSyntax, newInvocationExpressionSyntax);
            var newDocument = document.WithSyntaxRoot(newRoot);

            return newDocument.Project.Solution;
        }
    }
}
