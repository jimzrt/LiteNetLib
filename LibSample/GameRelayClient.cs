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
using static LibSample.GameRelayServer;

namespace LibSample
{

    class GameRelayClient
    {
        Dictionary<int, Player> playersLobby = new Dictionary<int, Player>();
        Dictionary<int, Player> playersRoom = new Dictionary<int, Player>();
        NetManager _netClient;

        public void PollNetwork()
        {
            while (true)
            {
                _netClient.PollEvents();
                Thread.Sleep(10);
            }

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
                        }
                        else
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
                        for (int i = 0; i < lobbyCount; i++)
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


            _netClient = new NetManager(netListener);
            var _dataWriter = new NetDataWriter();

            var random = new Random();
            // _netClient.UnconnectedMessagesEnabled = true;
            _netClient.IPv6Enabled = false;
            _netClient.UpdateTime = 15;
            _netClient.Start();
            _netClient.Connect(ipAddress, 50010, "test_key");

            new Thread(new ThreadStart(PollNetwork)).Start();


            while (true)
            {

                var input = Console.ReadLine();
                var input_arr = input.Split(' ');
                var command = input_arr[0];
                if (command == "name")
                {
                    var name = input_arr[1];
                    _dataWriter.Put((byte)RequestType.PlayerUpdateName);
                    _dataWriter.Put(name);
                    _netClient.SendToAll(_dataWriter, DeliveryMethod.ReliableOrdered);
                    _dataWriter.Reset();
                }
                else if (command == "create_room")
                {

                    _dataWriter.Put((byte)RequestType.CreateRoom);
                    _netClient.SendToAll(_dataWriter, DeliveryMethod.ReliableOrdered);
                    _dataWriter.Reset();

                }
                else if (command == "join_room")
                {
                    if (!Int32.TryParse(input_arr[1], out int roomNumber))
                    {
                        Console.WriteLine("Parameter has to be a integer!");
                        continue;
                    }
                    _dataWriter.Put((byte)RequestType.JoinRoom);
                    _dataWriter.Put(roomNumber);
                    _netClient.SendToAll(_dataWriter, DeliveryMethod.ReliableOrdered);
                    _dataWriter.Reset();


                }
                else if (command == "diag")
                {
                    PPrintPlayerLobby();
                    PPintPlayerRoom();
                }
                else if (command == "quit" || command == "exit")
                {
                    Console.WriteLine("goodbye!");
                    _netClient.DisconnectAll();
                    Console.ReadLine();
                    Environment.Exit(0);
                }






            }






        }

        private void PPintPlayerRoom()
        {
            Console.WriteLine("-----Lobby----");
            Console.WriteLine("count: {0}", playersLobby.Count);
            foreach (var entry in playersLobby)
            {
                Console.WriteLine("id: {0} - name {1}", entry.Key, entry.Value.Name);
            }
            Console.WriteLine("-------------");
        }

        private void PPrintPlayerLobby()
        {
            Console.WriteLine("-----Room----");
            Console.WriteLine("count: {0}", playersRoom.Count);
            foreach (var entry in playersRoom)
            {
                Console.WriteLine("id: {0} - name {1}", entry.Key, entry.Value.Name);
            }
            Console.WriteLine("-------------");
        }
    }
}
