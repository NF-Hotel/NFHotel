
using Xunit;

namespace KeyRotationPrototype.Tests;

public class ProgramTests
{
    [Fact]
    public void Main_KeyRotationDemo_CompletesSuccessfully()
    {
        // Arrange
        using var writer = new StringWriter();
        var originalOutput = Console.Out;

        try
        {
            Console.SetOut(writer);

            // Act
            Program.RunDemo();

            // Assert
            string output = writer.ToString();

            Assert.Contains("Step 1: Encrypt with v1", output);
            Assert.Contains("Step 2: Rotate encryption key", output);
            Assert.Contains("Step 3: Decrypt old v1 data", output);
            Assert.Contains("Step 4: Re-encrypt existing data", output);
            Assert.Contains("Step 5: Verify migrated data", output);

            Assert.Contains("Successful: True", output);
            Assert.Contains("Uses v2: True", output);
        }
        finally
        {
            Console.SetOut(originalOutput);
        }
    }
}
