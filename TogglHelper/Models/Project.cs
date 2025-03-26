using System.Text.Json.Serialization;

namespace TogglHelper.Models;

public class Project
{
    [JsonPropertyName("id")]
    public long Id { get; set; }
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}