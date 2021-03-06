using log4net.Appender;
using log4net.Repository.Hierarchy;
using System;
using System.Diagnostics;
using System.Linq;

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

        /// <summary>
        /// ログファイルのパスの設定
        /// </summary>
        /// <param name="path"></param>
        public static void SetLogPath(string path)
        {
            var rootLogger = ((Hierarchy)_logger.Logger.Repository).Root;

            var appender = rootLogger.GetAppender("logFileAbc") as FileAppender;

            var dt = DateTime.Now;

            var datetime = String.Format("log-{0}.log", dt.ToString("yyyyMMdd_HHmmss"));       // デフォルトファイル名

            // 出力先フォルダとログファイル名をC#で変更したい
            appender.File = path + "\\" + datetime;

            appender.ActivateOptions();
        }

        /// <summary>
        /// ログ画面の表示（表示のみ）
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="args"></param>
        private static void BaseViewLog(String msg, params string[] args)
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
            BaseViewLog(msg, args);

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
        ///  ログ画面への表示とwarningとして log ファイルへの書き出し
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="args"></param>
        public static void WarnWithView(String msg, params string[] args)
        {
            var logText = string.Format(msg, args);

            BaseViewLog(logText);

            _logger.Warn(logText);
        }

        /// <summary>
        /// ログ画面への表示とerrorとして log ファイルへの書き出し
        /// </summary>
        /// <param name="errormsg"></param>
        /// <param name="args"></param>
        public static void ErrorWithView(string errormsg = null, params string[] args)
        {
            BaseViewLog(string.Format(errormsg, args));

            var stackFrames = new StackTrace().GetFrames();

            var callingframe = stackFrames.ElementAt(1);

            var method = callingframe.GetMethod().Name;

            var logText = string.Format("[" + method + "]" + errormsg, args);

            _logger.Error(logText);
        }
    }
}
