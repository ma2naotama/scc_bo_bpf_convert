using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConvertDaiwaForBPF
{
    public class GlobalVariables
    {
        public enum ENCORDTYE
        {
            SJIS,
            UTF8
        };

        public enum ERRORCOSE
        {
            ERROR_NONE = 100,
            ERROR_HEADER_IS_EMPTY,
            ERROR_BODY_IS_EMPTY,
        };


        Dictionary<ERRORCOSE, string> ERRORMSG = new Dictionary<ERRORCOSE, string>(){
            {ERRORCOSE.ERROR_NONE,              "ERROR_NONE"},
            {ERRORCOSE.ERROR_HEADER_IS_EMPTY,   "受信ヘッダーが空です。"},
            {ERRORCOSE.ERROR_BODY_IS_EMPTY,     "受信データが空です。"}
        };


        string GetErrorMsg(ERRORCOSE errorcode)
        {
            return ERRORMSG[errorcode].ToString();
        }
    }
}
