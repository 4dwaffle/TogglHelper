using TogglHelper;
using TogglHelper.Models;
using Xunit;

namespace TogglHelper.Tests;

public class RvfProcessorTests
{
    private readonly RvfProcessor _processor;
    private readonly string _testDataPath;

    public RvfProcessorTests()
    {
        _processor = new RvfProcessor();
        _testDataPath = Path.Combine(Directory.GetCurrentDirectory(), "TestData");
        Directory.CreateDirectory(_testDataPath);
    }

    [Fact]
    public async Task ParseRvfAsync_WithJsonFormat_ShouldExtractImages()
    {
        // Arrange
        var jsonRvf = """
        {
            "content": "Test document with images",
            "images": [
                {
                    "id": "image1",
                    "data": "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==",
                    "format": "png",
                    "width": 1,
                    "height": 1
                }
            ]
        }
        """;
        
        var testFile = Path.Combine(_testDataPath, "test.rvf");
        await File.WriteAllTextAsync(testFile, jsonRvf);

        // Act
        var document = await _processor.ParseRvfAsync(testFile);

        // Assert
        Assert.NotNull(document);
        Assert.Single(document.Images);
        Assert.Equal("image1", document.Images[0].Id);
        Assert.Equal("png", document.Images[0].Format);
        Assert.NotEmpty(document.Images[0].Data);
        
        // Cleanup
        File.Delete(testFile);
    }

    [Fact]
    public async Task ParseRvfAsync_WithHtmlFormat_ShouldExtractBase64Images()
    {
        // Arrange
        var htmlRvf = """
        <html>
            <body>
                <img src="data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==" />
                <img src="data:image/jpeg;base64,/9j/4AAQSkZJRgABAQEAYABgAAD/2wBDAAEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQH/2wBDAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQH/wAARCAABAAEDASIAAhEBAxEB/8QAFQABAQAAAAAAAAAAAAAAAAAAAAv/xAAUEAEAAAAAAAAAAAAAAAAAAAAA/8QAFQEBAQAAAAAAAAAAAAAAAAAAAAX/xAAUEQEAAAAAAAAAAAAAAAAAAAAA/9oADAMBAAIRAxEAPwA/AB//2Q==" />
            </body>
        </html>
        """;
        
        var testFile = Path.Combine(_testDataPath, "test.rvf");
        await File.WriteAllTextAsync(testFile, htmlRvf);

        // Act
        var document = await _processor.ParseRvfAsync(testFile);

        // Assert
        Assert.NotNull(document);
        Assert.Equal(2, document.Images.Count);
        Assert.Equal("base64_image_1", document.Images[0].Id);
        Assert.Equal("png", document.Images[0].Format);
        Assert.Equal("base64_image_2", document.Images[1].Id);
        Assert.Equal("jpeg", document.Images[1].Format);
        
        // Cleanup
        File.Delete(testFile);
    }

    [Fact]
    public async Task ParseRvfAsync_WithRtfFormat_ShouldExtractHexImages()
    {
        // Arrange  
        var rtfRvf = @"{\rtf1\ansi\deff0
        {\pict\pngblip\picw26\pich26\picwgoal15\pichgoal15
89504e470d0a1a0a0000000d49484452000000010000000108060000001f15c4
890000000d4944415478da634080ee0f0038600600a2b47d6b0000000049454e44
ae426082}}";
        
        var testFile = Path.Combine(_testDataPath, "test.rvf");
        await File.WriteAllTextAsync(testFile, rtfRvf);

        // Act
        var document = await _processor.ParseRvfAsync(testFile);

        // Assert
        Assert.NotNull(document);
        Assert.Single(document.Images);
        Assert.Equal("rtf_image_1", document.Images[0].Id);
        Assert.Equal("hex", document.Images[0].Format);
        Assert.NotEmpty(document.Images[0].Data);
        
        // Cleanup
        File.Delete(testFile);
    }

    [Fact]
    public async Task ParseRvfAsync_WithEmptyFile_ShouldReturnEmptyDocument()
    {
        // Arrange
        var testFile = Path.Combine(_testDataPath, "empty.rvf");
        await File.WriteAllTextAsync(testFile, "");

        // Act
        var document = await _processor.ParseRvfAsync(testFile);

        // Assert
        Assert.NotNull(document);
        Assert.Empty(document.Images);
        Assert.Equal("", document.Content);
        
        // Cleanup
        File.Delete(testFile);
    }

    [Fact]
    public async Task ParseRvfAsync_WithNonExistentFile_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var nonExistentFile = Path.Combine(_testDataPath, "nonexistent.rvf");

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() => _processor.ParseRvfAsync(nonExistentFile));
    }

    [Fact]
    public async Task ExtractImagesAsJpegAsync_WithValidImages_ShouldCreateJpegFiles()
    {
        // Arrange
        var document = new RvfDocument();
        document.Images.Add(new RvfImage
        {
            Id = "test_image",
            Data = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==", // 1x1 PNG
            Format = "png"
        });

        var outputDir = Path.Combine(_testDataPath, "output");

        // Act
        var extractedFiles = await _processor.ExtractImagesAsJpegAsync(document, outputDir);

        // Assert
        Assert.Single(extractedFiles);
        var outputFile = extractedFiles[0];
        Assert.True(File.Exists(outputFile));
        Assert.EndsWith(".jpg", outputFile);
        Assert.Contains("test_image", outputFile);
        
        // Cleanup
        if (Directory.Exists(outputDir))
            Directory.Delete(outputDir, true);
    }

    [Fact]
    public async Task ExtractImagesAsJpegAsync_WithInvalidImageData_ShouldHandleGracefully()
    {
        // Arrange
        var document = new RvfDocument();
        document.Images.Add(new RvfImage
        {
            Id = "invalid_image",
            Data = "invalid_base64_data",
            Format = "png"
        });

        var outputDir = Path.Combine(_testDataPath, "output");

        // Act
        var extractedFiles = await _processor.ExtractImagesAsJpegAsync(document, outputDir);

        // Assert
        Assert.Empty(extractedFiles); // Should not extract invalid images
        
        // Cleanup
        if (Directory.Exists(outputDir))
            Directory.Delete(outputDir, true);
    }
}