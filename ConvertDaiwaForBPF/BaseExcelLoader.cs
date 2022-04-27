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

    public class ExcelOption
    {
        //シート番号（どのシートに対するオプションなのか判断する為に使用する）
        public string sheetName { get; set; }
        public int HeaderRowStartNumber { get; set; }

        public int HeaderColumnStartNumber { get; set; }
        public int HeaderColumnEndNumber { get; set; }

        //取り出す行の開始位置
        public int DataRowStartNumber { get; set; }

        public ExcelOption()
        {
            this.HeaderRowStartNumber = 1;
            this.HeaderColumnStartNumber = 1;
            this.HeaderColumnEndNumber = 10000;

            this.DataRowStartNumber = 2;
        }

        public ExcelOption(string sheetName, int headerRowStartNumber, int headerColumnStartNumber)
        {
            this.sheetName = sheetName;
            this.HeaderRowStartNumber = headerRowStartNumber;
            this.HeaderColumnStartNumber = headerColumnStartNumber;
            this.HeaderColumnEndNumber = 10000;

            this.DataRowStartNumber = headerRowStartNumber+1;
        }

        public int GetColumnMax()
        {
            return HeaderColumnEndNumber - HeaderColumnStartNumber;
        }
    }

    internal class BaseExcelLoader
    {
        //コールバックの定義
        public delegate void CallbackLoader(long processLength, List<string> colums);

        private bool mbCancel;


        private List<ExcelOption> mExcelOption = new List<ExcelOption>();

        public BaseExcelLoader()
        {
            mbCancel = false;

            ExcelOption option = new ExcelOption();
            mExcelOption.Add(option);
        }

        public void Cancel()
        {
            mbCancel = true;
        }


        public void SetExcelOption(ExcelOption option)
        {
            //シート番号で検索
            int optindex = mExcelOption.FindIndex(x => x.sheetName == option.sheetName);
            if(optindex <0)
            {
                mExcelOption.Add(option);
                return;
            }

            //書き換え
            mExcelOption[optindex] = option;
        }

        public void SetExcelOptionArray(ExcelOption []options)
        {
            foreach (ExcelOption opt in options)
            {
                SetExcelOption(opt);
            }
        }


        private ExcelOption GetExcelOption(string sheetName)
        {
            ExcelOption option =
                mExcelOption.Find(x => x.sheetName == sheetName);


            if(option == null)
            {
                //Dbg.Log("初期設定のオプション");
                return new ExcelOption();
            }

            return option;

        }

        /*
        /// <summary>
        /// Excelの読み込み処理
        /// </summary>
        /// <param name="filepath">開くファイルのパス</param>
        public void ReadSheet(string path, CallbackLoader callback, string sheetName = null)
        {
            //既にエクセルが開いている場合でも読める様にする
            FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            //Excelファイルを開く
            using (var workbook = new XLWorkbook(fs, XLEventTracking.Disabled))
            {

                Dbg.Log(""+workbook.Worksheets.Count);

                //シートを選択する　シート名で取得する
                var worksheet = workbook.Worksheet(sheetName);

                //取得するセルの最大行数
                int RowsMax = worksheet.LastRowUsed().RowNumber();

                //TODO:最大行数の確認は現在しない

                //最大カラム数の確認
                //取得するセルの最大カラム番号（個数ではなく番号）
                int columnNum = worksheet.LastColumnUsed().ColumnNumber();
                if(columnNum<0)
                {
                    Dbg.Log("データがありません。");
                    return;
                }

                //シート番号で検索
                ExcelOption option = GetExcelOption(sheetName);
                if(option != null)
                {
                    if(columnNum > option.GetColumnMax())
                    {
                        columnNum = option.GetColumnMax();
                    }
                }


                //取得するセルの列番号
                int processLinse = 0;

                //実データの開始行から開始
                for (int rownum = option.DataRowStartNumber; rownum <= RowsMax; rownum++)
                {
                    if (mbCancel)
                    {
                        Dbg.Log("cancel:" + path);
                        break;
                    }

                    var row = GetRow(worksheet, rownum, option.HeaderColumnStartNumber, columnNum);

                    callback(processLinse, row);

                    processLinse++;
                }

            }
        }

        */


        /// <summary>
        /// エクセルシートから１行分取り出す
        /// </summary>
        /// <param name="rowIndex">取得する行番号</param>
        /// <param name="columnStart">取得するカラムの開始番号</param>
        /// <param name="columnMax">取得するカラムの個数</param>
        /// <returns1行分の文字列のList</returns>
        private List<string> GetRow(IXLWorksheet worksheet, int rowIndex, int columnStart, int columnMax)
        {
            //一旦リストに変換
            var data = new List<string>(); 

            for (int col = 0; col < columnMax; col++)
            {
                //行、列の順に指定することで値を取得する
                var cell = worksheet.Cell(rowIndex, col + columnStart);
                if(cell == null)
                {
                    continue;
                }

                //取得したデータをListに加える
                if (cell.CachedValue != null)
                { 
                    data.Add(cell.CachedValue.ToString());
                }
                else
                {
                    data.Add(cell.Value.ToString());
                }
            }

            //1行分の文字列のList
            return data;
        }

        //データベース以外にも、Excelで言うと
        //DataSet   = エクセルのBook
        //DataTable = エクセルのシート
        //DataRow   = エクセルシートの1行

        /// <summary>
        /// Excelの読み込み処理
        /// 必ずヘッダーがあるエクセルファイルを想定
        /// </summary>
        /// <param name="filepath">開くファイルのパス</param>
        /// <returns>全シート分のDataTable</returns>
        public Dictionary<string, DataTable> ReadAllSheets(string path)
        {
            DataSet dataSet = new DataSet();
            Dictionary<string, DataTable> dataTables = new Dictionary<string, DataTable>();

            //既にエクセルが開いている場合でも読める様にする
            FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            //Excelファイルを開く
            using (var workbook = new XLWorkbook(fs, XLEventTracking.Disabled))
            {

                //Dbg.Log("sheeet count:" + workbook.Worksheets.Count);

                for(int sheet =1; sheet <= workbook.Worksheets.Count; sheet++)
                {
                    string sheeetname = workbook.Worksheets.Worksheet(sheet).Name;

                    //Dbg.Log("load sheeet:" + sheeetname);

                    //シートを選択する　シート名で取得する
                    var worksheet = workbook.Worksheet(sheeetname);

                    //最大カラム数の確認
                    //取得するセルの最大カラム番号（個数ではなく番号）
                    int columnNum = worksheet.LastColumnUsed().ColumnNumber();
                    if (columnNum < 0)
                    {
                        Dbg.Log("データがありません。sheeetname:" + sheeetname);
                        break;
                    }

                    //取得するセルの最大行数
                    int RowsMax = worksheet.LastRowUsed().RowNumber();
                    if (RowsMax <=0)
                    {
                        Dbg.Log("データがありません。sheeetname:" + sheeetname);
                        break;
                    }

                    //シート番号で検索
                    ExcelOption option = GetExcelOption(sheeetname);
                    if (option != null)
                    {
                        if (columnNum > option.GetColumnMax())
                        {
                            columnNum = option.GetColumnMax();
                        }
                    }


                    DataTable dt = new DataTable();

                    //シート名保存
                    dt.TableName = sheeetname;

                    //最初の行
                    var row = GetRow(worksheet
                        , option.HeaderRowStartNumber
                        , option.HeaderColumnStartNumber
                        , columnNum);

                    for (int i = 0; i < columnNum; i++)
                    {
                        //カラム名を設定します。
                        dt.Columns.Add(row[i]);
                    }

                    dataSet.Tables.Add(dt);

                    //実データの開始行から開始
                    for (int rownum = option.DataRowStartNumber; rownum <= RowsMax; rownum++)
                    {
                        if (mbCancel)
                        {
                            Dbg.Log("cancel:" + path);
                            break;
                        }

                        row = GetRow(worksheet
                            , rownum
                            , option.HeaderColumnStartNumber
                            , columnNum);


                        DataRow r = dt.NewRow();

                        for(int i = 0; i < row.Count; i++)
                        {
                            r[i] = row[i];
                        }

                        dt.Rows.Add(r);
                    }

                    //１シート分を保存
                    dataTables.Add(sheeetname, dt);
                }
            }

            //全シート分のDataTableを返す
            return dataTables;
        }


        //CSVの最大行数を取得する
        public long GetFileMaxLines(string path)
        {
            int lines =0;

            //既にエクセルが開いている場合でも読める様にする
            FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            //Excelファイルを開く
            using (var workbook = new XLWorkbook(fs, XLEventTracking.Disabled))
            {
                var worksheet = workbook.Worksheet(1);
                lines = worksheet.LastRowUsed().RowNumber();
            }

            Dbg.Log("GetFileMaxLines:" + lines);
            return lines;
        }

    }
}
