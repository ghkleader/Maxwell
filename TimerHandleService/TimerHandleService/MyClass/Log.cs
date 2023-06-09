﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
//using System.Windows.Forms;

namespace EasyLog
{
    /// <summary>
    /// 表示一个日志记录的对象
    /// </summary>
    public class Msg
    {
        //日志记录的时间
        private DateTime datetime;
        //日志记录的内容
        private string text;
        //日志记录的类型
        private MsgType type;

        /// <summary>
        /// 创建新的日志记录实例;日志记录的内容为空,消息类型为MsgType.Unknown,日志时间为当前时间
        /// </summary>
        public Msg()
            : this("", MsgType.System)
        {
        }

        /// <summary>
        /// 创建新的日志记录实例;日志事件为当前时间
        /// </summary>
        /// <param name="t">日志记录的文本内容</param>
        /// <param name="p">日志记录的消息类型</param>
        public Msg(string t, MsgType p)
            : this(DateTime.Now, t, p)
        {
        }

        /// <summary>
        /// 创建新的日志记录实例;
        /// </summary>
        /// <param name="dt">日志记录的时间</param>
        /// <param name="t">日志记录的文本内容</param>
        /// <param name="p">日志记录的消息类型</param>
        public Msg(DateTime dt, string t, MsgType p)
        {
            datetime = dt;
            type = p;
            text = t;
        }

        /// <summary>
        /// 获取或设置日志记录的时间
        /// </summary>
        public DateTime Datetime
        {
            get { return datetime; }
            set { datetime = value; }
        }

        /// <summary>
        /// 获取或设置日志记录的文本内容
        /// </summary>
        public string Text
        {
            get { return text; }
            set { text = value; }
        }

        /// <summary>
        /// 获取或设置日志记录的消息类型
        /// </summary>
        public MsgType Type
        {
            get { return type; }
            set { type = value; }
        }

        public new string ToString()
        {
            return datetime.ToString("yyyy-MM-dd HH:mm:ss") + "\t" + text + "\n";
        }
    }

    /// <summary>
    /// 日志消息类型的枚举
    /// </summary>
    public enum MsgType
    {
        /// <summary>
        /// 指示未知信息类型的日志记录
        /// </summary>
        Sfc,

        /// <summary>
        /// 指示普通信息类型的日志记录
        /// </summary>
        System,

        /// <summary>
        /// 指示警告信息类型的日志记录
        /// </summary>
        CCD,

        /// <summary>
        /// 指示错误信息类型的日志记录
        /// </summary>
        Error,

        /// <summary>
        /// 指示成功信息类型的日志记录
        /// </summary>
        Success,
        /// <summary>
        /// 与MACmini连接的日志记录
        /// </summary>
        Socket,

        MES,

        WaferCode,

        RFID,
        AGV,
        AGV1,
        AGV2,

        TerminalDisplay,

        RemoteCmd,
    }



    /// <summary>
    /// 日志类型的枚举
    /// </summary>
    /// <remarks>日志类型枚举指示日志文件创建的方式，如果日志比较多可考虑每天创建一个日志文件
    /// 如果日志量比较小可考虑每周、每月或每年创建一个日志文件</remarks>
    public enum LogType
    {
        /// <summary>
        /// 此枚举指示每天创建一个新的日志文件
        /// </summary>
        Daily,

        /// <summary>
        /// 此枚举指示每周创建一个新的日志文件
        /// </summary>
        Weekly,

        /// <summary>
        /// 此枚举指示每月创建一个新的日志文件
        /// </summary>
        Monthly,

        /// <summary>
        /// 此枚举指示每年创建一个新的日志文件
        /// </summary>
        Annually
    }


    public class Log : IDisposable
    {
        private static Log _log = new Log("D:\\MESLog\\", LogType.Daily);
        //日志对象的缓存队列
        private static Queue<Msg> msgs;
        //日志文件保存的路径
        private static string path;
        //日志写入线程的控制标记
        private static bool state;
        //日志记录的类型
        private static LogType type;
        //日志文件生命周期的时间标记
        private static DateTime TimeSign;
        //日志文件写入流对象
        private static StreamWriter writer;
        private static bool m_bExit = false;
        /// <summary>
        /// 创建日志对象的新实例，采用默认当前程序位置作为日志路径和默认的每日日志文件类型记录日志
        /// </summary>
        private Log()
            : this("D:\\DRLaser\\Log\\", LogType.Daily)
        {
            m_bExit = false;
        }

        /// <summary>
        /// 创建日志对象的新实例，采用默认当前程序位置作为日志路径并指定日志类型
        /// </summary>
        /// <param name="t">日志文件创建方式的枚举</param>
        private Log(LogType t)
            : this("D:\\DRLaser\\Log\\", t)
        {
        }

        /// <summary>
        /// 创建日志对象的新实例，根据指定的日志文件路径和指定的日志文件创建类型
        /// </summary>
        /// <param name="p">日志文件保存路径</param>
        /// <param name="t">日志文件创建方式的枚举</param>
        private Log(string p, LogType t)
        {
            if (msgs == null)
            {
               
                state = true;
                path = p;
                type = t;
                msgs = new Queue<Msg>();
                Thread thread = new Thread(work);
                thread.Start();
            }
        }

        public static Log getInstance()
        {
            return _log;
        }

