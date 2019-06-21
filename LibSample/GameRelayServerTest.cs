﻿using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static LibSample.GameRelayServer;

namespace LibSample
{


    class GameRelayServerTest
    {

        Dictionary<int, Player> playersLobby = new Dictionary<int, Player>();
        Dictionary<int, Player> playersRoom = new Dictionary<int, Player>();

        public static string GenerateName(int len)
        {
            Random r = new Random();
            string[] consonants = { "b", "c", "d", "f", "g", "h", "j", "k", "l", "m", "l", "n", "p", "q", "r", "s", "sh", "zh", "t", "v", "w", "x" };
            string[] vowels = { "a", "e", "i", "o", "u", "ae", "y" };
            string Name = "";
            Name += consonants[r.Next(consonants.Length)].ToUpper();
            Name += vowels[r.Next(vowels.Length)];
            int b = 2; //b tells how many times a new letter has been added. It's 2 right now because the first two letters are already in the name.
            while (b < len)
            {
                Name += consonants[r.Next(consonants.Length)];
                b++;
                Name += vowels[r.Next(vowels.Length)];
                b++;
            }

            return Name;


        }


        public void Run()
        {

            string host = "localhost";
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
            netListener.NetworkReceiveEvent += (peer, reader, deliveryMethod) =>
            {

                var _requestType = (RequestType)reader.GetByte();
                Console.WriteLine("CLIENT: Got Request {0} from Server!", _requestType);
                Player player = null;
                switch (_requestType)
                {
                    case RequestType.PlayerUpdateName:
                        var playerId = reader.GetInt();
                        var playerName = reader.GetString();
                        if (playersLobby.ContainsKey(playerId))
                        {
                            player = playersLobby[playerId];
                            player.Name = playerName;
                        } 
                        else if (playersRoom.ContainsKey(playerId))
                        {
                            player = playersRoom[playerId];
                            player.Name = playerName;
                        } else
                        {
                            throw new Exception("Player neither in lobbyPlayers nor in roomPlayers");
                        }
                        
                        break;
                    case RequestType.PlayerLobbyJoined:
                        playerId = reader.GetInt();
                        player = new Player(playerId);
                        playersLobby.Add(playerId, player);
                        break;
                    case RequestType.PlayerLeft:
                        playerId = reader.GetInt();
                        if (playersLobby.ContainsKey(playerId))
                        {
                            playersLobby.Remove(playerId);
                        }
                        else if (playersRoom.ContainsKey(playerId))
                        {
                            playersRoom.Remove(playerId);
                        }
                        else
                        {
                            throw new Exception("Player neither in lobbyPlayers nor in roomPlayers");
                        }
                        break;
                    case RequestType.LobbyInformation:
                        var lobbyCount = reader.GetInt();
                        for(int i = 0; i < lobbyCount; i++)
                        {
                            playerId = reader.GetInt();
                            playerName = reader.GetString();
                            player = new Player(playerId, playerName);
                            playersLobby.Add(playerId, player);
                        }
                        break;
                    case RequestType.PlayerRoomJoined:
                        playerId = reader.GetInt();
                        playerName = reader.GetString();
                        player = new Player(playerId, playerName);
                        playersRoom.Add(playerId, player);
                        break;
                    case RequestType.RoomUpdate:
                        break;
                    case RequestType.RoomInformation:      
                        break;
                    case RequestType.JoinRoom:
                        var success = reader.GetBool();
                        if (success)
                        {
                            var roomPlayerCount = reader.GetInt();
                            for (int i = 0; i < roomPlayerCount; i++)
                            {
                                playerId = reader.GetInt();
                                playerName = reader.GetString();
                                player = new Player(playerId, playerName);
                                playersRoom.Add(playerId, player);
                            }
                        }
                        break;
                    case RequestType.CreateRoom:
                        success = reader.GetBool();
                        break;
                }
            };


            var _netClient = new NetManager(netListener);
            var _dataWriter = new NetDataWriter();

            var random = new Random();
            // _netClient.UnconnectedMessagesEnabled = true;
            _netClient.IPv6Enabled = false;
            _netClient.UpdateTime = 15;
            _netClient.Start();
            _netClient.Connect(ipAddress, 50010, "test_key");

            int waitTime = random.Next(10000);
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            while(stopWatch.ElapsedMilliseconds < waitTime)
            {
                _netClient.PollEvents();
                Thread.Sleep(15);
            }
            stopWatch.Reset();


            var randomName = GenerateName(8);
            _dataWriter.Put((byte)GameRelayServer.RequestType.PlayerUpdateName);
            _dataWriter.Put(randomName);
            _netClient.SendToAll(_dataWriter, DeliveryMethod.ReliableOrdered);
            _dataWriter.Reset();


            waitTime = random.Next(30000);
            stopWatch = new Stopwatch();
            stopWatch.Start();
            while (stopWatch.ElapsedMilliseconds < waitTime)
            {
                _netClient.PollEvents();
                Thread.Sleep(15);
            }
            stopWatch.Reset();


            GameRelayServer.RequestType requestType;
            if(random.Next(10) >= 5)
            {
                requestType = GameRelayServer.RequestType.CreateRoom;
                _dataWriter.Put((byte)requestType);
                _netClient.SendToAll(_dataWriter, DeliveryMethod.ReliableOrdered);
            } else
            {
                requestType = GameRelayServer.RequestType.JoinRoom;
                var roomNumber = random.Next(20);
                _dataWriter.Put((byte)requestType);
                _dataWriter.Put(roomNumber);
                _netClient.SendToAll(_dataWriter, DeliveryMethod.ReliableOrdered);
            }

            waitTime = random.Next(30000);
            stopWatch = new Stopwatch();
            stopWatch.Start();
            while (stopWatch.ElapsedMilliseconds < waitTime)
            {
                _netClient.PollEvents();
                Thread.Sleep(15);
            }
            stopWatch.Reset();

            _dataWriter.Reset();
            _netClient.DisconnectAll();

        }

    }
}
