using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Pick6.Core;

namespace Pick6.Tests
{
    /// <summary>
    /// Test that our logging improvements work correctly
    /// </summary>
    public class LoggingImprovementsTests
    {
        [Fact]
        public void ConsoleLogSink_WritesLogMessages()
        {
            // Arrange
            var sink = new ConsoleLogSink();
            var originalOut = Console.Out;
            var stringWriter = new StringWriter();
            Console.SetOut(stringWriter);

            try
            {
                // Act
                sink.WriteLog(LogLevel.Info, DateTime.Now, "Test message");

                // Assert
                var output = stringWriter.ToString();
                Assert.Contains("[Info] Test message", output);
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        [Fact]
        public void ConsoleLogSink_HandlesErrorLevel()
        {
            // Arrange
            var sink = new ConsoleLogSink();
            var originalOut = Console.Out;
            var stringWriter = new StringWriter();
            Console.SetOut(stringWriter);

            try
            {
                // Act
                sink.WriteLog(LogLevel.Error, DateTime.Now, "Error message");

                // Assert
                var output = stringWriter.ToString();
                Assert.Contains("[Error] Error message", output);
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        [Fact]
        public void Log_WithConsoleSink_OutputsToConsole()
        {
            // Arrange
            Log.ClearSinks();
            Log.AddSink(new ConsoleLogSink());
            var originalOut = Console.Out;
            var stringWriter = new StringWriter();
            Console.SetOut(stringWriter);

            try
            {
                // Act
                Log.Info("Test info message");
                Log.Error("Test error message");

                // Assert
                var output = stringWriter.ToString();
                Assert.Contains("[Info] Test info message", output);
                Assert.Contains("[Error] Test error message", output);
            }
            finally
            {
                Console.SetOut(originalOut);
                Log.ClearSinks();
            }
        }
    }
}