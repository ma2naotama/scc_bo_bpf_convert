using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConvertDaiwaForBPF
{
    /// <summary>
    /// エクセルのシートの読み込み設定
    /// シート名や列位置や行位置を指定
    /// </summary>
    public class ExcelOption
    {
        //シート番号（どのシートに対するオプションなのか判断する為に使用する）
        public string sheetName { get; set; }

        public bool isActive { get; set; }

        public int HeaderRowStartNumber { get; set; }

        public int HeaderColumnStartNumber { get; set; }
        public int HeaderColumnEndNumber { get; set; }

        //取り出す行の開始位置
        public int DataRowStartNumber { get; set; }

        public ExcelOption()
        {
            isActive = false;      //初期設定ではシートは読まない
            HeaderRowStartNumber = 1;
            HeaderColumnStartNumber = 1;
            HeaderColumnEndNumber = 10000;

            DataRowStartNumber = 2;
        }

        /// <summary>
        /// 読み込むシート名の設定
        /// </summary>
        /// <param name="sheetName"></param>
        /// <param name="headerRowStartNumber"></param>
        /// <param name="headerColumnStartNumber"></param>
        /// <param name="active"></param>
        public ExcelOption(string sheetName, int headerRowStartNumber, int headerColumnStartNumber, bool active)
        {
            this.sheetName = sheetName;

            HeaderRowStartNumber = headerRowStartNumber;
            HeaderColumnStartNumber = headerColumnStartNumber;
            HeaderColumnEndNumber = 10000;

            DataRowStartNumber = headerRowStartNumber + 1;

            isActive = active;
        }

        /// <summary>
        /// 最大カラム数の取得
        /// </summary>
        /// <returns></returns>
        public int GetColumnMax()
        {
            return HeaderColumnEndNumber - HeaderColumnStartNumber;
        }
    }
}
