using Microsoft.Data.Sqlite;

namespace GreenHome;
public enum Energy { A, B, C, D, E, F, G }
public record HomeDraft(string Title, long PriceOre, int SquareMeters, Energy Energy, string Postcode);
public record Home(long Id, string Title, long PriceOre, int SquareMeters, Energy Energy, string Postcode, string City)
{
    public decimal PriceDkk => PriceOre / 100m;
    public decimal PricePerSquareMeter => PriceDkk / SquareMeters;
}

public sealed class PropertyRepository : IDisposable
{
    private readonly SqliteConnection connection;
    public PropertyRepository(string dataSource = ":memory:")
    {
        connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = dataSource, ForeignKeys = true }.ToString());
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS cities(postcode TEXT PRIMARY KEY, name TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS homes(
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                title TEXT NOT NULL CHECK(length(trim(title)) BETWEEN 1 AND 100),
                price_ore INTEGER NOT NULL CHECK(price_ore > 0),
                square_meters INTEGER NOT NULL CHECK(square_meters > 0),
                energy TEXT NOT NULL CHECK(energy IN ('A','B','C','D','E','F','G')),
                postcode TEXT NOT NULL REFERENCES cities(postcode)
            );
            CREATE INDEX IF NOT EXISTS ix_homes_price ON homes(price_ore);
            INSERT OR IGNORE INTO cities VALUES('1000','Copenhagen'),('4000','Roskilde'),('8000','Aarhus');
            """;
        command.ExecuteNonQuery();
    }
    public IReadOnlyList<Home> Search(long maxPriceOre = long.MaxValue, Energy maxEnergy = Energy.G, string text = "")
    {
        if (maxPriceOre <= 0 || !Enum.IsDefined(maxEnergy)) throw new ArgumentOutOfRangeException(nameof(maxPriceOre));
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT h.id,h.title,h.price_ore,h.square_meters,h.energy,h.postcode,c.name
            FROM homes h JOIN cities c ON c.postcode=h.postcode
            WHERE h.price_ore <= $budget AND instr('ABCDEFG',h.energy) <= $rank
              AND h.title LIKE $text ESCAPE '\'
            ORDER BY h.price_ore,h.id;
            """;
        command.Parameters.AddWithValue("$budget", maxPriceOre);
        command.Parameters.AddWithValue("$rank", (int)maxEnergy + 1);
        command.Parameters.AddWithValue("$text", "%" + text.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%");
        using var reader = command.ExecuteReader();
        var result = new List<Home>();
        while (reader.Read()) result.Add(new(reader.GetInt64(0), reader.GetString(1), reader.GetInt64(2), reader.GetInt32(3), Enum.Parse<Energy>(reader.GetString(4)), reader.GetString(5), reader.GetString(6)));
        return result;
    }
    public long Add(HomeDraft home) => AddMany([home])[0];
    public IReadOnlyList<long> AddMany(IEnumerable<HomeDraft> homes)
    {
        using var transaction = connection.BeginTransaction();
        var ids = new List<long>();
        foreach (var home in homes)
        {
            if (string.IsNullOrWhiteSpace(home.Title) || home.Title.Trim().Length > 100 || home.PriceOre <= 0 || home.SquareMeters <= 0 || !Enum.IsDefined(home.Energy))
                throw new ArgumentException("Home needs a title, positive price/area and an A–G energy label.");
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "INSERT INTO homes(title,price_ore,square_meters,energy,postcode) VALUES($title,$price,$area,$energy,$postcode); SELECT last_insert_rowid();";
            command.Parameters.AddWithValue("$title", home.Title.Trim());
            command.Parameters.AddWithValue("$price", home.PriceOre);
            command.Parameters.AddWithValue("$area", home.SquareMeters);
            command.Parameters.AddWithValue("$energy", home.Energy.ToString());
            command.Parameters.AddWithValue("$postcode", home.Postcode);
            ids.Add((long)command.ExecuteScalar()!);
        }
        transaction.Commit();
        return ids;
    }
    public bool UpdatePrice(long id, long priceOre)
    {
        if (priceOre <= 0) throw new ArgumentOutOfRangeException(nameof(priceOre));
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE homes SET price_ore=$price WHERE id=$id";
        command.Parameters.AddWithValue("$price", priceOre);
        command.Parameters.AddWithValue("$id", id);
        return command.ExecuteNonQuery() == 1;
    }
    public bool Delete(long id)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM homes WHERE id=$id";
        command.Parameters.AddWithValue("$id", id);
        return command.ExecuteNonQuery() == 1;
    }
    public void Dispose() => connection.Dispose();
}
