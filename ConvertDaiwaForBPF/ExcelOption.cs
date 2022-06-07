
namespace ConvertDaiwaForBPF
{
    /// <summary>
    /// エクセルのシートの読み込み設定
    /// シート名や列位置や行位置を指定
    /// </summary>
    internal class ExcelOption
    {
        /// <summary>
        /// シート番号（どのシートに対するオプションなのか判断する為に使用する）
        /// </summary>
        public string SheetName { get; set; } = null;

        /// <summary>
        /// ヘッダーの開始行
        /// </summary>
        public int HeaderRowStartNumber { get; set; } = 0;

        /// <summary>
        /// ヘッダー列番号
        /// </summary>
        public int HeaderColumnStartNumber { get; set; } = 0;

        /// <summary>
        /// ヘッダー列の最後番号
        /// </summary>
        public int HeaderColumnEndNumber { get; set; } = 0;

        /// <summary>
        /// 取り出す行の開始位置
        /// </summary>
        public int DataRowStartNumber { get; set; } = 0;

        /// <summary>
        /// カラムの最大数
        /// </summary>
        const int COLUMN_MAX = 10000;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public ExcelOption()
        {
            // ヘッダーは1行名から開始
            HeaderRowStartNumber = 1;

            // ヘッダーは1カラムから開始
            HeaderColumnStartNumber = 1;

            // ヘッダーの最大カラム
            HeaderColumnEndNumber = COLUMN_MAX;

            // データはヘッダーの次の行から開始
            DataRowStartNumber = 2;
        }

        /// <summary>
        /// 読み込むシート名の設定
        /// </summary>
        /// <param name="sheetName">読み込むシート名</param>
        /// <param name="headerRowStartNumber">読み込み開始行</param>
        /// <param name="headerColumnStartNumber">読み込み開始列番</param>
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
        /// <returns>int 最大カラム数</returns>
        public int GetColumnMax()
        {
            return HeaderColumnEndNumber - HeaderColumnStartNumber;
        }
    }
}
