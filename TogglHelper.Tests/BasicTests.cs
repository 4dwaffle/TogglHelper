using Xunit;

namespace TogglHelper.Tests;

public class BasicTests
{
    [Fact]
    public void Basic_Test_Passes()
    {
        // Arrange & Act & Assert
        Assert.True(true);
    }
    
    [Fact]
    public void TimeSpan_Extensions_Can_Be_Tested()
    {
        // This is a placeholder test to verify the test infrastructure works
        // Future tests can test actual functionality like TimeSpanExtensions
        var timeSpan = TimeSpan.FromMinutes(5);
        Assert.Equal(5, timeSpan.Minutes);
    }
}