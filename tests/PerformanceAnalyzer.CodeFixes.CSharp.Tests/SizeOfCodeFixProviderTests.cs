using PerformanceAnalyzer.Analyzers.CSharp;
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.CodeFixVerifier<PerformanceAnalyzer.Analyzers.CSharp.SizeOfAnalyzer, PerformanceAnalyzer.CodeFixes.CSharp.SizeOfCodeFixProvider>;

namespace PerformanceAnalyzer.CodeFixes.CSharp.Tests;

public sealed class SizeOfCodeFixProviderTests
{
    [Fact]
    public async Task TestCodeFixProvider()
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

        const string fixedTest = $@"
    using System.Runtime.CompilerServices;

    namespace PerformanceAnalyzer.TestSamples.{SizeOfAnalyzer.DiagnosticId}
    {{
        public class Program
        {{   
            public void Test() {{
                var s = sizeof(long);
            }}
        }}
    }}";
        var expected = Verify.Diagnostic(SizeOfAnalyzer.DiagnosticId)
            .WithSpan(9, 25, 9, 46);
        await Verify.VerifyCodeFixAsync(test, expected, fixedTest);
    }
}