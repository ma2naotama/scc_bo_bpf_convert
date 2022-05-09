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
            ERROR_NONE = 0,
            ERROR_READMASTER,
            ERROR_MASTER_IS_NOTMATCH,
            ERROR_HEADER_IS_EMPTY,
            ERROR_BODY_IS_EMPTY,
            ERROR_BODY_IS_NOUSERDATA,
        };


        private static Dictionary<ERRORCOSE, string> ERRORMSG = new Dictionary<ERRORCOSE, string>(){
            {ERRORCOSE.ERROR_NONE,                  "ERROR_NONE"},
            {ERRORCOSE.ERROR_READMASTER,            "マスターファイルが読めませんでした。"},
            {ERRORCOSE.ERROR_MASTER_IS_NOTMATCH,    "マスターファイルが異常です。"},
            {ERRORCOSE.ERROR_HEADER_IS_EMPTY,       "受信ヘッダーが空です。"},
            {ERRORCOSE.ERROR_BODY_IS_EMPTY,         "受信データが空です。"},
            {ERRORCOSE.ERROR_BODY_IS_NOUSERDATA,    "受信データに該当ユーザーがいません{1}。"}
        };


        public static string GetErrorMsg(ERRORCOSE errorcode, params string[] args)
        {
            return string.Format(ERRORMSG[errorcode].ToString(), args);
        }
    }
}
