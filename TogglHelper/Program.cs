using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using TogglHelper;
using TogglHelper.Extensions;
using TogglHelper.Models;

var builder = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddUserSecrets<Program>()
    .Build();

var appSettings = new AppSettings();
builder.Bind(appSettings);

Console.OutputEncoding = Encoding.UTF8;

var today = DateOnly.FromDateTime(DateTime.Today);
var processingDate = args.Length > 0 ? DateOnly.ParseExact(args[0], "yyMMdd") : today;
var processingThreshold = args.Length > 1 ? TimeSpan.Parse(args[1]) : appSettings.Threshold;

var httpClient = new HttpClient
{
    BaseAddress = appSettings.Toggl.Url,
    DefaultRequestHeaders =
    {
        Authorization = new AuthenticationHeaderValue("Basic",
            Convert.ToBase64String(
                Encoding.ASCII.GetBytes(
                    $"{appSettings.Toggl.Token}:api_token"
                )
            )
        )
    }
};

Console.Write("Checking API connection... ");
var meResponse = await httpClient.GetAsync("/api/v9/me");
if (meResponse.IsSuccessStatusCode)
{
    using (ConsoleColorScope.Green) Console.WriteLine("OK");

    do
    {
        var minDate = today.AddDays(-appSettings.Toggl.LimitDays);
        if (processingDate < minDate)
        {
            using (ConsoleColorScope.Red) Console.WriteLine($"Cannot process entries older than {appSettings.Toggl.LimitDays} days.");
            break;
        }

        var startDate = processingDate;
        var endDate = startDate.AddDays(1);

        Console.Write($"Fetching entries for {startDate:yyyy-MM-dd}...");
        var getTimeEntriesResponse = await httpClient.GetAsync($"api/v9/me/time_entries?start_date={startDate:yyyy-MM-dd}&end_date={endDate:yyyy-MM-dd}");
        var getTimeEntriesResponseJson = await getTimeEntriesResponse.Content.ReadAsStringAsync();
        if (getTimeEntriesResponse.IsSuccessStatusCode)
        {
            using (ConsoleColorScope.Green) Console.WriteLine($"OK ({getTimeEntriesResponse.Content.Headers.ContentLength} Bytes)");

            var backupFilePath = $"{DateTime.UtcNow:yyyyMMddhhmmss}.bak.json";
            Console.Write($"Backing up payload as {backupFilePath}... ");
            await using (var file = File.Create(backupFilePath))
            {
                var buffer = Encoding.UTF8.GetBytes(getTimeEntriesResponseJson);
                await file.WriteAsync(buffer);
                using (ConsoleColorScope.Green) Console.WriteLine($"OK ({buffer.Length} Bytes)");
            }

            Console.Write("Deserializing entries...");
            var timeEntries = JsonSerializer.Deserialize<TimeEntry[]>(getTimeEntriesResponseJson)!;
            using (ConsoleColorScope.Green) Console.WriteLine($"OK ({timeEntries.Length} entries)");


            var projectNames = new Dictionary<(long?, long?), string?>();
            var workspaceIds = timeEntries.Where(e => e.WorkspaceId.HasValue).Select(e => e.WorkspaceId).Distinct();
            foreach (var workspaceId in workspaceIds)
            {
                Console.Write($"Fetching projects for workspace #{workspaceId}...");
                var getProjectsResponse = await httpClient.GetAsync($"api/v9/workspaces/{workspaceId}/projects");
                var getProjectsResponseResponseJson = await getProjectsResponse.Content.ReadAsStringAsync();
                var projects = JsonSerializer.Deserialize<Project[]>(getProjectsResponseResponseJson)!;
                using (ConsoleColorScope.Green) Console.WriteLine($"OK ({projects.Length} projects)");
                foreach (var project in projects)
                {
                    projectNames[(workspaceId, project.Id)] = project.Name;
                }
            }

            Console.Write("Processing entries...");
            var updates = TimeEntryProcessor.Process(processingThreshold, timeEntries).ToList();
            using (ConsoleColorScope.Green) Console.WriteLine($"OK ({updates.Count} updates)");

            var totalDifference = TimeSpan.Zero;
            foreach (var entry in timeEntries.OrderBy(u => u.Start))
            {
                Console.WriteLine($"┠─#{entry.Id}────────────────────────────────────");
                Console.Write($"┃   {entry.Start.ToLocalTime():HH:mm:ss}      ");
                Console.WriteLine(string.IsNullOrWhiteSpace(entry.Description) ? "No description" : entry.Description.Truncate(30));
                Console.Write($"┃   {(entry.Stop.HasValue ? $"{entry.Stop.GetValueOrDefault().ToLocalTime():HH:mm:ss}" : "        ")}      ");
                projectNames.TryGetValue((entry.WorkspaceId, entry.ProjectId), out var projectName);
                Console.WriteLine(string.IsNullOrWhiteSpace(projectName) ? "No project" : projectName.Truncate(30));
                var update = updates.SingleOrDefault(u => u.OriginalEntry.Id == entry.Id);
                if (update is null) continue;
                Console.Write("┃ ");
                var (prefix, color) = update.Duration > entry.Duration ? (" +", ConsoleColor.Green) : (" ", ConsoleColor.Red);
                var difference = TimeSpan.FromSeconds(update.Duration - entry.Duration);
                using (new ConsoleColorScope(color)) Console.WriteLine(prefix + difference);
                totalDifference += difference;
            }
            Console.WriteLine("┖ Done");
            if (totalDifference != TimeSpan.Zero)
            {
                Console.Write("Net difference: ");
                using (new ConsoleColorScope(totalDifference > TimeSpan.Zero ? ConsoleColor.Green : ConsoleColor.Red)) Console.WriteLine(totalDifference);
            }

            if (updates.Count != 0)
            {
                using (ConsoleColorScope.Yellow) Console.WriteLine($"Apply {updates.Count} updates? [y/N]");
                var input = Console.ReadLine() ?? string.Empty;
                switch (input.ToLowerInvariant())
                {
                    case "y":
                        foreach (var update in updates)
                        {
                            Console.Write($"Updating #{update.OriginalEntry.Id}... ");
                            var updateJson = JsonSerializer.Serialize(update);
                            var updateContent = new StringContent(updateJson, Encoding.UTF8, MediaTypeNames.Application.Json);
                            var updateResponse = await httpClient.PutAsync($"api/v9/workspaces/{update.OriginalEntry.WorkspaceId!.Value}/time_entries/{update.OriginalEntry.Id}", updateContent);
                            if (updateResponse.IsSuccessStatusCode)
                            {
                                using (ConsoleColorScope.Green) Console.WriteLine("OK");
                            }
                            else
                            {
                                using (ConsoleColorScope.Red)
                                {
                                    Console.WriteLine(updateResponse.ReasonPhrase);
                                    Console.WriteLine("Stopping further processing.");
                                }
                                break;
                            }
                        }
                        break;
                    default:
                        Console.WriteLine("No changes applied.");
                        break;
                }
            }
            using (ConsoleColorScope.Yellow) Console.WriteLine("Go deeper? [Enter]");
            var goDeeperResponse = Console.ReadLine() ?? string.Empty;
            if (goDeeperResponse.Trim().ToLowerInvariant() is not ("no" or "n" or "exit"))
            {
                processingDate = processingDate.AddDays(-1);
            }
            else break;
        }
        else
        {
            using (ConsoleColorScope.Red) Console.WriteLine(getTimeEntriesResponse.ReasonPhrase);
        }

    } while (true);
}
else
{
    using (ConsoleColorScope.Red) Console.WriteLine(meResponse.ReasonPhrase);
}