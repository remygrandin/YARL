using System.Reactive;
using Avalonia.Threading;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using YARL.Infrastructure.Launch;

namespace YARL.UI.ViewModels;

public partial class LaunchOverlayViewModel : ReactiveObject
{
    private readonly GameLaunchService _launchService;
    private DispatcherTimer? _playTimer;
    private DateTime _timerStartTime;

    [Reactive] private LaunchOverlayState _state = LaunchOverlayState.Hidden;
    [Reactive] private string _gameTitle = "";
    [Reactive] private string? _coverArtPath;
    [Reactive] private TimeSpan _elapsedPlayTime;
    [Reactive] private bool _isVisible;
    [Reactive] private string? _errorMessage;

    public ReactiveCommand<Unit, Unit> SwitchToGameCommand { get; }
    public ReactiveCommand<Unit, Unit> DismissCommand { get; }

    public LaunchOverlayViewModel(GameLaunchService launchService)
    {
        _launchService = launchService;

        // Register as the state callback on the launch service
        _launchService.SetStateCallback(HandleStateChanged);

        SwitchToGameCommand = ReactiveCommand.Create(() =>
        {
            _launchService.BringToFront();
        });

        DismissCommand = ReactiveCommand.Create(() =>
        {
            StopTimer();
            State = LaunchOverlayState.Hidden;
            IsVisible = false;
        });
    }

    public void StartLaunch(string gameTitle, string? coverArtPath)
    {
        GameTitle = gameTitle;
        CoverArtPath = coverArtPath;
        ElapsedPlayTime = TimeSpan.Zero;
        ErrorMessage = null;
    }

    public void HandleStateChanged(LaunchOverlayState newState, string? errorMessage)
    {
        State = newState;
        ErrorMessage = errorMessage;

        switch (newState)
        {
            case LaunchOverlayState.Hidden:
                IsVisible = false;
                StopTimer();
                break;
            case LaunchOverlayState.Launching:
                IsVisible = true;
                break;
            case LaunchOverlayState.Running:
                IsVisible = true;
                StartTimer();
                break;
            case LaunchOverlayState.Failed:
                IsVisible = true;
                StopTimer();
                break;
        }
    }

    private void StartTimer()
    {
        _timerStartTime = DateTime.UtcNow;
        _playTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _playTimer.Tick += (_, _) =>
        {
            ElapsedPlayTime = DateTime.UtcNow - _timerStartTime;
        };
        _playTimer.Start();
    }

    private void StopTimer()
    {
        _playTimer?.Stop();
        _playTimer = null;
    }
}
