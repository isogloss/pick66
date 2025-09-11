namespace Pick66.Core;

/// <summary>
/// Implementation of INumberPickerService with Fisher-Yates algorithm for unique selection
/// </summary>
public class NumberPickerService : INumberPickerService
{
    private readonly Random _random;

    public NumberPickerService()
    {
        _random = new Random();
    }

    /// <summary>
    /// Generate a single ticket with specified parameters
    /// </summary>
    public int[] GenerateTicket(int numbersPerTicket, int minInclusive, int maxInclusive, bool unique)
    {
        ValidateParameters(numbersPerTicket, minInclusive, maxInclusive, unique);

        if (unique)
        {
            return GenerateUniqueNumbers(numbersPerTicket, minInclusive, maxInclusive);
        }
        else
        {
            return GenerateRandomNumbers(numbersPerTicket, minInclusive, maxInclusive);
        }
    }

    /// <summary>
    /// Generate multiple tickets asynchronously
    /// </summary>
    public async Task<List<int[]>> GenerateTicketsAsync(int ticketCount, int numbersPerTicket, int minInclusive, int maxInclusive, bool unique, IProgress<int>? progress = null, CancellationToken cancellationToken = default)
    {
        ValidateParameters(numbersPerTicket, minInclusive, maxInclusive, unique);

        if (ticketCount <= 0)
            throw new ArgumentException("Ticket count must be greater than 0", nameof(ticketCount));

        var tickets = new List<int[]>(ticketCount);

        for (int i = 0; i < ticketCount; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Add some artificial delay for demonstrating async progress
            if (i > 0 && i % 10 == 0)
            {
                await Task.Delay(1, cancellationToken);
            }

            var ticket = GenerateTicket(numbersPerTicket, minInclusive, maxInclusive, unique);
            tickets.Add(ticket);

            // Report progress
            progress?.Report(i + 1);
        }

        return tickets;
    }

    /// <summary>
    /// Generate unique numbers using Fisher-Yates shuffle algorithm
    /// </summary>
    private int[] GenerateUniqueNumbers(int count, int min, int max)
    {
        int range = max - min + 1;
        
        // Create array of all possible numbers
        var numbers = new int[range];
        for (int i = 0; i < range; i++)
        {
            numbers[i] = min + i;
        }

        // Fisher-Yates shuffle - only shuffle the first 'count' positions
        for (int i = 0; i < count; i++)
        {
            int j = _random.Next(i, range);
            (numbers[i], numbers[j]) = (numbers[j], numbers[i]);
        }

        // Return only the first 'count' numbers and sort them
        var result = new int[count];
        Array.Copy(numbers, result, count);
        Array.Sort(result);
        return result;
    }

    /// <summary>
    /// Generate random numbers (duplicates allowed)
    /// </summary>
    private int[] GenerateRandomNumbers(int count, int min, int max)
    {
        var numbers = new int[count];
        for (int i = 0; i < count; i++)
        {
            numbers[i] = _random.Next(min, max + 1);
        }
        Array.Sort(numbers);
        return numbers;
    }

    /// <summary>
    /// Validate input parameters
    /// </summary>
    private static void ValidateParameters(int numbersPerTicket, int minInclusive, int maxInclusive, bool unique)
    {
        if (numbersPerTicket <= 0)
            throw new ArgumentException("Numbers per ticket must be greater than 0", nameof(numbersPerTicket));

        if (minInclusive > maxInclusive)
            throw new ArgumentException("Minimum value cannot be greater than maximum value", nameof(minInclusive));

        if (unique && numbersPerTicket > (maxInclusive - minInclusive + 1))
            throw new ArgumentException($"Cannot generate {numbersPerTicket} unique numbers from range {minInclusive}-{maxInclusive}");
    }
}