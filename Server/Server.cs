using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using System.IO;
using System.Web;

namespace Server
{
    class Server
    {
        // Status Key
        private const char CHECK_ROOM_LIST = '0';
        private const char REQUEST_ROOM_MSG = '1';
        private const char SEND_MSG = '2';
        private const char DISCONNECT = '3';
        private const char IS_RECEIVE_MSG = '4';
        private const char IS_NOT_RECEIVE_MSG = '5';
        private const char INVALID_MESSAGE = '6';

        private const string ipAddr = "172.21.22.161";  // watching IP
        private const int port = 3000;                  // watching port

        private static Thread threadWatch = null;          // Thread which watches the connection request from client
        private static Socket socketWatch = null;          // Socket which watches the Server

        // Saved all thread that receive message from client
        public static Dictionary<string, Thread> dictThread = new Dictionary<string, Thread>();
        // Saved all sockets for all clients
        public static Dictionary<string, Socket> dictSocket = new Dictionary<string, Socket>();
        // Saved all handler for all clients
        public static Dictionary<string, Handler> dictHandler = new Dictionary<string, Handler>();

        // File locker
        public static Dictionary<string, Object> dictLock = new Dictionary<string, object>();

        private static Object disconnectLock = new Object();

        public Server()
        {
        }

        #region --- Start Server ---
        /// <summary>
        ///     start server
        /// </summary>
        public static void StartServer()
        {
            dictLock.Add("room", new Object());

            // Create a socket, use IPv4, stream connection and TCP
            socketWatch = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // create a endpoint which binds the IP and port 
            IPAddress ipAddress = IPAddress.Parse(ipAddr);
            IPEndPoint endPoint = new IPEndPoint(ipAddress, port);

            try
            {
                // bind the watching socket to the endpoint
                socketWatch.Bind(endPoint);

                // set the length of waiting queue
                socketWatch.Listen(20);

                // Create a watching thread
                threadWatch = new Thread(WatchConnection);
                // Set the background property
                threadWatch.IsBackground = true;
                // Start the thread
                threadWatch.Start();

                InitRoomLock();

                Console.WriteLine();
                Console.WriteLine("                               ---Server Start---");
                Console.WriteLine();

                return;
            }
            catch (SocketException se)
            {
                Console.WriteLine("[SocketError]" + se.Message);
                return;
            }
            catch (Exception e)
            {
                Console.WriteLine("[Error]" + e.Message);
                return;
            }
        }
        #endregion

        #region --- Watch Connection From Client ---
        /// <summary>
        ///     watch connection from client
        /// </summary>
        private static void WatchConnection()
        {
            // keep watching 
            while (true)
            {
                Socket socketConnection = null;
                try
                {
                    // watch the request, if has, create a socket for it
                    socketConnection = socketWatch.Accept();
                    string socketKey = socketConnection.RemoteEndPoint.ToString();
                    
                    // save every socket with the key in the form of IP : port
                    dictSocket.Add(socketKey, socketConnection);

                    Console.WriteLine("User IP : {0} has connected...", socketKey);

                    // Create a thread to watch the data sent from client for every socket
                    // Create the communicate thread
                    Thread threadCommunicate = new Thread(ReceiveMsg);
                    threadCommunicate.IsBackground = true;
                    threadCommunicate.Start(socketConnection);

                    dictThread.Add(socketKey, threadCommunicate);

                    Handler handler = new Handler();
                    dictHandler.Add(socketKey, handler);

                }
                catch (SocketException se)
                {
                    Console.WriteLine("[Error]" + se.Message);
                    return;
                }
                catch (Exception e)
                {
                    Console.WriteLine("[Error]" + e.Message);
                    return;
                }
            }
        }
        #endregion

