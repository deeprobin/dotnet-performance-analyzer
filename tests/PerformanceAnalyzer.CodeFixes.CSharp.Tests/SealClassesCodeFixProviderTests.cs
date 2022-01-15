using PerformanceAnalyzer.Analyzers.CSharp;
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.CodeFixVerifier<PerformanceAnalyzer.Analyzers.CSharp.SealClassesAnalyzer, PerformanceAnalyzer.CodeFixes.CSharp.SealClassesCodeFixProvider>;

namespace PerformanceAnalyzer.CodeFixes.CSharp.Tests;

public sealed class SealClassesCodeFixProviderTests
{
    [Fact]
    public async Task TestCodeFixProvider()
    {
        const string test = $@"
using System.Runtime.CompilerServices;

namespace PerformanceAnalyzer.TestSamples.{SealClassesAnalyzer.DiagnosticId}
{{
    public class SealSample
    {{   

    }}
}}";

        const string fixedTest = $@"
using System.Runtime.CompilerServices;

namespace PerformanceAnalyzer.TestSamples.{SealClassesAnalyzer.DiagnosticId}
{{
    public sealed class SealSample
    {{   

    }}
}}";

        var expected = Verify.Diagnostic(SealClassesAnalyzer.DiagnosticId)
            .WithSpan(6, 16, 6, 21);
        await Verify.VerifyCodeFixAsync(test, expected, fixedTest);
    }
}