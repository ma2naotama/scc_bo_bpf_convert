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
        public static void Log(String logText)
        {
            FormMain main = FormMain.GetInstance();
            if(main == null)
            {
                Console.WriteLine(logText);
                return;
            }

            main.WriteLog(logText);
        }

    }
}
