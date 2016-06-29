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


namespace easyBJUT
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public String studentName;
        private Process p;
        FileSystemWatcher fsw;
        private bool flag;
        public MainWindow()
        {
            InitializeComponent();
            try
            {
                p = new Process();
                p.StartInfo.FileName = @"v1.2.exe ";
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardInput = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.CreateNoWindow = true;
                p.Start();

                //p.StandardInput.WriteLine(@"v1.2.exe");
                p.StandardInput.WriteLine(@"1");
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
            flag = true;

            System.Threading.Thread.Sleep(500);

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


            /*fsw = new FileSystemWatcher();
            fsw.Path = System.Environment.CurrentDirectory;
            fsw.Filter = "score.xls";
            fsw.NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.Size;
            fsw.Created += new FileSystemEventHandler(changed);  //绑定事件触发后处理数据的方法。  
            fsw.Changed += new FileSystemEventHandler(changed);
            fsw.EnableRaisingEvents = true;*/
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

                GradeWindow GradeWindow = new GradeWindow();
                GradeWindow.Show();
                this.Close();
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

                p.StandardInput.WriteLine(@"1");

                System.Threading.Thread.Sleep(500);

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

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {

        }

    }
}
