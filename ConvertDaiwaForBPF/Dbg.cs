using ConvertDaiwaForBPF;
using System;
using System.Collections.Generic;
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


        //ログ画面への表示のみ
        public static void ViewLog(String msg, params string[] args)
        {
            string logText = string.Format(msg, args);

            FormMain main = FormMain.GetInstance();
            if(main == null)
            {
                Console.WriteLine(logText);
                return;
            }

            main.ViewLog(logText);
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
        public static void ErrorWithView(string errormsg, params string[] args)
        {
            string logText = string.Format(errormsg, args);

            ViewLog(logText);

            _logger.Error(logText);
        }

    }
}
