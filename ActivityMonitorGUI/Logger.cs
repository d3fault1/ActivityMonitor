using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace ActivityMonitorMain
{
    static class Logger
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WH_MOUSE_LL = 14;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_RBUTTONUP = 0x0205;
        private const int WM_MOUSEMOVE = 0x0200;
        private const int WM_MOUSEWHEEL = 0x020A;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYUP = 0x0105;
        private static uint[] keyMap = new uint[10];
        private static int currentCount = 0, n = 0, m = 0;
        [StructLayout(LayoutKind.Sequential)]
        internal struct POINT
        {
            public int x;
            public int y;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }
        private static MSLLHOOKSTRUCT p, q;
        private static KBDLLHOOKSTRUCT k;
        private static IntPtr hook = IntPtr.Zero;
        private static IntPtr mhook = IntPtr.Zero;
        private static LowLevelKeyboardProc keyproc;
        private static LowLevelMouseProc mouseproc;
        private static System.Windows.Forms.Timer timecnt;
        private static FileStream fp;
        private static StreamWriter fw;
        private static StreamReader fr;
        private static bool fn = false, noprint = false;
        private static string typed = "", img = "";
        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")]
        static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);
        [DllImport("user32.dll")]
        static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr GetModuleHandle(string lpModuleName);
        [DllImport("user32.dll")]
        static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")]
        static extern bool UnhookWindowsHookEx(IntPtr hhk);
        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Ansi)]
        static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPStr)] string lpFileName);

        private static string Filter(string inp, bool cased = false)
        {
            string output = String.Empty;
            switch (inp)
            {
                case "D1":
                    if (cased) output = "!";
                    else output = "1";
                    break;
                case "NumPad1":
                    output = "1";
                    break;
                case "D2":
                    if (cased) output = "@";
                    else output = "2";
                    break;
                case "NumPad2":
                    output = "2";
                    break;
                case "D3":
                    if (cased) output = "#";
                    else output = "3";
                    break;
                case "NumPad3":
                    output = "3";
                    break;
                case "D4":
                    if (cased) output = "$";
                    else output = "4";
                    break;
                case "NumPad4":
                    output = "4";
                    break;
                case "D5":
                    if (cased) output = "%";
                    else output = "5";
                    break;
                case "NumPad5":
                    output = "5";
                    break;
                case "D6":
                    if (cased) output = "^";
                    else output = "6";
                    break;
                case "NumPad6":
                    output = "6";
                    break;
                case "D7":
                    if (cased) output = "&";
                    else output = "7";
                    break;
                case "NumPad7":
                    output = "7";
                    break;
                case "D8":
                    if (cased) output = "*";
                    else output = "8";
                    break;
                case "NumPad8":
                    output = "8";
                    break;
                case "D9":
                    if (cased) output = "(";
                    else output = "9";
                    break;
                case "NumPad9":
                    output = "9";
                    break;
                case "D0":
                    if (cased) output = ")";
                    else output = "0";
                    break;
                case "NumPad0":
                    output = "0";
                    break;
                case "LShiftKey":
                case "RShiftKey":
                    output = "shift";
                    break;
                case "LControlKey":
                case "RControlKey":
                    output = "ctrl";
                    break;
                case "LMenu":
                case "RMenu":
                    output = "alt";
                    break;
                case "LWin":
                case "RWin":
                    output = "win";
                    break;
                case "OemMinus":
                    if (cased) output = "_";
                    else output = "-";
                    break;
                case "Oemplus":
                    if (cased) output = "+";
                    else output = "=";
                    break;
                case "Oemtilde":
                    if (cased) output = "~";
                    else output = "`";
                    break;
                case "OemOpenBrackets":
                    if (cased) output = "{";
                    else output = "[";
                    break;
                case "Oem6":
                    if (cased) output = "}";
                    else output = "]";
                    break;
                case "Oem5":
                    if (cased) output = "|";
                    else output = "\\";
                    break;
                case "Oem1":
                    if (cased) output = ":";
                    else output = ";";
                    break;
                case "Oem7":
                    if (cased) output = "\"";
                    else output = "'";
                    break;
                case "Oemcomma":
                    if (cased) output = "<";
                    else output = ",";
                    break;
                case "OemPeriod":
                    if (cased) output = ">";
                    else output = ".";
                    break;
                case "OemQuestion":
                    if (cased) output = "?";
                    else output = "/";
                    break;
                default:
                    if (cased || Control.IsKeyLocked(Keys.Capital)) output = inp.ToUpper();
                    else output = inp.ToLower();
                    break;
            }
            return output;
        }

        private static int Modifier(Keys key)
        {
            if (key == Keys.LShiftKey || key == Keys.RShiftKey) return 1;
            if (key == Keys.LControlKey || key == Keys.RControlKey) return 2;
            if (key == Keys.LWin || key == Keys.RWin) return 3;
            if (key == Keys.LMenu || key == Keys.RMenu) return 4;
            else return 5;
        }

        public static void Init()
        {
            AppDomain.CurrentDomain.UnhandledException += crashManagement;
            AppDomain.CurrentDomain.ProcessExit += onExit;
            timecnt = new System.Windows.Forms.Timer();
            timecnt.Interval = 501;
            timecnt.Tick += new EventHandler(timecnt_Tick);
            keyproc = new LowLevelKeyboardProc(hookCallback);
            mouseproc = new LowLevelMouseProc(mhookCallback);
        }

        private static void onExit(object sender, EventArgs e)
        {
            try
            {
                cancel();
            }
            catch(Exception f)
            {
                Environment.Exit(1);
            }
        }

        private static void crashManagement(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                cancel();
            }
            catch (Exception f)
            {
                Environment.Exit(1);
            }
        }

        public static void startLogging()
        {
            if (Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Images"))) Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Images"), true);
            fp = File.Create(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @".\Temp\mn926.dat"));
            fw = new StreamWriter(fp);
            fr = new StreamReader(fp);
            fw.AutoFlush = true;
            fw.WriteLine("[");
            hook = SetHook(keyproc);
            mhook = SetmHook(mouseproc);
        }

        public static void stopLogging()
        {
            UnhookWindowsHookEx(hook);
            UnhookWindowsHookEx(mhook);
            if (!String.IsNullOrEmpty(typed))
            {
                fw.WriteLine("    {");
                fw.WriteLine("        type: \"" + "kbd" + "\",");
                fw.WriteLine("        value: \"" + typed + "\",");
                fw.WriteLine($"        timeStamp: \"{parse_Timestamp()}\"");
                fw.WriteLine("    },");
                typed = "";
            }
        }

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            IntPtr processHandle = LoadLibrary("user32.dll");
            return SetWindowsHookEx(WH_KEYBOARD_LL, proc, processHandle, 0);
        }

        private static IntPtr SetmHook(LowLevelMouseProc mproc)
        {
            IntPtr processHandle = LoadLibrary("user32.dll");
            return SetWindowsHookEx(WH_MOUSE_LL, mproc, processHandle, 0);
        }

        private static IntPtr hookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if ((nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN) || (nCode >= 0 && wParam == (IntPtr)WM_SYSKEYDOWN))
            {
                k = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
                uint currentKey = k.vkCode;
                int i;
                for (i = 0; i < currentCount; i++)
                {
                    if (keyMap[i] == currentKey)
                    {
                        n++;
                        break;
                    }
                }
                if (i == currentCount)
                    keyMap[currentCount++] = currentKey;
                noprint = false;
            }
            if ((nCode >= 0 && wParam == (IntPtr)WM_KEYUP) || (nCode >= 0 && wParam == (IntPtr)WM_SYSKEYUP))
            {
                k = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
                if (!noprint && !isWindowExcluded()) printKeys();
                uint currentKey = k.vkCode;
                int i;
                for (i = 0; i < currentCount; ++i)
                {
                    if (keyMap[i] == currentKey)
                        break;
                }
                for (; i < currentCount - 1; ++i)
                {
                    keyMap[i] = keyMap[i + 1];
                }
                keyMap[currentCount--] = 0;
                if (currentCount == 0)
                    n = 0;
            }
            return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }

        private static IntPtr mhookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                if (!String.IsNullOrEmpty(typed))
                {
                    fw.WriteLine("    {");
                    fw.WriteLine("        type: \"" + "kbd" + "\",");
                    fw.WriteLine("        value: \"" + typed + "\",");
                    fw.WriteLine($"        timeStamp: \"{parse_Timestamp()}\"");
                    fw.WriteLine("    },");
                    typed = "";
                }
                if (isWindowExcluded()) goto ret;
                if (wParam == (IntPtr)WM_LBUTTONDOWN)
                {

                }
                else if (wParam == (IntPtr)WM_RBUTTONDOWN)
                {

                }
                else if (wParam == (IntPtr)WM_LBUTTONUP)
                {
                    p = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
                    if (fn == true)
                    {
                        timecnt.Stop();
                        fw.WriteLine("    {");
                        fw.WriteLine("        type: \"dblclick\",");
                        fw.WriteLine($"        image: \"{img}\",");
                        fw.WriteLine("        coord: {");
                        fw.WriteLine("            x: " + p.pt.x + ",");
                        fw.WriteLine("            y: " + p.pt.y);
                        fw.WriteLine("        },");
                        fw.WriteLine($"        timeStamp: \"{parse_Timestamp()}\"");
                        fw.WriteLine("    },");
                        fn = false;
                        return IntPtr.Zero;
                    }
                    if (!timecnt.Enabled)
                    {
                        img = takeSnap(p.pt.x, p.pt.y);
                        timecnt.Start();
                        fn = true;
                    }
                }
                else if (wParam == (IntPtr)WM_RBUTTONUP)
                {
                    q = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
                    img = takeSnap(q.pt.x, q.pt.y);
                    fw.WriteLine("    {");
                    fw.WriteLine("        type: \"rclick\",");
                    fw.WriteLine($"        image: \"{img}\",");
                    fw.WriteLine("        coord: {");
                    fw.WriteLine("            x: " + q.pt.x + ",");
                    fw.WriteLine("            y: " + q.pt.y);
                    fw.WriteLine("        },");
                    fw.WriteLine($"        timeStamp: \"{parse_Timestamp()}\"");
                    fw.WriteLine("    },");
                }
            }
        ret:
            return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }

        private static void keyPress(bool cased = false)
        {
            int i = 0;
            string toPrint = "";
            if (cased) i++; //Experimental
            for (; i < currentCount; i++)
            {
                if ((keyMap[i] >= 48 && keyMap[i] <= 57) || (keyMap[i] >= 65 && keyMap[i] <= 90) || (Control.IsKeyLocked(Keys.NumLock) && keyMap[i] >= 96 && keyMap[i] <= 105))
                {
                    toPrint += "kbd";
                }
                else
                    toPrint += "kp";
                toPrint += "," + ((Keys)keyMap[i]).ToString() + ",";
            }
            parse_Json(toPrint, cased);
        }

        private static void keyHold()
        {
            string toPrint = "";
            toPrint += "kh";
            for (int i = 0; i < currentCount; i++)
            {
                toPrint += "," + ((Keys)keyMap[i]).ToString();
            }
            parse_Json(toPrint);
        }

        private static void keyComb()
        {
            string toPrint = "";
            toPrint += "kc";
            for (int i = 0; i < currentCount; i++)
            {
                toPrint += "," + ((Keys)keyMap[i]).ToString();
            }
            parse_Json(toPrint);
        }

        private static void printKeys()
        {
            noprint = true;
            int cased = 0;
            if (currentCount > 1)
            {
                for (int i = 0; i < currentCount; i++)
                {
                    if (Modifier((Keys)keyMap[i]) == 1)
                    {
                        cased++; //Experimental
                        continue;
                    }
                    if (Modifier((Keys)keyMap[i]) == 2 || Modifier((Keys)keyMap[i]) == 3 || Modifier((Keys)keyMap[i]) == 4)
                    {
                        cased += Modifier((Keys)keyMap[i]); //Experimental
                        continue;
                    }
                    if ((Keys)keyMap[i] == Keys.Capital || (Keys)keyMap[i] == Keys.Scroll || (Keys)keyMap[i] == Keys.NumLock)
                    {
                        keyPress();
                        break;
                    }
                    else
                    {
                        if (!(keyMap[i] >= 48 && keyMap[i] <= 57) && !(keyMap[i] >= 65 && keyMap[i] <= 90) && !(keyMap[i] >= 186 && keyMap[i] <= 192) && !(keyMap[i] >= 219 && keyMap[i] <= 222) && cased == 1)
                        {
                            cased += 5;
                        }
                        if (cased == 1) keyPress(true); //Experimental
                        else if (cased > 1) keyComb();
                        else keyPress();
                        break;
                    }
                }

            }
            else if (n > 1)
            {
                keyHold();
            }
            else
            {
                keyPress();
            }

        }

        private static void parse_Json(string input, bool cased = false)
        {
            if (!input.StartsWith("kbd") && !String.IsNullOrEmpty(typed))
            {
                fw.WriteLine("    {");
                fw.WriteLine("        type: \"" + "kbd" + "\",");
                fw.WriteLine("        value: \"" + typed + "\",");
                fw.WriteLine($"        timeStamp: \"{parse_Timestamp()}\"");
                fw.WriteLine("    },");
                typed = "";
            }
            String[] strs = input.Split(',');
        kpkh:
            if (strs[0] == "kp" || strs[0] == "kh")
            {
                for (int i = 1; i < strs.Length; i++)
                {
                    if (strs[i] == "kbd")
                    {
                        input = "";
                        for (; i < strs.Length; i++)
                        {
                            input += strs[i] + ",";
                        }
                        strs = input.Split(',');
                        goto kbd;
                    }
                    if (strs[i] == "kp" || strs[i] == "kh") continue;
                    if (strs[i] == "") continue;
                    fw.WriteLine("    {");
                    fw.WriteLine("        type: \"" + strs[0] + "\",");
                    fw.WriteLine("        value: \"" + Filter(strs[i], cased) + "\",");
                    fw.WriteLine($"        timeStamp: \"{parse_Timestamp()}\"");
                    fw.WriteLine("    },");
                }
            }
        kbd:
            if (strs[0] == "kbd")
            {
                for (int i = 1; i < strs.Length; i++)
                {
                    if (strs[i] == "kp" || strs[i] == "kh")
                    {
                        input = "";
                        for (; i < strs.Length; i++)
                        {
                            input += strs[i] + ",";
                        }
                        strs = input.Split(',');
                        goto kpkh;
                    }
                    if (strs[i] == "kbd") continue;
                    if (strs[i] == "") continue;
                    strs[i] = Filter(strs[i], cased);
                    typed += strs[i];
                }
            }
            if (strs[0] == "kc")
            {
                fw.WriteLine("    {");
                fw.WriteLine("        type: \"" + "kbd_shortcut" + "\",");
                fw.Write("        value: [\"" + Filter(strs[1], cased));
                for (int i = 2; i < strs.Length; i++)
                {
                    if (strs[i] == "") continue;
                    fw.Write("\", \"" + Filter(strs[i], cased));
                }
                fw.Write("\"]," + Environment.NewLine);
                fw.WriteLine($"        timeStamp: \"{parse_Timestamp()}\"");
                fw.WriteLine("    },");
            }
        }

        private static double parse_Timestamp()
        {
            return (TimeZoneInfo.ConvertTimeToUtc(DateTime.Now) - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
        }

        private static void timecnt_Tick(object sender, EventArgs e)
        {
            timecnt.Stop();
            fw.WriteLine("    {");
            fw.WriteLine("        type: \"lclick\",");
            fw.WriteLine($"        image: \"{img}\",");
            fw.WriteLine("        coord: {");
            fw.WriteLine("            x: " + p.pt.x + ",");
            fw.WriteLine("            y: " + p.pt.y);
            fw.WriteLine("        },");
            fw.WriteLine($"        timeStamp: \"{parse_Timestamp()}\"");
            fw.WriteLine("    },");
            fn = false;
        }

        public static void finish(string path)
        {
            if (path == "")
            {
                cancel();
                return;
            }
            while (timecnt.Enabled)
            {
                Thread.Sleep(100);
            }
            fw.Write("]");
            fp.Seek(-4, SeekOrigin.Current);
            fw.Write(" ");
            fp.Close();
            if (File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @".\Temp\mn926.dat")))
            {
                if (File.Exists(path)) File.Delete(path);
                File.Copy(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @".\Temp\mn926.dat"), path);
                File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @".\Temp\mn926.dat"));
            }
            if (Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Images")))
            {
                if (Directory.Exists(Path.Combine(Path.GetDirectoryName(path), @"Images"))) Directory.Delete(Path.Combine(Path.GetDirectoryName(path), @"Images"), true);
                Directory.CreateDirectory(Path.Combine(Path.GetDirectoryName(path), @"Images"));
                foreach (string file in Directory.GetFiles(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Images")))
                {
                    string name = Path.GetFileName(file);
                    string dest = Path.Combine(Path.Combine(Path.GetDirectoryName(path), @"Images"), name);
                    File.Copy(file, dest);
                }
                Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Images"), true);
            }
        }

        public static void cancel()
        {
            if (timecnt.Enabled) timecnt.Stop();
            if (fp != null) fp.Close();
            if (File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @".\Temp\mn926.dat")) && Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Images")))
            {
                File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @".\Temp\mn926.dat"));
                Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Images"), true);
            }
        }

        private static string takeSnap(int x, int y)
        {
            string filename = @"image_" + m.ToString().PadLeft(2, '0') + @".jpg";
            string filepath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Images\" + filename;
            if (!Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Images"))) Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Images"));
            Bitmap bmp = new Bitmap(60, 70, PixelFormat.Format32bppArgb);
            Graphics grp = Graphics.FromImage(bmp);
            grp.CopyFromScreen(x - 30, y - 35, 0, 0, bmp.Size, CopyPixelOperation.SourceCopy);
            bmp.Save(filepath, ImageFormat.Jpeg);
            m++;
            return filename;
        }

        private static bool isWindowExcluded()
        {
            uint pID;
            string processName;
            GetWindowThreadProcessId(GetForegroundWindow(), out pID);
            processName = Process.GetProcessById((int)pID).ProcessName;
            return (processName == "chrome" || processName == "firefox" || processName == "opera" || processName == "msedge" || processName == "iexplore");
        }
    }
}
