using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data.SQLite;
using CommonLang.Log;
using CommonLang.Reflection;
using Quartz;

namespace CommonSchedule.QuartzImpl
{
    public class QuartzSQLite
    {
        public QuartzSQLite(Dictionary<string, string> properties)
        {
            string strConnectionString = string.Empty;/*SQLite连接字符串，刚开始没有，暂时留空*/
            string strDataSource = @"D:\test.db";//SQLite数据库文件存放物理地址
                                                 //用SQLiteConnectionStringBuilder构建SQLite连接字符串
            System.Data.SQLite.SQLiteConnectionStringBuilder scBuilder = new SQLiteConnectionStringBuilder();
            scBuilder.DataSource = strDataSource;//SQLite数据库地址
            scBuilder.Password = "123456";//密码
            strConnectionString = scBuilder.ToString();
            using (SQLiteConnection connection = new SQLiteConnection(strConnectionString))
            {
                //验证数据库文件是否存在
                if (System.IO.File.Exists(strDataSource) == false)
                {
                    //创建数据库文件
                    SQLiteConnection.CreateFile(strDataSource);
                }
                //打开数据连接
                connection.Open();
                //Command
                SQLiteCommand command = new SQLiteCommand(connection);
                command.CommandText = "CREATE TABLE tb_User(ID int,UserName varchar(60));INSERT INTO [tb_User](ID,UserName) VALUES(1,'A')";// "CREATE TABLE tb_User(ID int,UserName varchar(60));";
                command.CommandType = System.Data.CommandType.Text;
                //执行SQL
                int iResult = command.ExecuteNonQuery();
                //可省略步骤=======关闭连接
                connection.Close();
            }
        }
        public void Shutdown()
        {

        }
    }

}
