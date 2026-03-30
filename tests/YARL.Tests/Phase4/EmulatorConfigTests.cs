// Wave 0 stub — EMU-01: AppConfig JSON round-trip for emulator config persistence.
// The test class body is guarded by #if false until Plan 02 creates the EmulatorConfig record.
// TODO: uncomment when EmulatorConfig exists:
//   using YARL.Infrastructure.Config;

namespace YARL.Tests.Phase4;

/// <summary>
/// Stub: AppConfigService JSON round-trip for EmulatorConfig persistence.
/// Will be filled in by Plan 02 (EMU-01 implementation).
/// </summary>
[Trait("Category", "Phase4")]
[Trait("Class", "EmulatorConfigTests")]
public class EmulatorConfigTests
{
    [Fact]
    [Trait("Category", "Phase4")]
    public void Stub_FailsUntilImplemented() => Assert.Fail("Not implemented - Wave 0 stub");
}

#if false
// --- Stubs below reference EmulatorConfig which does not exist yet (Plan 02 creates it). ---

namespace YARL.Tests.Phase4
{
    using System.Text.Json;
    using YARL.Infrastructure.Config;

    public class EmulatorConfigTests_Full
    {
        [Fact]
        [Trait("Category", "Phase4")]
        public void EmulatorConfig_RoundTrip_JsonSerializeDeserialize()
        {
            // Will test: AppConfig with EmulatorConfigs dictionary survives AppConfigService Load/Save round-trip.
            // Steps:
            //   1. Create AppConfig with EmulatorConfigs = { "snes" -> new EmulatorConfig { Path = "/usr/bin/snes9x", Args = "{rompath}" } }
            //   2. Serialize to temp file via AppConfigService.Save()
            //   3. Load back via AppConfigService.Load()
            //   4. Assert EmulatorConfigs["snes"].Path == "/usr/bin/snes9x"
            Assert.Fail("Not implemented — Wave 0 stub");
        }

        [Fact]
        [Trait("Category", "Phase4")]
        public void EmulatorConfig_DefaultArgs_IsRompath()
        {
            // Will test: new EmulatorConfig().Args == "{rompath}"
            Assert.Fail("Not implemented — Wave 0 stub");
        }

        [Fact]
        [Trait("Category", "Phase4")]
        public void EmulatorConfig_EmptyDictionary_DeserializesToEmpty()
        {
            // Will test: loading config JSON with no "emulatorConfigs" key gives empty dict (not null).
            Assert.Fail("Not implemented — Wave 0 stub");
        }
    }
}
#endif
