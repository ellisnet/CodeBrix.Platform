using Xunit.Sdk;
using Xunit.v3;

// This suite runs one test at a time, on purpose.
//
// Three things it touches are process-wide, not per-control: the fake IDisplayInformationExtension
// it registers with the platform's extensibility registry (one registration for the whole process,
// and DisplayInformation caches one instance per window id), the framework's application-wide
// resource dictionaries, which the add-in's default styles are resolved through, and the icon
// rasterisation cache, which is keyed by source, theme, size, scale and tint and shared by every
// icon in the process. A test that changes one of them and restores it in a finally is only safe if
// nothing else is running meanwhile.
//
// xUnit.net v3 4.0 made CollectionBehaviorAttribute.DisableTestParallelization obsolete-as-error;
// Xunit.v3.ParallelizationAttribute is its replacement, and ParallelMode.None is "do not run tests
// side by side". The named collection in DisplayScale.cs stays as well: it says WHICH switch a
// class touches, and it keeps the guarantee if this attribute is ever relaxed.
[assembly: Parallelization(Mode = ParallelMode.None)]
