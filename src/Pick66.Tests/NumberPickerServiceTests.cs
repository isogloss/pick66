using Pick66.Core;
using Xunit;

namespace Pick66.Tests;

/// <summary>
/// Unit tests for NumberPickerService
/// </summary>
public class NumberPickerServiceTests
{
    private readonly INumberPickerService _service;

    public NumberPickerServiceTests()
    {
        _service = new NumberPickerService();
    }

    [Fact]
    public void GenerateTicket_WithValidParameters_ReturnsCorrectCount()
    {
        // Arrange
        var numbersPerTicket = 6;
        var min = 1;
        var max = 49;

        // Act
        var result = _service.GenerateTicket(numbersPerTicket, min, max, true);

        // Assert
        Assert.Equal(numbersPerTicket, result.Length);
    }

    [Fact]
    public void GenerateTicket_WithUniqueTrue_ReturnsUniqueNumbers()
    {
        // Arrange
        var numbersPerTicket = 10;
        var min = 1;
        var max = 50;

        // Act
        var result = _service.GenerateTicket(numbersPerTicket, min, max, true);

        // Assert
        Assert.Equal(numbersPerTicket, result.Distinct().Count());
    }

    [Fact]
    public void GenerateTicket_WithValidRange_ReturnsNumbersInRange()
    {
        // Arrange
        var numbersPerTicket = 5;
        var min = 10;
        var max = 20;

        // Act
        var result = _service.GenerateTicket(numbersPerTicket, min, max, true);

        // Assert
        Assert.All(result, number => Assert.InRange(number, min, max));
    }

    [Fact]
    public void GenerateTicket_ReturnsSortedNumbers()
    {
        // Arrange
        var numbersPerTicket = 6;
        var min = 1;
        var max = 49;

        // Act
        var result = _service.GenerateTicket(numbersPerTicket, min, max, true);

        // Assert
        Assert.Equal(result.OrderBy(x => x).ToArray(), result);
    }

    [Theory]
    [InlineData(0, 1, 49)]
    [InlineData(-1, 1, 49)]
    public void GenerateTicket_WithInvalidNumbersPerTicket_ThrowsArgumentException(int numbersPerTicket, int min, int max)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _service.GenerateTicket(numbersPerTicket, min, max, true));
    }

    [Fact]
    public void GenerateTicket_WithMinGreaterThanMax_ThrowsArgumentException()
    {
        // Arrange
        var numbersPerTicket = 5;
        var min = 50;
        var max = 10;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _service.GenerateTicket(numbersPerTicket, min, max, true));
    }

    [Fact]
    public void GenerateTicket_WithUniqueButInsufficientRange_ThrowsArgumentException()
    {
        // Arrange
        var numbersPerTicket = 10;
        var min = 1;
        var max = 5; // Only 5 unique numbers possible

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _service.GenerateTicket(numbersPerTicket, min, max, true));
    }

    [Fact]
    public void GenerateTicket_WithNonUniqueAndInsufficientRange_Succeeds()
    {
        // Arrange
        var numbersPerTicket = 10;
        var min = 1;
        var max = 5;

        // Act
        var result = _service.GenerateTicket(numbersPerTicket, min, max, false);

        // Assert
        Assert.Equal(numbersPerTicket, result.Length);
        Assert.All(result, number => Assert.InRange(number, min, max));
    }

    [Fact]
    public async Task GenerateTicketsAsync_WithValidParameters_ReturnsCorrectCount()
    {
        // Arrange
        var ticketCount = 5;
        var numbersPerTicket = 6;
        var min = 1;
        var max = 49;

        // Act
        var result = await _service.GenerateTicketsAsync(ticketCount, numbersPerTicket, min, max, true);

        // Assert
        Assert.Equal(ticketCount, result.Count);
        Assert.All(result, ticket => Assert.Equal(numbersPerTicket, ticket.Length));
    }

    [Fact]
    public async Task GenerateTicketsAsync_WithProgress_ReportsProgress()
    {
        // Arrange
        var ticketCount = 10;
        var progressValues = new System.Collections.Concurrent.ConcurrentBag<int>();
        var progress = new Progress<int>(value => progressValues.Add(value));

        // Act
        var result = await _service.GenerateTicketsAsync(ticketCount, 6, 1, 49, true, progress);

        // Give progress a moment to complete
        await Task.Delay(10);

        // Assert
        Assert.Equal(ticketCount, result.Count);
        Assert.Equal(ticketCount, progressValues.Count);
        
        var sortedProgress = progressValues.OrderBy(x => x).ToArray();
        Assert.Equal(Enumerable.Range(1, ticketCount).ToArray(), sortedProgress);
    }

    [Fact]
    public async Task GenerateTicketsAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _service.GenerateTicketsAsync(100, 6, 1, 49, true, null, cts.Token));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public async Task GenerateTicketsAsync_WithInvalidTicketCount_ThrowsArgumentException(int ticketCount)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.GenerateTicketsAsync(ticketCount, 6, 1, 49, true));
    }

    [Fact]
    public void GenerateTicket_Fisher_Yates_ProducesUniformDistribution()
    {
        // Arrange
        var numbersPerTicket = 3;
        var min = 1;
        var max = 10;
        var iterations = 1000;
        var frequency = new Dictionary<int, int>();

        // Initialize frequency counter
        for (int i = min; i <= max; i++)
        {
            frequency[i] = 0;
        }

        // Act
        for (int i = 0; i < iterations; i++)
        {
            var ticket = _service.GenerateTicket(numbersPerTicket, min, max, true);
            foreach (var number in ticket)
            {
                frequency[number]++;
            }
        }

        // Assert - Each number should appear roughly the same number of times
        var expectedFrequency = (double)(iterations * numbersPerTicket) / (max - min + 1);
        var tolerance = expectedFrequency * 0.2; // 20% tolerance

        foreach (var kvp in frequency)
        {
            Assert.InRange(kvp.Value, expectedFrequency - tolerance, expectedFrequency + tolerance);
        }
    }
}