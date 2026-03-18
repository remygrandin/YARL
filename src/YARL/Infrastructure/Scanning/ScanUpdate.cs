namespace YARL.Infrastructure.Scanning;

public record ScanUpdate(string PlatformName, int GamesFound, int TotalProcessed, bool IsComplete = false);