        #region --- Watch The Data Sent From Client ---
        /// <summary>
        ///     watch the data send from client
        /// </summary>
        private static void ReceiveMsg(object socketClientPara)
        {
            Socket socketClient = socketClientPara as Socket;
            string socketKey = socketClient.RemoteEndPoint.ToString();

            while (true)
            {
                // define a buffer for received message
                byte[] msgReceiver = new byte[1024 * 1024 * 2];

                // length of received message
                int length = -1;

                try
                {
                    length = socketClient.Receive(msgReceiver);
                }
                catch (SocketException se)
                {
                    Console.WriteLine("[ERROR]" + socketKey + " receive error. Message: " + se.Message);

                    dictSocket[socketKey].Close();
                    Thread tmp = dictThread[socketKey];

                    // Remove the error object
                    dictSocket.Remove(socketKey);
                    dictThread.Remove(socketKey);
                    tmp.Abort();

                    return;
                }
                catch (Exception e)
                {
                    Console.WriteLine("[ERROR]" + e.Message);
                    return;
                }

                string msg = Encoding.UTF8.GetString(msgReceiver, 0, length);

                string[] str = msg.Split('<');

                foreach (string s in str)
                {
                    string tmp = WebUtility.HtmlDecode(s);
                    if (tmp.Length > 0)
                    {
                        if (tmp[0] == CHECK_ROOM_LIST)
                            dictHandler[socketKey].CheckRoomList(tmp.Substring(1, tmp.Length - 1));
                        else if (tmp[0] == REQUEST_ROOM_MSG)
                            dictHandler[socketKey].GetRoomMsg(socketKey, tmp.Substring(1, tmp.Length - 1));
                        else if (tmp[0] == SEND_MSG)
                            dictHandler[socketKey].AddMsgToFile(socketKey, tmp.Substring(1, tmp.Length - 1));
                        else if (tmp[0] == DISCONNECT)
                            RemoveOfflineUser(socketKey);
                        else
                            dictHandler[socketKey].InvalidMsg(socketKey);
                    }
                }                
            }
        }
        #endregion

        #region --- Remove Offline User ---
        /// <summary>
        ///     remove off line user
        /// </summary>
        /// <param name="clientIP">the client IP</param>
        public static void RemoveOfflineUser(string clientIP)
        {
            Thread tmp = dictThread[clientIP];
            try
            {
                lock (disconnectLock)
                {
                    Console.WriteLine("User IP : " + clientIP + " has went off line.");

                    if (dictSocket.ContainsKey(clientIP))
                    {
                        dictSocket[clientIP].Close();
                        dictSocket.Remove(clientIP);
                    }

                    if (dictThread.ContainsKey(clientIP))
                    {
                        
                        dictThread.Remove(clientIP);
                        
                    }
                } 
            }
            catch (Exception e)
            {
                Console.WriteLine("[Error]Exception happens during deleting friends! [ExceptionMsg]" + e.Message);
            }
            tmp.Abort();        
        }
        #endregion

        #region --- Init Room Locker ---
        /// <summary>
        ///     Init Room Locker
        /// </summary>
        private static void InitRoomLock()
        {
            try
            {
                lock (Server.dictLock["room"])
                {
                    string roomFile = "room.txt";
                    if (!File.Exists(@roomFile))
                    {
                        FileStream fs = new FileStream(roomFile, FileMode.Create);
                        fs.Close();
                    }

                    StreamReader sr = new StreamReader(roomFile, Encoding.UTF8);

                    String lineMsg;
                    while ((lineMsg = sr.ReadLine()) != null)
                        dictLock.Add(lineMsg.Trim(), new Object());
                    sr.Close();
                } 
            }
            catch (Exception e)
            {
                Console.WriteLine("[Error]Exception happens during initializing the file lock. [ExceptionMsg]" + e.Message);
            }

        }
        #endregion
    }

    class Handler
    {
        // Status Key
        private const char CHECK_ROOM_LIST = '0';
        private const char REQUEST_ROOM_MSG = '1';
        private const char SEND_MSG = '2';
        private const char DISCONNECT = '3';
        private const char IS_RECEIVE_MSG = '4';
        private const char IS_NOT_RECEIVE_MSG = '5';
        private const char INVALID_MESSAGE = '6';

