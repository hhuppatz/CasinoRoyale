using System;

Console.WriteLine("Starting CasinoRoyaleServer...");

try
{
    // Start the unified game
    using var game = new CasinoRoyale.CasinoRoyaleGame();
    game.Run();
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
    Console.WriteLine("Press any key to exit...");
    Console.ReadKey();
}
