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
    /// <summary>
    /// ログ画面の出力とログファイルへの書き出し
    /// </summary>
    internal class Dbg
    {
        /// <summary>
        /// lo4netのロガー
        /// </summary>
        private static readonly log4net.ILog _logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public Dbg()
        {

        }

        /// <summary>
        /// ログファイルのパスの設定
        /// </summary>
        /// <param name="path"></param>
        public static void SetLogPath(string path)
        {
            var rootLogger = ((Hierarchy)_logger.Logger.Repository).Root;

            var appender = rootLogger.GetAppender("logFileAbc") as FileAppender; //

            //string filename = Path.GetFileName(appender.File);

            var dt = DateTime.Now;
            var datetime = String.Format("log-{0}.log", dt.ToString("yyyyMMdd_HHmmss"));       // デフォルトファイル名
            
            // 出力先フォルダとログファイル名をC#で変更したい
            appender.File = path +"\\"+ datetime;
            appender.ActivateOptions();
        }

        /// <summary>
        /// ログ画面の表示（表示のみ）
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="args"></param>
        private static void _ViewLog(String msg, params string[] args)
        {
            var logText = string.Format(msg, args);
            var main = FormMain.GetInstance();
            if (main == null)
            {
                Console.WriteLine(logText);
                return;
            }

            main.ViewLog(logText);
        }

        /// <summary>
        /// ログ画面の表示とログファイルへの書き出し
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="args"></param>
        public static void ViewLog(String msg, params string[] args)
        {
            _ViewLog(msg, args);
            Debug(msg, args);
        }


        /// <summary>
        /// errorとして ログファイルへの書き出し
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="args"></param>
        public static void Error(String msg, params string[] args)
        {
            var logText = string.Format(msg, args);
            _logger.Error(logText);
        }


        /// <summary>
        /// debugとして ログファイルへの書き出し
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="args"></param>
        public static void Debug(String msg, params string[] args)
        {
            var logText = string.Format(msg, args);
            _logger.Debug(logText);
        }


        /// <summary>
        /// infoとして log ファイルへの書き出し
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="args"></param>
        public static void Info(String msg, params string[] args)
        {
            var logText = string.Format(msg, args);
            _logger.Info(logText);
        }

        /// <summary>
        /// warningとして log ファイルへの書き出し
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="args"></param>
        public static void Warn(String msg, params string[] args)
        {
            var logText = string.Format(msg, args);
            _logger.Warn(logText);
        }

        /// <summary>
        /// ログ画面への表示とerror log ファイルへの書き出し
        /// </summary>
        /// <param name="errormsg"></param>
        /// <param name="args"></param>
        public static void ErrorWithView(string errormsg = null, params string[] args)
        {
            _ViewLog(string.Format(errormsg, args));

            var stackFrames = new StackTrace().GetFrames();
            var callingframe = stackFrames.ElementAt(1);

            var method = callingframe.GetMethod().Name;

            var logText = string.Format("[" + method +"]"+ errormsg, args);

            _logger.Error(logText);
        }

    }
}
