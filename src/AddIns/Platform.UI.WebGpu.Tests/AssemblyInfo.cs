using Xunit.Sdk;
using Xunit.v3;

// This suite runs one test at a time, on purpose. It shares one GPU (every GPU test creates a real WebGPU device), the
// XAML object model (the element tests construct the view under the host-free bootstrap, which makes every thread
// report dispatcher access - and the object model is not thread-safe), and wall-clock measurements (the Continuous
// rate test counts frames against a stopwatch, which a neighbouring GPU test would skew).
[assembly: Parallelization(Mode = ParallelMode.None)]
