using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ConvertDaiwaForBPF
{
    internal class UtilCsv
    {
        //コールバックの定義
        public delegate void CallbackCsvLoader(long processLength, List<string> colums); 

        private bool mbCancel;

        public UtilCsv()
        {
            mbCancel = false;
        }

        public void Cancel()
        {
            mbCancel = true;
        }


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
            try
            { 
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
                    Dbg.Log("カラム数:" + n);
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
                    dt.NewRow();
                    dt.Rows.Add(row);

                    // ファイルの終端までループ
                    while (!parser.EndOfData)
                    {
                        if (mbCancel)
                        {
                            Dbg.Log("cancel:" + path);
                            dt.Clear();
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
            }
            catch (Exception ex)
            {
                Dbg.Log(ex.ToString());
                throw ex;
            }

            Dbg.Log("dt.Rows.Count:"+dt.Rows.Count);
            return dt;
        }


        //CSVの最大行数を取得する
        public long GetFileMaxLines(string path)
        {
            int lines = File.ReadLines(path).Count();

            Dbg.Log("GetFileMaxLines:" + lines);
            return lines;
        }


        public void WriteFile(string path, DataTable dt, List<string>overwriteColumnName = null)
        {

            try
            {
                if (dt != null && dt.Rows.Count > 0)
                {
                    StringBuilder sb = new StringBuilder();

                    string[] columnNames = dt.Columns.Cast<DataColumn>().Select(column => column.ColumnName).ToArray();

                    if (overwriteColumnName != null)
                    {
                        try
                        {
                            if (columnNames.Length != overwriteColumnName.Count)
                            {
                                throw new MyException("ヘッダーの数が合っていません。");
                            }
                        }
                        catch (Exception ex)
                        {
                           Dbg.Error(ex.ToString());
                        }

                        sb.AppendLine(string.Join(",", overwriteColumnName));
                    }
                    else
                    { 
                        sb.AppendLine(string.Join(",", columnNames));
                    }

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
                Dbg.Error(ex.ToString());
                throw ex;
            }
        }



        public DataTable CreateDataTable(IEnumerable source)
        {
            var table = new DataTable();
            int index = 0;
            var properties = new List<PropertyInfo>();
            foreach (var obj in source)
            {
                if (index == 0)
                {
                    foreach (var property in obj.GetType().GetProperties())
                    {
                        if (Nullable.GetUnderlyingType(property.PropertyType) != null)
                        {
                            continue;
                        }
                        properties.Add(property);
                        table.Columns.Add(new DataColumn(property.Name, property.PropertyType));
                    }
                }
                object[] values = new object[properties.Count];
                for (int i = 0; i < properties.Count; i++)
                {
                    values[i] = properties[i].GetValue(obj);
                }
                table.Rows.Add(values);
                index++;
            }
            return table;
        }

    }
}
