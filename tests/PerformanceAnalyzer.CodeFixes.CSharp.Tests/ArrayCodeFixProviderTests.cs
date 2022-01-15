using PerformanceAnalyzer.Analyzers.CSharp;
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.CodeFixVerifier<PerformanceAnalyzer.Analyzers.CSharp.ArrayAnalyzer, PerformanceAnalyzer.CodeFixes.CSharp.ArrayCodeFixProvider>;

namespace PerformanceAnalyzer.CodeFixes.CSharp.Tests;

public sealed class ArrayCodeFixProviderTests
{
    [Fact]
    public async Task TestCodeFixProvider()
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

        const string fixedTest = $@"
            using System.Runtime.CompilerServices;

            namespace PerformanceAnalyzer.TestSamples.{ArrayAnalyzer.DiagnosticId}
            {{
                public class Program
                {{   
                    public void Test() {{
                        var pool = ArrayPool<int>.Shared;
                        int[] array = pool.Rent(1000);
                        try {{
                            // Your implementation goes here
                        }}
                        finally {{
                            pool.Return(array);
                        }}
                    }}
                }}
            }}";
        var expected = Verify.Diagnostic(ArrayAnalyzer.DiagnosticId)
            .WithSpan(9, 31, 9, 44);
        await Verify.VerifyCodeFixAsync(test, expected, fixedTest);
    }
}