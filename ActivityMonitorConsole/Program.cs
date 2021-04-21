using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;

namespace ActivityMonitorConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 2 && args[0].ToLower() == "start")
            {
                if (!Directory.Exists(args[1])) Console.WriteLine("Directory Does Not Exist...");
                else
                {
                    Console.WriteLine();
                    Console.WriteLine($"Output Directory Selected: {args[1]}");
                    NamedPipeClientStream client = new NamedPipeClientStream(@"MonPipe");
                    StreamWriter sw = new StreamWriter(client);
                    StreamReader sr = new StreamReader(client);
                    try
                    {
                        client.Connect(600);
                    }
                    catch (Exception e)
                    {
                        if (e is TimeoutException)
                        {
                            Console.WriteLine("Error: Service not running...");
                            return;
                        }
                        else
                        {
                            Console.WriteLine("Unspecified Error...");
                            return;
                        }
                    }
                    if (client.IsConnected)
                    {
                        for (int i = 0; i < 2; i++)
                        {
                            sw.WriteLine(args[i]);
                            Thread.Sleep(30);
                        }
                        sw.Flush();
                        string read = sr.ReadLine();
                        if (read == "OK") Console.WriteLine("Keylogging Started...");
                        sw.WriteLine(encode("disconnect"));
                        sw.Flush();
                        read = sr.ReadLine();
                        if (read != "OK") Console.WriteLine("Error Disconnecting Service...");
                        else Thread.Sleep(100);
                    }
                    else Console.WriteLine("Error Connecting Service.");
                    client.Close();
                    client.Dispose();
                }
                Console.WriteLine("Press Any Key...");
                Console.ReadKey(true);
                return;
            }
            else if (args.Length == 1 && args[0].ToLower() == "stop")
            {
                NamedPipeClientStream client = new NamedPipeClientStream(@"MonPipe");
                StreamWriter sw = new StreamWriter(client);
                StreamReader sr = new StreamReader(client);
                try
                {
                    client.Connect(600);
                }
                catch (Exception e)
                {
                    if (e is TimeoutException)
                    {
                        Console.WriteLine("Error: Service not running...");
                        return;
                    }
                    else
                    {
                        Console.WriteLine("Unspecified Error...");
                        return;
                    }
                }
                if (client.IsConnected)
                {
                    for (int i = 0; i < 1; i++)
                    {
                        sw.WriteLine(args[i]);
                        Thread.Sleep(30);
                    }
                    sw.Flush();
                    string read = sr.ReadLine();
                    if (read == "OK") Console.WriteLine("Keylogging Stopped...");
                    sw.WriteLine(encode("disconnect"));
                    sw.Flush();
                    read = sr.ReadLine();
                    if (read != "OK") Console.WriteLine("Error Disconnecting Service...");
                    else Thread.Sleep(100);
                }
                else Console.WriteLine("Error Connecting Service...");
                client.Close();
                client.Dispose();
                Console.WriteLine("Press Any Key...");
                Console.ReadKey(true);
            }
            else
            {
                Console.WriteLine("Invalid Arguments");
                Console.WriteLine("Usage: actmon.exe start/stop [outpath]");
                Console.WriteLine("Outpath: Only with start param");
                Console.WriteLine();
                return;
            }
        }

        public static string encode(string input)
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            return Convert.ToBase64String(bytes);
        }
    }
}
