using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using UnityEngine.UI;

public class NetworkedServer : MonoBehaviour
{
    int maxConnections = 1000;
    int reliableChannelID;
    int unreliableChannelID;
    int hostID;
    int socketPort = 5491;

  
    LinkedList<PlayerAccount> playerAccounts;
    // Start is called before the first frame update

    int playerWaitingForMatchWithID = -1;
    LinkedList<GameRoom> gameRooms;
    void Start()
    {
        NetworkTransport.Init();
        ConnectionConfig config = new ConnectionConfig();
        reliableChannelID = config.AddChannel(QosType.Reliable);
        unreliableChannelID = config.AddChannel(QosType.Unreliable);
        HostTopology topology = new HostTopology(config, maxConnections);
        hostID = NetworkTransport.AddHost(topology, socketPort, null);
     
        playerAccounts = new LinkedList<PlayerAccount>();
        gameRooms = new LinkedList<GameRoom>();

    }

    // Update is called once per frame
    void Update()
    {

        int recHostID;
        int recConnectionID;
        int recChannelID;
        byte[] recBuffer = new byte[1024];
        int bufferSize = 1024;
        int dataSize;
        byte error = 0;

        NetworkEventType recNetworkEvent = NetworkTransport.Receive(out recHostID, out recConnectionID, out recChannelID, recBuffer, bufferSize, out dataSize, out error);
        
        switch (recNetworkEvent)
        {
            case NetworkEventType.Nothing:
                break;
            case NetworkEventType.ConnectEvent:
                Debug.Log("Connection, " + recConnectionID);
                break;
            case NetworkEventType.DataEvent:
                string msg = Encoding.Unicode.GetString(recBuffer, 0, dataSize);
                ProcessRecievedMsg(msg, recConnectionID);
                break;
            case NetworkEventType.DisconnectEvent:
                Debug.Log("Disconnection, " + recConnectionID);
                break;
        }

    }

    public void SendMessageToClient(string msg, int id)
    {
        byte error = 0;
        byte[] buffer = Encoding.Unicode.GetBytes(msg);
        NetworkTransport.Send(hostID, id, reliableChannelID, buffer, msg.Length * sizeof(char), out error);
    }

