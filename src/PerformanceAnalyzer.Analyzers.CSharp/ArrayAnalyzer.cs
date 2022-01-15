using System.Buffers;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace PerformanceAnalyzer.Analyzers.CSharp
{
    /// <summary>
    /// This analyzer suggests you to use an <see cref="ArrayPool{T}"/> if the array exceeds 1000 entries. 
    /// This is useful in many cases.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ArrayAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "PERF0005";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Resources.ArrayTitle, Resources.ArrayMessageFormat, "Performance", DiagnosticSeverity.Info, true, Resources.ArrayDescription);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeArrayCreationDeclaration, SyntaxKind.ArrayCreationExpression);
        }

        private static void AnalyzeArrayCreationDeclaration(SyntaxNodeAnalysisContext context)
        {
            var arrayCreationNode = (ArrayCreationExpressionSyntax)context.Node;
            var arrayTypeSyntax = arrayCreationNode.Type;
            if (arrayTypeSyntax is null) return;

            var rankSpecifiers = arrayTypeSyntax.RankSpecifiers.ToImmutableArray();
            if (rankSpecifiers.IsEmpty) return;

            var sizes = rankSpecifiers[0].Sizes.ToImmutableArray();
            if (sizes.Length != 1) return;

            var size = sizes[0];

            if (!int.TryParse(size.ToString(), out var sizeNumber)) return;
            if (sizeNumber < 1000) return;
            
            context.ReportDiagnostic(Diagnostic.Create(Rule, arrayCreationNode.GetLocation()));
        }
    }
}
