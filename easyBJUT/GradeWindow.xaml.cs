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

namespace easyBJUT
{
    /// <summary>
    /// GradeWindow.xaml 的交互逻辑
    /// </summary>
    public partial class GradeWindow : Window
    {
        public GradeWindow()
        {
            InitializeComponent();
            MainWindow m = new MainWindow();
            courseName.Text = m.studentName;
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
            GradeHandler.LoadDataFromExcel();
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
            Application.Current.Shutdown();
            base.OnClosing(e);
        }
    }
}
