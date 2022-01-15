using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace PerformanceAnalyzer.Analyzers.CSharp
{
    /// <summary>
    /// <see cref="string.Empty"/> currently results in bad code generation when using <see cref="string.Empty"/> (<a href="https://github.com/dotnet/runtime/issues/42694">dotnet/runtime#42694</a>).
    /// 
    /// This <see cref="DiagnosticAnalyzer"/> preemptively ensures that this does not happen.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class EmptyStringAnalyzer : DiagnosticAnalyzer
    {
        private const string DiagnosticId = "PERF0004";

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.EmptyStringTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.EmptyStringMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.EmptyStringDescription), Resources.ResourceManager, typeof(Resources));

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category.JitAssistanceCategory, DiagnosticSeverity.Info, true, Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.EnableConcurrentExecution();
            
            context.RegisterSyntaxNodeAction(HandleMemberAccess, SyntaxKind.SimpleMemberAccessExpression);
        }

        private static void HandleMemberAccess(SyntaxNodeAnalysisContext context)
        {
            var node = (MemberAccessExpressionSyntax)context.Node;
            if (!node.Expression.IsKind(SyntaxKind.PredefinedType)) return;
            var expression = (PredefinedTypeSyntax)node.Expression;
            if (!expression.Keyword.IsKind(SyntaxKind.StringKeyword)) return;

            var nameSyntax = node.Name;
            if (nameSyntax.Identifier.Text != nameof(string.Empty)) return;
            context.ReportDiagnostic(Diagnostic.Create(Rule, nameSyntax.GetLocation()));
        }
    }
}
