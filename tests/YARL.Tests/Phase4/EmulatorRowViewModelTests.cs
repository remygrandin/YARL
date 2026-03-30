using YARL.Infrastructure.Config;
using YARL.UI.ViewModels;

namespace YARL.Tests.Phase4;

[Trait("Category", "Phase4")]
[Trait("Class", "EmulatorRowViewModelTests")]
public class EmulatorRowViewModelTests
{
    [Fact]
    [Trait("Category", "Phase4")]
    public void PathValidity_ExistingFile_ShowsValid()
    {
        var tmpFile = Path.GetTempFileName();
        try
        {
            var vm = new EmulatorRowViewModel("snes", "SNES");
            vm.ExePath = tmpFile;
            System.Threading.Thread.Sleep(50);
            Assert.True(vm.IsPathValid, $"Expected IsPathValid == true for existing file: {tmpFile}");
        }
        finally
        {
            File.Delete(tmpFile);
        }
    }

    [Fact]
    [Trait("Category", "Phase4")]
    public void PathValidity_NonExistentFile_ShowsInvalid()
    {
        var vm = new EmulatorRowViewModel("snes", "SNES");
        vm.ExePath = "/nonexistent/definitely/not/a/file/abc123.exe";
        System.Threading.Thread.Sleep(50);
        Assert.False(vm.IsPathValid, "Expected IsPathValid == false for non-existent path");
    }

    [Fact]
    [Trait("Category", "Phase4")]
    public void IsFlatpakAvailable_OnWindows_ReturnsFalse()
    {
        var vm = new EmulatorRowViewModel("snes", "SNES");
        Assert.False(vm.IsFlatpakAvailable, "IsFlatpakAvailable should be false on Windows");
    }

    [Fact]
    [Trait("Category", "Phase4")]
    public void FlatpakChecked_SkipsFileExistsCheck()
    {
        var vm = new EmulatorRowViewModel("gcn", "GameCube");
        vm.IsFlatpak = true;
        vm.ExePath = "org.DolphinEmu.dolphin-emu";
        System.Threading.Thread.Sleep(50);
        Assert.True(vm.IsPathValid, "IsPathValid should be true when IsFlatpak=true (Flatpak app ID, not file path)");
    }

    [Fact]
    [Trait("Category", "Phase4")]
    public void SaveCommand_PersistsToAppConfig()
    {
        var appConfig = new AppConfig();
        appConfig.EmulatorConfigs["snes"] = new EmulatorConfig();

        var vm = new EmulatorRowViewModel(
            "snes",
            "SNES",
            appConfig,
            null,
            null);

        vm.ExePath = "/usr/bin/snes9x";
        vm.Args = "{rompath} --fullscreen";
        vm.IsFlatpak = false;
        System.Threading.Thread.Sleep(50);
        vm.SaveCommand.Execute().Subscribe();
        System.Threading.Thread.Sleep(50);

        Assert.True(appConfig.EmulatorConfigs.ContainsKey("snes"), "EmulatorConfigs should contain 'snes' after save");
        Assert.Equal("/usr/bin/snes9x", appConfig.EmulatorConfigs["snes"].ExePath);
        Assert.Equal("{rompath} --fullscreen", appConfig.EmulatorConfigs["snes"].Args);
        Assert.False(appConfig.EmulatorConfigs["snes"].IsFlatpak);
    }
}
