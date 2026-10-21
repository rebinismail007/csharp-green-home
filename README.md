# Green Home — C# / ADO.NET / SQLite

A console property catalogue reconstructed from the supplied **Domain Model: Green Home** and **Noter Normalformer og spørgsmål** PDFs. New implementation, October 2026; no original project folder was recovered.

The PDFs describe properties with price, area, energy label and postcode, plus a separate city/postcode table and buyer/seller/broker roles. This deliberately smaller implementation covers the property and database parts, using fictional listings and no personal accounts.

## Run

.NET 10 SDK.

```sh
dotnet run
dotnet run -- --max-price 4000000 --energy C
dotnet run -- --search flat
dotnet run -- --db homes.db
dotnet test tests/GreenHome.Tests.csproj
```

By default the database is in memory. `--db` persists a local SQLite database; three demo homes are seeded when it is empty. `--energy C` includes A, B and C. The console exposes searching; the repository and tests also demonstrate create, update and delete operations.

## Implementation

- Direct ADO.NET commands, readers and transactions through `Microsoft.Data.Sqlite`.
- Separate `cities` and `homes` tables joined by a foreign key, reflecting the normalization notes.
- Integer øre storage and decimal conversion instead of the PDF's floating-point price field.
- Parameterized SQL and escaped LIKE wildcards; injected text remains data.
- Atomic batch insertion: a missing postcode rolls back the entire batch.
- Five tests against actual temporary in-memory SQLite databases.

The SQL-injection material in **BASIC NETWORK CONCEPTS** informs an offline defensive test. No external site is contacted. The original plaintext account fields are not recreated. Energy labels are the simplified A–G teaching model, not a full implementation of Danish energy-certificate classes.
