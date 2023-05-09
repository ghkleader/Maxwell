using SqlSugar;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;


namespace SqlSugar.DAO
{
    /// <summary>
    /// SqlSugar
    /// </summary>
    public class SugarDao
    {
        //禁止实例化
        private SugarDao()
        {
        }

        private static string reval = ConfigurationManager.ConnectionStrings["mysqlConn"].ToString();  //这里可以动态根据cookies或session实现多库切换
        public static string ConnectionString
        {
            get
            {
                return reval;
            }
            set
            {
                reval = value;
            }
        }
        public static SqlSugarClient GetInstance()
        {
            //创建数据库对象 SqlSugarClient   
            try
            {
                SqlSugarClient db = new SqlSugarClient(new ConnectionConfig()
                {
                    ConnectionString = ConnectionString,
                    DbType = DbType.MySql,
                    IsAutoCloseConnection = true,//自动释放
                    InitKeyType = InitKeyType.Attribute
                });

                //5.1.3.24统一了语法和SqlSugarScope一样，老版本AOP可以写外面
                db.Aop.OnLogExecuting = (sql, pars) =>
                {
                    Console.WriteLine(sql);//输出sql,查看执行sql 性能无影响
                };              
                return db;

            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}