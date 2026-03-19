using Xunit;

namespace YARL.Tests.Phase3;

public class MetadataOverrideTests
{
    [Fact(Skip = "Wave 0 stub")]
    [Trait("Category", "Phase3")]
    public async Task Rescrape_SkipsOverriddenGame()
    {
        // META-04: Game with IsMetadataOverridden=true is NOT updated by scraper pipeline
        await Task.CompletedTask;
    }

    [Fact(Skip = "Wave 0 stub")]
    [Trait("Category", "Phase3")]
    public async Task ManualEdit_SetsIsMetadataOverridden()
    {
        // META-04: After user edits a field, IsMetadataOverridden is set to true
        await Task.CompletedTask;
    }

    [Fact(Skip = "Wave 0 stub")]
    [Trait("Category", "Phase3")]
    public async Task ManualEdit_PersistsToDatabase()
    {
        // META-04: Edited fields are saved to DB and survive re-read
        await Task.CompletedTask;
    }

    [Fact(Skip = "Wave 0 stub")]
    [Trait("Category", "Phase3")]
    public async Task RevertOverride_AllowsRescrape()
    {
        // META-04: Setting IsMetadataOverridden=false allows scraper to update fields again
        await Task.CompletedTask;
    }
}
