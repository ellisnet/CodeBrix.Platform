using Microsoft.VisualStudio.TestTools.UnitTesting;

// Several Simple-family types hold static state (SimpleServiceResolver.Instance,
// SimpleEnumHelper caches), so the suite runs sequentially like Platform.UI.Unit.Tests.
[assembly: DoNotParallelize]
