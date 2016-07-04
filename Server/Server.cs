using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using System.IO;

namespace Server
{
    class Server
    {
        // Status Key
        private const byte CHECK_ROOM_LIST = 0;
        private const byte REQUEST_ROOM_MSG = 1;
        private const byte SEND_MSG = 2;
        private const byte DISCONNECT = 3;
        private const byte IS_RECEIVE_MSG = 4;
        private const byte IS_NOT_RECEIVE_MSG = 5;
        private const byte INVALID_MESSAGE = 6;

        private const string ipAddr = "127.0.0.1";  // watching IP
        private const int port = 3000;              // watching port

        private Thread threadWatch = null;          // Thread which watches the connection request from client
        private Socket socketWatch = null;          // Socket which watches the Server

        // Saved all thread that receive message from client
        private Dictionary<string, Thread> dictThread = new Dictionary<string, Thread>();
        // Saved all sockets for all clients
        private Dictionary<string, Socket> dictSocket = new Dictionary<string, Socket>();


        public Server()
        {
        }

        #region --- Start Server ---
        /// <summary>
        ///     start server
        /// </summary>
        public void StartServer()
        {
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
        private void WatchConnection()
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
        private void ReceiveMsg(object socketClientPara)
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

                string msg = Encoding.UTF8.GetString(msgReceiver, 1, length - 1);

                if (msgReceiver[0] == CHECK_ROOM_LIST)
                    CheckRoomList(msg);
                else if (msgReceiver[0] == REQUEST_ROOM_MSG)
                    GetRoomMsg(socketKey, msg);
                else if (msgReceiver[0] == SEND_MSG)
                    AddMsgToFile(socketKey, msg);
                else if (msgReceiver[0] == DISCONNECT)
                    RemoveOfflineUser(socketKey);
                else
                    InvalidMsg(socketKey);
            }
        }
        #endregion

        #region --- Check Room List ---
        /// <summary>
        ///     Check the existence of every room in room list received from client
        /// </summary>
        /// <param name="msg">string of room list</param>
        private void CheckRoomList(string s_roomList)
        {
            // TODO : check the existence of chat file of each room
            List<string> roomList = (List<string>)JsonConvert.DeserializeObject(s_roomList, typeof(List<string>));

            foreach (string room in roomList)
            {
                string roomFile = room + ".txt";
                if (!File.Exists(@roomFile))
                {
                    FileStream fs = new FileStream(roomFile, FileMode.Create);
                    fs.Close();
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
        private void GetRoomMsg(string clientIP, string roomId)
        {
            // TODO : get the history of specific chat room
            string roomFile = roomId + ".txt";

            List<string> msgList = new List<string>();

            String sendMsg = "";

            if (File.Exists(@roomFile))
            {
                StreamReader sr = new StreamReader(roomFile, Encoding.Default);
                
                String lineMsg;
                while ((lineMsg = sr.ReadLine()) != null)
                    msgList.Add(lineMsg);
                sendMsg = JsonConvert.SerializeObject(msgList);
            }
            else
            {
                FileStream fs = new FileStream(roomFile, FileMode.Create);
                fs.Close();
            }

            SendMessage(clientIP, REQUEST_ROOM_MSG, sendMsg);
        }
        #endregion

        #region --- Add Message To File ---
        /// <summary>
        ///     put the message into the specific room
        /// </summary>
        /// <param name="clientIP">the client IP</param>
        /// <param name="msg">the string include the room id and received message</param>
        private void AddMsgToFile(string clientIP, string msg)
        {
            // TODO : put the message into the specific room
            MsgHandler msgHandler = (MsgHandler)JsonConvert.DeserializeObject(msg, typeof(MsgHandler));

            string roomFile = msgHandler.roomId + ".txt";

            FileStream fs = new FileStream(roomFile, FileMode.OpenOrCreate);
            StreamWriter sw = new StreamWriter(fs);
            sw.WriteLine(msgHandler.msg);
            sw.Close();
            fs.Close();
        }
        #endregion

        #region --- Remove Offline User ---
        /// <summary>
        ///     remove off line user
        /// </summary>
        /// <param name="clientIP">the client IP</param>
        private void RemoveOfflineUser(string clientIP)
        {
            Console.WriteLine("User IP : " + clientIP + " has went off line.");

            if (dictSocket.ContainsKey(clientIP))
            {
                dictSocket[clientIP].Close();
                dictSocket.Remove(clientIP);
            }               

            if (dictThread.ContainsKey(clientIP))
            {
                Thread tmp = dictThread[clientIP];
                dictThread.Remove(clientIP);
                tmp.Abort();
            }
                
        }
        #endregion

        #region --- Invalid Message ---
        /// <summary>
        ///     Handle the situation of invalid message
        /// </summary>
        /// <param name="clientIP">the client ip</param>
        private void InvalidMsg(string clientIP)
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
        private void SendMessage(string clientIP, byte flag, string msg)
        {
            try
            {
                byte[] arrMsg = Encoding.UTF8.GetBytes(msg);
                byte[] sendArrMsg = new byte[arrMsg.Length + 1];

                // set the msg type
                sendArrMsg[0] = flag;
                Buffer.BlockCopy(arrMsg, 0, sendArrMsg, 1, arrMsg.Length);

                dictSocket[clientIP].Send(sendArrMsg);
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
        public string msg;

        public MsgHandler(string r, string m)
        {
            roomId = r; msg = m;
        }
    }
}
