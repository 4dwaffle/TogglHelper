using System.Text.Json.Serialization;

namespace TogglHelper.Models;

public class TimeEntry
{
    [JsonPropertyName("id")]
    public long Id { get; set; }
    [JsonPropertyName("workspace_id")]
    public long? WorkspaceId { get; set; }
    [JsonPropertyName("start")]
    public DateTimeOffset Start { get; set; }
    [JsonPropertyName("stop")]
    public DateTimeOffset? Stop { get; set; }
    [JsonPropertyName("duration")]
    public long Duration { get; set; }
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("project_id")]
    public int? ProjectId { get; set; }
}