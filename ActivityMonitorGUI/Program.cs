using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Windows.Forms;

namespace ActivityMonitorMain
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            NamedPipeClientStream temp = new NamedPipeClientStream(@"MonPipe");
            StreamReader r = new StreamReader(temp);
            StreamWriter w = new StreamWriter(temp);
            try
            {
                temp.Connect(1000);
                if (temp.IsConnected)
                {
                    w.WriteLine(encode(@"RestoRe"));
                    w.Flush();
                    if (r.ReadLine() == "OK") 
                    {
                        w.WriteLine(encode("disconnect"));
                        w.Flush();
                        if (r.ReadLine() == "OK") Environment.Exit(0);
                    }
                    else Environment.Exit(1);
                }
            }
            catch(Exception e)
            {
                if (e is TimeoutException)
                {
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.Run(new Form1());
                }
                else Environment.Exit(1);
            }
        }
        public static string encode(string input)
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            return Convert.ToBase64String(bytes);
        }
    }
}
