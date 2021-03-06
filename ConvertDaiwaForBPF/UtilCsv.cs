using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ConvertDaiwaForBPF
{
    /// <summary>
    /// CSVファイルの読み込み
    /// </summary>
    internal class UtilCsv
    {
        /// <summary>
        /// CSVファイルから読み込み(クォートで囲まれたカラムも対応)
        /// </summary>
        /// <param name="path">ファイルパス</param>
        /// <param name="delimiters">区切り文字</param>
        /// <param name="encoding">エンコード指定。Encoding.GetEncoding("shift-jis")等</param>
        public DataTable ReadFile(string path, string delimiters = ",", bool hasheader = true, GlobalVariables.ENCORDTYPE encode = GlobalVariables.ENCORDTYPE.UTF8)
        {
            Encoding encoding;

            // 指定が無ければUTF-8
            if (encode == GlobalVariables.ENCORDTYPE.SJIS)
            {
                encoding = Encoding.GetEncoding("Shift_JIS");
            }
            else
            {
                // UTF8
                encoding = Encoding.GetEncoding("utf-8");
            }

            DataTable dt = null;
            try
            {
                //	パース開始
                using (var parser = new TextFieldParser(path, encoding))
                {
                    //  区切りの指定
                    parser.TextFieldType = FieldType.Delimited;

                    parser.SetDelimiters(delimiters);

                    // フィールドが引用符で囲まれているか
                    parser.HasFieldsEnclosedInQuotes = true;

                    // フィールドの空白トリム設定
                    parser.TrimWhiteSpace = false;

                    if (parser.EndOfData)
                    {
                        return null;
                    }

                    var row = parser.ReadFields();

                    var fileName = Path.GetFileName(path);

                    Dbg.ViewLog(fileName);

                    dt = new DataTable
                    {
                        // ファイル名をテーブル名にする
                        TableName = fileName
                    };

                    var dataSet = new DataSet();

                    dataSet.Tables.Add(dt);

                    // カラム数取得
                    var n = row.Count();

                    if (hasheader)
                    {
                        for (var i = 0; i < n; i++)
                        {
                            dt.Columns.Add(new DataColumn(row[i]));
                        }
                    }
                    else
                    {
                        for (var i = 0; i < n; i++)
                        {
                            // 仮のカラム名を設定します。
                            // 1始まり
                            dt.Columns.Add(new DataColumn("" + (i + 1)));
                        }
                    }

                    dt.NewRow();

                    dt.Rows.Add(row);

                    // ファイルの終端までループ
                    while (!parser.EndOfData)
                    {
                        row = parser.ReadFields();

                        dt.NewRow();

                        dt.Rows.Add(row);
                    }
                }
            }
            catch (Exception ex)
            {
                Dbg.Error(ex.ToString());

                throw ex;
            }

            //Dbg.Log("dt.Rows.Count:"+dt.Rows.Count);
            return dt;
        }

        /// <summary>
        /// CSVファイルの書き込み
        /// </summary>
        /// <param name="path">書き込み先パス</param>
        /// <param name="dt">書き込むDataTable</param>
        /// <param name="overwriteColumnName">列名の上書き用（上書きする場合指定）</param>
        public void WriteFile(string path, DataTable dt, List<string> overwriteColumnName = null)
        {
            try
            {
                if (dt != null && dt.Rows.Count > 0)
                {
                    var sb = new StringBuilder();

                    var columnNames = dt.Columns.Cast<DataColumn>().Select(column => column.ColumnName).ToArray();

                    if (overwriteColumnName != null)
                    {
                        try
                        {
                            if (columnNames.Length != overwriteColumnName.Count)
                            {
                                throw new MyException(Properties.Resources.E_MISMATCHED_HDR_COUNT);
                            }
                        }
                        catch (Exception ex)
                        {
                            throw ex;
                        }

                        // カンマ付きの文字列は、全体をダブルクォーテーションで囲む
                        var fields = overwriteColumnName.Select(field =>
                        {
                            if (field.ToString().IndexOf(',') > 0)
                            {
                                return string.Concat("\"", field.ToString().Replace("\"", "\"\""), "\"");
                            }
                            return field.ToString();
                        });

                        sb.AppendLine(string.Join(",", fields));
                    }
                    else
                    {
                        sb.AppendLine(string.Join(",", columnNames));
                    }

                    foreach (DataRow row in dt.Rows)
                    {
                        // カンマ付きの文字列は、全体をダブルクォーテーションで囲む
                        var fields = row.ItemArray.Select(field =>
                        {
                            if (field.ToString().IndexOf(',') > 0)
                            {
                                return string.Concat("\"", field.ToString().Replace("\"", "\"\""), "\"");
                            }
                            return field.ToString();
                        });

                        sb.AppendLine(string.Join(",", fields));
                    }

                    // ファイルを別アプリで開いている場合はエラーになる
                    File.WriteAllText(path, sb.ToString());
                }
            }
            catch (Exception ex)
            {
                Dbg.Error(ex.ToString());

                throw ex;
            }
        }

        /// <summary>
        /// IEnumerableからDataTableへの変換
        /// </summary>
        /// <param name="source">IEnumerable</param>
        /// <returns>DataTable</returns>
        public DataTable CreateDataTable(IEnumerable source)
        {
            var table = new DataTable();

            var index = 0;

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

                var values = new object[properties.Count];

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
