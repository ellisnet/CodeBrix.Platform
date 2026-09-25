using Xunit.Sdk;
using Xunit.v3;

// This suite runs one test at a time, on purpose.
//
// Every test here builds XAML elements in one host-free process, and the framework keeps process-wide state that a real
// application only ever touches from its one UI thread: the dependency-property registry (DependencyProperty's
// name-to-property cache), Application.Current, and the platform test doubles registered once for the whole process
// (TestPlatform.cs, AddInTestPlatform.cs). Two test classes running side by side construct elements on two threads at
// once and race on that registry (measured 2026-09-23: "SR.Argument_AddingDuplicate__" from
// DependencyProperty.NameToPropertyDictionary.Add, in 2 of 20 runs, in whichever class lost the race). The
// SkiaSharp.Views suite serializes for the same kind of reason.
[assembly: Parallelization(Mode = ParallelMode.None)]
