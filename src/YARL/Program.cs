using Avalonia;
using ReactiveUI.Avalonia.Splat;
using YARL;

// Temporary entry point — Plan 02 will replace with full Generic Host + DI bridge wiring.
var builder = AppBuilder.Configure<App>()
    .UsePlatformDetect()
    .WithInterFont()
    .LogToTrace()
    .UseReactiveUIWithMicrosoftDependencyResolver(_ => { });

builder.StartWithClassicDesktopLifetime(args);
