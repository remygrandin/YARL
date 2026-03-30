// Wave 0 stub — EMU-01: EmulatorRowViewModel path validity, Flatpak checkbox, and save flow.
// The test class body is guarded by #if false until Plan 02 creates EmulatorRowViewModel.
// TODO: uncomment when EmulatorRowViewModel exists:
//   using YARL.UI.ViewModels;
//   using YARL.Infrastructure.Config;
//   using NSubstitute;

namespace YARL.Tests.Phase4;

/// <summary>
/// Stub: EmulatorRowViewModel path validity indicator, IsFlatpakAvailable guard, and per-row save.
/// Will be filled in by Plan 02 (EMU-01 implementation).
/// </summary>
[Trait("Category", "Phase4")]
[Trait("Class", "EmulatorRowViewModelTests")]
public class EmulatorRowViewModelTests
{
    [Fact]
    [Trait("Category", "Phase4")]
    public void Stub_FailsUntilImplemented() => Assert.Fail("Not implemented - Wave 0 stub");
}

#if false
// --- Stubs below reference EmulatorRowViewModel which does not exist yet (Plan 02 creates it). ---

namespace YARL.Tests.Phase4
{
    using YARL.UI.ViewModels;
    using YARL.Infrastructure.Config;
    using NSubstitute;

    public class EmulatorRowViewModelTests_Full
    {
        [Fact]
        [Trait("Category", "Phase4")]
        public void PathValidity_ExistingFile_ShowsValid()
        {
            // Will test: EmulatorRowViewModel.IsPathValid returns true when the configured path points to an existing file.
            // Steps:
            //   1. Create a temp file on disk
            //   2. Construct EmulatorRowViewModel with Path = temp file path
            //   3. Assert IsPathValid == true
            Assert.Fail("Not implemented — Wave 0 stub");
        }

        [Fact]
        [Trait("Category", "Phase4")]
        public void PathValidity_NonExistentFile_ShowsInvalid()
        {
            // Will test: EmulatorRowViewModel.IsPathValid returns false when path does not exist on disk.
            Assert.Fail("Not implemented — Wave 0 stub");
        }

        [Fact]
        [Trait("Category", "Phase4")]
        public void IsFlatpakAvailable_OnWindows_ReturnsFalse()
        {
            // Will test: On non-Linux (Windows/macOS), EmulatorRowViewModel.IsFlatpakAvailable == false.
            // Uses RuntimeInformation.IsOSPlatform(OSPlatform.Linux) guard.
            Assert.Fail("Not implemented — Wave 0 stub");
        }

        [Fact]
        [Trait("Category", "Phase4")]
        public void FlatpakChecked_HidesFileBrowse()
        {
            // Will test: Setting IsFlatpak = true on the ViewModel makes the file-browse control hidden
            // (e.g. IsFileBrowseVisible == false or equivalent property).
            Assert.Fail("Not implemented — Wave 0 stub");
        }

        [Fact]
        [Trait("Category", "Phase4")]
        public void SaveCommand_PersistsToAppConfig()
        {
            // Will test: Invoking SaveCommand on EmulatorRowViewModel calls AppConfigService.Save()
            // with the updated EmulatorConfigs entry for this row's platform.
            Assert.Fail("Not implemented — Wave 0 stub");
        }
    }
}
#endif
