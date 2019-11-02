using UnityEngine;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

public struct EndPoint_Connection {
    //クライアントのIP
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
    public string connectionIP;
    //クライアントのID
    public int ID;
}

public struct PositionSync {
    public Vector3 position;
    public int ID;
}

public class UDP_Client : MonoBehaviour {
    List<PositionSync> positionQue = new List<PositionSync>();

    List<GameObject> netObj = new List<GameObject>();

    int ID;

    int tickRate = 1;

    EndPoint_Connection m_connection;
    private const int firstContact_port = 50764;
    private Socket firstConnection_socket = null;

    public PositionSync positionSync;
    public Socket positionSync_socket;
    private const int positionSync_port = 50766;

    public Socket receive_socket;
    private const int receive_port = 50767;


    // 接続先のIPアドレス.
    private string address = "";

    // 接続先のポート番号.
    private const int m_port = 50765;

    // 通信用変数
    private Socket socket = null;

    // 状態.
    State state;

    // 状態定義
    private enum State {
        SelectHost = 0,
        CreateListener,
        ReceiveMessage,
        CloseListener,
        SendMessage,
        SendPosition,
        Endcommunication,
    }

    GameObject obj;


    // Use this for initialization
    void Start() {
        ID = GameObject.Find("StartClient_Button").GetComponent<IP_Input>().id;
        address = GameObject.Find("StartClient_Button").GetComponent<IP_Input>().ip;

        state = State.SelectHost;

        IPHostEntry hostEntry = Dns.GetHostEntry(Dns.GetHostName());
        System.Net.IPAddress hostAddress = hostEntry.AddressList[0];
        //address = hostAddress.ToString();

        m_connection = new EndPoint_Connection();
        m_connection.connectionIP = hostAddress.ToString();
        m_connection.ID = ID;

        RequestConnection();

        positionSync.ID = ID;

        obj = GameObject.Find("Player");

        receive_socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        receive_socket.Bind(new IPEndPoint(IPAddress.Any, receive_port));

        state = State.SendPosition;

        var thread = new Thread(Loop);
        thread.Start();
    }

    int cnt = 0;

    private void FixedUpdate() {
        if (cnt > tickRate) {
            SendPosition(obj.transform.position);
            cnt = 0;
        }
        ReceivePosition();
        PositionSync();
        cnt++;
    }

    // Update is called once per frame
    void Update() {
    }

    void Loop() {
        while (true) {
            ReceivePosition();
        }
    }

    void SendPosition(Vector3 position) {


        positionSync.position = position;
        // サーバへ接続.
        // ソケットを生成します.
        positionSync_socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        
        // メッセージ送信.
        byte[] buffer = new byte[1400];
        IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse(address), positionSync_port);

        int size = Marshal.SizeOf(positionSync);

        byte[] bytes = new byte[size];

        GCHandle gchw = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        Marshal.StructureToPtr(positionSync, gchw.AddrOfPinnedObject(), false);

        positionSync_socket.SendTo(bytes, bytes.Length, SocketFlags.None, endpoint);

        gchw.Free();

        state = State.ReceiveMessage;
    }

    void RequestConnection() {
        Debug.Log("[UDP]First communication.");
        // サーバへ接続.
        // ソケットを生成します.
        firstConnection_socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        // 使用するポート番号を割り当てます.
        //firstConnection_socket.Bind(new IPEndPoint(IPAddress.Any, firstContact_port));
        // メッセージ送信.
        byte[] buffer = new byte[1400];
        IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse(address), firstContact_port);

        int size = Marshal.SizeOf(m_connection);

        byte[] bytes = new byte[size];

        GCHandle gchw = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        Marshal.StructureToPtr(m_connection, gchw.AddrOfPinnedObject(), false);

        firstConnection_socket.SendTo(bytes, bytes.Length, SocketFlags.None, endpoint);

        gchw.Free();

        Debug.Log("Send add request to server(" + address + ")");
    }

    void ReceivePosition() {
        byte[] buffer = new byte[1400];
        IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
        EndPoint senderRemote = (EndPoint)sender;

            if (receive_socket.Poll(0, SelectMode.SelectRead)) {
            int recvSize = receive_socket.ReceiveFrom(buffer, SocketFlags.None, ref senderRemote);
            if (recvSize > 0) {
                GCHandle gch = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                PositionSync temp = (PositionSync)Marshal.PtrToStructure(gch.AddrOfPinnedObject(), typeof(PositionSync));
                if (temp.ID != ID) {
                    positionQue.Add(temp);
                }
                gch.Free();
            }
        }

        state = State.SendPosition;
    }

    void PositionSync() {

        if(positionQue.Count == 0) {
            return;
        }

        PositionSync temp = positionQue[0];

        int i = 0;
        bool t = false;
        while (i < netObj.Count) {
            if (netObj[i].name.Equals(temp.ID.ToString())) {
                if (ID != temp.ID) {
                    netObj[i].transform.position = temp.position;
                }

                t = true;
                break;
            }
            i++;
        }


        if (t == false && ID.Equals(temp.ID) == false) {
            Debug.Log("Receive new Player");
            GameObject tempObj = Instantiate<GameObject>(Resources.Load<GameObject>("NetObj"));
            tempObj.transform.position = temp.position;
            tempObj.name = temp.ID.ToString();
            netObj.Add(tempObj);
        }

        positionQue.RemoveAt(0);
    }

    // 待ち受け終了.
    public void CloseListener() {
        // 待ち受けを終了します.
        if (socket != null) {
            firstConnection_socket.Close();
            positionSync_socket.Close();
            receive_socket.Close();
            firstConnection_socket = null;
            positionSync_socket = null;
            receive_socket = null;
        }

        state = State.Endcommunication;

        Debug.Log("[UDP]End communication.");
    }
}
