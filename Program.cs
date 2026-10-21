using System.Globalization;
using GreenHome;
try
{
    var database = ":memory:";
    long budget = long.MaxValue;
    var energy = Energy.G;
    var search = "";
    for (var i = 0; i < args.Length; i++)
    {
        if (args[i] == "--help") { Console.WriteLine("Green Home: [--db homes.db] [--max-price DKK] [--energy A-G] [--search text]"); return 0; }
        if (i + 1 >= args.Length) throw new ArgumentException("Every option needs a value.");
        var option = args[i++];
        switch (option)
        {
            case "--db": database = args[i]; break;
            case "--max-price": budget = checked(long.Parse(args[i], CultureInfo.InvariantCulture) * 100); break;
            case "--energy": energy = Enum.Parse<Energy>(args[i].ToUpperInvariant()); break;
            case "--search": search = args[i]; break;
            default: throw new ArgumentException($"Unknown option: {option}");
        }
    }
    using var repository = new PropertyRepository(database);
    if (repository.Search().Count == 0) repository.AddMany([
        new("Demo courtyard flat", 320_000_000, 80, Energy.B, "1000"),
        new("Demo brick house", 480_000_000, 145, Energy.D, "4000"),
        new("Demo city loft", 410_000_000, 95, Energy.C, "8000")
    ]);
    var homes = repository.Search(budget, energy, search);
    Console.WriteLine("GREEN HOME — fictional property catalogue\n");
    foreach (var home in homes) Console.WriteLine($"{home.Id}: {home.Title} | {home.PriceDkk:N0} DKK | {home.SquareMeters} m² | Energy {home.Energy} | {home.Postcode} {home.City} | {home.PricePerSquareMeter:N0} DKK/m²");
    Console.WriteLine($"\n{homes.Count} matching home(s).");
    return 0;
}
catch (Exception error) when (error is ArgumentException or FormatException or OverflowException or Microsoft.Data.Sqlite.SqliteException)
{
    Console.Error.WriteLine("Error: " + error.Message);
    return 2;
}
