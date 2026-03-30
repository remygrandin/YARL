// Wave 0 stub — EMU-02: LaunchOverlayViewModel state machine transitions.
// The test class body is guarded by #if false until Plan 03 creates LaunchOverlayViewModel.
// TODO: uncomment when LaunchOverlayViewModel exists:
//   using YARL.UI.ViewModels;

namespace YARL.Tests.Phase4;

/// <summary>
/// Stub: LaunchOverlayViewModel state machine: Hidden -> Launching -> Running -> Hidden on exit,
/// Failed on error, and DismissCommand behavior.
/// Will be filled in by Plan 03 (EMU-02 implementation).
/// </summary>
[Trait("Category", "Phase4")]
[Trait("Class", "LaunchOverlayViewModelTests")]
public class LaunchOverlayViewModelTests
{
    [Fact]
    [Trait("Category", "Phase4")]
    public void Stub_FailsUntilImplemented() => Assert.Fail("Not implemented - Wave 0 stub");
}

#if false
// --- Stubs below reference LaunchOverlayViewModel which does not exist yet (Plan 03 creates it). ---

namespace YARL.Tests.Phase4
{
    using YARL.UI.ViewModels;

    public class LaunchOverlayViewModelTests_Full
    {
        [Fact]
        [Trait("Category", "Phase4")]
        public void InitialState_IsHidden()
        {
            // Will test: A freshly constructed LaunchOverlayViewModel has State == OverlayState.Hidden
            // (or IsVisible == false). No process has been launched.
            Assert.Fail("Not implemented — Wave 0 stub");
        }

        [Fact]
        [Trait("Category", "Phase4")]
        public void StartLaunch_TransitionsToLaunching()
        {
            // Will test: Calling StartLaunch(game) on the ViewModel transitions State from
            // Hidden -> Launching and sets the GameTitle property to the game's title.
            Assert.Fail("Not implemented — Wave 0 stub");
        }

        [Fact]
        [Trait("Category", "Phase4")]
        public void ProcessStarted_TransitionsToRunning()
        {
            // Will test: After StartLaunch(), when the service signals that the process has
            // actually started (e.g. NotifyProcessStarted()), State transitions to Running
            // and the play-time timer begins.
            Assert.Fail("Not implemented — Wave 0 stub");
        }

        [Fact]
        [Trait("Category", "Phase4")]
        public void ProcessExited_TransitionsToHidden()
        {
            // Will test: When the process exits normally (exit code 0), State transitions
            // Running -> Hidden (overlay auto-dismisses).
            Assert.Fail("Not implemented — Wave 0 stub");
        }

        [Fact]
        [Trait("Category", "Phase4")]
        public void ProcessFailed_TransitionsToFailed()
        {
            // Will test: When the process exits with a non-zero exit code within the grace period,
            // State transitions to Failed and an error message is available.
            Assert.Fail("Not implemented — Wave 0 stub");
        }

        [Fact]
        [Trait("Category", "Phase4")]
        public void DismissCommand_SetsHidden_DoesNotKillProcess()
        {
            // Will test: Executing DismissCommand while State == Running sets State = Hidden
            // without terminating the emulator process. The process continues running in the background.
            Assert.Fail("Not implemented — Wave 0 stub");
        }
    }
}
#endif
