using Xunit.Sdk;
using Xunit.v3;

// This suite runs one test at a time, on purpose: the engines keep process-wide state (the ApiExtensibility registry,
// AppSettingsService's static store), and EngineIsolation's assertion is about the whole process - a test running
// beside another could see (or cause) an assembly load that belongs to the other.
[assembly: Parallelization(Mode = ParallelMode.None)]
