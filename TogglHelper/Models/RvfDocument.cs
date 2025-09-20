using System.Text.Json.Serialization;

namespace TogglHelper.Models;

public class RvfDocument
{
    [JsonPropertyName("content")]
    public string? Content { get; set; }
    
    [JsonPropertyName("images")]
    public List<RvfImage> Images { get; set; } = new();
    
    [JsonPropertyName("metadata")]
    public Dictionary<string, string> Metadata { get; set; } = new();
}

public class RvfImage
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("data")]
    public string Data { get; set; } = string.Empty;
    
    [JsonPropertyName("format")]
    public string Format { get; set; } = string.Empty;
    
    [JsonPropertyName("width")]
    public int? Width { get; set; }
    
    [JsonPropertyName("height")]
    public int? Height { get; set; }
}