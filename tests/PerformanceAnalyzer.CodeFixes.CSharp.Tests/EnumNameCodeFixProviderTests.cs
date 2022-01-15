using PerformanceAnalyzer.Analyzers.CSharp;
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.CodeFixVerifier<PerformanceAnalyzer.Analyzers.CSharp.EnumNameAnalyzer, PerformanceAnalyzer.CodeFixes.CSharp.EnumNameCodeFixProvider>;

namespace PerformanceAnalyzer.CodeFixes.CSharp.Tests;

public sealed class EnumNameCodeFixProviderTests
{
    [Fact]
    public async Task TestCodeFixProvider()
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

        const string fixedTest = $@"
    namespace PerformanceAnalyzer.TestSamples.{EnumNameAnalyzer.DiagnosticId}
    {{
        public class Program
        {{   
            public enum Foo {{
                A,
                B
            }}
            
            public void Test(string s) {{
                if(s == nameof(Foo.A)) {{ }}    
            }}
        }}
    }}";
        var expected = Verify.Diagnostic(EnumNameAnalyzer.DiagnosticId)
            .WithSpan(12, 25, 12, 41);
        await Verify.VerifyCodeFixAsync(test, expected, fixedTest);
    }
}