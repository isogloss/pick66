using Pick66.Core;

namespace Pick66.Console;

/// <summary>
/// Console demo application to test Pick66.Core functionality
/// This demonstrates the core lottery logic that powers the WPF application
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        System.Console.WriteLine("╔══════════════════════════════════════════════════════════════════════════════╗");
        System.Console.WriteLine("║                              PICK66 DEMO                                    ║");
        System.Console.WriteLine("║                         Lottery Number Generator                             ║");
        System.Console.WriteLine("║                    (Console demo of WPF functionality)                      ║");
        System.Console.WriteLine("╚══════════════════════════════════════════════════════════════════════════════╝");
        System.Console.WriteLine();

        var numberPicker = new NumberPickerService();
        
        // Demo 1: Single ticket generation
        System.Console.WriteLine("=== DEMO 1: Single Ticket Generation ===");
        System.Console.WriteLine("Parameters: 6 numbers from 1-49 (unique)");
        
        var singleTicket = numberPicker.GenerateTicket(6, 1, 49, true);
        System.Console.WriteLine($"Generated ticket: {string.Join(" ", singleTicket.Select(n => n.ToString("D2")))}");
        System.Console.WriteLine();

        // Demo 2: Multiple tickets with progress
        System.Console.WriteLine("=== DEMO 2: Multiple Ticket Generation ===");
        System.Console.WriteLine("Parameters: 5 tickets, 6 numbers each from 1-49 (unique)");
        System.Console.WriteLine();

        var progress = new Progress<int>(count =>
        {
            System.Console.Write($"\rGenerating tickets... {count}/5");
        });

        var tickets = await numberPicker.GenerateTicketsAsync(5, 6, 1, 49, true, progress);
        
        System.Console.WriteLine("\n");
        for (int i = 0; i < tickets.Count; i++)
        {
            var numbers = string.Join(" ", tickets[i].Select(n => n.ToString("D2")));
            System.Console.WriteLine($"Ticket {i + 1:D3}: {numbers}");
        }
        System.Console.WriteLine();

        // Demo 3: Non-unique numbers
        System.Console.WriteLine("=== DEMO 3: Non-Unique Numbers ===");
        System.Console.WriteLine("Parameters: 8 numbers from 1-10 (duplicates allowed)");
        
        var nonUniqueTicket = numberPicker.GenerateTicket(8, 1, 10, false);
        System.Console.WriteLine($"Generated ticket: {string.Join(" ", nonUniqueTicket.Select(n => n.ToString("D2")))}");
        System.Console.WriteLine();

        // Demo 4: Edge case testing
        System.Console.WriteLine("=== DEMO 4: Edge Cases ===");
        
        try
        {
            System.Console.WriteLine("Testing invalid parameters (6 unique from 1-5)...");
            numberPicker.GenerateTicket(6, 1, 5, true);
        }
        catch (ArgumentException ex)
        {
            System.Console.WriteLine($"✓ Validation works: {ex.Message}");
        }
        
        System.Console.WriteLine();

        // Demo 5: Fisher-Yates algorithm demonstration
        System.Console.WriteLine("=== DEMO 5: Fisher-Yates Algorithm Demonstration ===");
        System.Console.WriteLine("Generating 10 tickets with 5 unique numbers from 1-20");
        System.Console.WriteLine("Notice how the algorithm ensures unique numbers within each ticket:");
        System.Console.WriteLine();

        var demoTickets = await numberPicker.GenerateTicketsAsync(10, 5, 1, 20, true);
        
        for (int i = 0; i < demoTickets.Count; i++)
        {
            var numbers = string.Join(" ", demoTickets[i].Select(n => n.ToString("D2")));
            var uniqueCount = demoTickets[i].Distinct().Count();
            System.Console.WriteLine($"Ticket {i + 1:D2}: {numbers} (Unique: {uniqueCount}/5)");
        }

        System.Console.WriteLine();
        System.Console.WriteLine("╔══════════════════════════════════════════════════════════════════════════════╗");
        System.Console.WriteLine("║                            DEMO COMPLETE                                    ║");
        System.Console.WriteLine("║                                                                              ║");
        System.Console.WriteLine("║  This demonstrates the core lottery logic that powers the WPF interface.    ║");
        System.Console.WriteLine("║  The WPF app provides the same functionality with a modern GUI.             ║");
        System.Console.WriteLine("╚══════════════════════════════════════════════════════════════════════════════╝");
        System.Console.WriteLine();
        System.Console.WriteLine("Press any key to exit...");
        System.Console.ReadKey();
    }
}