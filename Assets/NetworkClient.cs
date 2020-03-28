using UnityEngine;
using Unity.Collections;
using Unity.Networking.Transport;
using NetworkMessages;
using System;
using System.Text;
using System.Collections.Generic;

public class NetworkClient : MonoBehaviour
{
    public NetworkDriver m_Driver;
    public NetworkConnection m_Connection;
    public string serverIP;
    public ushort serverPort;
    public GameObject playerPrefab;

    private string ownID;
    private Dictionary<String, GameObject> localPlayerList;
    
    void Start ()
    {
        m_Driver = NetworkDriver.Create();
        m_Connection = default(NetworkConnection);
        var endpoint = NetworkEndPoint.Parse(serverIP,serverPort);
        m_Connection = m_Driver.Connect(endpoint);
        localPlayerList = new Dictionary<string, GameObject>();

        InvokeRepeating("HeartBeat", 1f, 1f);
    }
    
    void SendToServer(string message){
        var writer = m_Driver.BeginSend(m_Connection);
        NativeArray<byte> bytes = new NativeArray<byte>(Encoding.ASCII.GetBytes(message),Allocator.Temp);
        writer.WriteBytes(bytes);
        m_Driver.EndSend(writer);
    }

    void OnConnect(){
        Debug.Log("We are now connected to the server");

        //// Example to send a handshake message:
        // HandshakeMsg m = new HandshakeMsg();
        // m.player.id = m_Connection.InternalId.ToString();
        // SendToServer(JsonUtility.ToJson(m));
    }

    void OnData(DataStreamReader stream){
        NativeArray<byte> bytes = new NativeArray<byte>(stream.Length,Allocator.Temp);
        stream.ReadBytes(bytes);
        string recMsg = Encoding.ASCII.GetString(bytes.ToArray());
        NetworkHeader header = JsonUtility.FromJson<NetworkHeader>(recMsg);

        switch(header.cmd){
            case Commands.HANDSHAKE:
                HandshakeMsg hsMsg = JsonUtility.FromJson<HandshakeMsg>(recMsg);
                ownID = hsMsg.player.id;
                Debug.Log("Handshake message received!");
                Debug.Log("Client ID is " + ownID);
                break;
            case Commands.PLAYER_UPDATE:
                PlayerUpdateMsg puMsg = JsonUtility.FromJson<PlayerUpdateMsg>(recMsg);
                Debug.Log("Player update message received!");
            break;
            case Commands.SERVER_UPDATE:
                ServerUpdateMsg suMsg = JsonUtility.FromJson<ServerUpdateMsg>(recMsg);
                foreach(var it in suMsg.players)
                {
                    if(localPlayerList.ContainsKey(it.id))
                    {
                        localPlayerList[it.id].transform.position = it.pos;
                    }
                }
                //Debug.Log("Server update message received!");
            break;
            case Commands.PLAYER_ADD:
                PlayerAddMsg aMsg = JsonUtility.FromJson<PlayerAddMsg>(recMsg);
                SpawnPlayer(aMsg.player.id, aMsg.player.cubeColor);
                Debug.Log("Add New Player, ID: " + aMsg.player.id);
            break;
            case Commands.LIST_UPDATE:
                ListUpdateMsg lMsg = JsonUtility.FromJson<ListUpdateMsg>(recMsg);
                foreach (var it in lMsg.players)
                {
                    SpawnPlayer(it.id, it.cubeColor);
                    Debug.Log("Spawn other player, ID: " + it.id);
                }
            break;
            case Commands.PLAYER_DROP:
                PlayerDropMsg dropMsg = JsonUtility.FromJson<PlayerDropMsg>(recMsg);
                foreach (var it in dropMsg.dplayers)
                {
                    if(localPlayerList.ContainsKey(it.id))
                    {
                        GameObject temp = localPlayerList[it.id];
                        localPlayerList.Remove(it.id);
                        Destroy(temp);
                    }
                }
            break;
            default:
                Debug.Log("Unrecognized message received!");
            break;
        }
    }

    void Disconnect(){
        m_Connection.Disconnect(m_Driver);
        m_Connection = default(NetworkConnection);
    }

    void OnDisconnect(){
        Debug.Log("Client got disconnected from server");
        m_Connection = default(NetworkConnection);
    }

    public void OnDestroy()
    {
        m_Driver.Dispose();
    }   
    void Update()
    {
        m_Driver.ScheduleUpdate().Complete();

        if (!m_Connection.IsCreated)
        {
            return;
        }

        DataStreamReader stream;
        NetworkEvent.Type cmd;
        cmd = m_Connection.PopEvent(m_Driver, out stream);
        while (cmd != NetworkEvent.Type.Empty)
        {
            if (cmd == NetworkEvent.Type.Connect)
            {
                OnConnect();
            }
            else if (cmd == NetworkEvent.Type.Data)
            {
                OnData(stream);
            }
            else if (cmd == NetworkEvent.Type.Disconnect)
            {
                OnDisconnect();
            }

            cmd = m_Connection.PopEvent(m_Driver, out stream);
        }
    }

    void SpawnPlayer(string id, Color color)
    {
        if (localPlayerList.ContainsKey(id))
            return;
        GameObject temp = Instantiate(playerPrefab, new Vector3(0, 0, 0), Quaternion.identity);
        temp.GetComponent<Renderer>().material.color = color;
        if(id == ownID)
        {
            temp.AddComponent<PlayerController>();
            temp.GetComponent<PlayerController>().networkClient = this;
        }
        localPlayerList.Add(id, temp);
    }

    public void SendPosition(Vector3 pos)
    {
        //Debug.Log("Update Player Position: " + pos);
        PlayerUpdateMsg m = new PlayerUpdateMsg();
        m.player.pos = pos;
        SendToServer(JsonUtility.ToJson(m));
    }

    void HeartBeat()
    {
        Heartbeat hb = new Heartbeat();
        hb.player.id = ownID;
        SendToServer(JsonUtility.ToJson(hb));
    }
}