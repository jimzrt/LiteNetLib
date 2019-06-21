using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Threading;

namespace LibSample
{
    internal class PeerData
    {
        public string Name {get;set;}
        public Room CurrentRoom { get; set; }

        public PeerData(String name)
        {
            Name = name;
        }

        public PeerData(string name, Room room)
        {
            Name = name;
            CurrentRoom = room;
        }
        
    }

    internal class Room
    {
        bool inGame = false;
        public HashSet<NetPeer> player = new HashSet<NetPeer>();
        public Room(NetPeer peer)
        {
            Id = peer.Id;
            player.Add(peer);
        }

        public int Id { get; }

        public bool AddPlayer(NetPeer peer)
        {
            return player.Add(peer);
        }

        public bool RemovePlayer(NetPeer peer)
        {
            return player.Remove(peer);
        }
    }


    internal class GameRelayServer
    {
        private const int ServerPort = 50010;
        private const string ConnectionKey = "test_key";
        private NetDataWriter _dataWriter;
        private NetManager _netServer;

        public enum RequestType : byte {
            PlayerUpdateName,
            PlayerLobbyJoined,
            PlayerLeft,
            LobbyInformation,
            PlayerRoomJoined,
            RoomUpdate,
            RoomInformation,
            JoinRoom,
            CreateRoom
        };

       // private Dictionary<int, NetPeer> allPlayer = new Dictionary<int, NetPeer>();
        private Dictionary<int, Room> allRooms = new Dictionary<int, Room>();
        //HashSet<int> lobbyPlayer = new HashSet<int>();
        HashSet<NetPeer> lobbyPlayers = new HashSet<NetPeer>();
        HashSet<int> availableRooms = new HashSet<int>();
        //Dictionary<int, int> playerToRoomMap = new Dictionary<int, int>();

