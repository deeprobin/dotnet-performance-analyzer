using Verify = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<PerformanceAnalyzer.Analyzers.CSharp.ArrayAnalyzer>;

namespace PerformanceAnalyzer.Analyzers.CSharp.Tests;

public sealed class SealClassesAnalyzerTests
{
    [Fact]
    public async Task TestAnalyzer()
    {
        const string test = $@"
    using System.Runtime.CompilerServices;

    namespace PerformanceAnalyzer.TestSamples.{SealClassesAnalyzer.DiagnosticId}
    {{
        public class SealSample
        {{   

        }}
    }}";

        var expected = Verify.Diagnostic(SealClassesAnalyzer.DiagnosticId)
            .WithSpan(6, 16, 6, 21);
        await Verify.VerifyAnalyzerAsync(test, expected);
    }
}