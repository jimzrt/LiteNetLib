using System;
using System.Threading;
using LiteNetLib.Utils;

namespace LibSample
{
    class Program
    {
        static void Main(string[] args)
        {
            //Test ntp
            NtpRequest ntpRequest = null;
            ntpRequest = NtpRequest.Create("pool.ntp.org", ntpPacket =>
            {
                ntpRequest.Close();
                if (ntpPacket != null)
                    Console.WriteLine("[MAIN] NTP time test offset: " + ntpPacket.CorrectionOffset);
                else
                    Console.WriteLine("[MAIN] NTP time error");
            });
            ntpRequest.Send();

            Random random = new Random();
            var gameServer = new GameRelayServer();
             new Thread(new ThreadStart(gameServer.Run)).Start();
            Thread.Sleep(1000);
            Console.WriteLine("Starting Client");
            var gameServerTest = new GameRelayServerTest();
            for(int i = 0; i < 1000; i++)
            {
                new Thread(new ThreadStart(gameServerTest.Run)).Start();
                Thread.Sleep(random.Next(1000));
            }
           

        }
    }
}

