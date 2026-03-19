using Xunit;

namespace YARL.Tests.Phase3;

public class LibraryViewModelPhase3Tests
{
    [Fact(Skip = "Wave 0 stub")]
    [Trait("Category", "Phase3")]
    public void SearchText_FiltersFilteredGames_ByTitle()
    {
        // LIB-04: Setting SearchText to "Mario" filters FilteredGames to only games containing "Mario" in Title
    }

    [Fact(Skip = "Wave 0 stub")]
    [Trait("Category", "Phase3")]
    public void SearchText_MatchesAliases()
    {
        // LIB-04: Setting SearchText to "Kirby no" matches a game with Japanese alias containing "Kirby no"
    }

    [Fact(Skip = "Wave 0 stub")]
    [Trait("Category", "Phase3")]
    public void SearchText_Debounces250ms()
    {
        // LIB-04: Using TestScheduler, verify filter is NOT applied until 250ms after last keystroke
    }

    [Fact(Skip = "Wave 0 stub")]
    [Trait("Category", "Phase3")]
    public void GenreFilter_FiltersGamesByGenre()
    {
        // LIB-05: Setting ActiveGenreFilter to "RPG" keeps only RPG games in FilteredGames
    }

    [Fact(Skip = "Wave 0 stub")]
    [Trait("Category", "Phase3")]
    public void YearFilter_FiltersGamesByYear()
    {
        // LIB-05: Setting year range 1990-1995 keeps only games with ReleaseYear in that range
    }

    [Fact(Skip = "Wave 0 stub")]
    [Trait("Category", "Phase3")]
    public void DeveloperFilter_FiltersGamesByDeveloper()
    {
        // LIB-05: Setting ActiveDeveloperFilter to "Nintendo" keeps only Nintendo-developed games
    }

    [Fact(Skip = "Wave 0 stub")]
    [Trait("Category", "Phase3")]
    public void MultipleFilters_CombineWithAndLogic()
    {
        // LIB-05: Genre="RPG" + Developer="Square" returns only games matching BOTH criteria
    }

    [Fact(Skip = "Wave 0 stub")]
    [Trait("Category", "Phase3")]
    public void ClearFilters_RestoresFullPlatformList()
    {
        // LIB-05: After setting genre + year, clearing all filters returns all games for the platform
    }
}
