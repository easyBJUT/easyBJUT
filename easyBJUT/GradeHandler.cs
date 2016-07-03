using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

/************************
 * 
 * @author: CZ_vino
 * @lastUpdate: 2016/6/1
 * @version: v1.0
 * 
 ************************/


namespace MsgHandler
{
    #region ----------Useless This Time----------
    public enum CourseType
    {
        CAMPUS_OPTIONAL,                        //校公选课
        PUBLIC_BASIC_REQUIRED,                  //公共基础必修课
        SUBJECT_BASIC_REQUIRED,                 //学科基础必修课
        SUBJECT_BASIC_OPTIONAL,                 //学科基础选修课
        MAJOR_LIMITED,                          //专业限选课
        MAJOR_OPTIONAL,                         //专业任选课
        PRACTICAL_REQUIRED,                     //实践环节必修课
        PRACTICAL_OPTIONAL,                     //实践环节选修课
        INNOVATIVE_PRACTICE,                    //创新实践环节
        LIBERAL_OPTIONAL                        //通识教育选修课
    }
    
    /// <summary>
    ///     <成绩>结构体，存有爬下来的成绩数据，分别包括<学年>、<学期>、<课程名称>、<课程种类>、<学分>、<绩点>、<成绩>属性
    /// </summary>
    public struct Grades
    {
        public string academicYear;             //学年
        public int semester;                    //学期
        public string courseName;               //课程名称
        public CourseType courseType;           //课程种类
        public double credit;                   //学分
        public double GPA;                      //绩点
        public string grade;                    //成绩，种类有：百分制成绩和“通过”，所以定义为string

        /// <summary>
        ///     带参构造方法
        /// </summary>
        /// <param name="academicYear">学年</param>
        /// <param name="semester">学期</param>
        /// <param name="courseName">课程名称</param>
        /// <param name="courseType">课程性质</param>
        /// <param name="credit">学分</param>
        /// <param name="GPA">绩点</param>
        /// <param name="grade">成绩</param>
        public Grades(string academicYear, int semester, string courseName, CourseType courseType, double credit, double GPA, string grade)
        {
            this.academicYear = academicYear;
            this.semester = semester;
            this.courseName = courseName;
            this.courseType = courseType;
            this.credit = credit;
            this.GPA = GPA;
            this.grade = grade;
        }
    }
    #endregion

    /// <summary>
    ///     成绩查询、加权计算
    /// </summary>
    static class GradeHandler
    {
        private static DataTable gradesTable;                                               //存储成绩表
        private static bool hasLoadData = false;                                            //标志成绩表是否读取成功

        private const string pattern = @"\d+(\.\d+)?";                                      //正则表达式匹配数字

        // TODO: Need to change filepath and sheet name here
        private const string filePath = "score.xls";                                          //excel路径及名称
        private const string sheetName = "score";                                              //excel中表单名称

        #region ----------读取数据----------
        /// <summary>
        ///    从excel中读取成绩数据，结果存入gradeSet，成功标志hasLoadData
        /// </summary>
        public static void LoadDataFromExcel()
        {
            try
            {
                string strConn;
                strConn = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + filePath + ";Extended Properties='Excel 8.0;HDR=False;IMEX=1'";
                OleDbConnection OleConn = new OleDbConnection(strConn);
                OleConn.Open();
                String sql = "SELECT * FROM ["+sheetName+"$]";

                OleDbDataAdapter OleDaExcel = new OleDbDataAdapter(sql, OleConn);
                gradesTable = new DataTable();
                DataSet ds = new DataSet();
                OleDaExcel.Fill(ds, "GradeSheet");
                OleConn.Close();
                gradesTable = ds.Tables[0];
                hasLoadData = true;
                return;
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message);
                //Console.WriteLine("{0}", err.Message);
                hasLoadData = false;
                return;
            }
        }
        #endregion

        #region ----------查询数据----------
        /// <summary>
        ///     查询数据
        /// </summary>
        /// <param name="queryString">查询命令</param>
        /// <param name="queryResult">查询结果</param>
        /// <param name="weightedMean">加权平均分</param>
        /// <returns>查询成功与否</returns>
        public static bool QueryData(String queryString, out DataTable queryResult)
        {
            //初始化DataTable
            queryResult = new DataTable();
            queryResult.Columns.Add("学年", typeof(string));
            queryResult.Columns.Add("学期", typeof(string));
            queryResult.Columns.Add("课程名称", typeof(string));
            queryResult.Columns.Add("课程性质", typeof(string));
            queryResult.Columns.Add("学分", typeof(string));
            queryResult.Columns.Add("绩点", typeof(string));
            queryResult.Columns.Add("成绩", typeof(string));
            queryResult.Columns.Add("辅修标记", typeof(string));

            //如果数据被成功导入，则执行查询操作
            if (hasLoadData)
            {
                //查询信息为“*”意味着查询全部成绩信息
                if (queryString.Equals("*"))
                {
                    queryResult = gradesTable;
                    return true;
                }

                //否则按照条件查询
                try
                {
                    //获取查询结果
                    DataRow []tmpQureResult = gradesTable.Select(queryString);

                    //未查到匹配信息则弹窗示意
                    if (tmpQureResult == null || tmpQureResult.Length == 0)
                        MessageBox.Show("无查询匹配项！");
                        //Console.WriteLine("无查询匹配项！");
                    
                    //否则将结果存入queryResult中
                    else
                        foreach (DataRow dr in tmpQureResult)
                            queryResult.Rows.Add(dr.ItemArray);
                    return true;
                }
                catch(Exception e)
                {
                    MessageBox.Show(e.Message);
                    //Console.WriteLine("{0}",e.Message);
                    return false;
                }
            }
            else
            {
                MessageBox.Show("[ERROR]Grades data hasn't loaded successfully.");
                //Console.WriteLine("[ERROR]Grades data hasn't loaded successfully.");
                return false;
            }
        }
        #endregion


        #region ----------计算加权平均分----------
        /// <summary>
        ///     计算加权平均分
        /// </summary>
        /// <param name="calculateData">筛选后的成绩列表</param>
        /// <param name="weightedMean">加权平均分</param>
        /// <returns>计算是否成功</returns>
        public static bool CalculateWeightedMean(DataTable calculateData, out double weightedMean)
        {
            //若输入数据表为空，则不计算。
            if (calculateData.Rows.Count == 0)
            {
                weightedMean = 0;
                MessageBox.Show("[ERROR]No data to calculate weighted mean.");
                return false;
            }

            try
            {
                //计算学分和、成绩*学分和
                double sumOfCredit = 0, sumOfGrade = 0;
                foreach (DataRow dr in calculateData.Rows)
                {
                    //如果成绩为数字且不是第二课堂性质的课程，计算加权
                    if (Regex.IsMatch(Convert.ToString(dr["成绩"]), pattern) && !Convert.ToString(dr["课程性质"]).Equals("校选修课") && Convert.ToInt32(dr["成绩"]) >= 60 && Convert.ToInt32(dr["辅修标记"]) == 0)
                    {
                        sumOfCredit += Convert.ToDouble(dr["学分"]);
                        sumOfGrade += Convert.ToDouble(dr["成绩"]) * Convert.ToDouble(dr["学分"]);
                    }

                }
                weightedMean = sumOfGrade / sumOfCredit;
                return true;
            }
            catch (Exception e)
            {
                weightedMean = -1;
                MessageBox.Show("{0}", e.Message);
                return false;
            }
        }
        #endregion
    }
}