    private void ProcessRecievedMsg(string msg, int id)
    {
        Debug.Log("msg recieved = " + msg + ".  connection id = " + id);
        string[] csv = msg.Split(',');
        int signifier = int.Parse(csv[0]);
        if (signifier == ClientToServerSignifiers.CreateAccount)
        {

            bool errorFound = false;
            foreach (PlayerAccount pa in playerAccounts)
            {
                if (pa.name == csv[1])
                {
                    SendMessageToClient(ServerToCientSignifiers.CreateAccountFail + "", id);
                    errorFound = true;
                    


                }
            }
            if (!errorFound)
            {
                SendMessageToClient(ServerToCientSignifiers.CreateAccountSuccess + "", id);
                playerAccounts.AddLast(new PlayerAccount(csv[1], csv[2]));
            }
        }
        else if (signifier == ClientToServerSignifiers.logInAccount)
        {

            bool userNameFound = false;

            foreach (PlayerAccount pa in playerAccounts)
            {
                if (pa.name == csv[1])
                {
                    userNameFound = true;
                    if (pa.password == csv[2])
                    {
                        SendMessageToClient(ServerToCientSignifiers.logInSuccess + "", id);

                    }
                    else
                    {
                        SendMessageToClient(ServerToCientSignifiers.logInFail + "", id);

                    }
                }
            }

            if (!userNameFound)
            {
                Debug.Log("user fail");
                SendMessageToClient(ServerToCientSignifiers.logInFail + "", id);
            }
        }
      

        else if (signifier == ClientToServerSignifiers.JoinGameRoomQueue)
        {
            if (playerWaitingForMatchWithID == -1)
                playerWaitingForMatchWithID = id;
            else
            {
                GameRoom gr = new GameRoom(playerWaitingForMatchWithID, id);
                gameRooms.AddLast(gr);
                SendMessageToClient(ServerToCientSignifiers.chatStart+ "", gr.playerID1);
                SendMessageToClient(ServerToCientSignifiers.chatStart + "", gr.playerID2);
                playerWaitingForMatchWithID = -1;
            }
        }
        else if (signifier == ClientToServerSignifiers.tictactoe)
        {
            
                GameRoom gr = GetGameRoomWithClientID(id);
                gameRooms.AddLast(gr);
                SendMessageToClient(ServerToCientSignifiers.GameStart + ","+ gr.playerID1, gr.playerID1);
                SendMessageToClient(ServerToCientSignifiers.GameStart + ","+ gr.playerID2, gr.playerID2);
                playerWaitingForMatchWithID = -1;
            }
        else if (signifier == ClientToServerSignifiers.Playing)
        {

            GameRoom gr = GetGameRoomWithClientID(id);
            if (gr != null)
            {
                if (gr.playerID1 == id)
                {
                    Debug.Log("room1");
                    SendMessageToClient(ServerToCientSignifiers.OpponentPlay + "," + gr.playerID2 + "," + csv[1], gr.playerID2);
                    SendMessageToClient(ServerToCientSignifiers.PlayerWait + "," + gr.playerID1 + "," + csv[1], gr.playerID1);

                }
                else
                {
                    Debug.Log("room2");
                    SendMessageToClient(ServerToCientSignifiers.OpponentPlay + "," + gr.playerID1 + "," + csv[1], gr.playerID1);
                    SendMessageToClient(ServerToCientSignifiers.PlayerWait + "," + gr.playerID2 + "," + csv[1], gr.playerID2);
                }
                   
            }
        }


        else if (signifier == ClientToServerSignifiers.chat)
        {
          
        
            GameRoom gr = GetGameRoomWithClientID(id);
            if (gr != null)
            {
               
                if (gr.playerID1 == id)
                {
                    SendMessageToClient(ServerToCientSignifiers.chatReply + "," + csv[1] + "," + gr.playerID1, gr.playerID1);
                  //  if(gr.playerID2 != gr.playerID1)
                    SendMessageToClient(ServerToCientSignifiers.chatReply + "," + csv[1] + "," + gr.playerID1, gr.playerID2);
                  
                }
                else
                {

                    SendMessageToClient(ServerToCientSignifiers.chatReply + "," + csv[1] + "," + gr.playerID2, gr.playerID2);
                  //  if (gr.playerID2 != gr.playerID1)
                        SendMessageToClient(ServerToCientSignifiers.chatReply + "," + csv[1] + "," + gr.playerID2, gr.playerID1);
                }
            }
        }

    }
    private GameRoom GetGameRoomWithClientID(int id)
    {
        foreach(GameRoom gr in gameRooms)
        {
            if (gr.playerID1 == id || gr.playerID2 == id)
                return gr;
        }
        return null;
    }
}

public class PlayerAccount
{
    public string name;
   public  string password;
    public PlayerAccount()
    {

    }
  

    public PlayerAccount(string Name, string Password)
    {
        name = Name;
        password = Password;
    }
}

public class GameRoom
{
   public  int playerID1, playerID2;

    public GameRoom(int playerId1, int playerId2)
    {
        playerID1 = playerId1;
        playerID2 = playerId2;
    }


}

static public class ClientToServerSignifiers
{
    public const int CreateAccount = 1;
   public const int logInAccount = 2;
    public const int JoinGameRoomQueue = 3;
    public const int tictactoe = 4;
    public const int chat = 5;
    public const int Playing = 6;
}
static public class ServerToCientSignifiers
{
    public const int CreateAccountFail = 1;
    public const int logInFail = 2;

    public const int CreateAccountSuccess = 3;
    public const int logInSuccess = 4;
    public const int OpponentPlay = 5;
    public const int GameStart = 6;
    public const int chatReply = 7;
    public const int chatStart = 8;
    public const int PlayerWait = 9;
}