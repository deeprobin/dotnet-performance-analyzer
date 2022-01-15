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
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace PerformanceAnalyzer.CodeFixes.CSharp
{
    /// <summary>
    /// <see cref="CodeFixProvider"/> of <see cref="ArrayAnalyzer"/>
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ArrayCodeFixProvider)), Shared]
    public sealed class ArrayCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(ArrayAnalyzer.DiagnosticId);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var declaration = (ArrayCreationExpressionSyntax)root.FindNode(diagnosticSpan);

            var declarationParent = declaration.Parent;

            var declarationParentParent = declarationParent?.Parent;
            if (declarationParentParent == null) return;

            if (!declarationParentParent.IsKind(SyntaxKind.VariableDeclarator)) return;

            var varDeclaration = declarationParentParent.Parent;

            var localDeclarationStatement = varDeclaration?.Parent;
            if (localDeclarationStatement is null) return;
            if (!localDeclarationStatement.IsKind(SyntaxKind.LocalDeclarationStatement)) return;
            var localDeclaration = (LocalDeclarationStatementSyntax)localDeclarationStatement;

            context.RegisterCodeFix(
                CodeAction.Create(
                    CodeFixResources.EnumNameTitle,
                    ct => UseArrayPoolAsync(context.Document, localDeclaration, declaration, ct),
                    nameof(CodeFixResources.EnumNameTitle)),
                diagnostic);
        }

        private static async Task<Solution> UseArrayPoolAsync(Document document, LocalDeclarationStatementSyntax localDeclarationSyntax, ArrayCreationExpressionSyntax invocationExpressionSyntax,
            CancellationToken cancellationToken)
        {
            var elementType = invocationExpressionSyntax.Type.ElementType;
            var rankSpecifiers = invocationExpressionSyntax.Type.RankSpecifiers.ToImmutableArray();

            if(localDeclarationSyntax.Declaration.Variables.Count == 0) return document.Project.Solution;
            var arrayIdentifier = localDeclarationSyntax.Declaration.Variables[0].Identifier;

            if (rankSpecifiers.IsEmpty) return document.Project.Solution;

            var sizes = rankSpecifiers[0].Sizes.ToImmutableArray();
            if (sizes.Length != 1) return document.Project.Solution;

            var size = sizes[0];

            if (!int.TryParse(size.ToString(), out var sizeNumber)) return document.Project.Solution;
            if (sizeNumber < 1000) return document.Project.Solution;

            var block = BuildArrayPoolUsage(elementType, arrayIdentifier, sizeNumber);

            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var newRoot = root.ReplaceNode(localDeclarationSyntax, block.WithOpenBraceToken(new SyntaxToken()).WithCloseBraceToken(new SyntaxToken()));
            var newDocument = document.WithSyntaxRoot(newRoot);

            return newDocument.Project.Solution;
        }

        private static BlockSyntax BuildArrayPoolUsage(TypeSyntax typeSyntax, SyntaxToken arrayIdentifier, int rentCount)
        {
            return Block(
                LocalDeclarationStatement(
                    VariableDeclaration(
                            IdentifierName(
                                Identifier(
                                    TriviaList(),
                                    SyntaxKind.VarKeyword,
                                    "var",
                                    "var",
                                    TriviaList())))
                        .WithVariables(
                            SingletonSeparatedList(
                                VariableDeclarator(
                                        Identifier("pool"))
                                    .WithInitializer(
                                        EqualsValueClause(
                                            MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                GenericName(
                                                        Identifier("ArrayPool"))
                                                    .WithTypeArgumentList(
                                                        TypeArgumentList(
                                                            SingletonSeparatedList(
                                                                typeSyntax))),
                                                IdentifierName("Shared"))))))),
                LocalDeclarationStatement(
                    VariableDeclaration(
                            ArrayType(
                                    typeSyntax)
                                .WithRankSpecifiers(
                                    SingletonList(
                                        ArrayRankSpecifier(
                                            SingletonSeparatedList<ExpressionSyntax>(
                                                OmittedArraySizeExpression())))))
                        .WithVariables(
                            SingletonSeparatedList(
                                VariableDeclarator(
                                        arrayIdentifier)
                                    .WithInitializer(
                                        EqualsValueClause(
                                            InvocationExpression(
                                                    MemberAccessExpression(
                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                        IdentifierName("pool"),
                                                        IdentifierName("Rent")))
                                                .WithArgumentList(
                                                    ArgumentList(
                                                        SingletonSeparatedList(
                                                            Argument(
                                                                LiteralExpression(
                                                                    SyntaxKind.NumericLiteralExpression,
                                                                    Literal(rentCount))))))))))),
                TryStatement()
                    .WithBlock(
                        Block()
                            .WithCloseBraceToken(
                                Token(
                                    TriviaList(
                                        Comment("// Your implementation goes here \n")),
                                    SyntaxKind.CloseBraceToken,
                                    TriviaList())))
                    .WithFinally(
                        FinallyClause(
                            Block(
                                SingletonList<StatementSyntax>(
                                    ExpressionStatement(
                                        InvocationExpression(
                                                MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                    IdentifierName("pool"),
                                                    IdentifierName("Return")))
                                            .WithArgumentList(
                                                ArgumentList(
                                                    SingletonSeparatedList(
                                                        Argument(
                                                            IdentifierName(arrayIdentifier)))))))))));
        }
    }
}
