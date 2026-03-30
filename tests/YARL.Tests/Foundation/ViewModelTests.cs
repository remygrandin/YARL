using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using YARL.Infrastructure.Config;
using YARL.Infrastructure.Launch;
using YARL.UI.ViewModels;

namespace YARL.Tests.Foundation;

public class ViewModelTests
{
    private static MainViewModel CreateMainVm(LibraryViewModel? libraryVm = null)
    {
        libraryVm ??= new LibraryViewModel();
        var launchService = new GameLaunchService(new AppConfig(), Substitute.For<IServiceScopeFactory>());
        var launchOverlay = new LaunchOverlayViewModel(launchService);
        return new MainViewModel(libraryVm, new SettingsViewModel(), launchOverlay);
    }

    [Fact]
    public void MainViewModel_ExposesLibraryViewModel()
    {
        var libraryVm = new LibraryViewModel();
        var mainVm = CreateMainVm(libraryVm);

        Assert.Same(libraryVm, mainVm.LibraryViewModel);
    }

    [Fact]
    public void MainViewModel_ImplementsIScreen()
    {
        var mainVm = CreateMainVm();

        Assert.NotNull(mainVm.Router);
    }

    [Fact]
    public void LibraryViewModel_HasDefaultStatusMessage()
    {
        var vm = new LibraryViewModel();

        Assert.NotNull(vm.StatusMessage);
        Assert.NotEmpty(vm.StatusMessage);
    }

    [Fact]
    public void LibraryViewModel_StatusMessageRaisesPropertyChanged()
    {
        var vm = new LibraryViewModel();
        string? changedProperty = null;
        vm.PropertyChanged += (_, e) => changedProperty = e.PropertyName;

        vm.StatusMessage = "Scanning...";

        Assert.Equal("StatusMessage", changedProperty);
        Assert.Equal("Scanning...", vm.StatusMessage);
    }

    [Fact]
    public void BothShellsShareSameLibraryViewModel()
    {
        // Simulates DI: both shells get the same MainViewModel which holds the shared LibraryViewModel
        var libraryVm = new LibraryViewModel();
        var mainVm = CreateMainVm(libraryVm);

        // Both shells would receive mainVm as DataContext
        // Their bindings access mainVm.LibraryViewModel — same instance
        Assert.Same(mainVm.LibraryViewModel, libraryVm);

        // Verify state changes propagate
        libraryVm.StatusMessage = "Scanning 42 files...";
        Assert.Equal("Scanning 42 files...", mainVm.LibraryViewModel.StatusMessage);
    }
}
