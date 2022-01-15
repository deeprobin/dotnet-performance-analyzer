using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace PerformanceAnalyzer.Analyzers.CSharp
{
    /// <summary>
    /// This analyzer tries to make you avoid <see cref="Unsafe.SizeOf{T}"/>
    /// due bad code generation (<a href="https://github.com/dotnet/runtime/issues/55472">dotnet/runtime#55472</a>).
    ///
    /// <br/>
    /// <br/>
    /// Code Generation of <see cref="Unsafe.SizeOf{T}"/>
    /// <code>
    ///     mov eax, [rcx+8]
    ///     cmp eax, 4
    ///     setl al
    ///     movzx eax, al
    ///     ret
    /// </code>
    ///
    /// Code Generation of <see langword="sizeof"/>
    /// <code>
    ///     cmp dword ptr [rcx+8], 4
    ///     setl al
    ///     movzx eax, al
    ///     ret
    /// </code>
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class SizeOfAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "PERF0004";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Resources.SizeOfTitle, Resources.SizeOfMessageFormat, Category.JitAssistanceCategory,
            DiagnosticSeverity.Warning, true, Resources.SizeOfDescription);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.EnableConcurrentExecution();

            context.RegisterOperationAction(AnalyzeOperation, OperationKind.Invocation);
        }

        private static void AnalyzeOperation(OperationAnalysisContext context)
        {
            var operation = (IInvocationOperation)context.Operation;
            
            var method = operation.TargetMethod;

            if (method.ContainingNamespace == null || method.ContainingNamespace.Name != nameof(System.Runtime.CompilerServices) ||
                method.ContainingNamespace.ContainingNamespace == null ||
                method.ContainingNamespace.ContainingNamespace.Name != nameof(System.Runtime) ||
                method.ContainingNamespace.ContainingNamespace.ContainingNamespace == null ||
                method.ContainingNamespace.ContainingNamespace.ContainingNamespace.Name != nameof(System) ||
                method.ContainingType.Name != nameof(Unsafe) || method.Name != nameof(Unsafe.SizeOf) ||
                !method.IsGenericMethod) return;

            var typeParameters = method.TypeArguments;
            if (typeParameters.Length != 1) return;

            var typeParam = typeParameters[0];
            if (!(typeParam is INamedTypeSymbol namedType)) return;
                
            // sizeof only supports unmanaged types
            if (!namedType.IsUnmanagedType) return;
                
            context.ReportDiagnostic(Diagnostic.Create(Rule,
                context.Operation.Syntax.GetLocation()));
        }
    }
}
