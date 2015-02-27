using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace 测试
{
    class Program
    {
        static int UDP_PORT_TEST = 9331;
        private static string NET_GET = "g";
        private static string NET_CTRL = "c";
        private static string NET_NAME = "Test";
        static void Main(string[] args)
        {
            UDPListener();
        }

        private static void UDPListener()
        {
            IPEndPoint ipe = new IPEndPoint(IPAddress.Broadcast, 9330);
            UdpClient lisener = new UdpClient(UDP_PORT_TEST);
            while (true)
            {
                // find pc
                byte[] sendByte = Encoding.UTF8.GetBytes(NET_GET);
                lisener.Send(sendByte, sendByte.Length, ipe);
                IPEndPoint ipee = new IPEndPoint(IPAddress.Any, 9330);
                Thread.Sleep(10000);
                byte[] receivedByte = lisener.Receive(ref ipee);
                string receivedString = System.Text.Encoding.UTF8.GetString(receivedByte);
                Debug.WriteLine(receivedString + "---ttt---" + ipee.ToString());

                //// send start
                //byte[] sendCtrlByte = Encoding.UTF8.GetBytes(NET_CTRL + "Start");
                //lisener.Send(sendCtrlByte, sendCtrlByte.Length, ipee);
                //Thread.Sleep(3000);

                //// send fastfoward
                //sendCtrlByte = Encoding.UTF8.GetBytes(NET_CTRL + "FastForward");
                //lisener.Send(sendCtrlByte, sendCtrlByte.Length, ipee);

                Thread.Sleep(3000);
            }
        }
    }
}
