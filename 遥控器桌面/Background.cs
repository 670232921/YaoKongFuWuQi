using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using System.Threading.Tasks;
using System.Threading;

namespace 遥控器桌面
{
    class Background
    {
        private const string ROOTNAME = "Medias";
        private const string CONFIG_FILE_NAME = "MediaConfig.xml";
        private const string LOG_FILE_NAME = "Log.txt";
        private const string MEDIA_THREAD_NAME = "MediaName";
        private const int UDP_PORT_DESKTOP = 9330;
        private const byte NET_GET = (byte)'g';
        private const byte NET_CTRL = (byte)'c';
        private readonly string NET_NAME = Dns.GetHostName();
        List<Dictionary<string, string>> mediaList;

        #region NativeMethods

        [DllImport("User32.dll")]
        static extern int SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern void mouse_event(uint dwFlags, Int32 dx, Int32 dy, uint dwData, UIntPtr dwExtraInfo);
        const uint MOUSEEVENTF_ABSOLUTE = 0x8000;
        const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        const uint MOUSEEVENTF_LEFTUP = 0x0004;
        const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
        const uint MOUSEEVENTF_MOVE = 0x0001;
        const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        const uint MOUSEEVENTF_XDOWN = 0x0080;
        const uint MOUSEEVENTF_XUP = 0x0100;
        const uint MOUSEEVENTF_WHEEL = 0x0800;
        const uint MOUSEEVENTF_HWHEEL = 0x01000;
        #endregion

        public Background()
        {
            LoadXML();
        }
        
        public void test()
        {
            Start();
        }

        public void Start()
        {
            UDPListener();
            while(true)
            {
                Thread.Sleep(500);
            }
        }

        private void PressKey(string key)
        {
            if (mediaList == null || mediaList.Count == 0)
            {
                Log("No XML Aviable");
                return;
            }

            foreach (var media in mediaList)
            {
                string mediaName = media[MEDIA_THREAD_NAME];
                if (FindProcessAndPutFront(mediaName))
                {
                    string value = media[key];
                    if (value == null)
                    {
                        Log("No " + key + "defined in " + mediaName);
                        break;
                    }

                    System.Windows.Forms.SendKeys.SendWait(value);
                }
            }
        }

        private Task PressKeyTask(string ctrlKey)
        {
            return Task.Factory.StartNew(() => PressKey(ctrlKey));
        }

        private bool FindProcessAndPutFront(string processName)
        {
            Process[] processs = Process.GetProcessesByName(processName);
            if (processs.Length > 0)
            {
                Process kmp = processs[0];
                SetForegroundWindow(kmp.MainWindowHandle);
                return true;
            }
            return false;
        }

        private void MouseLeftClick()
        {
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
        }

        private void MouseRightClick()
        {
            mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, UIntPtr.Zero);
            mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, UIntPtr.Zero);
        }

        private void MouseMove(int x, int y, int speed = 1)
        {
            mouse_event(MOUSEEVENTF_MOVE, x * speed, y * speed, 0, UIntPtr.Zero);
        }

        private void UDPListener()
        {
            UdpClient lisener = new UdpClient(UDP_PORT_DESKTOP);
            while (true)
            {
                IPEndPoint ipe = new IPEndPoint(IPAddress.Any, 0);
                byte[] receivedByte = lisener.Receive(ref ipe);
                string receivedString = System.Text.Encoding.UTF8.GetString(receivedByte);
                Debug.WriteLine("UDP Receive " + receivedString + "------" + ipe.ToString());

                switch (receivedByte[0])
                {
                    case NET_GET:
                        {
                            byte[] sendByte = Encoding.UTF8.GetBytes(NET_NAME);
                            lisener.SendAsync(sendByte, sendByte.Length, ipe);
                            break;
                        }
                    case NET_CTRL:
                        {
                            string ctrlCode = receivedString.Substring(1, receivedString.Length - 1);
                            PressKeyTask(ctrlCode);
                            break;
                        }
                    default:
                        {
                            break;
                        }
                }
            }
        }

        private bool LoadXML()
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
                            string value = keyvalue.InnerText;
                            if (value.StartsWith("\"") && value.EndsWith("\""))
                            {
                                value = value.Substring(1, value.Length - 2);
                                dict.Add(keyvalue.Name, value);
                            }
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

        private void Log(string str)
        {
            FileInfo file = new FileInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LOG_FILE_NAME));
            StreamWriter writer = file.AppendText();
            writer.WriteLine(DateTime.Now.ToString() + "----" + str);
            writer.Close();
        }
    }
}

//<?xml version="1.0" encoding="UTF-8"?>
//<Medias>
//    <Media>
//        <MediaName>"KMPlayer"</MediaName>
//        <Start>" "</Start>
//        <Pause>" "</Pause>
//        <Stop>"{ESC}"</Stop>
//        <FastForward>"{RIGHT}"</FastForward>
//        <FastBackward>"{LEFT}"</FastBackward>
//        <Previous>"{PGUP}"</Previous>
//        <Next>"{PGDN}"</Next>
//    </Media>
//</Medias>
