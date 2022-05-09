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

        private Dictionary<string, DataTable> ReadMasterFile(string path)
        {
            //Dbg.Log("master.xlsx 読み込み中...");

            UtilExcel excel = new UtilExcel();

            ExcelOption[] optionarray = new ExcelOption[]
            {
                new ExcelOption ( "config",             2, 1, true),
                new ExcelOption ( "DHPTV001HED",        2, 1, true),
                new ExcelOption ( "DHPTV001DTL",        2, 1, true),
                new ExcelOption ( "JLAC10変換",         2, 1, true),
                new ExcelOption ( "項目マッピング",     2, 1, true),
                new ExcelOption ( "コードマッピング",   2, 1, true),
                new ExcelOption ( "ロジックマッピング", 2, 1, true),
                new ExcelOption ( "オーダーマッピング", 2, 1, true),
            };

            excel.SetExcelOptionArray(optionarray);

            Dictionary<string, DataTable> master = excel.ReadAllSheets(path);
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
            if(mHdr != null)
            {
                mHdr.Clear();
                mHdr = null;
            }

            if (mTdl != null)
            {
                mTdl.Clear();
                mTdl = null;
            }

            if (mHdrRows != null)
            {
                mHdrRows = null;
            }

            GC.Collect();
        }

        //スレッド内の処理（これ自体をキャンセルはできない）
        private DataTable mHdr = null;
        private DataTable mTdl = null;
        private DataRow[] mHdrRows = null;
        private int mHdrIndex = 0;


        private enum CONVERT_STATE
        {
            READ_MASTER = 0,
            READ_HEADER,
            READ_DATA,
            CONVERT_GETUSER,
            CONVERT_MAIN,
            END = 100,
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
                    switch (mState)
                    { 
                        case CONVERT_STATE.READ_MASTER:
                            {
                                string filename = "\\master_v3.xlsm";

                                mMasterSheets = ReadMasterFile(mPathInput + filename);
                                if(mMasterSheets == null)
                                {
                                    Dbg.ErrorLog(GlobalVariables.ERRORCOSE.ERROR_READMASTER, mPathInput + filename);
                                    mState = CONVERT_STATE.END;
                                    break;
                                }

                                //次の処理へ
                                mState = CONVERT_STATE.READ_HEADER;
                            }
                            break;

                        case CONVERT_STATE.READ_HEADER:
                            {
                                DataRow[] rows =
                                    mMasterSheets["config"].AsEnumerable()
                                      .Where(x => x["受信ファイル名"].ToString() != "")
                                      .ToArray();

                                Dbg.Log(""+ rows[0][0]);

                                mHdr = mCsvHDR.ReadFile(mPathInput + "\\" +rows[0][0], ",", GlobalVariables.ENCORDTYE.SJIS);
                                if (mHdr == null)
                                {
                                    return 0;

                                }

                                if (mHdr.Rows.Count == 0)
                                {
                                    return 0;
                                }

                                SetColumnName(mHdr, mMasterSheets["DHPTV001HED"]);

                                //次の処理へ
                                mState = CONVERT_STATE.READ_DATA;
                            }
                            break;

                        case CONVERT_STATE.READ_DATA:
                            {
                                DataRow[] rows =
                                    mMasterSheets["config"].AsEnumerable()
                                      .Where(x => x["受信ファイル名"].ToString() != "")
                                      .ToArray();

                                Dbg.Log("" + rows[1][0]);

                                mTdl = mCsvDTL.ReadFile(mPathInput + "\\" + rows[1][0], ",", GlobalVariables.ENCORDTYE.SJIS);
                                if (mTdl == null)
                                {
                                    return 0;
                                }

                                if (mTdl.Rows.Count == 0)
                                {
                                    return 0;
                                }

                                SetColumnName(mTdl, mMasterSheets["DHPTV001DTL"]);


                                //次の処理へ
                                mState = CONVERT_STATE.CONVERT_GETUSER;
                            }
                            break;


                        case CONVERT_STATE.CONVERT_GETUSER:
                            {
                                //ヘッダーの削除フラグが0だけ抽出
                                mHdrRows =
                                    mHdr.AsEnumerable()
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
                                mHdrIndex = 0;

                                if (mHdrIndex >= mHdrRows.Length)
                                {
                                    //次の処理へ
                                    mState = CONVERT_STATE.END;
                                    break;
                                }


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

                                //UtilCsv csv = new UtilCsv();
                                //csv.WriteFile(".\\HEADER.csv", dt);

                                //結合して取得
                                var query =
                                        from h in hdt.AsEnumerable()
                                        join d in mTdl.AsEnumerable() on h.Field<string>("組合C").Trim() equals d.Field<string>("組合C").Trim()
                                    where
                                        h.Field<string>("健診基本情報管理番号").Trim() == d.Field<string>("健診基本情報管理番号").Trim()
                                        && d.Field<string>("削除フラグ").Trim() == "0"
                                        && d.Field<string>("未実施FLG").Trim() == "0"
                                        && d.Field<string>("測定不能FLG").Trim() == "0"
                                        select new
                                        {
                                            PersonNo = h.Field<string>("個人番号").Trim(),
                                            KenshinNo = h.Field<string>("健診基本情報管理番号").Trim(),
                                            KenshinDate = h.Field<string>("健診実施日").Trim(),        //yyymmdd
                                            KensakoumokuCode = d.Field<string>("検査項目コード").Trim(),
                                            KensakoumokuName = d.Field<string>("検査項目名称").Trim(),
                                            KenshinmeisaiNo = d.Field<string>("健診明細情報管理番号").Trim(),
                                            Value = d.Field<string>("結果値").Trim(),
                                            //KenshinkikanName = h.Field<string>("健診実施機関名称"),
                                            Comment = d.Field<string>("コメント").Trim(),
                                        };

                                if (query.Count() <= 0)
                                {
                                    //結合した結果データが無い
                                    Dbg.ErrorLog(GlobalVariables.ERRORCOSE.ERROR_BODY_IS_NOUSERDATA, 
                                        hrow["個人番号"].ToString());
                                    mState++;
                                    break;
                                }

                                //var merge = query.ToArray();


                                //結合テーブルの作成
                                //DataTable merge = CreateDataTable(query);

                                //UtilCsv csv = new UtilCsv();
                                //csv.WriteFile(".\\結合.csv", merge);

                                /*
                                //重複の確認　TODO:ユーザー毎の重複となるので、ユーザーが重複してても分からない
                                var dr_array = from row in merge.AsEnumerable()
                                                where (
                                                    from _row in merge.AsEnumerable()
                                                    where
                                                    row["PersonNo"].ToString() == _row["PersonNo"].ToString()
                                                    && row["KenshinNo"].ToString() == _row["KenshinNo"].ToString()
                                                    && row["KenshinDate"].ToString() == _row["KenshinDate"].ToString()
                                                    && row["KensakoumokuCode"].ToString() == _row["KensakoumokuCode"].ToString()
                                                    && row["KenshinmeisaiNo"].ToString() == _row["KenshinmeisaiNo"].ToString()
                                                    select _row["PersonNo"]
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
                                    csv.WriteFile(".\\重複.csv", queryResult);
                                }
                                */

                                //項目マッピング
                                DataTable itemSheet = mMasterSheets["項目マッピング"];

                                var mergeMapped =
                                        from m in query.AsEnumerable()
                                        join t in itemSheet.AsEnumerable() on m.KensakoumokuCode.ToString() equals t.Field<string>("検査項目コード").Trim()
                                        select new
                                        {
                                            PersonNo = m.PersonNo,
                                            KenshinNo = m.KenshinNo,
                                            KenshinDate = m.KenshinDate,
                                            KensakoumokuCode = m.KensakoumokuCode,
                                            KensakoumokuName = m.KensakoumokuName,
                                            KenshinmeisaiNo = m.KenshinmeisaiNo,
                                            Value = m.Value,
                                            //KenshinkikanName = h.Field<string>("健診実施機関名称"),
                                            Comment = m.Comment,
                                            MItemName = t.Field<string>("項目名"),
                                            MAttribute = t.Field<string>("属性"),
                                            MCodeID = t.Field<string>("コードID"),
                                            MOutputIndex = t.Field<string>("★列番号"),
                                            MOutputHeader = t.Field<string>("ヘッダ項目名"),
                                            MOutputFormat = t.Field<string>("出力文字フォーマット"),
                                        };

                                UtilCsv csv = new UtilCsv();
                                csv.WriteFile(".\\MergeMappedcsv", csv.CreateDataTable(mergeMapped));

                                //コードマッピング
                                //CodeMapping()

                                //次のユーザー
                                mHdrIndex++;

                                //次のユーザー
                                //mState = CONVERT_STATE.CONVERT_GETUSER;

                                //テスト用の為、１ユーザー分で終了
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

        private IEnumerable CodeMapping(DataTable masterSheet, IEnumerable merged)
        {
            return null;
        }
    }
}
