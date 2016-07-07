using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MsgHandler;
using System.Data;
using System.IO;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using Newtonsoft.Json;
using System.Collections.ObjectModel;


namespace easyBJUT
{
    /// <summary>
    /// GradeWindow.xaml 的交互逻辑
    /// </summary>
    public partial class GradeWindow : Window
    {
        // Status Key
        private const byte CHECK_ROOM_LIST = 0;
        private const byte REQUEST_ROOM_MSG = 1;
        private const byte SEND_MSG = 2;
        private const byte DISCONNECT = 3;
        private const byte IS_RECEIVE_MSG = 4;
        private const byte IS_NOT_RECEIVE_MSG = 5;
        private const byte INVALID_MESSAGE = 6;

        private const string ipAddr = "172.21.22.161";  // watching IP
        private const int port = 3000;              // watching port

        private Object sendLock = new Object();

        // client thread, used for receive message
        private Thread threadClient = null;
        // client socket, used for connect server
        private Socket socketClient = null;

        private List<string> chatRoom;

        public GradeWindow()
        {
            InitializeComponent();
            String filePath = System.Environment.CurrentDirectory + "/webwxgetmsgimg.png";

            GradeHandler.LoadDataFromExcel();

            BinaryReader binReader = new BinaryReader(File.Open(filePath, FileMode.Open));
            FileInfo fileInfo = new FileInfo(filePath);
            byte[] bytes = binReader.ReadBytes((int)fileInfo.Length);
            binReader.Close();

            // Init bitmap
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource = new MemoryStream(bytes);
            bitmap.EndInit();
            title.Source = bitmap;

            chatRoom = new List<string>();
            courseList.ItemsSource = chatRoom;

            // get IP address
            IPAddress address = IPAddress.Parse(ipAddr);
            // create the endpoint
            IPEndPoint endpoint = new IPEndPoint(address, port);
            // create the socket, use IPv4, stream connection and TCP protocol
            socketClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                // Connect to the Server
                socketClient.Connect(endpoint);

                threadClient = new Thread(ReceiveMsg);
                threadClient.IsBackground = true;
                threadClient.Start();

                DataTable dataTable;
                while(!GradeHandler.GetCourseIdAndName(out dataTable));

                foreach (DataRow dr in dataTable.Rows)
                {
                    string courseName = Convert.ToString(dr["课程名称"]);
                    string courseId = Convert.ToString(dr["课程代码"]);

                    courseName = courseName.Replace('/', '_');
                    courseName = courseName.Replace('\\', '_');
                    courseName = courseName.Replace(':', '_');
                    courseName = courseName.Replace('*', '_');
                    courseName = courseName.Replace('?', '_');
                    courseName = courseName.Replace('\"', '_');
                    courseName = courseName.Replace('>', '_');
                    courseName = courseName.Replace('<', '_');
                    courseName = courseName.Replace('|', '_');

                    courseId = courseId.Replace('/', '_');
                    courseId = courseId.Replace('\\', '_');
                    courseId = courseId.Replace(':', '_');
                    courseId = courseId.Replace('*', '_');
                    courseId = courseId.Replace('?', '_');
                    courseId = courseId.Replace('\"', '_');
                    courseId = courseId.Replace('>', '_');
                    courseId = courseId.Replace('<', '_');
                    courseId = courseId.Replace('|', '_');

                    chatRoom.Add(courseName + "(" + courseId + ")");
                }
                    

                CheckRoomList(chatRoom);

            }
            catch (SocketException se)
            {
                MessageBox.Show("[SocketError]Connection failed: " + se.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show("[Error]Connection failed: " + ex.Message);
            }
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {


            string queryString = "";
            string course_Name = courseName.Text;
            string school_Year = schoolYear.Text;
            string _Semester = semester.Text;
            string course_Type = courseType.Text;
            string _Credit = credit.Text;
            DataTable dt = new DataTable();
            bool flag = false;
            if (!school_Year.Equals(""))
            {
                queryString += "学年='" + school_Year + "'";
                flag = true;
            }
            if (!course_Name.Equals(""))
            {
                if (flag)
                    queryString += " and 课程名称 like '%" + course_Name + "%'";
                else
                {
                    queryString += "课程名称 like '%" + course_Name + "%'";
                    flag = true;
                }
            }
            if (!_Semester.Equals(""))
            {
                if (flag)
                    queryString += " and 学期='" + _Semester + "'";
                else
                {
                    queryString += "学期='" + _Semester + "'";
                    flag = true;
                }
            }
            if (!course_Type.Equals(""))
            {
                if (flag)
                    queryString += " and 课程性质='" + course_Type + "'";
                else
                {
                    queryString += "课程性质='" + course_Type + "'";
                    flag = true;
                }
            }
            if (!_Credit.Equals(""))
            {
                if (flag)
                    queryString += " and 学分='" + _Credit + "'";
                else
                {
                    queryString += "学分='" + _Credit + "'";
                    flag = true;
                }
            }
            if (!flag)
                queryString = "*";
            flag = false;

            if (GradeHandler.QueryData(queryString, out dt))
            {
                dataGrid1.ItemsSource = dt.DefaultView;
            }
            double _weight;
            if (GradeHandler.CalculateWeightedMean(dt, out _weight))
                weighting.Text = Convert.ToString(Math.Round(_weight, 2));
            else
                weighting.Text = "";
        }

        private void exit_Click(object sender, RoutedEventArgs e)
        {
            MainWindow myWindow = new MainWindow();
            myWindow.Show();
            this.Close();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            string filespath = Directory.GetCurrentDirectory() + "/score.xls";
            if (File.Exists(filespath))
            {
                FileInfo fi = new FileInfo(filespath);
                if (fi.Attributes.ToString().IndexOf("ReadOnly") != -1)
                    fi.Attributes = FileAttributes.Normal;
                File.Delete(filespath);
            }
            GoOffLine();
            base.OnClosing(e);
        }

        #region --- Check Room List ---
        /// <summary>
        ///     check room list
        /// </summary>
        /// <param name="roomList"></param>
        private void CheckRoomList(List<string> roomList)
        {
            string s_roomList = JsonConvert.SerializeObject(roomList);

            SendMsg(CHECK_ROOM_LIST, s_roomList);
        }
        #endregion

        #region --- Add New Message ---
        /// <summary>
        ///     add new message
        /// </summary>
        /// <param name="roomId"></param>
        /// <param name="msg"></param>
        private void AddNewMsg(string roomId, string msg)
        {
            List<string> msgList = new List<string>();
            msgList.Add(msg);
            MsgHandler msgHandler = new MsgHandler(roomId, msgList);
            string sendMsg = JsonConvert.SerializeObject(msgHandler);
            
            SendMsg(SEND_MSG, sendMsg);
        }
        #endregion

        #region --- Off Line ---
        /// <summary>
        ///     go off line
        /// </summary>
        private void GoOffLine()
        {
            SendMsg(DISCONNECT, "");
        }
        #endregion

        #region --- Request Room Message ---
        /// <summary>
        ///     request message
        /// </summary>
        private void RequestMsg(string roomId)
        {
            SendMsg(REQUEST_ROOM_MSG, roomId);
        }
        #endregion

        #region --- Send Message ---
        /// <summary>
        ///     Send Message
        /// </summary>
        /// <param name="flag">msg type</param>
        /// <param name="msg">message</param>
        private void SendMsg(byte flag, string msg)
        {
            lock(sendLock)
            {
                try
                {
                    byte[] arrMsg = Encoding.UTF8.GetBytes(msg);
                    byte[] sendArrMsg = new byte[arrMsg.Length + 1];

                    // set the msg type
                    sendArrMsg[0] = flag;
                    Buffer.BlockCopy(arrMsg, 0, sendArrMsg, 1, arrMsg.Length);

                    socketClient.Send(sendArrMsg);
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
        }
        #endregion

        #region --- Receive Message ---
        /// <summary>
        ///     receive message
        /// </summary>
        private void ReceiveMsg()
        {
            while (true)
            {
                // define a buffer for received message
                byte[] arrMsg = new byte[1024 * 1024 * 2];

                // length of message received
                int length = -1;

                try
                {
                    // get the message
                    length = socketClient.Receive(arrMsg);

                    // encoding the message
                    string msgReceive = Encoding.UTF8.GetString(arrMsg, 1, length-1);

                    if (arrMsg[0] == SEND_MSG)
                    {
                        ReceiveMsgFromServer(msgReceive);
                    }
                    else if (arrMsg[0] == IS_RECEIVE_MSG)
                    {
                        Application.Current.Dispatcher.Invoke(new Action(delegate
                        {
                            MessageBox.Show("发送消息成功");
                        }));
                    }
                    else if (arrMsg[0] == IS_NOT_RECEIVE_MSG)
                    {
                        Application.Current.Dispatcher.Invoke(new Action(delegate
                        {
                            MessageBox.Show("[Error]发送消息失败");
                        }));
                    }
                    else if (arrMsg[0] == INVALID_MESSAGE)
                    {
                        Application.Current.Dispatcher.Invoke(new Action(delegate
                        {
                            MessageBox.Show("[Error]通信过程出错");
                        }));
                    }
                    else
                    {
                        Application.Current.Dispatcher.Invoke(new Action(delegate
                        {
                            MessageBox.Show("[Error]通信过程出错");
                        }));
                    }

                }
                catch (SocketException se)
                {
                    //MessageBox.Show("【错误】接收消息异常：" + se.Message);
                    return;
                }
                catch (Exception e)
                {
                    //MessageBox.Show("【错误】接收消息异常：" + e.Message);
                    return;
                }
            }
        }
        #endregion

        #region --- Receive Room History Message ---
        /// <summary>
        ///     Receive Message
        /// </summary>
        /// <param name="msgReceive"></param>
        private void ReceiveMsgFromServer(string msgReceive)
        {
            MsgHandler msgHandler = (MsgHandler)JsonConvert.DeserializeObject(msgReceive, typeof(MsgHandler));
            string roomId = msgHandler.roomId;
            List<string> msgList = msgHandler.msgList;
            byte backR, backG, backB;
            Random ran=new Random();
            Application.Current.Dispatcher.Invoke(new Action(delegate
            {
                tucaoWall.Document.Blocks.Clear();
                string room = (string)courseList.SelectedItem;
                 if (room.Equals(roomId))
                    foreach (string msg in msgList)
                    {
                        // TODO : 将消息逐一添加到显示框中
                        Paragraph newParagraph = new Paragraph();
                        backR = (byte)ran.Next(0x80, 0xFF);
                        backG = (byte)ran.Next(0x80, 0xFF);
                        backB = (byte)ran.Next(0x80, 0xFF);
                        InlineUIContainer inlineUIContainer = new InlineUIContainer()
                        {
                            
                            Child = new TextBlock()
                            {
                                Background = new SolidColorBrush(Color.FromArgb(0xBF, backR, backG, backB)),
                                Foreground = new SolidColorBrush(Colors.Black),
                                TextWrapping = TextWrapping.Wrap,
                                Text = msg + "\r\n"
                            }
                        };
                        newParagraph.Inlines.Add(inlineUIContainer);

                        tucaoWall.Document.Blocks.Add(newParagraph);
                    }
                 
            }));
        }
        #endregion

        private void courseList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string room = (string)courseList.SelectedItem;

            tucaoWall.Document.Blocks.Clear();

            RequestMsg(room);
        }

        private void sendMsg_Click(object sender, RoutedEventArgs e)
        {
            List<string> msgList=new List<string>();
            msgList.Add("日狗");
            msgList.Add("sb");
            msgList.Add("傻逼");
            msgList.Add("cnm");
            msgList.Add("我操");
            msgList.Add("fuck");
            if(inputTextBox.Text=="")
            {
                MessageBox.Show("请输入吐槽内容！");
            }
            else 
            {
                bool flag=true;
                foreach (string message in msgList)
                {
                    if (inputTextBox.Text.Contains(message) || nickname.Text.Contains(message))
                    {
                        MessageBox.Show("请注意素质，文明用语！");
                        flag = false;
                        break;
                    }
                }
                if (flag)
                {
                    string room = (string)courseList.SelectedItem;
                    string msg = (string)inputTextBox.Text;

                    inputTextBox.Text = "";

                    AddNewMsg(room, nickname.Text.Trim() + "：" + DateTime.Now.ToString().Substring(0, DateTime.Now.ToString().Length - 3) + "\r\n    " + msg);
                }
            }
        }

        private void update_Click(object sender, RoutedEventArgs e)
        {
            string room = (string)courseList.SelectedItem;
            RequestMsg(room);
        }

        private void nickname_LostFocus(object sender, RoutedEventArgs e)
        {
            if (nickname.Text.Trim().Equals(""))
                nickname.Text = "学生";
        }

        private void nickname_GotFocus(object sender, RoutedEventArgs e)
        {
            if (nickname.Text.Trim().Equals("学生"))
                nickname.Text = "";
        }
    }
}
