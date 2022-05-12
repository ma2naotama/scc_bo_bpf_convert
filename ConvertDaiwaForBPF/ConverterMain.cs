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
        private DataSet  mMasterSheets = null;

        public ConverterMain()
        {
        }

        private DataSet ReadMasterFile(string path)
        {
            //Dbg.Log("master.xlsx 読み込み中...");

            UtilExcel excel = new UtilExcel();

            ExcelOption[] optionarray = new ExcelOption[]
            {
                new ExcelOption ( "config",             2, 1, true),
                new ExcelOption ( "DHPTV001HED",        2, 1, true),
                new ExcelOption ( "DHPTV001DTL",        2, 1, true),
                new ExcelOption ( "項目マッピング",     2, 1, true),
                new ExcelOption ( "コードマッピング",   2, 1, true),
                //new ExcelOption ( "オーダーマッピング", 2, 1, true),
                new ExcelOption ( "出力ヘッダー",       2, 1, true),
            };

            excel.SetExcelOptionArray(optionarray);

            DataSet master = excel.ReadAllSheets(path); 
            if(master == null)
            {
                return null;
            }


            //検索のサンプル
            /*
            DataTable sheet = master["項目マッピング"];
            DataRow[] rows =
                sheet.AsEnumerable()
                  .Where(x => x["★列番号"].ToString() != "")
                  .ToArray();

            foreach (DataRow row in rows)
                Dbg.Log(row["★列番号"].ToString());
            */

            /*
            DataTable sheet = master["項目マッピング"];
            DataRow[] rows =
                sheet.AsEnumerable()
                  .Where(x => x["テスト項目"].ToString() != "")
                  .ToArray();

            foreach (DataRow row in rows)
                Dbg.Log(row["テスト項目"].ToString());
            */

            return master;
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
            mState = CONVERT_STATE.READ_MASTER;
        }


        //読み込んだCSVデータの解放
        private void PurgeLoadedMemory()
        {
            //メモリ解放
            if(mHdrTbl != null)
            {
                mHdrTbl.Clear();
                mHdrTbl = null;
            }

            if (mTdlTbl != null)
            {
                mTdlTbl.Clear();
                mTdlTbl = null;
            }

            if (mHdrRows != null)
            {
                mHdrRows = null;
            }

            GC.Collect();
        }

        //スレッド内の処理（これ自体をキャンセルはできない）
        private DataTable mHdrTbl = null;
        private DataTable mTdlTbl = null;
        private DataRow[] mHdrRows = null;
        private int mHdrIndex = 0;


        private DataTable mOutputCsv = null;

        private enum CONVERT_STATE
        {
            READ_MASTER = 0,
            READ_HEADER,
            READ_DATA,
            CONVERT_GETUSER,
            CONVERT_MAIN,
            CONVERT_OUTPUT,
            END = 100,
        }

        private class ItemMap
        {
            public string KensakoumokuCode { get; set; }

            public string KensakoumokuName { get; set; }

            public string KenshinmeisaiNo { get; set; }

            public string Value { get; set; }

            public string MItemName { get; set; }           //項目名
            public string MAttribute { get; set; }          //属性
            public string MCodeID { get; set; }             //コードID
            public string MOutputIndex { get; set; }        //★列番号
            //public string MOutputHeader { get; set; }       //ヘッダ項目名
            public string MOutputFormat { get; set; }       //出力文字フォーマット
        }

        private CONVERT_STATE mState = CONVERT_STATE.READ_MASTER;

        public override int MultiThreadMethod()
        {
            Dbg.Log("変換中...");

            bool loop = true;

            while (loop)
            {
                //キャンセル処理
                if (Cancel)
                {
                    PurgeLoadedMemory();
                    mState = CONVERT_STATE.READ_MASTER;
                    return 0;
                }

                try
                {
                    //Dbg.Log("mState:"+ mState);

                    switch (mState)
                    { 
                        case CONVERT_STATE.READ_MASTER:
                            {
                                string filename = "\\master_v4.xlsm";

                                mMasterSheets = ReadMasterFile(mPathInput + filename);
                                if(mMasterSheets == null)
                                {
                                    Dbg.ErrorLog(GlobalVariables.ERRORCOSE.ERROR_READMASTER, mPathInput + filename);
                                    mState = CONVERT_STATE.END;
                                    break;
                                }


                                //出力用CSVの初期化
                                DataRow[] rows = mMasterSheets.Tables["出力ヘッダー"].AsEnumerable()
                                      .Where(x => x["列名"].ToString() != "")
                                      .ToArray();

                                mOutputCsv = new DataTable();

                                //予めカラム名に同じカラム名はセットできないので、番号をセットしておく
                                int i = 1;
                                foreach (var row in rows)
                                {
                                    /*
                                    if(row.Field<string>("★列番号") == "")
                                    {
                                        continue;
                                    }

                                    string colname = row.Field<string>("ヘッダ項目名") == "" ? row.Field<string>("項目名"): row.Field<string>("ヘッダ項目名");
                                    if(colname == "-")
                                    {
                                        colname = "" +i;
                                    }

                                    //出力ヘッダーのカラム名で重複がある
                                    try
                                    {
                                        mOutputCsv.Columns.Add(colname, typeof(string));
                                    }
                                    catch (DuplicateNameException ex)
                                    {
                                        //カラム名の２重登録
                                        Dbg.Log("["+colname + "] は既に登録されています。");

                                        mOutputCsv.Columns.Add("" + i, typeof(string));
                                    }
                                    */
                                    mOutputCsv.Columns.Add("" + i, typeof(string));
                                    i++;
                                }

                                //次の処理へ
                                mState = CONVERT_STATE.READ_HEADER;
                            }
                            break;

                        case CONVERT_STATE.READ_HEADER:
                            {
                                DataRow[] rows =
                                    mMasterSheets.Tables["config"].AsEnumerable()
                                      .Where(x => x["受信ファイル名"].ToString() != "")
                                      .ToArray();

                                Dbg.Log(""+ rows[0][0]);

                                mHdrTbl = mCsvHDR.ReadFile(mPathInput + "\\" +rows[0][0], ",", GlobalVariables.ENCORDTYE.SJIS);
                                if (mHdrTbl == null)
                                {
                                    return 0;

                                }

                                if (mHdrTbl.Rows.Count == 0)
                                {
                                    return 0;
                                }

                                SetColumnName(mHdrTbl, mMasterSheets.Tables["DHPTV001HED"]);

                                //次の処理へ
                                mState = CONVERT_STATE.READ_DATA;
                            }
                            break;

                        case CONVERT_STATE.READ_DATA:
                            {
                                DataRow[] rows =
                                    mMasterSheets.Tables["config"].AsEnumerable()
                                      .Where(x => x["受信ファイル名"].ToString() != "")
                                      .ToArray();

                                Dbg.Log("" + rows[1][0]);

                                mTdlTbl = mCsvDTL.ReadFile(mPathInput + "\\" + rows[1][0], ",", GlobalVariables.ENCORDTYE.SJIS);
                                if (mTdlTbl == null)
                                {
                                    return 0;
                                }

                                if (mTdlTbl.Rows.Count == 0)
                                {
                                    return 0;
                                }

                                SetColumnName(mTdlTbl, mMasterSheets.Tables["DHPTV001DTL"]);


                                //次の処理へ
                                mState = CONVERT_STATE.CONVERT_GETUSER;

                                mHdrIndex = 0;
                            }
                            break;


                        case CONVERT_STATE.CONVERT_GETUSER:
                            {
                                //ヘッダーの削除フラグが0だけ抽出
                                mHdrRows =
                                    mHdrTbl.AsEnumerable()
                                    .Where(x => x["削除フラグ"].ToString() == "0")
                                    .ToArray();

                                if (mHdrRows.Length <= 0)
                                {
                                    Dbg.ErrorLog(GlobalVariables.ERRORCOSE.ERROR_HEADER_IS_EMPTY);
                                    mState = CONVERT_STATE.END;
                                    break;
                                }

                                //重複の確認(何をもって重複とするか検討)
                                var dr_array = from row in mHdrRows.AsEnumerable()
                                               where (
                                                   from _row in mHdrRows.AsEnumerable()
                                                   where
                                                   //row["組合C"].ToString() == _row["組合C"].ToString()
                                                   //&& row["健診基本情報管理番号"].ToString() == _row["健診基本情報管理番号"].ToString()
                                                   row["個人番号"].ToString() == _row["個人番号"].ToString()
                                                   && row["健診実施日"].ToString() == _row["健診実施日"].ToString()
                                                   //&& row["健診実施機関名称"].ToString() == _row["健診実施機関名称"].ToString()
                                                   select _row["個人番号"]
                                               ).Count() > 1 //重複していたら、２つ以上見つかる
                                               select row;

                                //DataTableが大きすぎるとここで処理が終わらない事がある。
                                //※現在ユーザー毎に処理する様に変更した為問題は起きないはず。
                                int overlapcount = dr_array.Count();
                                Dbg.Log("重複件数：" + overlapcount);

                                if (overlapcount > 0)
                                {
                                    DataTable queryResult = new DataTable();
                                    queryResult = dr_array.CopyToDataTable();

                                    UtilCsv csv = new UtilCsv();
                                    csv.WriteFile(".\\重複ユーザー.csv", queryResult);

                                    //重複していたら終了
                                    mState = CONVERT_STATE.END;
                                    break;
                                }

                                //次の処理へ
                                mState = CONVERT_STATE.CONVERT_MAIN;
                            }
                            break;

                        //メモリが不足して正常に動作しない為、ヘッダーの一行毎に処理する
                        case CONVERT_STATE.CONVERT_MAIN:
                            {
                                DataRow hrow = mHdrRows[mHdrIndex];

                                DataTable hdt = new DataTable();
                                hdt.Columns.Add("組合C", typeof(string));
                                hdt.Columns.Add("健診基本情報管理番号", typeof(string));
                                hdt.Columns.Add("健診実施日", typeof(string));
                                hdt.Columns.Add("個人番号", typeof(string));

                                hdt.Rows.Add(
                                        hrow["組合C"].ToString().Trim(), 
                                        hrow["健診基本情報管理番号"].ToString().Trim(),
                                        hrow["健診実施日"].ToString().Trim(),
                                        hrow["個人番号"].ToString().Trim()
                                    );

                                Dbg.Log("個人番号:"+hrow["個人番号"].ToString());

                                //UtilCsv csv = new UtilCsv();
                                //csv.WriteFile(".\\HEADER.csv", dt);

                                //結合して取得
                                var query =
                                        from h in hdt.AsEnumerable()
                                        join d in mTdlTbl.AsEnumerable() on h.Field<string>("組合C").Trim() equals d.Field<string>("組合C").Trim()
                                    where
                                        h.Field<string>("健診基本情報管理番号").Trim() == d.Field<string>("健診基本情報管理番号").Trim()
                                        && d.Field<string>("削除フラグ").Trim() == "0"
                                        && d.Field<string>("未実施FLG").Trim() == "0"
                                        && d.Field<string>("測定不能FLG").Trim() == "0"
                                        select new
                                        {
                                            //ヘッダー情報は、人事データ結合時に処理する。
                                            //PersonNo = h.Field<string>("個人番号").Trim(),
                                            //KenshinNo = h.Field<string>("健診基本情報管理番号").Trim(),
                                            //KenshinDate = h.Field<string>("健診実施日").Trim(),        //yyymmdd
                                            //KenshinkikanName = h.Field<string>("健診実施機関名称"),   
                                            KensakoumokuCode = d.Field<string>("検査項目コード").Trim(),
                                            KensakoumokuName = d.Field<string>("検査項目名称").Trim(),
                                            KenshinmeisaiNo = d.Field<string>("健診明細情報管理番号").Trim(),
                                            Value = (d.Field<string>("結果値データタイプ").Trim() == "4") ? d.Field<string>("コメント").Trim():d.Field<string>("結果値").Trim(),
                                        };

                                if (query.Count() <= 0)
                                {
                                    //結合した結果データが無い
                                    Dbg.ErrorLog(GlobalVariables.ERRORCOSE.ERROR_BODY_IS_NOUSERDATA, 
                                        hrow["個人番号"].ToString());
                                    mState = CONVERT_STATE.END;
                                    break;
                                }

                                //項目マッピング処理
                                //項目マッピングから該当する検査項目コード一覧を抽出
                                DataTable itemSheet = mMasterSheets.Tables["項目マッピング"];

                                var mergeMapped =
                                        from m in query.AsEnumerable()
                                        join t in itemSheet.AsEnumerable() on m.KensakoumokuCode.ToString() equals t.Field<string>("検査項目コード").Trim()
                                        select new ItemMap
                                        {
                                            //PersonNo = m.PersonNo,
                                            //KenshinNo = m.KenshinNo,
                                            //KenshinDate = m.KenshinDate,
                                            KensakoumokuCode = m.KensakoumokuCode,
                                            KensakoumokuName = m.KensakoumokuName,
                                            KenshinmeisaiNo = m.KenshinmeisaiNo,
                                            Value = m.Value,
                                            MItemName = t.Field<string>("項目名"),
                                            MAttribute = t.Field<string>("属性"),
                                            MCodeID = t.Field<string>("コードID"),
                                            MOutputIndex = t.Field<string>("★列番号"),
                                            //MOutputHeader = t.Field<string>("ヘッダ項目名"),
                                            MOutputFormat = t.Field<string>("出力文字フォーマット"),
                                        };

                                //TODO:オーダーマッピング（特定の検査項目コードの絞込）
                                //OrderMapping(itemSheet, mergeMapped);


                                //TODO:コードマッピング（属性が「コード」の場合、値の置換）
                                //CodeMapping(itemSheet, mergeMapped);


                                //TODO:アウトプット用にセット（「★列番号」及び「ヘッダ項目名」、出力文字フォーマット、人事情報）
                                DataRow r = mOutputCsv.NewRow();

                                foreach(var item in mergeMapped)
                                {
                                    //if(item.MOutputFormat != "")
                                    //{
                                    //    r[item.MOutputIndex] = string.Format(item.MOutputFormat, item.Value);
                                    //}
                                    //else
                                    //{
                                    //    r[item.MOutputIndex] = item.Value;
                                    //}
                                    int index = int.Parse(item.MOutputIndex);

                                    //カラム番号に対して、値をセットする
                                    r[index] = item.Value;
                                }

                                mOutputCsv.Rows.Add(r);

                                //後処理
                                hdt.Clear();
                                hdt = null;

                                mergeMapped = null;

                                //次のユーザー
                                mHdrIndex++;
                                if (mHdrIndex >= mHdrRows.Length || mHdrIndex > 10)
                                {
                                    mState = CONVERT_STATE.CONVERT_OUTPUT;
                                    break;
                                }


                                //テスト用の為、１ユーザー分で終了
                                //mState = CONVERT_STATE.END;
                            }
                            break;

                        case CONVERT_STATE.CONVERT_OUTPUT:
                            {
                                Dbg.Log("csvへ書き出す。mHdrIndex：" + mHdrIndex);

                                //出力用CSVのカラム名をDataRowの配列で取得（3018行分）
                                var rows = mMasterSheets.Tables["出力ヘッダー"].AsEnumerable()
                                      .Where(x => x["列名"].ToString() != "")
                                      .ToArray();

                                //var str_arry = rows.Select(c => c.ToString()).ToArray();

                                //最適化できそう
                                List<string> str_arry = new List<string>();
                                foreach(var r in rows)
                                {
                                    str_arry.Add(r.Field<string>("列名"));
                                }

                                UtilCsv csv = new UtilCsv();
                                csv.WriteFile(mPathOutput, mOutputCsv, str_arry);

                                mState = CONVERT_STATE.END;
                            }
                            break;

                        //終了
                        default:
                            {
                                Dbg.Log("state:"+ mState);
                                PurgeLoadedMemory();
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

                GC.Collect();
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
            DataRow[] rows = sheet.AsEnumerable()
                .Where(x => x["項目"].ToString() != "")
                .ToArray();

            int n = dt.Columns.Count;
            for (int i=0; i< rows.Count(); i++)
            {
                //Dbg.Log(rows[i][0].ToString());
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

        private IEnumerable OrderMapping(DataTable itemMapped, IEnumerable merged)
        {
            return null;
        }
        private IEnumerable CodeMapping(DataTable itemMapped, IEnumerable merged)
        {
            return null;
        }
        /*
        private IEnumerable CsvMapping(DataTable dst, IEnumerable<ItemMap> merged, DataTable hdr, DataTable humanInfo)
        {
            Dictionary<string, string> dic = merged.ToDictionary();

            dst.Rows[]

            return null;
        }
        */
    }
}
