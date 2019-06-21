using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LibSample
{
    class GameRelayServerTest
    {
        public void Run()
        {

            string host = "jimzrt.duckdns.org";
            string ipAddress = "";
            IPAddress[] addresslist = Dns.GetHostAddresses(host);

            foreach (IPAddress theaddress in addresslist)
            {
                if (theaddress.AddressFamily != AddressFamily.InterNetwork)
                    continue;
                ipAddress = theaddress.ToString();
                break;
            }

            if (ipAddress.Length == 0)
            {
                Console.WriteLine("Could not determine ip address!");
                return;
            }

            var netListener = new EventBasedNetListener();
            var _netClient = new NetManager(netListener);
            var _dataWriter = new NetDataWriter();
            // _netClient.UnconnectedMessagesEnabled = true;
            _netClient.IPv6Enabled = false;
            _netClient.UpdateTime = 15;
            _netClient.Start();
            _netClient.Connect(ipAddress, 50010, "test_key");

            Random random = new Random();
            Thread.Sleep(random.Next(240000));

            _netClient.DisconnectAll();

        }
    }
}
