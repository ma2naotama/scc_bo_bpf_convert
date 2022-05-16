using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConvertDaiwaForBPF
{
    public class GlobalVariables
    {
        public enum ENCORDTYPE
        {
            SJIS,
            UTF8
        };

        public enum ERRORCODE
        {
            READFAILED_MASTER  = 101,
            READFAILED_HDR,
            READFAILED_TDL,
            HDR_IS_EMPTY,
            TDL_IS_EMPTY,
            MERGED_DATA_IS_EMPTY,
            NO_USERDATA,
            NO_TDLDATA,
            DUPLICATE_USER,
        };


        private static Dictionary<ERRORCODE, string> ERRORMSG = new Dictionary<ERRORCODE, string>()
        {
            {ERRORCODE.READFAILED_MASTER,           "設定ファイルが読めませんでした。"},
            {ERRORCODE.READFAILED_HDR,              "健診ヘッダーが読めませんでした。"},
            {ERRORCODE.READFAILED_TDL,              "健診データが読めませんでした。"},
            {ERRORCODE.HDR_IS_EMPTY,                "健診ヘッダーが空です。"},
            {ERRORCODE.TDL_IS_EMPTY,                "健診データが空です。"},
            {ERRORCODE.MERGED_DATA_IS_EMPTY,        "結合したデータが空です。"},
            {ERRORCODE.NO_USERDATA,                 "人事データに該当ユーザーがいません。個人番号：{1}"},
            {ERRORCODE.NO_TDLDATA,                  "健診データに該当ユーザーがいません。個人番号：{1}"},
            {ERRORCODE.DUPLICATE_USER,              "健診データが重複しています。個人番号：{1},受信日：{2}"}
        };


        public static string GetErrorMsg(ERRORCODE errorcode, params string[] args)
        {
            return string.Format(ERRORMSG[errorcode].ToString(), args);
        }
    }
}
