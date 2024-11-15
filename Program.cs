using System;

string s = Console.ReadLine();
if (s.Equals("Host"))
{
    using var host = new CSharpFirstPerson.Host();
    host.Run();
}
else
{
    using var client = new CSharpFirstPerson.Client();
    client.Run();
}