        //日志文件写入线程执行的方法
        private void work()
        {
            while (!m_bExit)
            {
                //判断队列中是否存在待写入的日志
                if (msgs.Count > 0)
                {
                    Msg msg = null;
                    lock (msgs)
                    {
                        msg = msgs.Dequeue();
                    }
                    if (msg != null)
                    {
                        FileWrite(msg);
                    }
                }
                else
                {
                    //判断是否已经发出终止日志并关闭的消息
                    if (state)
                    {
                        Thread.Sleep(1);
                    }
                    else
                    {
                        FileClose();
                    }
                }
            }
        }

        //根据日志类型获取日志文件名，并同时创建文件到期的时间标记
        //通过判断文件的到期时间标记将决定是否创建新文件。
        private string GetFilename()
        {
            DateTime now = DateTime.Now;
            string format = "";
            switch (type)
            {
                case LogType.Daily:
                    TimeSign = new DateTime(now.Year, now.Month, now.Day);
                    TimeSign = TimeSign.AddDays(1);
                    format = "yyyy-MM-dd'.log'";
                    break;
                case LogType.Weekly:
                    TimeSign = new DateTime(now.Year, now.Month, now.Day);
                    TimeSign = TimeSign.AddDays(7);
                    format = "yyyy-MM-dd'.log'";
                    break;
                case LogType.Monthly:
                    TimeSign = new DateTime(now.Year, now.Month, 1);
                    TimeSign = TimeSign.AddMonths(1);
                    format = "yyyy-MM'.log'";
                    break;
                case LogType.Annually:
                    TimeSign = new DateTime(now.Year, 1, 1);
                    TimeSign = TimeSign.AddYears(1);
                    format = "yyyy'.log'";
                    break;
            }
            return now.ToString(format);
        }

        //写入日志文本到文件的方法
        private void FileWrite(Msg msg)
        {

            string strPath = path + msg.Type+"\\";
            try
            {

                DirectoryInfo di = new DirectoryInfo(strPath);               
                if (!di.Exists)
                {

                    Directory.CreateDirectory(strPath);
                }
               
                FileOpen(strPath);          
                writer.Write(msg.Datetime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                writer.Write("   ");           
                
                writer.WriteLine(msg.Text);
                writer.Flush();                        
                FileClose();

            }
            catch (Exception e)
            {
                Console.Out.Write(e);
            }
        }

        //打开文件准备写入
        private void FileOpen(string strpath)
        {
            writer = new StreamWriter(strpath + GetFilename(), true, Encoding.UTF8);
        }

        //关闭打开的日志文件
        private void FileClose()
        {
            if (writer != null)
            {
                writer.Flush();
                writer.Close();
                writer.Dispose();
                writer = null;
            }
        }



        /// <summary>
        /// 写入新日志，根据指定的日志对象Msg
        /// </summary>
        /// <param name="msg">日志内容对象</param>
        public void Write(Msg msg)
        {
            if (msg != null)
            {
                lock (msgs)
                {
                    msgs.Enqueue(msg);
                }
            }
        }

        /// <summary>
        /// 写入新日志，根据指定的日志内容和信息类型，采用当前时间为日志时间写入新日志
        /// </summary>
        /// <param name="text">日志内容</param>
        /// <param name="type">信息类型</param>
        public void Write(string text, MsgType type = MsgType.System)
        {
            Write(new Msg(text, type));
        }

        /// <summary>
        /// 写入新日志，根据指定的日志时间、日志内容和信息类型写入新日志
        /// </summary>
        /// <param name="dt">日志时间</param>
        /// <param name="text">日志内容</param>
        /// <param name="type">信息类型</param>
        public void Write(DateTime dt, string text, MsgType type)
        {
            Write(new Msg(dt, text, type));
        }


        /// <summary>
        /// 写入新日志，根据指定的异常类和信息类型写入新日志
        /// </summary>
        /// <param name="e">异常对象</param>
        /// <param name="type">信息类型</param>
        public void Write(Exception e, MsgType type)
        {
            Write(new Msg(e.Message, type));
        }



        #region IDisposable 成员

        /// <summary>
        /// 销毁日志对象
        /// </summary>
        public void Dispose()
        {
            state = false;
        }
        

        #endregion

        public void DeleteFile(string fileDirect, int saveDay, string endWith)
        {
            try
            {
                DateTime nowTime = DateTime.Now;
                string[] files = Directory.GetFiles(fileDirect, "*."+ endWith , SearchOption.AllDirectories);  //获取该目录下所有 .txt文件
                foreach (string file in files)
                {
                    FileInfo fileInfo = new FileInfo(file);
                    TimeSpan t = DateTime.Now - fileInfo.CreationTime;  //当前时间  减去 文件创建时间
                    int day = t.Days;
                    if (day > saveDay && (string.IsNullOrEmpty(endWith) || fileInfo.Name.EndsWith(endWith)))   //保存的时间，单位：天
                    {
                            try 
	                        {	        
		                        System.IO.File.Delete(fileInfo.FullName); //删除文件
	                        }
	                        catch (Exception)
	                        {
		                        this.Write("定时删除文件失败！", MsgType.AGV);
	                        }                                                  
                    }
                }
            }
            catch (Exception )
            {

            }
        }      
    }
}
