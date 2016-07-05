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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Diagnostics;
using System.Threading;


namespace easyBJUT
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private Process p;
        private bool flag = true;
        private bool flagYzm = true;
        private FileSystemWatcher watcher = new FileSystemWatcher();
        private FileSystemWatcher fsw;
        public MainWindow()
        {
            InitializeComponent();


            string filespath = Directory.GetCurrentDirectory() + "//error.txt";
            if (File.Exists(filespath))
            {
                FileInfo fi = new FileInfo(filespath);
                if (fi.Attributes.ToString().IndexOf("ReadOnly") != -1)
                    fi.Attributes = FileAttributes.Normal;
                File.Delete(filespath);
            }

            watcher.Path = Directory.GetCurrentDirectory();
            watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            watcher.Filter = "*.txt";
            watcher.Created += new FileSystemEventHandler(OnChanged);
            watcher.EnableRaisingEvents = true;
           
            fsw = new FileSystemWatcher();
            fsw.Path = System.Environment.CurrentDirectory;
            fsw.Filter = "image.jpg";
            fsw.NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.Size;
            fsw.Created += new FileSystemEventHandler(changed);  //绑定事件触发后处理数据的方法。  
            fsw.Changed += new FileSystemEventHandler(changed);
            fsw.EnableRaisingEvents = true;

            try
            {
                p = new Process();
                p.StartInfo.FileName = @"Data.exe";
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardInput = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.CreateNoWindow = true;
                p.Start();

                //p.StandardInput.WriteLine(@"v1.2.exe");
                flagYzm = true;
                p.StandardInput.WriteLine(@"1");
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }

            while (flagYzm)
            {

            }

            Thread.Sleep(10);
            String filePath = System.Environment.CurrentDirectory + "/image.jpg";


            BinaryReader binReader = new BinaryReader(File.Open(filePath, FileMode.Open));
            FileInfo fileInfo = new FileInfo(filePath);
            byte[] bytes = binReader.ReadBytes((int)fileInfo.Length);
            binReader.Close();

            // Init bitmap
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource = new MemoryStream(bytes);
            bitmap.EndInit();
            identifyingCodeImage.Source = bitmap;
        }

        private void changed(object source, FileSystemEventArgs e)
        {
            flagYzm = false;
        }

        private void OnChanged(object source, FileSystemEventArgs e)
        {
            // Specify what is done when a file is changed, created, or deleted.
            flag = false;
            
        }



        private void loginButton_Click(object sender, RoutedEventArgs e)
        {


            String u_name;
            String u_password;
            String icode;
            u_name = username.Text;
            u_password = password.Password;
            icode = identifying_code.Text;
            if (u_name == "" || u_password == "" || icode == "")
                MessageBox.Show("请输入正确的登录信息！");
            else
            {
                p.StandardInput.WriteLine(@"2");
                p.StandardInput.WriteLine(u_name);
                p.StandardInput.WriteLine(u_password);
                p.StandardInput.WriteLine(icode);
                p.WaitForExit();
                p.Close();
                p.Dispose(); 
                if (flag)
                {
                    GradeWindow GradeWindow = new GradeWindow();
                    GradeWindow.Show();
                    this.Close();
                }
                else
                {
                    string filespath = Directory.GetCurrentDirectory() + "/error.txt";
                    string str;
                    StreamReader sr = new StreamReader(filespath, Encoding.Default);
                    str = sr.ReadLine().ToString();
                    sr.Close();
                    MessageBox.Show(str);
                    if (File.Exists(filespath))
                    {
                        FileInfo fi = new FileInfo(filespath);
                        if (fi.Attributes.ToString().IndexOf("ReadOnly") != -1)
                            fi.Attributes = FileAttributes.Normal;
                        File.Delete(filespath);
                    }
                    p = new Process();
                    p.StartInfo.FileName = @"Data.exe";
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.RedirectStandardInput = true;
                    p.StartInfo.RedirectStandardOutput = true;
                    p.StartInfo.RedirectStandardError = true;
                    p.StartInfo.CreateNoWindow = true;
                    p.Start();

                    flagYzm = true;
                    p.StandardInput.WriteLine(@"1");

                    while (flagYzm)
                    {

                    }
                    Thread.Sleep(10);
                    flag = true;

                    password.Password = "";
                    identifying_code.Text = "";

                    identifyingCodeImage.Source = new BitmapImage();
                    try
                    {

                       

                        String filePath = System.Environment.CurrentDirectory + "/image.jpg";
                        BinaryReader binReader = new BinaryReader(File.Open(filePath, FileMode.Open));
                        FileInfo fileInfo = new FileInfo(filePath);
                        byte[] bytes = binReader.ReadBytes((int)fileInfo.Length);
                        binReader.Close();

                        // Init bitmap
                        BitmapImage bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.StreamSource = new MemoryStream(bytes);
                        bitmap.EndInit();
                        identifyingCodeImage.Source = bitmap;
                    }
                    catch (Exception err)
                    {
                        MessageBox.Show(err.Message);
                    }
                }

            }
        }

        private void resetButton_Click(object sender, RoutedEventArgs e)
        {
            username.Text = "";
            password.Password = "";
            identifying_code.Text = "";
        }

        private void changeButton_Click(object sender, RoutedEventArgs e)
        {
            identifyingCodeImage.Source = new BitmapImage();
            try
            {

                flagYzm = true;
                p.StandardInput.WriteLine(@"1");

                while (flagYzm)
                {

                }
                Thread.Sleep(10);

                String filePath = System.Environment.CurrentDirectory + "/image.jpg";
                BinaryReader binReader = new BinaryReader(File.Open(filePath, FileMode.Open));
                FileInfo fileInfo = new FileInfo(filePath);
                byte[] bytes = binReader.ReadBytes((int)fileInfo.Length);
                binReader.Close();

                // Init bitmap
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = new MemoryStream(bytes);
                bitmap.EndInit();
                identifyingCodeImage.Source = bitmap;
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message);
            }
        }
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            string filespath = Directory.GetCurrentDirectory() + "/image.jpg";
            if (File.Exists(filespath))
            {
                FileInfo fi = new FileInfo(filespath);
                if (fi.Attributes.ToString().IndexOf("ReadOnly") != -1)
                    fi.Attributes = FileAttributes.Normal;
                File.Delete(filespath);
            }
            base.OnClosing(e);
        }
    }
}
