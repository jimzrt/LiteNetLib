using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Threading;

namespace LibSample
{
    internal class Room
    {
        bool inGame = false;
        public HashSet<int> player = new HashSet<int>();
        public Room(int id)
        {
            Id = id;
            player.Add(id);
        }

        public int Id { get; }

        public bool AddPlayer(int id)
        {
            return player.Add(id);
        }

        public bool RemovePlayer(int id)
        {
            return player.Remove(id);
        }
    }


    internal class GameRelayServer
    {
        private const int ServerPort = 50010;
        private const string ConnectionKey = "test_key";
        private NetDataWriter _dataWriter;
        private NetManager _netServer;

        private enum RequestType : byte {
            PlayerLobbyJoined,
            PlayerLeft,
            LobbyInformation,
            PlayerRoomJoned,
            RoomUpdate,
            RoomInformation,
            JoinRoom,
            CreateRoom
        };

        private Dictionary<int, NetPeer> allPlayer = new Dictionary<int, NetPeer>();
        private Dictionary<int, Room> allRooms = new Dictionary<int, Room>();
        HashSet<int> lobbyPlayer = new HashSet<int>();
        HashSet<int> availableRooms = new HashSet<int>();
        Dictionary<int, int> playerToRoomMap = new Dictionary<int, int>();

        public void Run()
        {
            Console.WriteLine("=== GameRelayServer ===");

            EventBasedNetListener netListener = new EventBasedNetListener();

            netListener.PeerConnectedEvent += peer =>
            {
                Console.WriteLine("Player with id {0} connected, adding to lobby", peer.Id);
                allPlayer.Add(peer.Id, peer);
                Console.WriteLine("Got {0} player total", allPlayer.Count);
                foreach(var playerId in lobbyPlayer)
                {
                    SendPlayerJoinedLobbyEvent(allPlayer[playerId], peer.Id);
                }
                lobbyPlayer.Add(peer.Id);
                SendLobbyInformation(peer);
            };

            netListener.ConnectionRequestEvent += request =>
            {
                request.AcceptIfKey(ConnectionKey);
            };


            netListener.PeerDisconnectedEvent += (peer, disconnectInfo) =>
            {
               // clients.Remove(peer);

                Console.WriteLine("Player with id {0} disconnecte with Reason {1}", peer.Id, disconnectInfo.Reason);
                if (disconnectInfo.AdditionalData.AvailableBytes > 0)
                {
                    Console.WriteLine("Disconnect data: " + disconnectInfo.AdditionalData.GetInt());
                }

                allPlayer.Remove(peer.Id);
                Console.WriteLine("Got {0} player total", allPlayer.Count);
                if (lobbyPlayer.Contains(peer.Id))
                {
                    Console.WriteLine("Player was in lobby, notify other player");
                    lobbyPlayer.Remove(peer.Id);
              
                    foreach (var playerId in lobbyPlayer)
                    {
                        SendPlayerLeftLobbyEvent(allPlayer[playerId], peer.Id);
                    }
                } else if (allRooms.ContainsKey(peer.Id))
                {
                    Room playerRoom = allRooms[peer.Id];
                    Console.WriteLine("Player was creator of room {0}, notify other player and remove room", playerRoom.Id);
                    playerRoom.RemovePlayer(peer.Id);
                    foreach (var playerId in playerRoom.player)
                    {
                        var player = allPlayer[playerId];
                        SendCreatorLeftRoom(player, peer.Id);
                        player.Disconnect();
                        allPlayer.Remove(playerId);
                    }
                    allRooms.Remove(peer.Id);
                    playerToRoomMap.Remove(peer.Id);
                    
                }
                else if (playerToRoomMap.ContainsKey(peer.Id))
                {
                    Room playerRoom = allRooms[playerToRoomMap[peer.Id]];
                    Console.WriteLine("Player was in room {0}, notify other player", playerRoom.Id);
                    playerRoom.RemovePlayer(peer.Id);
                    foreach (var playerId in playerRoom.player)
                    {
                        SendPlayerLeftRoomEvent(allPlayer[playerId], peer.Id);
                    }
                }


            };

            netListener.NetworkReceiveEvent += (peer, reader, deliveryMethod) =>
            {

                RequestType requestType = (RequestType)reader.GetByte();
                Console.WriteLine("Got Request {0}, doing nothing for now!", requestType);
                switch (requestType)
                {
                    case RequestType.PlayerLobbyJoined:
                        break;
                    case RequestType.PlayerLeft:
                        break;
                    case RequestType.LobbyInformation:
                        break;
                    case RequestType.PlayerRoomJoned:
                        break;
                    case RequestType.RoomUpdate:
                        break;
                    case RequestType.RoomInformation:
                        break;
                    case RequestType.JoinRoom:
                        break;
                    case RequestType.CreateRoom:
                        break;
                }

                //  NetDataWriter writer = new NetDataWriter();
                //   writer.Put((byte)RequestType.PlayerMove);
                //writer.Put(peer.Id);
                //writer.PutArray(reader.GetFloatArray());
                //foreach (NetPeer client in clients)
                //{
                //    if (client == peer)
                //    {
                //        continue;
                //    }
                //    client.Send(writer, DeliveryMethod.ReliableOrdered);


                //}
            };

            _dataWriter = new NetDataWriter();
            _netServer = new NetManager(netListener)
            {
                //   _netServer.SimulatePacketLoss = true;
                // _netServer.SimulationPacketLossChance = 80;
                //  _netServer.BroadcastReceiveEnabled = true;
                UpdateTime = 15,
                IPv6Enabled = false
            };
            _netServer.Start(ServerPort);



            // keep going until ESCAPE is pressed
            Console.WriteLine("Press ESC to quit");

            while (true)
            {
                if (Console.KeyAvailable)
                {
                    ConsoleKey key = Console.ReadKey(true).Key;
                    if (key == ConsoleKey.Escape)
                    {
                        break;
                    }

                }

                _netServer.PollEvents();


                Thread.Sleep(10);
            }

        }

        private void SendPlayerLeftRoomEvent(NetPeer netPeer, int id)
        {
            Console.WriteLine("TODO: SendPlayerLeftRoom to {0}", netPeer.Id);
        }

        private void SendLobbyInformation(NetPeer peer)
        {
            Console.WriteLine("TODO: SendLobbyInformation to {0}", peer.Id);
        }

        private void SendCreatorLeftRoom(NetPeer netPeer, int id)
        {
            Console.WriteLine("TODO: SendCreatorLeftRoom to {0}", netPeer.Id);
        }

        private void SendPlayerLeftLobbyEvent(NetPeer netPeer, int id)
        {
            Console.WriteLine("TODO: SendPlayerLeftLobbyEvent to {0}", netPeer.Id);
        }

        private void SendPlayerJoinedLobbyEvent(NetPeer netPeer, int id)
        {
            Console.WriteLine("TODO: SendPlayerJoinedLobbyEvent to {0}", netPeer.Id);
        }
    }
}
