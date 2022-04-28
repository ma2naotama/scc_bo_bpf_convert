using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConvertDaiwaForBPF
{
    internal class UtilCsv
    {
        //コールバックの定義
        //public delegate void CallbackCsvLoader(long processLength, List<string> colums); 

        private bool mbCancel;

        public UtilCsv()
        {
            mbCancel = false;
        }

        public void Cancel()
        {
            mbCancel = true;
        }

        //先頭の一行はヘッダーとして格納
        //private List<string> mCsvHeader = new List<string>();

        /// <summary>
        /// CSVファイルから読み込み(クォートで囲まれたカラムも対応)
        /// </summary>
        /// <param name="path">ファイルパス</param>
        /// <param name="delimiters">区切り文字</param>
        /// <param name="encoding">エンコード指定。Encoding.GetEncoding("shift-jis")等</param>
        public DataTable ReadFile(string path, string delimiters = ",", GlobalVariables.ENCORDTYE encode = GlobalVariables.ENCORDTYE.UTF8)
        {
            Encoding encoding;

            //	指定が無ければUTF-8
            if (encode == GlobalVariables.ENCORDTYE.SJIS)
            {
                encoding = Encoding.GetEncoding("Shift_JIS");
            }
            else
            {
                //UTF8
                encoding = Encoding.GetEncoding("utf-8");
            }

            DataTable dt = null;

            //	パース開始
            var parser = new TextFieldParser(path, encoding);
            using (parser)
            {
                //  区切りの指定
                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(delimiters);

                // フィールドが引用符で囲まれているか
                parser.HasFieldsEnclosedInQuotes = true;

                // フィールドの空白トリム設定
                parser.TrimWhiteSpace = false;

                //一旦リストに変換
                if(parser.EndOfData)
                {
                    Dbg.Log("データがありません。"+ path);
                    return null;
                }

                var row = parser.ReadFields();

                //シート名保存
                string fileName = Path.GetFileName(path);
                Dbg.Log("fileName:" + fileName);
                dt = new DataTable();

                dt.TableName = fileName;

                int n = row.Count();    //カラム数取得
                for (int i = 0; i < n; i++)
                {
                    //仮のカラム名を設定します。
                    dt.Columns.Add(""+(i+1));       //1始まり
                }

                DataSet dataSet = new DataSet();
                dataSet.Tables.Add(dt);

                //var v = new List<string>();
                //v.Clear();
                //v.AddRange(row);
                dt.Rows.Add(row);

                // ファイルの終端までループ
                while (!parser.EndOfData)
                {
                    if (mbCancel)
                    {
                        Dbg.Log("cancel:" + path);
                        break;
                    }

                    //一旦リストに変換
                    row = parser.ReadFields();

                    //v.Clear();
                    //v.AddRange(row);
                    //callback(processLinse, v);

                    dt.NewRow();
                    dt.Rows.Add(row);
                }
            }

            return dt;
        }

        //CSVの最大行数を取得する
        public long GetFileMaxLines(string path)
        {
            int lines = File.ReadLines(path).Count();

            Dbg.Log("GetFileMaxLines:" + lines);
            return lines;
        }


        void WriteFile(string path, DataTable dt)
        {

            try
            {
                if (dt != null && dt.Rows.Count > 0)
                {
                    StringBuilder sb = new StringBuilder();

                    string[] columnNames = dt.Columns.Cast<DataColumn>().Select(column => column.ColumnName).ToArray();

                    sb.AppendLine(string.Join(",", columnNames));

                    foreach (DataRow row in dt.Rows)
                    {
                        //IEnumerable<string> fields = row.ItemArray.Select(field => string.Concat("\"", field.ToString().Replace("\"", "\"\""), "\""));

                        //カンマ付きの文字列は、全体をダブルクォーテーションで囲む
                        IEnumerable<string> fields = row.ItemArray.Select(field => {
                            if (field.ToString().IndexOf(',') > 0)
                            {
                                return string.Concat("\"", field.ToString().Replace("\"", "\"\""), "\"");
                            }
                            return field.ToString();
                        });

                        sb.AppendLine(string.Join(",", fields));
                    }

                    //ファイルを別アプリで開いている場合はエラーになる
                    File.WriteAllText(path, sb.ToString());
                }
            }
            catch (Exception ex)
            {
                Dbg.FileLog(ex.ToString());
                throw ex;
            }
        }



    }
}
