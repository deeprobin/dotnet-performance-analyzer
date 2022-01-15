using Verify = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<PerformanceAnalyzer.Analyzers.CSharp.ArrayAnalyzer>;

namespace PerformanceAnalyzer.Analyzers.CSharp.Tests
{
    public sealed class EnumNameAnalyzerTests
    {
        [Fact]
        public async Task TestAnalyzer()
        {
            const string test = $@"
    namespace PerformanceAnalyzer.TestSamples.{EnumNameAnalyzer.DiagnosticId}
    {{
        public class Program
        {{   
            public enum Foo {{
                A,
                B
            }}
            
            public void Test(string s) {{
                if(s == Foo.A.ToString()) {{ }}    
            }}
        }}
    }}";

            var expected = Verify.Diagnostic(EnumNameAnalyzer.DiagnosticId)
                .WithSpan(12, 25, 12, 41);
            await Verify.VerifyAnalyzerAsync(test, expected);
        }
    }
}
