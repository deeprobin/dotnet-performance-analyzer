using Verify = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<PerformanceAnalyzer.Analyzers.CSharp.ArrayAnalyzer>;

namespace PerformanceAnalyzer.Analyzers.CSharp.Tests
{
    public sealed class ArrayAnalyzerTests
    {
        [Fact]
        public async Task TestAnalyzer()
        {
            const string test = $@"
    using System.Runtime.CompilerServices;

    namespace PerformanceAnalyzer.TestSamples.{ArrayAnalyzer.DiagnosticId}
    {{
        public class Program
        {{   
            public void Test() {{
                int[] array = new int[1000];
            }}
        }}
    }}";

            var expected = Verify.Diagnostic(ArrayAnalyzer.DiagnosticId)
                .WithSpan(9, 31, 9, 44);
            await Verify.VerifyAnalyzerAsync(test, expected);
        }
    }
}