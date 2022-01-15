using Verify = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<PerformanceAnalyzer.Analyzers.CSharp.ArrayAnalyzer>;

namespace PerformanceAnalyzer.Analyzers.CSharp.Tests
{
    public sealed class SizeOfAnalyzerTests
    {
        [Fact]
        public async Task TestAnalyzer()
        {
            const string test = $@"
    using System.Runtime.CompilerServices;

    namespace PerformanceAnalyzer.TestSamples.{SizeOfAnalyzer.DiagnosticId}
    {{
        public class Program
        {{   
            public void Test() {{
                var s = Unsafe.SizeOf<long>();
            }}
        }}
    }}";

            var expected = Verify.Diagnostic(SizeOfAnalyzer.DiagnosticId)
                .WithSpan(9, 25, 9, 46);
            await Verify.VerifyAnalyzerAsync(test, expected);
        }
    }
}
