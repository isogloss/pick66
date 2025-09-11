namespace Pick66.Core;

/// <summary>
/// Service interface for generating lottery number tickets
/// </summary>
public interface INumberPickerService
{
    /// <summary>
    /// Generate a single ticket with specified parameters
    /// </summary>
    /// <param name="numbersPerTicket">Number of numbers to pick per ticket</param>
    /// <param name="minInclusive">Minimum number (inclusive)</param>
    /// <param name="maxInclusive">Maximum number (inclusive)</param>
    /// <param name="unique">Whether numbers should be unique within each ticket</param>
    /// <returns>Array of picked numbers</returns>
    int[] GenerateTicket(int numbersPerTicket, int minInclusive, int maxInclusive, bool unique);

    /// <summary>
    /// Generate multiple tickets asynchronously
    /// </summary>
    /// <param name="ticketCount">Number of tickets to generate</param>
    /// <param name="numbersPerTicket">Number of numbers to pick per ticket</param>
    /// <param name="minInclusive">Minimum number (inclusive)</param>
    /// <param name="maxInclusive">Maximum number (inclusive)</param>
    /// <param name="unique">Whether numbers should be unique within each ticket</param>
    /// <param name="progress">Progress reporting callback</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of generated tickets</returns>
    Task<List<int[]>> GenerateTicketsAsync(int ticketCount, int numbersPerTicket, int minInclusive, int maxInclusive, bool unique, IProgress<int>? progress = null, CancellationToken cancellationToken = default);
}