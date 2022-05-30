
namespace ConvertDaiwaForBPF
{
    /// <summary>
    /// エクセルのシートの読み込み設定
    /// シート名や列位置や行位置を指定
    /// </summary>
    public class ExcelOption
    {
        /// <summary>
        /// シート番号（どのシートに対するオプションなのか判断する為に使用する）
        /// </summary>
        public string SheetName { get; set; }

        /// <summary>
        /// ヘッダーの開始行
        /// </summary>
        public int HeaderRowStartNumber { get; set; }

        /// <summary>
        /// ヘッダー列番号
        /// </summary>
        public int HeaderColumnStartNumber { get; set; }

        /// <summary>
        /// ヘッダー列の最後番号
        /// </summary>
        public int HeaderColumnEndNumber { get; set; }

        /// <summary>
        /// 取り出す行の開始位置
        /// </summary>
        public int DataRowStartNumber { get; set; }

        /// <summary>
        /// カラムの最大数
        /// </summary>
        const int COLUMN_MAX = 10000;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public ExcelOption()
        {
            HeaderRowStartNumber = 1;           // ヘッダーは1行名から開始
            HeaderColumnStartNumber = 1;        // ヘッダーは1カラムから開始
            HeaderColumnEndNumber = COLUMN_MAX; // ヘッダーの最大カラム
            DataRowStartNumber = 2;             // データはヘッダーの次の行から開始
        }

        /// <summary>
        /// 読み込むシート名の設定
        /// </summary>
        /// <param name="sheetName"></param>
        /// <param name="headerRowStartNumber"></param>
        /// <param name="headerColumnStartNumber"></param>
        public ExcelOption(string sheetName, int headerRowStartNumber, int headerColumnStartNumber)
        {
            SheetName = sheetName;

            HeaderRowStartNumber = headerRowStartNumber;
            HeaderColumnStartNumber = headerColumnStartNumber;
            HeaderColumnEndNumber = COLUMN_MAX;

            DataRowStartNumber = headerRowStartNumber + 1;
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
