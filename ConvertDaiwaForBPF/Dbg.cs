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


        public static void Log(String msg, params string[] args)
        {
            string logText = string.Format(msg, args);

            FormMain main = FormMain.GetInstance();
            if(main == null)
            {
                Console.WriteLine(logText);
                return;
            }

            main.WriteLog(logText);
        }


        public static void Error(String logText)
        {
            _logger.Error(logText);
        }

        public static void Debug(String logText)
        {
            _logger.Debug(logText);
        }

        public static void Info(String logText)
        {
            _logger.Info(logText);
        }

        public static void ErrorLog(string errormsg, params string[] args)
        {
            string logText = string.Format(errormsg, args);

            Log(logText);

            _logger.Error(logText);
        }

    }
}
