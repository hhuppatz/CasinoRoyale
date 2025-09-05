using System;

Console.WriteLine("Starting CasinoRoyaleServer...");
Console.WriteLine("Type 'Host' to start as server, or 'Client' to start as client:");
string s = Console.ReadLine();
try
{
    if (s.Equals("Host"))
    {
        Console.WriteLine("Starting as Host (Server)...");
        using var host = new CSharpFirstPerson.Host();
        host.Run();
    }
    else
    {
        Console.WriteLine("Starting as Client...");
        using var client = new CSharpFirstPerson.Client();
        client.Run();
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
    Console.WriteLine("Press any key to exit...");
    Console.ReadKey();
}