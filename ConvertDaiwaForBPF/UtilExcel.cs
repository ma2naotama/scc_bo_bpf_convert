using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClosedXML.Excel;

namespace ConvertDaiwaForBPF
{
    /// <summary>
    /// エクセルファイルの読み込み
    /// </summary>
    internal class UtilExcel
    {
        /// <summary>
        /// エクセルオプションのリスト
        /// </summary>
        private List<ExcelOption> mExcelOption = new List<ExcelOption>();

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public UtilExcel()
        {
            var option = new ExcelOption();
            mExcelOption.Add(option);
        }


        /// <summary>
        /// オプション設定の登録
        /// </summary>
        /// <param name="option"></param>
        public void SetExcelOption(ExcelOption option)
        {
            // シート番号で検索
            var optindex = mExcelOption.FindIndex(x => x.SheetName == option.SheetName);
            if(optindex <0)
            {
                mExcelOption.Add(option);
                return;
            }

            // 書き換え
            mExcelOption[optindex] = option;
        }

        /// <summary>
        /// オプションの設定を登録
        /// </summary>
        /// <param name="options"></param>
        public void SetExcelOptionArray(ExcelOption []options)
        {
            foreach (ExcelOption opt in options)
            {
                SetExcelOption(opt);
            }
        }

        /// <summary>
        /// シート名からオプションの設定を取得
        /// </summary>
        /// <param name="sheetName"></param>シート名
        /// <returns>ExcelOption　未設定の場合は初期値を返す</returns>
        private ExcelOption GetExcelOption(string sheetName)
        {
            var option = mExcelOption.Find(x => x.SheetName == sheetName);
            if(option == null)
            {
                return new ExcelOption();
            }

            return option;

        }

        /// <summary>
        /// エクセルシートから１行分取り出す
        /// </summary>
        /// <param name="rowIndex">取得する行番号</param>
        /// <param name="columnStart">取得するカラムの開始番号</param>
        /// <param name="columnMax">取得するカラムの個数</param>
        /// <returns1行分の文字列のList</returns>
        private List<string> GetRow(IXLWorksheet worksheet, int rowIndex, int columnStart, int columnMax)
        {
            // 一旦リストに変換
            var data = new List<string>(); 

            for (int col = 0; col < columnMax; col++)
            {
                // 行、列の順に指定することで値を取得する
                var cell = worksheet.Cell(rowIndex, col + columnStart);
                if(cell == null)
                {
                    continue;
                }

                // 取得したデータをListに加える
                if (cell.CachedValue != null)
                { 
                    data.Add(cell.CachedValue.ToString());
                }
                else
                {
                    data.Add(cell.Value.ToString());
                }
            }

            // 1行分の文字列のList
            return data;
        }

        /// <summary>
        /// Excelの読み込み処理
        /// 必ずヘッダーがあるエクセルファイルを想定
        /// </summary>
        /// <param name="filepath">開くファイルのパス</param>
        /// <returns>全シート分のDataTable</returns>
        public DataSet ReadAllSheets(string path) 
        {
            var dataSet = new DataSet();

            try
            {

                // 既にエクセルが開いている場合でも読める様にする
                var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                // Excelファイルを開く
                // XLEventTracking.Disabled 追跡を無効
                using (var workbook = new XLWorkbook(fs, XLEventTracking.Disabled))
                {

                    for (var sheet =1; sheet <= workbook.Worksheets.Count; sheet++)
                    {
                        var sheeetname = workbook.Worksheets.Worksheet(sheet).Name;

                        // シートを選択する　シート名で取得する
                        var worksheet = workbook.Worksheet(sheeetname);

                        // 最大カラム数の確認
                        // 取得するセルの最大カラム番号（個数ではなく番号）
                        var columnNum = worksheet.LastColumnUsed().ColumnNumber();
                        if (columnNum < 0)
                        {
                            Dbg.ViewLog(Properties.Resources.E_EMPTY_SHEET, sheeetname);
                            continue;
                        }

                        // 取得するセルの最大行数
                        var RowsMax = worksheet.LastRowUsed().RowNumber();
                        if (RowsMax <=0)
                        {
                            Dbg.ViewLog(Properties.Resources.E_EMPTY_SHEET, sheeetname);
                            continue;
                        }

                        // シート名で検索
                        var option = GetExcelOption(sheeetname);
                        if (option != null)
                        {
                            if (columnNum > option.GetColumnMax())
                            {
                                columnNum = option.GetColumnMax();
                            }
                        }


                        var dt = new DataTable();

                        // シート名保存
                        dt.TableName = sheeetname;

                        // 最初の行
                        var row = GetRow(worksheet
                            , option.HeaderRowStartNumber
                            , option.HeaderColumnStartNumber
                            , columnNum);

                        for (var i = 0; i < columnNum; i++)
                        {
                            // カラム名を設定します。
                            dt.Columns.Add(row[i]);
                        }

                        dataSet.Tables.Add(dt);

                        // 実データの開始行から開始
                        for (var rownum = option.DataRowStartNumber; rownum <= RowsMax; rownum++)
                        {
                            row = GetRow(worksheet
                                , rownum
                                , option.HeaderColumnStartNumber
                                , columnNum);

                            var r = dt.NewRow();
                            for(var i = 0; i < row.Count; i++)
                            {
                                r[i] = row[i];
                            }

                            dt.Rows.Add(r);
                        }

                    }
                }
            }
            catch(Exception ex)
            {
                Dbg.Error(ex.ToString());
                throw ex;
            }

            // 全シート分のDataTableを返す
            return dataSet;
        }

    }
}

