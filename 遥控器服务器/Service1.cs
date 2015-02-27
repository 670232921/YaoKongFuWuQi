using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Text;
using System.Xml;
using System.Timers;
using System.Runtime.InteropServices;

namespace 遥控器服务器
{
    public partial class Service1 : ServiceBase
    {
        private readonly string ROOTNAME = "Medias";
        private readonly string CONFIG_FILE_NAME = "MediaConfig.xml";
        private readonly string LOG_FILE_NAME = "Log.txt";
        private readonly string MEDIA_THREAD_NAME = "MediaName";

        #region NativeMethods
        [DllImport("user32.dll")]
        public static extern uint SendInput(uint nInputs, [MarshalAs(UnmanagedType.LPArray), In] INPUT[] pInputs, int cbSize);

        [StructLayout(LayoutKind.Sequential)]
        public struct INPUT
        {
            public Int32 type;
            public InputUnion u;
        }
        [StructLayout(LayoutKind.Explicit)]
        public struct InputUnion
        {
            [FieldOffset(0)] public MOUSEINPUT mi;
            [FieldOffset(0)] public KEYBDINPUT ki;
            [FieldOffset(0)] public HARDWAREINPUT hi;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct MOUSEINPUT
        {
            public Int32 dx;
            public Int32 dy;
            public Int32 MouseData;
            public Int32 dwFlag;
            public Int32 time;
            public IntPtr dwExtraInfo;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct KEYBDINPUT
        {
            public Int16 wVk;
            public Int16 wScan;
            public Int32 dwFlags;
            public Int32 time;
            public IntPtr dwExtraInfo;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct HARDWAREINPUT
        {
            public Int32 uMsg;
            public Int16 wParamL;
            public Int16 wParamH;
        }


        [DllImport("User32.dll")]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("User32.dll")]
        static extern int SetForegroundWindow(IntPtr hWnd);

        [DllImport("User32.Dll", EntryPoint = "PostMessageA")]
        static extern bool PostMessage(IntPtr hWnd, uint msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        static extern byte VkKeyScan(char ch);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);
        #endregion


        List<Dictionary<string, string>> mediaList;
        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            Timer timer = new Timer(1000);
            timer.Elapsed += timer_Elapsed;
            timer.Enabled = true;
            timer.Start();

            if (!LoadXML())
            {
                Log("Fail to open config file");
                return;
            }

        }

        private void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            KeyPress("sd");
            //System.Windows.Forms.SendKeys.Send("^a");
            Debug.WriteLine("terer");
        }

        void KeyPress(string key)
        {
            Process[] myProcesss = Process.GetProcessesByName("KMPlayer");

            if (myProcesss.Length > 0)
            {
                Process kmp = myProcesss[0];
                SetForegroundWindow(kmp.MainWindowHandle);
                System.Windows.Forms.SendKeys.SendWait("{RIGHT}");
            }


            //INPUT[] inputs = new INPUT[6];
            //for (int i = 0; i < 6; i ++)
            //{
            //    inputs[i].type = 1;
            //    if (i < 3)
            //    {
            //        inputs[i].u.ki.dwFlags = 0;
            //    }
            //    else
            //    {
            //        inputs[i].u.ki.dwFlags = 2;
            //    }
            //}

            //inputs[0].u.ki.wVk = inputs[3].u.ki.wVk = 0x11;
            //inputs[1].u.ki.wVk = inputs[4].u.ki.wVk = 0x12;
            //inputs[2].u.ki.wVk = inputs[5].u.ki.wVk = 0x5A;

            //SendInput(6, inputs, Marshal.SizeOf(typeof(INPUT)));


            //INPUT[] inputs = new INPUT[4];
            //for (int i = 0; i < 4; i++)
            //{
            //    inputs[i].type = 1;
            //    if (i < 2)
            //    {
            //        inputs[i].u.ki.dwFlags = 0;
            //    }
            //    else
            //    {
            //        inputs[i].u.ki.dwFlags = 2;
            //    }
            //}

            //inputs[0].u.ki.wVk = inputs[2].u.ki.wVk = 0x11;
            //inputs[1].u.ki.wVk = inputs[3].u.ki.wVk = 0x41;

            //SendInput(4, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        protected override void OnStop()
        {
        }

        bool LoadXML()
        {
            string xmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CONFIG_FILE_NAME);
            if (new FileInfo(xmlPath).Exists)
            {
                try
                {
                    XmlDocument xml = new XmlDocument();
                    xml.Load(xmlPath);
                    XmlNode rootNode = xml.SelectSingleNode(ROOTNAME);

                    mediaList = new List<Dictionary<string, string>>();
                    foreach (XmlNode mediaNode in rootNode.ChildNodes)
                    {
                        Dictionary<string, string> dict = new Dictionary<string, string>();

                        foreach (XmlNode keyvalue in mediaNode.ChildNodes)
                        {
                            dict.Add(keyvalue.Name, keyvalue.InnerText);
                        }

                        if (dict.ContainsKey(MEDIA_THREAD_NAME))
                        {
                            mediaList.Add(dict);
                        }
                    }

                    if (mediaList.Count > 0)
                    {
                        return true;
                    }
                    else
                    {
                        Log("Empty file: " + xmlPath);
                        return false;
                    }
                }
                catch (Exception e)
                {
                    Debug.Write(e);
                    return false;
                }
            }
            else
            {
                Log("Can't find " + xmlPath);
                return false;
            }
        }

        void Log(string str)
        {
            FileInfo file = new FileInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LOG_FILE_NAME));
            StreamWriter writer = file.AppendText();
            writer.WriteLine(DateTime.Now.ToString() + "----" + str);
            writer.Close();
        }
    }
}
