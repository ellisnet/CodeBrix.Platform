using Xunit.Sdk;
using Xunit.v3;

// One test at a time: the PlayTest head's provider registry is process-wide, and the registration tests change it.
[assembly: Parallelization(Mode = ParallelMode.None)]
