using TogglHelper.Extensions;
using TogglHelper.Models;

namespace TogglHelper;

public class TimeEntryProcessor
{
    public static IEnumerable<TimeEntryUpdate> Process(TimeSpan threshold, TimeEntry[] entries)
    {
        return entries
            .Where(e => e.Stop is not null && e.WorkspaceId is not null) // Can't adjust such entries
            .SelectMany(e => // Consider all other entries
                entries
                    .Except([e]) // Exclude the current entry from comparison
                    .Select(o => (Difference: o.Start - e.Stop.GetValueOrDefault(), Entry: o)) // Calculate the difference
                    .Where(o => o.Difference.Abs() < threshold) // Filter out distant entries
                    .OrderBy(o => o.Entry.Start).Take(1) // Select the closest entry
                    .Where(o => o.Difference != default) // Already aligned/none found
                    .Select(o => new TimeEntryUpdate
                    {
                        Stop = o.Entry.Start,
                        Duration = e.Duration + (int)o.Difference.TotalSeconds,
                        OriginalEntry = e
                    })
                );
    }
}