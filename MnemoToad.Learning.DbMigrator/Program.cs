using DbUp;
using Microsoft.Extensions.Configuration;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.Development.json", optional: true)
    .Build();

// Priority: explicit CLI arg > DB_ADMIN_CONNECTION_STRING env var (CI/Azure) > appsettings.Development.json (local)
var connectionString = args.Length > 0
    ? args[0]
    : Environment.GetEnvironmentVariable("DB_ADMIN_CONNECTION_STRING")
      ?? configuration.GetConnectionString("Default")
      ?? throw new ArgumentException(
          "No connection string found. Provide it as a CLI argument, set DB_ADMIN_CONNECTION_STRING, " +
          "or add ConnectionStrings:Default to appsettings.Development.json.");

var scriptsPath = Path.Combine(AppContext.BaseDirectory, "Scripts");

var upgrader = DeployChanges.To
    .PostgresqlDatabase(connectionString)
    .WithScriptsFromFileSystem(scriptsPath)
    .LogToConsole()
    .Build();

var result = upgrader.PerformUpgrade();

if (!result.Successful)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine(result.Error);
    Console.ResetColor();
    Environment.Exit(1);
}

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("Success!");
Console.ResetColor();
