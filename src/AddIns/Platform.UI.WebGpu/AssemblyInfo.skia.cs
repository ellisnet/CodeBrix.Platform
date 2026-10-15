using System.Runtime.CompilerServices;

// CodeBrix.Platform.UI.WebGpu.Skia (the element, the presenter and the hand-off): the add-in's own test suite.
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.WebGpu.Skia.Tests")]

// Its UIReqs pair: the direct-path scenarios read the view's picture through its surface's capture (a software head
// cannot composite the GPU image those scenarios produce).
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.AddIn.WebGpu.UIReqs")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.AddIn.WebGpu.UIReqs.Portrait")]