        public Handler() { }

        #region --- Check Room List ---
        /// <summary>
        ///     Check the existence of every room in room list received from client
        /// </summary>
        /// <param name="msg">string of room list</param>
        public void CheckRoomList(string s_roomList)
        {
            // TODO : check the existence of chat file of each room
            List<string> roomList = (List<string>)JsonConvert.DeserializeObject(s_roomList, typeof(List<string>));

            foreach (string room in roomList)
            {
                string roomFile = "DataBase\\"+room + ".txt";
                if (!Server.dictLock.ContainsKey(room))
                {
                    Server.dictLock.Add(room, new Object());

                    try
                    {
                        lock (Server.dictLock[room])
                        {
                            FileStream fs = new FileStream(roomFile, FileMode.Create);
                            fs.Close();
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("[Error]Exception happens during creating new room " + room + " in FUNC[CheckRoomList]. [ExceptionMsg]" + e.Message);
                    }

                    try
                    {
                        lock (Server.dictLock["room"])
                        {
                            string romFile = "room.txt";

                            FileStream f = File.OpenWrite(romFile);

                            f.Position = f.Length;

                            byte[] writeMsg = Encoding.UTF8.GetBytes(room + "\r\n");

                            f.Write(writeMsg, 0, writeMsg.Length);

                            f.Close();
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("[Error]Exception happens during add room " + room + " into roomlist file. [ExceptionMsg]" + e.Message);
                    }
                    
                    
                }
            }
        }
        #endregion

        #region --- Get Room Message ---
        /// <summary>
        ///     get the history message of the specific chat room
        /// </summary>
        /// <param name="clientIP">the client IP</param>
        /// <param name="msg">the room ID</param>
        public void GetRoomMsg(string clientIP, string roomId)
        {
            // TODO : get the history of specific chat room
            string roomFile = "DataBase\\"+roomId + ".txt";

            List<string> msgList = new List<string>();

            MsgHandler msgHandler;

            String sendMsg = "";

            if (Server.dictLock.ContainsKey(roomId))
            {
                try
                {
                    lock (Server.dictLock[roomId])
                    {
                        StreamReader sr = new StreamReader(roomFile, Encoding.UTF8);

                        String lineMsg;
                        while ((lineMsg = sr.ReadLine()) != null)
                        {
                            lineMsg += ("\r\n" + sr.ReadLine());
                            msgList.Add(lineMsg);
                        }


                        sr.Close();
                    }

                    msgHandler = new MsgHandler(roomId, msgList);

                    sendMsg = JsonConvert.SerializeObject(msgHandler);
                }
                catch(Exception e)
                {
                    Console.WriteLine("[Error]Exception happens during getting message from room " + roomId + ". [ExceptionMsg]" + e.Message);
                }
            }
            else
            {
                Server.dictLock.Add(roomId, new Object());

                lock (Server.dictLock[roomId])
                {
                    try
                    {
                        FileStream fs = new FileStream(roomFile, FileMode.Create);
                        fs.Close();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("[Error]Exception happens during creating new room " + roomId + "  in FUNC[GetRoomMsg]. [ExceptionMsg]" + e.Message);
                    }

                    try
                    {
                        lock (Server.dictLock["room"])
                        {
                            string romFile = "room.txt";

                            FileStream f = File.OpenWrite(romFile);

                            f.Position = f.Length;

                            byte[] writeMsg = Encoding.UTF8.GetBytes(roomId + "\r\n");

                            f.Write(writeMsg, 0, writeMsg.Length);

                            f.Close();

                            msgHandler = new MsgHandler(roomId);

                            sendMsg = JsonConvert.SerializeObject(msgHandler);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("[Error]Exception happens during getting message from room " + roomId + ". P.S:It shouldn't be here. [ExceptionMsg]" + e.Message);
                    }
                }
            }

            SendMessage(clientIP, SEND_MSG, sendMsg);          
        }
        #endregion

        #region --- Add Message To File ---
        /// <summary>
        ///     put the message into the specific room
        /// </summary>
        /// <param name="clientIP">the client IP</param>
        /// <param name="msg">the string include the room id and received message</param>
        public void AddMsgToFile(string clientIP, string msg)
        {
            // TODO : put the message into the specific room
            MsgHandler msgHandler = (MsgHandler)JsonConvert.DeserializeObject(msg, typeof(MsgHandler));

            string roomFile = "DataBase\\" + msgHandler.roomId + ".txt";

            

            if (Server.dictLock.ContainsKey(msgHandler.roomId))
            {
                try
                {
                    lock (Server.dictLock[msgHandler.roomId])
                    {
                        FileStream fs = File.OpenWrite(roomFile);

                        fs.Position = fs.Length;

                        foreach (string message in msgHandler.msgList)
                        {
                            byte[] writeMsg = Encoding.UTF8.GetBytes(message + "\r\n");
                            fs.Write(writeMsg, 0, writeMsg.Length);
                        }

                        fs.Close();
                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine("[Error]Exception happens during writing message into room " + msgHandler.roomId + ". [ExceptionMsg]" + e.Message);
                }
                
            }
            else
            {
                Server.dictLock.Add(msgHandler.roomId, new Object());
                
                try
                {
                    lock (Server.dictLock[msgHandler.roomId])
                    {
                        FileStream fs = new FileStream(roomFile, FileMode.Create);
                        fs.Close();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("[Error]Exception happens during creating new room " + msgHandler.roomId + "  in FUNC[AddMsgToFile]. [ExceptionMsg]" + e.Message);
                }
                try
                {
                    lock (Server.dictLock["room"])
                    {
                        string romFile = "room.txt";

                        FileStream f = File.OpenWrite(romFile);

                        f.Position = f.Length;

                        byte[] writeMsg = Encoding.UTF8.GetBytes(msgHandler.roomId + "\r\n");

                        f.Write(writeMsg, 0, writeMsg.Length);

                        f.Close();
                    }  
                }
                catch (Exception e)
                {
                    Console.WriteLine("[Error]Exception happens during writing message into room " + msgHandler.roomId + " in FUNC[AddMsgToFile]. [ExceptionMsg]" + e.Message);
                } 
                
            }

            GetRoomMsg(clientIP, msgHandler.roomId);
            

                     
        }
        #endregion

        #region --- Invalid Message ---
        /// <summary>
        ///     Handle the situation of invalid message
        /// </summary>
        /// <param name="clientIP">the client ip</param>
        public void InvalidMsg(string clientIP)
        {
            // TODO : send invalid warning to client
            SendMessage(clientIP, INVALID_MESSAGE, "");
        }
        #endregion

        #region --- Send Message ---
        /// <summary>
        ///     send message to specific client
        /// </summary>
        /// <param name="clientIP">the client IP</param>
        /// <param name="flag">the message type</param>
        /// <param name="msg">message</param>
        public void SendMessage(string clientIP, char flag, string msg)
        {
            try
            {
                
                msg = flag + msg;

                msg = WebUtility.HtmlEncode(msg);
                msg += '<';

                byte[] arrMsg = Encoding.UTF8.GetBytes(msg);

                Server.dictSocket[clientIP].Send(arrMsg);
            }
            catch (SocketException se)
            {
                Console.WriteLine("[SocketError] send message error : {0}", se.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("[Error] send message error : {0}", e.Message);
            }             
        }
        #endregion
    }

    public struct MsgHandler
    {
        public string roomId;
        public List<string> msgList;

        public MsgHandler(string r)
        {
            roomId = r;
            msgList = new List<string>();
        }

        public MsgHandler(string r, List<string> m)
        {
            roomId = r; msgList = m;
        }
    }
}
