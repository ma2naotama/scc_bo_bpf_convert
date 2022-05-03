using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ConvertDaiwaForBPF
{
    internal class ConverterMain : BaseThread
    {
        private Dictionary<string, DataTable> mMasterSheets = null;

        public ConverterMain()
        {
        }

        private void ReadMasterFile()
        {
            if (mMasterSheets != null)
            {
                //2重読み込み防止
                return;
            }

            //Dbg.Log("master.xlsx 読み込み中...");

            UtilExcel excel = new UtilExcel();

            ExcelOption[] optionarray = new ExcelOption[]
            {
                new ExcelOption ( "DHPTV001HED",        2, 1),
                new ExcelOption ( "DHPTV001DTL",        2, 1),
                new ExcelOption ( "JLAC10変換",         2, 1),
                new ExcelOption ( "項目マッピング",     2, 1),
                new ExcelOption ( "コードマッピング",   2, 1),
                new ExcelOption ( "ロジックマッピング", 2, 1),
                new ExcelOption ( "オーダーマッピング", 2, 1),
            };

            excel.SetExcelOptionArray(optionarray);

            mMasterSheets = excel.ReadAllSheets(".\\_master\\master.xlsm");
            Dbg.Log("master.xlsx 読み込み終了");

            //DataTable sheet = mMasterSheets["項目マッピング"];

            /*
             * 検索のサンプル
            DataRow[] rows =
                sheet.AsEnumerable()
                  .Where(x => Int32.Parse(x["No"].ToString()) > 1)
                  .ToArray();
            */

            /*
            DataRow[] rows =
            sheet.AsEnumerable()
              .Where(x => x["項目名"].ToString() != "")
              .ToArray();

            foreach (DataRow row in rows)
                Dbg.Log(row["項目名"].ToString());
            */

        }

        public override void MultiThreadCancel()
        {
            if (mCsvHDR != null)
            {
                mCsvHDR.Cancel();
            }

            if (mCsvDTL != null)
            {
                mCsvDTL.Cancel();
            }

            base.MultiThreadCancel();
        }


        string mPathInput;
        string mPathHR;
        string mPathOutput;

        private UtilCsv mCsvHDR = null;
        private UtilCsv mCsvDTL = null;

        public void InitConvert(string pathInput, string pathHR, string pathOutput)
        {
            mPathInput = pathInput;
            mPathHR = pathHR;
            mPathOutput = pathOutput;

            mCsvHDR = new UtilCsv();
            mCsvDTL = new UtilCsv();

            Cancel = false;
            mState = 0;
        }

        //スレッド内の処理（これ自体をキャンセルはできない）
        private int mState = -1;
        private DataTable mHdr = null;
        private DataTable mTdl = null;

        public override int MultiThreadMethod()
        {
            Dbg.Log("変換中...");

            bool loop = true;

            while (loop)
            {
                if (Cancel)
                {
                    mState = 0;
                    return 0;
                }

                try
                {
                    switch (mState)
                    { 
                        case 0:
                            {
                                ReadMasterFile();
                                mState = 1;
                            }
                            break;

                        case 1:
                            {
                                mHdr = mCsvHDR.ReadFile(mPathInput + "\\DHPTV001HED_458BNF.csv", ",", GlobalVariables.ENCORDTYE.SJIS);
                                if (mHdr == null)
                                {
                                    return 0;

                                }

                                if (mHdr.Rows.Count == 0)
                                {
                                    return 0;
                                }

                                mState = 2;
                            }
                            break;

                        case 2:
                            {
                                SetColumnName(mHdr, mMasterSheets["DHPTV001HED"]);
                                mState = 3;
                            }
                            break;

                        case 3:
                            {
                                mTdl = mCsvDTL.ReadFile(mPathInput + "\\DHPTV001DTL_458BNF.csv", ",", GlobalVariables.ENCORDTYE.SJIS);
                                if (mTdl == null)
                                {
                                    return 0;
                                }

                                if (mTdl.Rows.Count == 0)
                                {
                                    return 0;
                                }

                                mState = 4;
                            }
                            break;

                        case 4:
                            {

                                SetColumnName(mTdl, mMasterSheets["DHPTV001DTL"]);
                                mState = 5;
                            }
                            break;


                        case 5:
                            {
                                //結合して取得
                                var query =
                                     from h in mHdr.AsEnumerable()
                                     join d in mTdl.AsEnumerable() on h.Field<string>("組合C") equals d.Field<string>("組合C")
                                     where 
                                        h.Field<string>("健診基本情報管理番号") == d.Field<string>("健診基本情報管理番号")
                                        && h.Field<string>("削除フラグ")  == "0"
                                        && d.Field<string>("削除フラグ")  == "0"
                                        && d.Field<string>("未実施FLG")   == "0"
                                        && d.Field<string>("測定不能FLG") == "0"
                                     select new
                                     {
                                         PersonNo = h.Field<string>("個人番号"),
                                         KenshinDate = h.Field<string>("健診実施日"),
                                         KenshinkikanName = h.Field<string>("健診実施機関名称"),
                                         KensakoumokuCode = d.Field<string>("検査項目コード"),
                                         KensakoumokuName = d.Field<string>("検査項目名称"),
                                         KenshinmeisaiNo = d.Field<string>("健診明細情報管理番号"),
                                         Value = d.Field<string>("結果値"),
                                         Comment = d.Field<string>("コメント"),
                                     };

                                mHdr = null;
                                mTdl = null;


                                //重複の確認
                                var dr_array =
                                    from row in query.AsEnumerable()
                                     // group row by row.PersonNo  into g	// 一つのｷｰでgroup化するならこれで良い
                                    group row by new // 複数のｷｰでgroup化する場合
                                    {
                                        row.PersonNo,
                                        row.KenshinDate,
                                        row.KensakoumokuCode,
                                        row.KenshinkikanName
                                    } into g
                                    orderby g.Key.PersonNo //descending
                                    orderby g.Key.KenshinDate
                                    //select g;	// Count()計算しないならこれで良い。
                                    select new
                                    {
                                        g.Key.PersonNo,
                                        g.Key.KenshinDate,
                                        g.Key.KensakoumokuCode,
                                        g.Key.KenshinkikanName,
                                        Count = g.Count()
                                    };

                                var queryResult = dr_array.AsEnumerable()
                                              .OrderBy(x => x.PersonNo)
                                              .Where(x => (int)x.Count > 1)
                                              .Select (x => new {x.PersonNo, x.KenshinDate, x.KensakoumokuCode, x.KenshinkikanName });
                                //.CopyToDataTable();

                                //var queryResult = dr_array.ToList();
                                DataTable boundTable = CreateDataTable(queryResult);
                                Dbg.Log("重複件数：" + boundTable.Rows.Count);

                                UtilCsv csv = new UtilCsv();
                                csv.WriteFile(".\\重複.csv", boundTable);

                                /*
                                //int index = 0;
                                var boundTable = table.AsEnumerable()
                                    .GroupBy(r => new { Col1 = r["PersonNo"], Col2 = r["KenshinDate"] })  into grp
                                    .Select(g =>
                                    {
                                        var row = table.NewRow();
                                        row["PersonNo"] = g.Key.Col1;
                                        row["KenshinDate"] = g.Key.Col2;
                                        row["count"] = grp.
                                        return row;

                                    })
                                    .OrderBy(x => x["PersonNo"])
                                    .ThenBy(x => x["KenshinDate"])
                                    .CopyToDataTable();
                                */

                                /*
                                var dr_array = from row in table.AsEnumerable()
                                               where (
                                                   from _row in table.AsEnumerable()
                                                   where row["PersonNo"].ToString() == _row["PersonNo"].ToString() 
                                                   && row["KenshinDate"].ToString() == _row["KenshinDate"].ToString()
                                                   select _row["PersonNo"]
                                               ).Count() > 1 //重複していたら、２つ以上見つかる
                                               select row;

                                var boundTable = dr_array.CopyToDataTable();
                                */
                                /*
                                var dr_array = from row in query.AsEnumerable()
                                               where (
                                                   from _row in query.AsEnumerable()
                                                   where
                                                   row.PersonNo.ToString() == _row.PersonNo.ToString()
                                                   && row.KenshinDate.ToString() == _row.KenshinDate.ToString()
                                                   && row.KensakoumokuCode.ToString() == _row.KensakoumokuCode.ToString()
                                                   select _row.PersonNo
                                               ).Count() > 1 //重複していたら、２つ以上見つかる
                                               //orderby row["PersonNo"], row["KenshinDate"]
                                               select new { row.PersonNo };
                                */

                                //Dbg.Log("重複件数：" + boundTable.Rows.Count);
                                //csv.WriteFile(".\\重複.csv", boundTable);


                                // 2プロパティをグループ化、おまけで重複したグループ数を得る
                                /*
                                var points = Points.GroupBy(point => new { point.X, point.Y })
                                                    .Select(point => new { point.Key.X, point.Key.Y, Count = point.Count() });
                                */
                                /*
                                var boundTable = table.AsEnumerable()
                                   .GroupBy(r => new { PersonNo = r["PersonNo"], KenshinDate = r["KenshinDate"] })
                                   //.Select(g => g.OrderBy(r => r["PersonNo"]).ThenBy(x => x["KenshinDate"]).First())
                                   //.Select(g => g.Any())
                                   .Select(g =>
                                   {
                                       var row = table.NewRow();
                                       row["PersonNo"] = g.Key.PersonNo;
                                       row["KenshinDate"] = g.Key.KenshinDate;
                                       return row;
                                   })
                                   .CopyToDataTable();
                                */
                                /*
                                var dr_array = from row in table.AsEnumerable()
                                               where (
                                                   from _row in table.AsEnumerable()
                                                   where 
                                                   row["PersonNo"].ToString() == _row["PersonNo"].ToString() 
                                                   && row["KenshinDate"].ToString() == _row["KenshinDate"].ToString()
                                                   && row["KensakoumokuCode"].ToString() == _row["KensakoumokuCode"].ToString()
                                                   select _row["PersonNo"]
                                               ).Count() > 1 //重複していたら、２つ以上見つかる
                                               //orderby row["PersonNo"], row["KenshinDate"]
                                               select row;

                                var boundTable = dr_array.CopyToDataTable();
                                */
                                /*
                                DataTable result = data.AsEnumerable()
                                   .Where(x => x["Option"] == '1')
                                   .Select(x => new {
                                       id = x["ID"].ToString(),
                                       name = x["Name"].ToString()
                                   });
                                */
                                /*
                                var boundTable = table.AsEnumerable()
                                   .GroupBy(r => new { Col1 = r["Col1"], Col2 = r["Col2"] })
                                   .Select(g => g.OrderBy(r => r["PK"]).First())
                                   .CopyToDataTable();
                                */
                                //Dbg.Log("重複件数："+ boundTable.Rows.Count);
                                /*
                                foreach (var row in dr_array)
                                {
                                    string str = "";
                                    foreach (var item in row.ItemArray)
                                    {
                                        //byte[] bytesUTF8 = System.Text.Encoding.Default.GetBytes(item.ToString());
                                        //string stringSJIS = System.Text.Encoding.UTF8.GetString(bytesUTF8);
                                        str += item + " ";
                                    }
                                    Dbg.Log(str);
                                }
                                */

                                //UtilCsv csv = new UtilCsv();
                                //csv.WriteFile(".\\重複.csv", boundTable);

                                //DataTable dt_overlap = dr_array.CopyToDataTable();
                                //DataTable dt_overlap = CreateDataTable(dr_array);
                                /*
                                if (dr_array != null && dr_array.Count()>0)
                                {
                                    //DataTable dt_overlap = new DataTable();
                                    //dt_overlap = dr_array.CopyToDataTable();
                                    foreach (var item in dr_array)
                                    {
                                        //Dbg.Log(item["PersonNo"].ToString() + " " + item["KenshinDate"].ToString());
                                        Dbg.Log(item.PersonNo + " " + item.KenshinDate);
                                    }
                                }

                                */

                                //TODO:検査項目コードの置換

                                //TODO:項目マッピング

                                //TODO:コードマッピング

                                //TODO:ロジックマッピング

                                //TODO:オーダーマッピング

                                mState = 6;
                            }
                            break;

                        //終了
                        default:
                            {
                                Dbg.Log("state:"+ mState);
                                loop = false;
                                break;
                            }
                    }

                }
                catch(Exception ex)
                {
                    MultiThreadCancel();
                    Dbg.Log(ex.ToString());
                    Dbg.Log("state:" + mState);
                    return 0;
                }
            }

            /*
            DataRow[] rows =
            tdl.AsEnumerable()
                .Where(x => x["組合C"].ToString() != "")
                .ToArray();

            foreach (DataRow row in rows)
                Dbg.Log(row["組合C"].ToString());
            */

            //処理完了
            Completed = true;
            return 1;
        }

        void SetColumnName(DataTable dt, DataTable sheet)
        {
            DataRow[] rows =
            sheet.AsEnumerable()
                .Where(x => x["項目"].ToString() != "")
                .ToArray();

            int n = dt.Columns.Count;

            for (int i=0; i< rows.Count(); i++)
            {
                //Dbg.Log(rows[i][0].ToString());

                if (Cancel)
                {
                   return;
                }

                if(i<n)
                {
                    dt.Columns[""+(i+1)].ColumnName = rows[i][0].ToString();
                }
                else
                {
                    dt.Columns.Add(rows[i][0].ToString());
                }
            }
        }


        /// <summary>
        /// DataTableから重複しているデータを取得する
        /// </summary>
        /// <param name="dt">DataTable</param>
        /// <param name="columnName">重複をチェックするカラム名</param>
        private DataTable GetOverlapedRow(DataTable dt, string columnName)
        {
            var dr_array = from row in dt.AsEnumerable()
                           where (
                               from _row in dt.AsEnumerable()
                               where row[columnName].ToString() == _row[columnName].ToString()
                               select _row[columnName]
                           ).Count() > 1 //重複していたら、２つ以上見つかる
                           select row;

            DataTable dt_overlap = new DataTable();
            if (dr_array.Count() > 0)
            {
                dt_overlap = dr_array.CopyToDataTable();
            }

            return dt_overlap;
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
