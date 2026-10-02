using Xunit.Sdk;
using Xunit.v3;

// This suite runs one test at a time, on purpose: PlayTest preferences live in process-wide
// AppContext keys and environment variables (see PlayTestSettingsScope), and a test that changes
// one and restores it on Dispose is only safe if nothing else reads it meanwhile.
// Xunit.v3.ParallelizationAttribute replaces the obsolete-as-error
// CollectionBehaviorAttribute.DisableTestParallelization of xUnit.net v3 4.0.
[assembly: Parallelization(Mode = ParallelMode.None)]
