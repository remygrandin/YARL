// Wave 0 stub — EMU-02: GameLaunchService ProcessStartInfo construction and DB update after process exit.
// The test class body is guarded by #if false until Plan 03 creates GameLaunchService.
// TODO: uncomment when GameLaunchService exists:
//   using YARL.Infrastructure.Launch;
//   using YARL.Infrastructure.Config;
//   using YARL.Domain.Models;
//   using Microsoft.EntityFrameworkCore;

namespace YARL.Tests.Phase4;

/// <summary>
/// Stub: GameLaunchService ProcessStartInfo building (native + Flatpak), rompath quoting,
/// and DB update (LastPlayedAt, TotalPlayTime, Failed state) after process exit.
/// Will be filled in by Plan 03 (EMU-02 implementation).
/// </summary>
[Trait("Category", "Phase4")]
[Trait("Class", "GameLaunchServiceTests")]
public class GameLaunchServiceTests
{
    [Fact]
    [Trait("Category", "Phase4")]
    public void Stub_FailsUntilImplemented() => Assert.Fail("Not implemented - Wave 0 stub");
}

#if false
// --- Stubs below reference GameLaunchService which does not exist yet (Plan 03 creates it). ---

namespace YARL.Tests.Phase4
{
    using YARL.Infrastructure.Launch;
    using YARL.Infrastructure.Config;
    using YARL.Domain.Models;
    using Microsoft.EntityFrameworkCore;

    public class GameLaunchServiceTests_Full
    {
        [Fact]
        [Trait("Category", "Phase4")]
        public void BuildStartInfo_NativeEmulator_SetsFileNameAndArgs()
        {
            // Will test: For a native (non-Flatpak) emulator config, BuildStartInfo returns a
            // ProcessStartInfo where FileName == EmulatorConfig.Path and
            // Arguments contains the substituted rompath.
            // Example: EmulatorConfig { Path="/usr/bin/snes9x", Args="{rompath}", IsFlatpak=false }
            //          romPath="/home/user/roms/game.sfc"
            //          => psi.FileName == "/usr/bin/snes9x", psi.Arguments == "/home/user/roms/game.sfc"
            Assert.Fail("Not implemented — Wave 0 stub");
        }

        [Fact]
        [Trait("Category", "Phase4")]
        public void BuildStartInfo_Flatpak_SetsFileNameToFlatpakAndRunArgs()
        {
            // Will test: For a Flatpak emulator config, BuildStartInfo returns a ProcessStartInfo
            // where FileName == "flatpak" and Arguments starts with "run <AppId>".
            // Example: EmulatorConfig { AppId="org.snes9x.Snes9x", Args="{rompath}", IsFlatpak=true }
            //          => psi.FileName == "flatpak", psi.Arguments starts with "run org.snes9x.Snes9x"
            Assert.Fail("Not implemented — Wave 0 stub");
        }

        [Fact]
        [Trait("Category", "Phase4")]
        public void BuildStartInfo_RomPathWithSpaces_IsQuoted()
        {
            // Will test: When the ROM path contains spaces, the {rompath} substitution wraps it in
            // double-quotes so the external process receives a single argument.
            // Example: romPath="/home/user/roms/Super Mario World.sfc"
            //          => Arguments contains "\"Super Mario World.sfc\"" (or full quoted path)
            Assert.Fail("Not implemented — Wave 0 stub");
        }

        [Fact]
        [Trait("Category", "Phase4")]
        public void OnProcessExited_UpdatesLastPlayedAtAndTotalPlayTime()
        {
            // Will test: After the launched process exits normally (exit code 0),
            // GameLaunchService updates the Game row in an in-memory SQLite database:
            //   - LastPlayedAt is set to a recent UTC DateTime
            //   - TotalPlayTime is incremented by the session duration
            // Uses Microsoft.EntityFrameworkCore.InMemory or SQLite :memory: provider.
            Assert.Fail("Not implemented — Wave 0 stub");
        }

        [Fact]
        [Trait("Category", "Phase4")]
        public void OnProcessExited_NonZeroExitCodeWithinGrace_SetsFailed()
        {
            // Will test: When the process exits with a non-zero exit code AND the session
            // duration is less than the grace period (e.g. < 5 seconds), the service sets
            // a failed launch indicator (e.g. Game.LastLaunchFailed = true or equivalent).
            Assert.Fail("Not implemented — Wave 0 stub");
        }
    }
}
#endif
