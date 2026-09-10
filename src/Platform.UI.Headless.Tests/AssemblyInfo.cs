using Xunit.Sdk;
using Xunit.v3;

// This suite runs one test at a time, on purpose.
//
// The dispatcher overrides DispatcherInitializer installs are process-wide, and so are the
// framework's application-wide resource dictionaries that any templated type resolves through. The
// ported tests were written for a runner that ran them one at a time on a single UI thread, and
// running them side by side would be a different contract than the one they were written against.
//
// xUnit.net v3 4.0 made CollectionBehaviorAttribute.DisableTestParallelization obsolete-as-error;
// Xunit.v3.ParallelizationAttribute is its replacement, and ParallelMode.None is "do not run tests
// side by side". Same choice as the add-in suites.
[assembly: Parallelization(Mode = ParallelMode.None)]
