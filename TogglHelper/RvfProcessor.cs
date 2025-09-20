using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using System.Text.Json;
using System.Text.RegularExpressions;
using TogglHelper.Models;

namespace TogglHelper;

public class RvfProcessor
{
    private static readonly Regex Base64ImageRegex = new(@"data:image\/([a-zA-Z]*);base64,([^""]*)", RegexOptions.Compiled);
    private static readonly Regex HexImageRegex = new(@"\\pict[^{}]*[\r\n]+\s*([0-9a-fA-F\r\n\s]{32,})", RegexOptions.Compiled | RegexOptions.Multiline);

    public async Task<RvfDocument> ParseRvfAsync(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"RVF file not found: {filePath}");

        var content = await File.ReadAllTextAsync(filePath);
        
        var document = new RvfDocument
        {
            Content = content,
            Metadata = new Dictionary<string, string>
            {
                ["source_file"] = filePath,
                ["parsed_at"] = DateTime.UtcNow.ToString("O")
            }
        };

        // Try different parsing strategies based on file content
        if (content.TrimStart().StartsWith('{'))
        {
            // JSON-based RVF format
            document.Images.AddRange(await ParseJsonRvfAsync(content));
        }
        else if (content.Contains("\\rtf") || content.Contains("\\pic"))
        {
            // RTF-like format with embedded images
            document.Images.AddRange(await ParseRtfLikeRvfAsync(content));
        }
        else
        {
            // HTML-like format with base64 images
            document.Images.AddRange(await ParseHtmlLikeRvfAsync(content));
        }

        return document;
    }

    private async Task<List<RvfImage>> ParseJsonRvfAsync(string content)
    {
        var images = new List<RvfImage>();
        
        try
        {
            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;
            
            if (root.TryGetProperty("images", out var imagesElement) && imagesElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var imageElement in imagesElement.EnumerateArray())
                {
                    var image = JsonSerializer.Deserialize<RvfImage>(imageElement.GetRawText());
                    if (image != null)
                        images.Add(image);
                }
            }
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Warning: Failed to parse JSON RVF format: {ex.Message}");
        }

        return await Task.FromResult(images);
    }

    private async Task<List<RvfImage>> ParseRtfLikeRvfAsync(string content)
    {
        var images = new List<RvfImage>();
        var matches = HexImageRegex.Matches(content);
        
        for (int i = 0; i < matches.Count; i++)
        {
            var match = matches[i];
            var hexData = match.Groups[1].Value.Replace(" ", "").Replace("\n", "").Replace("\r", "");
            
            // Remove any non-hex characters from the beginning (like trailing RTF parameters)
            var validHexStart = 0;
            for (int j = 0; j < hexData.Length; j++)
            {
                if ((hexData[j] >= '0' && hexData[j] <= '9') || 
                    (hexData[j] >= 'a' && hexData[j] <= 'f') || 
                    (hexData[j] >= 'A' && hexData[j] <= 'F'))
                {
                    // Check if this starts a valid hex sequence (PNG signature: 89504e47)
                    if (j + 8 <= hexData.Length && 
                        (hexData.Substring(j, 8).Equals("89504e47", StringComparison.OrdinalIgnoreCase) ||
                         hexData.Substring(j, 8).Equals("ffd8ffe0", StringComparison.OrdinalIgnoreCase))) // JPEG signature
                    {
                        validHexStart = j;
                        break;
                    }
                }
            }
            
            if (validHexStart > 0)
                hexData = hexData.Substring(validHexStart);
            
            try
            {
                // Ensure even length
                if (hexData.Length % 2 != 0)
                    hexData = hexData.Substring(0, hexData.Length - 1);
                    
                var imageData = Convert.FromHexString(hexData);
                var image = new RvfImage
                {
                    Id = $"rtf_image_{i + 1}",
                    Data = Convert.ToBase64String(imageData),
                    Format = "hex"
                };
                images.Add(image);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to parse hex image data: {ex.Message}");
            }
        }

        return await Task.FromResult(images);
    }

    private async Task<List<RvfImage>> ParseHtmlLikeRvfAsync(string content)
    {
        var images = new List<RvfImage>();
        var matches = Base64ImageRegex.Matches(content);
        
        for (int i = 0; i < matches.Count; i++)
        {
            var match = matches[i];
            var format = match.Groups[1].Value;
            var data = match.Groups[2].Value;
            
            var image = new RvfImage
            {
                Id = $"base64_image_{i + 1}",
                Data = data,
                Format = format
            };
            images.Add(image);
        }

        return await Task.FromResult(images);
    }

    public async Task<List<string>> ExtractImagesAsJpegAsync(RvfDocument document, string outputDirectory)
    {
        if (!Directory.Exists(outputDirectory))
            Directory.CreateDirectory(outputDirectory);

        var extractedFiles = new List<string>();

        for (int i = 0; i < document.Images.Count; i++)
        {
            var image = document.Images[i];
            try
            {
                var imageBytes = Convert.FromBase64String(image.Data);
                
                using var imageStream = new MemoryStream(imageBytes);
                using var img = await Image.LoadAsync(imageStream);
                
                var outputFileName = Path.Combine(outputDirectory, $"{image.Id}.jpg");
                
                var encoder = new JpegEncoder { Quality = 90 };
                await img.SaveAsJpegAsync(outputFileName, encoder);
                
                extractedFiles.Add(outputFileName);
                Console.WriteLine($"Extracted image: {outputFileName} ({img.Width}x{img.Height})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting image {image.Id}: {ex.Message}");
            }
        }

        return extractedFiles;
    }
}