using ConvertDaiwaForBPF;
using log4net.Appender;
using log4net.Repository.Hierarchy;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConvertDaiwaForBPF
{
    //ターミナル出力
    internal class Dbg
    {
        private static readonly log4net.ILog _logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public Dbg()
        {

        }

        public static void SetLogPath(string path)
        {
            var rootLogger = ((Hierarchy)_logger.Logger.Repository).Root;

            FileAppender appender = rootLogger.GetAppender("logFileAbc") as FileAppender; //

            string filename = Path.GetFileName(appender.File);

            //DateTime dt = DateTime.Now;
            //var datetime = "\\" + String.Format("log-{0}.log", dt.ToString("yyyyMMdd_HHmmss"));       // デフォルトファイル名
            
            // 出力先フォルダとログファイル名をC#で変更したい
            appender.File = path +"\\"+ filename;
            appender.ActivateOptions();
        }

        //ログ画面への表示のみ
        private static void _ViewLog(String msg, params string[] args)
        {
            string logText = string.Format(msg, args);

            FormMain main = FormMain.GetInstance();
            if (main == null)
            {
                Console.WriteLine(logText);
                return;
            }

            main.ViewLog(logText);
        }

        public static void ViewLog(String msg, params string[] args)
        {
            _ViewLog(msg, args);
            Debug(msg, args);
        }


        //error log への書き出し
        public static void Error(String msg, params string[] args)
        {
            string logText = string.Format(msg, args);
            _logger.Error(logText);
        }

        //debug log への書き出し
        public static void Debug(String msg, params string[] args)
        {
            string logText = string.Format(msg, args);
            _logger.Debug(logText);
        }

        //info log ファイルへの書き出し
        public static void Info(String msg, params string[] args)
        {
            string logText = string.Format(msg, args);
            _logger.Info(logText);
        }

        //warning log ファイルへの書き出し
        public static void Warn(String msg, params string[] args)
        {
            string logText = string.Format(msg, args);
            _logger.Warn(logText);
        }

        //ログ画面への表示とerror log ファイルへの書き出し
        public static void ErrorWithView(string errormsg = null, params string[] args)
        {
            /*
            if(resourcename!=null)
            { 
                System.Resources.ResourceManager resource = Properties.Resources.ResourceManager;

                errormsg = resource.GetString(resourcename);
            }
            */

            _ViewLog(string.Format(errormsg, args));

            var stackFrames = new StackTrace().GetFrames();
            var callingframe = stackFrames.ElementAt(1);

            var method = callingframe.GetMethod().Name;

            string logText = string.Format("[" + method +"]"+ errormsg, args);

            _logger.Error(logText);
        }

    }
}
