using ReactiveUI.Builder;
using System.Runtime.CompilerServices;

/// <summary>
/// Module initializer that bootstraps ReactiveUI for unit tests.
/// ReactiveUI v23 requires initialization before WhenAnyValue/WhenAny can be used.
/// </summary>
internal static class ReactiveUITestInit
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        // Initialize ReactiveUI with core services only — no platform (Avalonia/WPF) required.
        // This satisfies EnsureInitialized() checks in WhenAnyValue/WhenAny.
        RxAppBuilder.CreateReactiveUIBuilder()
            .WithCoreServices()
            .BuildApp();
    }
}
