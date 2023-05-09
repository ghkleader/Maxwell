using EasyLog;
using Models;
using MySql.Data.MySqlClient;
using SqlSugar;
using SqlSugar.DAO;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TimerHandleService
{
    public partial class Service1 : ServiceBase
    {
        Log log = Log.getInstance();
        private static MySqlHelper mysql = new MySqlHelper();
        public static System.Threading.Timer onClockThreadTimer;
        private SqlSugarClient db;
        public string[] AlarmPLCMess = new string[11]{
            "NO.40：检测到激光光闸没有打开，自动化加工将自动退出，请检查！",//00 
            "NO.41：主机“进料”第一个模组堵料，请检查！",	   	//01
            "NO.42：检测到破片，请处理！",   	//02   
            "NO.43：检测到异常片，请处理！",	  	//03   
            "NO.44：主机“出料”第二个模组堵料，请检查！",	  	//04   
            "NO.45：主机“出料”缓存，释放异常，请检查！",   	//05   
            "NO.46：主机“进料”模组2，传感器被遮挡，请移除硅片，然后点击确认",  //06   
            "NO.47：检测到划伤，请立马处理！",			 //07   
            "NO.48：主机“下料”抓料异常，请检查并取走硅片！", 			  //08   
            "NO.49：台面A或者台面C负压异常，请检查！",				 //09   
            "NO.50：台面B或者台面D负压异常，请检查！", 				 //10 
        };

        /// <summary>
        /// 更新设备总进度定时器
        /// </summary>
        public static System.Threading.Timer updateEquipTotoalProcessTimer;

        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            log.Write("数据库定时任务服务-已启动", MsgType.System);

            onClockThreadTimer = new System.Threading.Timer(runOnceTask);

            //每小时
            setThreadTimeOneDay(new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.AddHours(1).Hour, 0, 0, 0));
            //setThreadTimeOneDay(DateTime.Now.AddMinutes(1));

            //隔10分钟执行一次
            //updateEquipTotoalProcessTimer = new Timer(UpdateEquipTotalProcess, null,100,1000*60*10);
        }

        protected override void OnStop()
        {
            log.Write("数据库定时任务服务-已关闭", MsgType.System);
        }




        /// <summary>
        /// 每天00:05定时插入设备状态任务
        /// </summary>
        /// <param name="state"></param>
        void runOnceTask(object state)
        {
            try
            {
                db = SugarDao.GetInstance();

                log.Write("定时任务执行一次", MsgType.Success);

                if (InsertEquipStatus())
                {
                    //成功
                    //setThreadTimeOneDay(DateTime.Today.AddMinutes(5));
                    //setThreadTimeOneDay(DateTime.Now.AddMinutes(1));
                    setThreadTimeOneDay(new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.AddHours(1).Hour, 0, 0, 0));
                }
                else
                {
                    //失败
                    setThreadTimeOneDay(DateTime.Now.AddMinutes(10));
                    log.Write("定时任务执行失败", MsgType.Error);
                }
            }
            catch (Exception ex)
            {
                log.Write("定时任务执行失败：" + ex.Message, MsgType.Error);
                setThreadTimeOneDay(DateTime.Now.AddMinutes(10));
            }

        }

        void setThreadTimeOneDay(DateTime setTime)//每天定时执行
        {
            if (DateTime.Now > setTime)
            {
                setTime = setTime.AddDays(1);
            }
            onClockThreadTimer.Change((int)((setTime - DateTime.Now).TotalMilliseconds), Timeout.Infinite);//第一参数：多长时间执行，第二参数：执行一次
        }


        public bool InsertEquipStatus()
        {
            try
            {
                //增加产能
                List<Productivity> proList = new List<Productivity>();
                var tempList = db.Queryable<Equipment>().ToList();
                int seed = (new Random()).Next(1, 20);
                int i = 0;
                foreach (var item in tempList)
                {
                    i++;
                    int proNum = (new Random(seed + i)).Next(2850, 3920);
                    Productivity pro = new Productivity()
                    {
                        eqpname = item.eqpname,
                        pro = proNum,
                    };
                    proList.Add(pro);
                }
                db.Insertable<Productivity>(proList).ExecuteCommand();

                //增加报警
                List<Alarm> alarmList = new List<Alarm>();
                var tempList1 = db.Queryable<Equipment>().ToList();
                foreach (var item in tempList1)
                {
                    i++;
                    int id1 = (new Random(seed + i)).Next(40, 50);
                    Alarm alarm1 = new Alarm()
                    {
                        eqpname = item.eqpname,
                        id = id1,
                        message = AlarmPLCMess[id1-40],                       
                    };
                    alarmList.Add(alarm1);

                    int id2 = (new Random(seed + 2 + i )).Next(40, 50);
                    Alarm alarm2 = new Alarm()
                    {
                        eqpname = item.eqpname,
                        id = id2,
                        message = AlarmPLCMess[id2 - 40],
                    };
                    alarmList.Add(alarm2);
                }

                for (int j = 0; j < 2; j++)
                {
                    db.Insertable<Alarm>(alarmList).ExecuteCommand();
                }

                return true;
            }
            catch (Exception)
            {                
                throw;
                return false;
            }
        }

        /// <summary>
        /// 更新设备总进度定时器回调函数
        /// </summary>
        /// <param name="state"></param>
        public void UpdateEquipTotalProcess(object state)
        {
            log.Write("==========执行更新设备总进度开始==========", MsgType.Success);
            string sql = "select sbh,zjd from zhizao_zongjindu_sum_v where zjd>0";

            try
            {
                DataTable dt = SQLServerHepler.ExecuteDataTable(SQLServerHepler.GetConnectionStringByConfig(), CommandType.Text, sql, null);

                foreach (DataRow g in dt.Rows)
                {
                    string equipNumber = g.Field<string>("sbh");
                    decimal totalProcess = g.Field<decimal>("zjd");

                    string mysqlString = "update equipment set totalprocess=@totalprocess where equipnumber=@equipnumber";
                    MySqlParameter[] parameters1 = { new MySqlParameter("totalprocess", totalProcess), new MySqlParameter("equipnumber", equipNumber) };
                    mysql.ExecuteSql(mysqlString, parameters1);


                }
            }catch(Exception ex)
            {
                log.Write("执行设备总进度更新失败," + ex.Message, MsgType.Error);
                log.Write(ex.StackTrace, MsgType.Error);
            }

            log.Write("==========执行更新设备总进度结束========", MsgType.Success);
        }

    }
}
