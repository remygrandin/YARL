using System.Reactive;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using YARL.Infrastructure.Config;
using YARL.Infrastructure.Launch;
using YARL.UI.ViewModels;

namespace YARL.Tests.Phase4;

[Trait("Category", "Phase4")]
[Trait("Class", "LaunchOverlayViewModelTests")]
public class LaunchOverlayViewModelTests
{
    private static LaunchOverlayViewModel CreateVm()
    {
        var appConfig = new AppConfig();
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var service = new GameLaunchService(appConfig, scopeFactory);
        return new LaunchOverlayViewModel(service);
    }

    [Fact]
    [Trait("Category", "Phase4")]
    public void InitialState_IsHidden()
    {
        var vm = CreateVm();
        Assert.Equal(LaunchOverlayState.Hidden, vm.State);
        Assert.False(vm.IsVisible);
    }

    [Fact]
    [Trait("Category", "Phase4")]
    public void HandleStateChange_Launching_SetsStateAndIsVisible()
    {
        var vm = CreateVm();
        vm.StartLaunch("TestGame", null);
        vm.HandleStateChanged(LaunchOverlayState.Launching, null);
        Assert.Equal(LaunchOverlayState.Launching, vm.State);
        Assert.True(vm.IsVisible);
        Assert.Equal("TestGame", vm.GameTitle);
    }

    [Fact]
    [Trait("Category", "Phase4")]
    public void HandleStateChange_Running_SetsState()
    {
        var vm = CreateVm();
        vm.HandleStateChanged(LaunchOverlayState.Running, null);
        Assert.Equal(LaunchOverlayState.Running, vm.State);
        Assert.True(vm.IsVisible);
    }

    [Fact]
    [Trait("Category", "Phase4")]
    public void HandleStateChange_Hidden_SetsHiddenAndNotVisible()
    {
        var vm = CreateVm();
        vm.HandleStateChanged(LaunchOverlayState.Running, null);
        vm.HandleStateChanged(LaunchOverlayState.Hidden, null);
        Assert.Equal(LaunchOverlayState.Hidden, vm.State);
        Assert.False(vm.IsVisible);
    }

    [Fact]
    [Trait("Category", "Phase4")]
    public void HandleStateChange_Failed_SetsFailedWithErrorMessage()
    {
        var vm = CreateVm();
        vm.HandleStateChanged(LaunchOverlayState.Failed, "exit code 1");
        Assert.Equal(LaunchOverlayState.Failed, vm.State);
        Assert.Equal("exit code 1", vm.ErrorMessage);
        Assert.True(vm.IsVisible);
    }

    [Fact]
    [Trait("Category", "Phase4")]
    public void DismissCommand_SetsHidden()
    {
        var vm = CreateVm();
        vm.HandleStateChanged(LaunchOverlayState.Running, null);
        vm.DismissCommand.Execute(Unit.Default).Subscribe();
        Assert.Equal(LaunchOverlayState.Hidden, vm.State);
        Assert.False(vm.IsVisible);
    }
}
