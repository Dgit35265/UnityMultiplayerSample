using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace NetworkMessages
{
    public enum Commands{
        PLAYER_UPDATE,
        SERVER_UPDATE,
        HANDSHAKE,
        LIST_UPDATE,
        PLAYER_ADD,
        PLAYER_DROP,
        HEARTBEAT
    }
    [System.Serializable]
    public class NetworkHeader{
        public Commands cmd;
    }
    [System.Serializable]
    public class HandshakeMsg:NetworkHeader{
        public NetworkObjects.NetworkPlayer player;
        public HandshakeMsg(){      // Constructor
            cmd = Commands.HANDSHAKE;
            player = new NetworkObjects.NetworkPlayer();
        }
    }
    [System.Serializable]
    public class PlayerUpdateMsg:NetworkHeader{
        public NetworkObjects.NetworkPlayer player;
        public PlayerUpdateMsg(){      // Constructor
            cmd = Commands.PLAYER_UPDATE;
            player = new NetworkObjects.NetworkPlayer();
        }
    };
    [System.Serializable]
    public class  ServerUpdateMsg:NetworkHeader{
        public List<NetworkObjects.NetworkPlayer> players;
        public ServerUpdateMsg(){      // Constructor
            cmd = Commands.SERVER_UPDATE;
            players = new List<NetworkObjects.NetworkPlayer>();
        }
    }
    public class ListUpdateMsg : NetworkHeader
    {
        public List<NetworkObjects.NetworkPlayer> players;
        public ListUpdateMsg()
        {      // Constructor
            cmd = Commands.LIST_UPDATE;
            players = new List<NetworkObjects.NetworkPlayer>();
        }
    }
    public class PlayerAddMsg:NetworkHeader
    {
        public NetworkObjects.NetworkPlayer player;
        public PlayerAddMsg()
        {      // Constructor
            cmd = Commands.PLAYER_ADD;
            player = new NetworkObjects.NetworkPlayer();
        }
    }
    public class PlayerDropMsg : NetworkHeader
    {
        public List<NetworkObjects.NetworkPlayer> dplayers;
        public PlayerDropMsg()
        {
            cmd = Commands.PLAYER_DROP;
            dplayers = new List<NetworkObjects.NetworkPlayer>();
        }
    }
    public class Heartbeat: NetworkHeader
    {
        public NetworkObjects.NetworkPlayer player;
        public Heartbeat()
        {      // Constructor
            cmd = Commands.HEARTBEAT;
            player = new NetworkObjects.NetworkPlayer();
        }
    }
} 

namespace NetworkObjects
{
    [System.Serializable]
    public class NetworkObject{
        public string id;
    }
    [System.Serializable]
    public class NetworkPlayer : NetworkObject{
        public Color cubeColor;
        public Vector3 pos;
        public float lastHB;

        public NetworkPlayer(){
            cubeColor = new Color();
        }
    }
}
