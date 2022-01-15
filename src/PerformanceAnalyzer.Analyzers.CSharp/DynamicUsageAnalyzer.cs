using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace PerformanceAnalyzer.Analyzers.CSharp
{
    /// <summary>
    /// This <see cref="DiagnosticAnalyzer"/> suggests you not to use <see langword="dynamic"/>s, as they significantly degrade the performance.
    /// There are only a few cases (such as interoperability) where this might be useful.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class DynamicUsageAnalyzer : DiagnosticAnalyzer
    {
        private const string DiagnosticId = "PERF0003";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Resources.DynamicUsageTitle, Resources.DynamicUsageMessageFormat, "Performance",
            DiagnosticSeverity.Warning, true, Resources.DynamicUsageDescription);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeVariableDeclaration, SyntaxKind.VariableDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeCastExpression, SyntaxKind.CastExpression);
        }

        private static void AnalyzeVariableDeclaration(SyntaxNodeAnalysisContext context)
        {
            var varDeclarationNode = (VariableDeclarationSyntax)context.Node;
            if (varDeclarationNode.Type.ToString() != "dynamic") return;

            context.ReportDiagnostic(Diagnostic.Create(Rule, varDeclarationNode.GetLocation()));
        }

        private static void AnalyzeCastExpression(SyntaxNodeAnalysisContext context)
        {
            var castExpressionNode = (CastExpressionSyntax)context.Node;
            if (castExpressionNode.Type.ToString() != "dynamic") return;

            context.ReportDiagnostic(Diagnostic.Create(Rule, castExpressionNode.GetLocation()));
        }
    }
}