        public void Run()
        {
            Console.WriteLine("=== GameRelayServer ===");

            EventBasedNetListener netListener = new EventBasedNetListener();

            netListener.PeerConnectedEvent += peer =>
            {
                Console.WriteLine("Player with id {0} connected, adding to lobby", peer.Id);
                //allPlayer.Add(peer.Id, peer);
                Console.WriteLine("Got {0} player total", _netServer.PeersCount);
                foreach(var lobbyPlayer in lobbyPlayers)
                {
                    SendPlayerJoinedLobbyEvent(lobbyPlayer, peer.Id);
                }
                SendLobbyInformation(peer);
                lobbyPlayers.Add(peer);
                peer.Tag = new PeerData(peer.Id.ToString());
               
            };

            netListener.ConnectionRequestEvent += request =>
            {
                request.AcceptIfKey(ConnectionKey);
            };


            netListener.PeerDisconnectedEvent += (peer, disconnectInfo) =>
            {
                // clients.Remove(peer);

                var peerData =  (PeerData)peer.Tag;

                Console.WriteLine("Player with id {0} disconnecte with Reason {1}", peer.Id, disconnectInfo.Reason);
                if (disconnectInfo.AdditionalData.AvailableBytes > 0)
                {
                    Console.WriteLine("Disconnect data: " + disconnectInfo.AdditionalData.GetInt());
                }

                //allPlayer.Remove(peer.Id);
                Console.WriteLine("Got {0} player total", _netServer.PeersCount);
                // no room -> player is in lobby
                if(peerData.CurrentRoom == null)
               // if (lobbyPlayer.Contains(peer.Id))
                {
                    Console.WriteLine("Player was in lobby, notify other player");
                    lobbyPlayers.Remove(peer);
              
                    foreach (var lobbyPlayer in lobbyPlayers)
                    {
                        SendPlayerLeftEvent(lobbyPlayer, peer.Id);
                    }
                }
                else if(peerData.CurrentRoom.Id == peer.Id)
                //else if (allRooms.ContainsKey(peer.Id))
                {
                    Room playerRoom = peerData.CurrentRoom;
                    Console.WriteLine("Player was creator of room {0}, notify other player and remove room", playerRoom.Id);
                    playerRoom.RemovePlayer(peer);
                    foreach (var player in playerRoom.player)
                    {
                        //var player = allPlayer[playerId];
                        SendCreatorLeftRoom(player, peer.Id);
                        player.Disconnect();
                   //     allPlayer.Remove(playerId);
                    }
                    allRooms.Remove(peer.Id);
                   // playerToRoomMap.Remove(peer.Id);
                    
                }
                else //if (playerToRoomMap.ContainsKey(peer.Id))
                {
                    Room playerRoom = peerData.CurrentRoom;// allRooms[playerToRoomMap[peer.Id]];
                    Console.WriteLine("Player was in room {0}, notify other player", playerRoom.Id);
                    playerRoom.RemovePlayer(peer);
                    foreach (var player in playerRoom.player)
                    {
                        SendPlayerLeftEvent(player, peer.Id);
                    }
                }


            };

            netListener.NetworkReceiveEvent += (peer, reader, deliveryMethod) =>
            {

                RequestType requestType = (RequestType)reader.GetByte();
                Console.WriteLine("Got Request {0} from player {1}:{2}!", requestType, peer.Id, ((PeerData)peer.Tag).Name);
                switch (requestType)
                {
                    case RequestType.RoomUpdate:
                        break;
                    case RequestType.JoinRoom:
                        var roomId = reader.GetInt();
                        Room room;
                        allRooms.TryGetValue(roomId, out room);
                        JoinRoom(peer, room);
                        break;
                    case RequestType.CreateRoom:
                        CreateRoom(peer);
                        break;
                    case RequestType.PlayerUpdateName:
                        var name = reader.GetString();
                        UpdateName(peer, name);
                        break;
                    default:
                        Console.WriteLine("Doing nothing...");
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

        private void CreateRoom(NetPeer peer)
        {
            var peerData = (PeerData)peer.Tag;
            var writer = new NetDataWriter();
            writer.Put((byte)RequestType.CreateRoom);
            if (peerData.CurrentRoom != null)
            {
                Console.WriteLine("Cannot create Room, because Player already joined room {0}", peerData.CurrentRoom.Id);
                writer.Put(false);
                peer.Send(writer, DeliveryMethod.ReliableOrdered);
                return;
            }

            var room = new Room(peer);
            allRooms.Add(room.Id, room);
            Console.WriteLine("Room with id {0} created!", room.Id);
            writer.Put(true);
            peer.Send(writer, DeliveryMethod.ReliableOrdered);
            peerData.CurrentRoom = room;   
            lobbyPlayers.Remove(peer);
            foreach (var player in lobbyPlayers)
            {
                SendPlayerLeftEvent(player, peer.Id);
            }

        }

        private void JoinRoom(NetPeer peer, Room room)
        {
            var peerData = (PeerData)peer.Tag;
            var writer = new NetDataWriter();
            writer.Put((byte)RequestType.JoinRoom);
            if (peerData.CurrentRoom != null)
            {
                Console.WriteLine("Cannot join Room, because Player already joined room {0}", peerData.CurrentRoom.Id);
                writer.Put(false);
                peer.Send(writer, DeliveryMethod.ReliableOrdered);
                return;
            }
 

            
       
            if (room == null)
            {
                Console.WriteLine("Cannot join Room, beacause Room doesn't exist!");
                writer.Put(false);
                peer.Send(writer, DeliveryMethod.ReliableOrdered);
                return;
            }
            else
            {
                Console.WriteLine("Sending room information for room with id {0}!", room.Id);
                writer.Put(true);
                writer.Put(room.player.Count);
                foreach (var roomPlayer in room.player)
                {
                    writer.Put(roomPlayer.Id);
                    writer.Put(((PeerData)roomPlayer.Tag).Name);
                }
                peer.Send(writer, DeliveryMethod.ReliableOrdered);
                foreach (var player in room.player)
                {
                    SendPlayerJoinedRoomEvent(player, peer);
                }

                peerData.CurrentRoom = room;
                lobbyPlayers.Remove(peer);
                foreach (var player in lobbyPlayers)
                {
                    SendPlayerLeftEvent(player, peer.Id);
                }

                return;
            }
            
        }



        private void UpdateName(NetPeer peer, string name)
        {
            var peerData = ((PeerData)peer.Tag);
            Console.WriteLine("Player {0}:{1} changed name to {2}", peer.Id, peerData.Name, name);
            peerData.Name = name;
            if(peerData.CurrentRoom == null)
            {
                Console.WriteLine("Player is in lobby, notify other player");
                foreach (var player in lobbyPlayers)
                {
                    if(player == peer)
                    {
                        continue;
                    }
                    SendUpdateNameEvent(player, peer);
                }
            }
            else
            {
                var room = peerData.CurrentRoom;
                Console.WriteLine("Player is in room {0}, notify other player", room.Id);
                foreach(var player in room.player)
                {
                    if (player == peer)
                    {
                        continue;
                    }
                    SendUpdateNameEvent(player, peer);
                }
            }
        }

        private void SendPlayerJoinedRoomEvent(NetPeer player, NetPeer peer)
        {
            var writer = new NetDataWriter();
            var type = (byte)RequestType.PlayerRoomJoined;
            writer.Put(type);
            writer.Put(peer.Id);
            writer.Put(((PeerData)peer.Tag).Name);
            player.Send(writer, DeliveryMethod.ReliableOrdered);
        }

        private void SendUpdateNameEvent(NetPeer player, NetPeer peer)
        {
            var writer = new NetDataWriter();
            var type = (byte)RequestType.PlayerUpdateName;
            var playerId = peer.Id;
            var newName = ((PeerData)peer.Tag).Name;
            writer.Put(type);
            writer.Put(playerId);
            writer.Put(newName);
            player.Send(writer, DeliveryMethod.ReliableOrdered);
        }

        private void SendPlayerLeftEvent(NetPeer netPeer, int id)
        {
            var writer = new NetDataWriter();
            var type = (byte)RequestType.PlayerLeft;
            writer.Put(type);
            writer.Put(id);
            netPeer.Send(writer, DeliveryMethod.ReliableOrdered);
        }

        private void SendLobbyInformation(NetPeer peer)
        {
            var writer = new NetDataWriter();
            var type = (byte)RequestType.LobbyInformation;
            writer.Put(type);
            writer.Put(lobbyPlayers.Count);
            foreach (var lobbyPlayer in lobbyPlayers)
            {
                writer.Put(lobbyPlayer.Id);
                writer.Put(((PeerData)lobbyPlayer.Tag).Name);
            }
            peer.Send(writer, DeliveryMethod.ReliableOrdered);
        }

        private void SendCreatorLeftRoom(NetPeer netPeer, int id)
        {
          //  Console.WriteLine("TODO: SendCreatorLeftRoom to {0}", netPeer.Id);
        }

        //private void SendPlayerLeftLobbyEvent(NetPeer netPeer, int id)
        //{
        //    var writer = new NetDataWriter();
        //    var type = (byte)RequestType.PlayerLeft;
        //    writer.Put(type);
        //    writer.Put(id);
        //    netPeer.Send(writer, DeliveryMethod.ReliableOrdered);
        //}

        private void SendPlayerJoinedLobbyEvent(NetPeer netPeer, int id)
        {
            var writer = new NetDataWriter();
            var type = (byte)RequestType.PlayerLobbyJoined;
            writer.Put(type);
            writer.Put(id);
            netPeer.Send(writer, DeliveryMethod.ReliableOrdered);
        }
    }
}
