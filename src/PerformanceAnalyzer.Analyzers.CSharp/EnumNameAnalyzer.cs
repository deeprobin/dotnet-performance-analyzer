using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace PerformanceAnalyzer.Analyzers.CSharp
{
    /// <summary>
    /// <see cref="Enum.ToString()"/> is slower than <see langword="nameof"/>.
    /// This analyzer takes care of suggesting <see langword="nameof"/> if this is possible in the semantic context.
    ///
    /// This will be replaced by a runtime analyzer in the future (<a href="https://github.com/dotnet/runtime/issues/62976">dotnet/runtime#62976</a>)..
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class EnumNameAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "PERF0001";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Resources.EnumNameTitle, Resources.EnumNameMessageFormat, Category.GeneralCategory,
            DiagnosticSeverity.Warning, true, Resources.EnumNameDescription);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.EnableConcurrentExecution();

            context.RegisterOperationAction(AnalyzeOperation, OperationKind.Invocation);
        }

        private static void AnalyzeOperation(OperationAnalysisContext context)
        {
            var method = ((IInvocationOperation)context.Operation).TargetMethod;
            if (method.ContainingType.SpecialType == SpecialType.System_Enum
                && method.Name == nameof(object.ToString)
                && method.OverriddenMethod?.ContainingType.SpecialType == SpecialType.System_ValueType)
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule,
                    context.Operation.Syntax.GetLocation()));
            }
        }
    }
}
