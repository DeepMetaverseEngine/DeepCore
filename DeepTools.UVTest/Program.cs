

using DeepCore;
using DeepTools.UVTest;


try
{
    var pargs = DeepCore.Properties.ParseArgs(args);
    var host = "127.0.0.1";
    var port = 19850;
    if (pargs.TryGetValue("host", out var _host))
    {
        host = _host;
    }
    else
    {
        Console.Write("Input Host : ");
        var line = Console.ReadLine();
        if (!string.IsNullOrEmpty(line))
        {
            host = line;
            Console.WriteLine($"Use : {host}");
        }
        else
        {
            Console.WriteLine($"Use Default : {host}");
        }
    }
    if (pargs.TryGetAsInt("port", out var _port))
    {
        port = _port;
    }
    else
    {
        Console.Write("Input Port : ");
        if (int.TryParse(Console.ReadLine(), out var p))
        {
            port = p;
            Console.WriteLine($"Use : {port}");
        }
        else
        {
            Console.WriteLine($"Use Default : {port}");
        }
    }

    Console.WriteLine($"Connect To : {host}:{port}");
    var session = new Session(host, port);
    while (true)
    {
        Console.WriteLine($"Input Command : ");
        var cmd = Console.ReadLine();
        if (cmd.ToLower() == "exit")
        {
            break;
        }
        var ack = session.SendCall(cmd);
        Console.WriteLine(ack);
    }
}
catch(Exception err)
{
    err.PrintStackTrace();
}
