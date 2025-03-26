using System.Text.Json.Serialization;

namespace TogglHelper.Models;

public class TimeEntryUpdate
{
    [JsonPropertyName("stop")]
    public DateTimeOffset? Stop { get; set; }
    [JsonPropertyName("duration")]
    public long Duration { get; set; }
    [JsonIgnore]
    public TimeEntry OriginalEntry { get; set; }
}