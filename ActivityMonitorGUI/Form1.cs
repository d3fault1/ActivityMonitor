using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ActivityMonitorMain
{
    public partial class Form1 : Form
    {
        private string outpath = "";
        private NamedPipeServerStream st;
        private StreamReader sr;
        private StreamWriter sw;
        private List<string> data;
        private Thread ipc;

        public Form1()
        {
            InitializeComponent();
            data = new List<string>();
            st = new NamedPipeServerStream(@"MonPipe");
            sr = new StreamReader(st);
            sw = new StreamWriter(st);
            ipc = new Thread(coms_server);
            Logger.Init();
            ipc.Start();
            notifyIcon1.Visible = true;
            notifyIcon1.ShowBalloonTip(3000, @"Information", @"Service is running", ToolTipIcon.Info);
        }

        private async void Form1_Closing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            this.WindowState = FormWindowState.Minimized;
            notifyIcon1.Visible = true;
            notifyIcon1.ShowBalloonTip(3000, @"Information", @"Minimized to system tray", ToolTipIcon.Info);
            await Task.Delay(80);
            this.Hide();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (label2.Text == "Running")
            {
                if (Visible)
                {
                    MessageBox.Show("Monitoring Already Running", "Error");
                }
                else if (notifyIcon1.Visible)
                {
                    notifyIcon1.ShowBalloonTip(3000, "Info", "Monitoring Already Running", ToolTipIcon.Warning);
                }
                return;
            }
            Logger.startLogging();
            status_update(1);
            if (!Visible && notifyIcon1.Visible)
            {
                notifyIcon1.ShowBalloonTip(3000, "Info", "Monitoring Started", ToolTipIcon.Info);
            }
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            if (label2.Text != "Running")
            {
                if (Visible)
                {
                    MessageBox.Show("Monitoring Not Running!", "Error");
                }
                else if (notifyIcon1.Visible)
                {
                    notifyIcon1.ShowBalloonTip(3000, "Error", "Monitoring Not Running", ToolTipIcon.Error);
                }
                return;
            }
            Logger.stopLogging();
            if (label4.Text == "Not Selected") saveFileDialog1.ShowDialog();
            await Task.Run(() => Logger.finish(outpath));
            status_update(0);
            if (!Visible && notifyIcon1.Visible)
            {
                notifyIcon1.ShowBalloonTip(3000, "Info", "Monitoring Stopped", ToolTipIcon.Info);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            saveFileDialog1.ShowDialog();
        }

        private void startMonitoringToolStripMenuItem_Click(object sender, EventArgs e)
        {
            button1_Click(button1, EventArgs.Empty);
        }

        private void stopMonitoringToolStripMenuItem_Click(object sender, EventArgs e)
        {
            button2_Click(button2, EventArgs.Empty);
        }

        private async void showWindowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Show();
            WindowState = FormWindowState.Normal;
            await Task.Delay(80);
            notifyIcon1.Visible = false;
            Focus();
        }

        private async void quitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            notifyIcon1.ShowBalloonTip(3000, @"Information", @"Stopping Service", ToolTipIcon.Info);
            stop_coms();
            if (label2.Text != "Running")
            {
                notifyIcon1.Visible = false;
                Environment.Exit(0);
            }
            Logger.stopLogging();
            if (label4.Text == "Not Selected") saveFileDialog1.ShowDialog();
            await Task.Run(() => Logger.finish(outpath));
            status_update(0);
            notifyIcon1.Visible = false;
            Environment.Exit(0);
        }

        private async void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Show();
            WindowState = FormWindowState.Normal;
            await Task.Delay(50);
            notifyIcon1.Visible = false;
        }

        private void saveFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            outpath = saveFileDialog1.FileName;
            label4.Text = "Selected";
        }

        private void status_update(int k)
        {
            if (k == 0)
            {
                label2.Invoke((MethodInvoker)delegate
                {
                    label2.Text = "Not Running";
                    label2.ForeColor = Color.Red;
                });
            }

            if (k == 1)
            {
                label2.Invoke((MethodInvoker)delegate
                {
                    label2.Text = "Running";
                    label2.ForeColor = Color.Green;
                });
            }
        }

        public void process_and_respond()
        {
            if (data[0].ToLower() == "start")
            {
                outpath = Path.Combine(data[1], "output.json");
                label4.Invoke((MethodInvoker)delegate { label4.Text = "Selected"; });
                button1.Invoke((MethodInvoker)delegate { button1_Click(button1, EventArgs.Empty); });
                sw.WriteLine("OK");
                sw.Flush();
                st.WaitForPipeDrain();
            }
            else if (data[0].ToLower() == "stop")
            {
                button2.Invoke((MethodInvoker)delegate { button2_Click(button2, EventArgs.Empty); });
                sw.WriteLine("OK");
                sw.Flush();
                st.WaitForPipeDrain();
            }
            if (data[0] == encode(@"RestoRe"))
            {
                if (!Visible && notifyIcon1.Visible)
                {
                    button1.Invoke((MethodInvoker)delegate { showWindowToolStripMenuItem_Click(showWindowToolStripMenuItem, EventArgs.Empty); });
                }
                sw.WriteLine("OK");
                sw.Flush();
                st.WaitForPipeDrain();
            }
        }

        public void coms_server()
        {
            while (true)
            {
                data.Clear();
                string data_read;
                st.WaitForConnection();
                while (st.IsConnected)
                {
                    do
                    {
                        data_read = sr.ReadLine();
                        Console.WriteLine(data_read);
                        if (data_read == encode("disconnect"))
                        {
                            sw.WriteLine("OK");
                            sw.Flush();
                            st.WaitForPipeDrain();
                            goto disconnect;
                        }
                        else if (data_read == encode("sHutDown"))
                        {
                            server_cleanup();
                            return;
                        }
                        else data.Add(data_read);
                    } while (sr.Peek() != -1);
                    process_and_respond();
                }
            disconnect:
                if (st.IsConnected) st.Disconnect();
            }
        }

        public void server_cleanup()
        {
            if (st.IsConnected) st.Disconnect();
            st.Close();
            st.Dispose();
        }

        public void stop_coms()
        {
            NamedPipeClientStream temp = new NamedPipeClientStream(@"MonPipe");
            StreamWriter tw = new StreamWriter(temp);
            temp.Connect(500);
            if (temp.IsConnected)
            {
                tw.WriteLine(encode("sHutDown"));
                tw.Flush();
            }
            temp.Close();
            temp.Dispose();
        }

        public string encode(string input)
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            return Convert.ToBase64String(bytes);
        }

        public string decode(string input)
        {
            var bytes = Convert.FromBase64String(input);
            return Encoding.UTF8.GetString(bytes);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            ShowInTaskbar = true;
            Hide();
        }
    }
}
