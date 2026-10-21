using GreenHome;
using Microsoft.Data.Sqlite;
using Xunit;

public class RepositoryTests
{
    private static HomeDraft Draft(string title = "Demo flat", string postcode = "1000") => new(title, 320_000_000, 80, Energy.B, postcode);
    [Fact] public void BudgetAndEnergyFiltersAreCombined()
    {
        using var repository = new PropertyRepository();
        repository.Add(Draft());
        repository.Add(new("Expensive demo", 700_000_000, 100, Energy.A, "4000"));
        repository.Add(new("Older demo", 200_000_000, 80, Energy.F, "8000"));
        Assert.Single(repository.Search(400_000_000, Energy.C));
        Assert.Equal("Copenhagen", repository.Search(400_000_000, Energy.C)[0].City);
    }
    [Fact] public void SearchTreatsInjectionAndLikeWildcardsAsLiteralText()
    {
        using var repository = new PropertyRepository();
        repository.Add(Draft("100% demo"));
        Assert.Empty(repository.Search(text: "' OR 1=1 --"));
        Assert.Single(repository.Search(text: "%"));
        Assert.Empty(repository.Search(text: "_"));
    }
    [Fact] public void FailedBatchRollsBackEarlierInserts()
    {
        using var repository = new PropertyRepository();
        Assert.Throws<SqliteException>(() => repository.AddMany([Draft(), Draft(postcode: "9999")]));
        Assert.Empty(repository.Search());
    }
    [Fact] public void CrudPreservesExactMoneyAndReportsMissingRows()
    {
        using var repository = new PropertyRepository();
        var id = repository.Add(Draft());
        Assert.True(repository.UpdatePrice(id, 12345));
        Assert.Equal(123.45m, repository.Search()[0].PriceDkk);
        Assert.True(repository.Delete(id));
        Assert.False(repository.Delete(id));
        Assert.False(repository.UpdatePrice(id, 100));
    }
    [Fact] public void InvalidDomainValuesAreRejected()
    {
        using var repository = new PropertyRepository();
        Assert.Throws<ArgumentException>(() => repository.Add(Draft() with { SquareMeters = 0 }));
        Assert.Throws<ArgumentException>(() => repository.Add(Draft() with { Energy = (Energy)999 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => repository.UpdatePrice(1, -100));
    }
}
