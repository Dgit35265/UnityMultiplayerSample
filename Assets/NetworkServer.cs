using UnityEngine;
using UnityEngine.Assertions;
using Unity.Collections;
using Unity.Networking.Transport;
using NetworkMessages;
using System;
using System.Text;
using Random = UnityEngine.Random;
using System.Collections.Generic;

public class NetworkServer : MonoBehaviour
{
    public NetworkDriver m_Driver;
    public ushort serverPort;
    private NativeList<NetworkConnection> m_Connections;

    private List<NetworkObjects.NetworkPlayer> playerList;

    void Start ()
    {
        m_Driver = NetworkDriver.Create();
        var endpoint = NetworkEndPoint.AnyIpv4;
        endpoint.Port = serverPort;
        if (m_Driver.Bind(endpoint) != 0)
            Debug.Log("Failed to bind to port " + serverPort);
        else
            m_Driver.Listen();

        m_Connections = new NativeList<NetworkConnection>(16, Allocator.Persistent);
        playerList = new List<NetworkObjects.NetworkPlayer>();

        InvokeRepeating("UpdatePosition", 1f, 0.33f);
    }

    void UpdatePosition()
    {
        ServerUpdateMsg m = new ServerUpdateMsg();
        foreach (var it in playerList)
        {
            m.players.Add(it);
        }
        foreach(var c in m_Connections)
        {
            SendToClient(JsonUtility.ToJson(m), c); // update position for every client
        }
    }

    void SendToClient(string message, NetworkConnection c){
        var writer = m_Driver.BeginSend(NetworkPipeline.Null, c);
        NativeArray<byte> bytes = new NativeArray<byte>(Encoding.ASCII.GetBytes(message),Allocator.Temp);
        writer.WriteBytes(bytes);
        m_Driver.EndSend(writer);
    }
    public void OnDestroy()
    {
        m_Driver.Dispose();
        m_Connections.Dispose();
    }

    void OnConnect(NetworkConnection c){
        Debug.Log("Accepted a connection");
        HandshakeMsg m = new HandshakeMsg();
        m.player.id = c.InternalId.ToString();
        m.player.cubeColor = new Color(Random.Range(0, 1f), Random.Range(0, 1f), Random.Range(0, 1f));
        SendToClient(JsonUtility.ToJson(m),c);

        ListUpdateMsg lm = new ListUpdateMsg();
        NetworkObjects.NetworkPlayer newPlayer = m.player;
        newPlayer.lastHB = 0f;
        playerList.Add(newPlayer); //
        lm.players = playerList;
        for (int i = 0; i < m_Connections.Length; ++i)
        {
            PlayerAddMsg am = new PlayerAddMsg();
            am.player = m.player;
            SendToClient(JsonUtility.ToJson(am), m_Connections[i]);
        }
        SendToClient(JsonUtility.ToJson(lm), c);
        m_Connections.Add(c);
        Debug.Log("Connect Init Finished");
    }

    void OnData(DataStreamReader stream, int i){
        NativeArray<byte> bytes = new NativeArray<byte>(stream.Length,Allocator.Temp);
        stream.ReadBytes(bytes);
        string recMsg = Encoding.ASCII.GetString(bytes.ToArray());
        NetworkHeader header = JsonUtility.FromJson<NetworkHeader>(recMsg);

        int index = MatchPlayerWithId(m_Connections[i].InternalId.ToString());

        switch (header.cmd) {
            case Commands.HANDSHAKE:
                HandshakeMsg hsMsg = JsonUtility.FromJson<HandshakeMsg>(recMsg);
                Debug.Log("Handshake message received!");
                break;
            case Commands.PLAYER_UPDATE:
                PlayerUpdateMsg puMsg = JsonUtility.FromJson<PlayerUpdateMsg>(recMsg);
                Debug.Log("Player - " + i +  puMsg.player.pos);
                playerList[index].pos = puMsg.player.pos;
                
                //Debug.Log("Player update message received!");
            break;
            case Commands.HEARTBEAT:
                Heartbeat hbM = JsonUtility.FromJson<Heartbeat>(recMsg);
                if(playerList[index].id == hbM.player.id)
                    playerList[index].lastHB = 0;
            break;
            default:
                Debug.Log("Unrecognized message received!");
            break;
        }
    }

    int MatchConnectionWithId(string id)
    {
        for (int i = 0; i < m_Connections.Length; i++)
        {
            if (m_Connections[i].InternalId.ToString() == id)
                return i;
        }
        return -1;
    }
    int MatchPlayerWithId(string id)
    {
        for (int i = 0; i < m_Connections.Length; i++)
        {
            if (playerList[i].id == id)
                return i;
        }
        return -1;
    }

    void OnDisconnect(int i)
    {
        Debug.Log("Client disconnected from server");
        m_Connections[i] = default(NetworkConnection);
    }

    void Update ()
    {
        m_Driver.ScheduleUpdate().Complete();


        PlayerDropMsg m = new PlayerDropMsg();
        for (int i = 0; i < playerList.Count; ++i)
        {
            playerList[i].lastHB += Time.deltaTime;
            if (playerList[i].lastHB > 5f)
            {
                int index = MatchConnectionWithId(playerList[i].id);
                Debug.Log("Dropped Player " + playerList[i].id);
                if (index >= 0)
                    m_Connections[i] = default(NetworkConnection);

                m.dplayers.Add(playerList[i]);
                playerList.RemoveAt(i);
                --i;
            }
        }

        // CleanUpConnections
        for (int i = 0; i < m_Connections.Length; i++)
        {
            if (!m_Connections[i].IsCreated)
            {
                m_Connections.RemoveAtSwapBack(i);
                Debug.Log("Remove Connection");
                --i;
            }
        }

        //recalculate m_connnections
        if(m.dplayers.Count > 0)
        {
            for(int i = 0; i < m_Connections.Length; ++i)
            {
                SendToClient(JsonUtility.ToJson(m), m_Connections[i]);
            }
        }

        // AcceptNewConnections
        NetworkConnection c = m_Driver.Accept();
        while (c  != default(NetworkConnection))
        {            
            OnConnect(c);

            // Check if there is another new connection
            c = m_Driver.Accept();
        }

        DataStreamReader stream;
        for (int i = 0; i < m_Connections.Length; i++)
        {
            Assert.IsTrue(m_Connections[i].IsCreated);
            
            NetworkEvent.Type cmd;
            cmd = m_Driver.PopEventForConnection(m_Connections[i], out stream);
            while (cmd != NetworkEvent.Type.Empty)
            {
                if (cmd == NetworkEvent.Type.Data)
                {
                    OnData(stream, i);
                }
                else if (cmd == NetworkEvent.Type.Disconnect)
                {
                    OnDisconnect(i);
                }

                cmd = m_Driver.PopEventForConnection(m_Connections[i], out stream);
            }
        }
    }
}