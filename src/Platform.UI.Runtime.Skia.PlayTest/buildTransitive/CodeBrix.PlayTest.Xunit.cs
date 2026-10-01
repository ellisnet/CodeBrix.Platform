// Compiled into xUnit v3 4+ consumers; no test or fixture annotations are needed.
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CodeBrix.Platform.PlayTest.Recording;
using Xunit;
using Xunit.v3;

[assembly: AssemblyFixture(typeof(CodeBrix.Platform.PlayTest.TestingPlatform.ScreenshotRecordingFixture))]
[assembly: CodeBrix.Platform.PlayTest.TestingPlatform.ScreenshotRecording]

namespace CodeBrix.Platform.PlayTest.TestingPlatform;

internal sealed class ScreenshotRecordingFixture : IAsyncLifetime, INotifyTestLifecycleAsync
{
    public ScreenshotRecordingFixture() { }
    public ValueTask InitializeAsync() => new(PlayTestRecording.StartRunAsync(typeof(ScreenshotRecordingFixture).Assembly));
    public ValueTask DisposeAsync() => new(PlayTestRecording.FinishRunAsync());

    public ValueTask OnTestStartingAsync(IXunitTest test) => new(PlayTestRecording.TestStartingAsync(
        test.UniqueID, test.TestDisplayName, test.TestMethod.TestClass.Class, test.TestMethod.Method,
        test.TestMethod.Method.GetCustomAttributes().Any(a => a is ITheoryAttribute),
        test.TestMethodArguments, test.TestCase.SourceFilePath, test.TestCase.SourceLineNumber));

    public ValueTask OnTestFinishedAsync(IXunitTest test)
    {
        var state = TestContext.Current.TestState;
        return new(PlayTestRecording.TestFinishedAsync(test.UniqueID, state?.Result.ToString().ToLowerInvariant() ?? "unknown", state?.ExceptionMessages));
    }
}

// Before/After bracket the body, after InitializeAsync and before DisposeAsync.
// The virtual UI has its own dispatcher; recording never needs this runner thread.
internal sealed class ScreenshotRecordingAttribute : BeforeAfterTestAttribute
{
    public override void Before(MethodInfo methodUnderTest, IXunitTest test)
        => PlayTestRecording.TestBodyStartingAsync().GetAwaiter().GetResult();
    public override void After(MethodInfo methodUnderTest, IXunitTest test)
        => PlayTestRecording.TestBodyFinishedAsync().GetAwaiter().GetResult();
}
