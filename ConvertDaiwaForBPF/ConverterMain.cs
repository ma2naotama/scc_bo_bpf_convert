using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
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
                new ExcelOption ( "項目マッピング",     4, 1, true),
                new ExcelOption ( "コードマッピング",   3, 1, true),
                new ExcelOption ( "オーダーマッピング", 2, 1, true),
                //new ExcelOption ( "出力ヘッダー",       2, 1, true),
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


        //健診ヘッダーと健診データの結合用
        private class MergedMap
        {
            //public string userId { get; set; }                  //個人番号

            //public string date_of_consult { get; set; }         //受診日

            public string KensakoumokuCode { get; set; }

            public string KensakoumokuName { get; set; }

            public string KenshinmeisaiNo { get; set; }

            public string Value { get; set; }
        }


        //設定ファイルの項目マッピングから、検査項目コードで抽出した結果
        private class ItemMap
        {
            public string KensakoumokuCode { get; set; }   //検査項目コード
            public string OutputHdrIndex { get; set; }     //列順(出力先の列番号)
            public string ItemName { get; set; }           //検査項目名
            public string Attribute { get; set; }          //属性
            public string CodeID { get; set; }             //コードID
            public string Type { get; set; }               //種別
            //public string Rate { get; set; }               //倍率
            //public string StringFormat { get; set; }       //文字フォーマット
            public string Value { get; set; }              //検査値
        }

        private CONVERT_STATE mState = CONVERT_STATE.READ_MASTER;

        public override int MultiThreadMethod()
        {
            Dbg.ViewLog("変換中...");

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
                                string filename = "設定ファイル.xlsx";

                                // 独自に設定した「appSettings」へのアクセス
                                NameValueCollection appSettings = (NameValueCollection) ConfigurationManager.GetSection("appSettings");

                                string path = appSettings["SettingPath"] + filename;
                                Dbg.ViewLog("設定ファイルの読み込み:"+ path);

                                mMasterSheets = ReadMasterFile(path);
                                if(mMasterSheets == null)
                                {
                                    Dbg.ErrorWithView(Properties.Resources.E_READFAILED_MASTER, path);
                                    mState = CONVERT_STATE.END;
                                    break;
                                }

                                //出力用CSVの初期化
                                DataRow[] rows = mMasterSheets.Tables["項目マッピング"].AsEnumerable()
                                      .Where(x => x["列順"].ToString() != "")
                                      .ToArray();

                                //項目マッピングの順列の最大値と項目数（個数）の確認
                                if (rows.Length != rows.Max(r => int.Parse(r["列順"].ToString())))
                                {
                                    Dbg.ErrorWithView(Properties.Resources.E_ITEMMAPPING_INDEX_FAILE);
                                    mState = CONVERT_STATE.END;
                                    break;
                                }

                                mOutputCsv = new DataTable();

                                //同じ列名（カラム名）はセットできないので、列順をセットしておく
                                foreach (var row in rows)
                                {
                                    //Dbg.Log("" + row["列順"]);
                                    mOutputCsv.Columns.Add("" + row["列順"], typeof(string));
                                }

                                //次の処理へ
                                mState = CONVERT_STATE.READ_HEADER;
                            }
                            break;

                        //健診ヘッダーの読み込み
                        case CONVERT_STATE.READ_HEADER:
                            {
                                DataRow[] rows =
                                    mMasterSheets.Tables["config"].AsEnumerable()
                                      .Where(x => x["受信ファイル名"].ToString() != "")
                                      .ToArray();

                                mHdrTbl = mCsvHDR.ReadFile(mPathInput + "\\" +rows[0][0], ",", GlobalVariables.ENCORDTYPE.SJIS);
                                if (mHdrTbl == null)
                                {
                                    //中断
                                    Dbg.ErrorWithView(Properties.Resources.E_READFAILED_HDR);
                                    return 0;
                                }

                                if (mHdrTbl.Rows.Count == 0)
                                {
                                    //中断
                                    Dbg.ErrorWithView(Properties.Resources.E_READFAILED_HDR);
                                    return 0;
                                }

                                SetColumnName(mHdrTbl, mMasterSheets.Tables["DHPTV001HED"]);

                                //次の処理へ
                                mState = CONVERT_STATE.READ_DATA;
                            }
                            break;

                        //健診データの読み込み
                        case CONVERT_STATE.READ_DATA:
                            {
                                DataRow[] rows =
                                    mMasterSheets.Tables["config"].AsEnumerable()
                                      .Where(x => x["受信ファイル名"].ToString() != "")
                                      .ToArray();

                                mTdlTbl = mCsvDTL.ReadFile(mPathInput + "\\" + rows[1][0], ",", GlobalVariables.ENCORDTYPE.SJIS);
                                if (mTdlTbl == null)
                                {
                                    //中断
                                    Dbg.ErrorWithView(Properties.Resources.E_READFAILED_TDL);
                                    return 0;
                                }

                                if (mTdlTbl.Rows.Count == 0)
                                {
                                    //中断
                                    Dbg.ErrorWithView(Properties.Resources.E_READFAILED_TDL);
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
                                //健診ヘッダーの削除フラグが0だけ抽出
                                mHdrRows =
                                    mHdrTbl.AsEnumerable()
                                    .Where(x => x["削除フラグ"].ToString() == "0")
                                    .ToArray();

                                if (mHdrRows.Length <= 0)
                                {
                                    Dbg.ErrorWithView(Properties.Resources.E_HDR_IS_EMPTY);
                                    mState = CONVERT_STATE.END;
                                    break;
                                }

                                //健診ヘッダーの重複の確認(何をもって重複とするか検討)
                                var dr_array = from row in mHdrRows.AsEnumerable()
                                               where (
                                                   from _row in mHdrRows.AsEnumerable()
                                                   where
                                                   row["個人番号"].ToString() == _row["個人番号"].ToString()
                                                   && row["健診実施日"].ToString() == _row["健診実施日"].ToString()
                                                   && row["健診実施機関名称"].ToString() == _row["健診実施機関名称"].ToString()
                                                   select _row["個人番号"]
                                               ).Count() > 1 //重複していたら、２つ以上見つかる
                                               select row;

                                //DataTableが大きすぎるとここで処理が終わらない事がある。
                                //※現在ユーザー毎に処理する様に変更した為問題は起きないはず。
                                int overlapcount = dr_array.Count();

                                if (overlapcount > 0)
                                {
                                    Dbg.Warn("受診者の重複件数：" + overlapcount);
                                    foreach (var row in dr_array )
                                    {
                                        Dbg.Warn("重複個人番号：{0} 健診実施日:{1} 健診実施機関名称:{2}"
                                            ,row["個人番号"].ToString()
                                            ,row["健診実施日"].ToString()
                                            ,row["健診実施機関名称"].ToString());
                                    }
                                }

                                //次の処理へ
                                mState = CONVERT_STATE.CONVERT_MAIN;
                            }
                            break;

                        //メモリが不足して正常に動作しない為、ヘッダーの一行毎に処理する
                        case CONVERT_STATE.CONVERT_MAIN:
                            {
                                DataRow hrow = mHdrRows[mHdrIndex];
                                //Dbg.Log("個人番号:" + hrow["個人番号"].ToString());

                                //健診ヘッダーと健診データを結合し、１ユーザー分の検査項目一覧を抽出する。
                                var merged = JoinHdrWithTdl(hrow, mTdlTbl);
                                if (merged.Count() <= 0)
                                {
                                    //結合した結果データが無い
                                    Dbg.ErrorWithView(Properties.Resources.E_MERGED_DATA_IS_EMPTY);

                                    //次のユーザーへ
                                    mHdrIndex++;
                                    if (mHdrIndex >= mHdrRows.Length || mHdrIndex > 10)
                                    {
                                        mState = CONVERT_STATE.CONVERT_OUTPUT;
                                    }
                                    break;
                                }

                                //出力情報の一行分作成
                                DataRow outputrow = mOutputCsv.NewRow();

                                //TODO:人事データ結合(ここで結合できない人事をワーニングとして出力する)

                                //TODO:個人番号をセット
                                outputrow[6]  = hrow["個人番号"].ToString();    //仮

                                //項目マッピング処理
                                DataTable itemSheet = mMasterSheets.Tables["項目マッピング"];

                                //出力情報に固定値をセット
                                var fixvalue = itemSheet.AsEnumerable()
                                     .Where(x => x["固定値"].ToString() != "");

                                foreach (var row in fixvalue)
                                {
                                    int index = int.Parse(row.Field<string>("列順"));

                                    outputrow[index] = row.Field<string>("固定値");
                                }

                                /*
                                //必須項目に値がセットされているか確認
                                var datearray = itemSheet.AsEnumerable()
                                    .Where(x => x["種別"].ToString().Trim() == "年月日");

                                foreach (var row in datearray)
                                {
                                    int index = int.Parse(row.Field<string>("列順"));
                                    string value = hrow["健診実施日"].ToString();

                                    //年月日の変換
                                    DateTime d;
                                    if (DateTime.TryParseExact(value, "yyyyMMdd", null, DateTimeStyles.None, out d))
                                    {
                                        //日付
                                        outputrow[index] = d.ToString("yyyy/MM/dd");
                                    }
                                    else
                                    {
                                        //TODO: エラー表示
                                    }
                                }
                                */

                                //必須項目に値がセットされているか確認
                                var required = itemSheet.AsEnumerable()
                                    .Where(x => x["必須"].ToString().Trim() == "○");

                                foreach (var row in required)
                                {
                                    int index = int.Parse(row.Field<string>("列順"));
                                    string value = outputrow[index].ToString();

                                    if (value == "-")
                                    {
                                        Dbg.ErrorWithView(Properties.Resources.E_NOT_REQUIRED_FIELD, row.Field<string>("項目名"));

                                        //必須項目に値が無い場合は、そのデータを作成しない。
                                        return 1;
                                    }

                                    //種別のチェック
                                    if (!CheckMappingType(row.Field<string>("種別"), value))
                                    {
                                        Dbg.ErrorWithView(Properties.Resources.E_ITEM_TYPE_MISMATCH, row.Field<string>("項目名"), row.Field<string>("種別"), value);

                                        //必須項目に値が無い場合は、そのデータを作成しない。
                                        return 1;
                                    }

                                }


                                //検査項目コードの重複の確認
                                var dr_overlaped = from row in merged.AsEnumerable()
                                               where (
                                                   from _row in merged.AsEnumerable()
                                                   where
                                                   row.KensakoumokuCode == _row.KensakoumokuCode        //検査項目コード
                                                   && row.KensakoumokuName == _row.KensakoumokuName     //検査項目名
                                                   && row.Value == _row.Value                           //値
                                                   select _row.KensakoumokuCode
                                               ).Count() > 1 //重複していたら、２つ以上見つかる
                                               select row;

                                int overlapcount = dr_overlaped.Count();
                                if (overlapcount > 0)
                                {
                                    Dbg.Warn("個人番号 :{0} 検査項目コードの重複件数：{1}",
                                        hrow["個人番号"].ToString(),
                                        overlapcount.ToString());

                                    foreach (var row in dr_overlaped)
                                    {
                                        Dbg.Warn("重複検査項目コード：{0} 検査項目名称:{1} 検査値:{2}"
                                          , row.KensakoumokuCode
                                          , row.KensakoumokuName
                                          , row.Value);
                                    }
                                }


                                //TODO:オーダーマッピング（特定の検査項目コードの絞込）
                                //OrderMapping(itemSheet, itemMapped);

                                //項目マッピングから該当する検査項目コード一覧を抽出（複数の検査項目コードも抽出される）
                                var itemMapped = JoinItemMapWithMergedMap(itemSheet, merged);


                                //必要な検査項目コード分ループ
                                foreach (var itemrow in itemMapped)
                                {
                                    //Dbg.Log(itemrow.OutputHdrIndex + " " + itemrow.Value);

                                    //種別のチェック
                                    string value = itemrow.Value;
                                    if (!CheckMappingType(itemrow.Type, value))
                                    {
                                        Dbg.ErrorWithView(Properties.Resources.E_ITEM_TYPE_MISMATCH, itemrow.ItemName, itemrow.Type, value);

                                        value = "";
                                    }

                                    //TODO:コードマッピング（属性が「コード」の場合、値の置換）
                                    //CodeMapping(itemMapped,itemSheet);

                                    //出力情報に指定列順で値をセット
                                    outputrow[itemrow.OutputHdrIndex] = value;
                                }

                                // CSV出力情報に追加
                                mOutputCsv.Rows.Add(outputrow);

                                //次のユーザー
                                mHdrIndex++;
                                if (mHdrIndex >= mHdrRows.Length || mHdrIndex > 1)
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
                                Dbg.ViewLog("CSV作成中...（件数{0})", mHdrIndex.ToString());

                                //出力用CSVのカラム名をDataRowの配列で取得（3018行分）
                                var rows = mMasterSheets.Tables["項目マッピング"].AsEnumerable()
                                      .Where(x => x["列順"].ToString() != "")
                                      .ToArray();

                                //int max = rows.Max(r => int.Parse(r["列順"].ToString()));
                                //int max = rows.Length;

                                //最適化できそう
                                List<string> str_arry = new List<string>();

                                //初期
                                foreach(var r in rows)
                                {
                                    str_arry.Add("-");
                                }

                                //列順の項目を書き換え
                                foreach (var r in rows)
                                {
                                    str_arry[int.Parse(r.Field<string>("列順"))-1] = r.Field<string>("項目名");
                                }

                                UtilCsv csv = new UtilCsv();
                                csv.WriteFile(mPathOutput, mOutputCsv, str_arry);

                                mState = CONVERT_STATE.END;
                            }
                            break;

                        //終了
                        default:
                            {
                                //Dbg.Log("終了");
                                PurgeLoadedMemory();
                                loop = false;
                                break;
                            }
                    }

                }
                catch(Exception ex)
                {
                    MultiThreadCancel();
                    Dbg.ErrorWithView("state:" + mState);
                    Dbg.ErrorWithView(ex.ToString());
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

        /// <summary>
        /// 健診ヘッダーと健診データを結合し、１ユーザー分の検査項目一覧を抽出する。
        /// </summary>
        /// <param name="DataRow">１ユーザー分の健診ヘッダー</param>
        /// <param name="DataTable">健診データ</param>
        /// <returns>１ユーザー分の検査項目一覧</returns>
        private IEnumerable<MergedMap> JoinHdrWithTdl(DataRow hrow, DataTable tdlTable)
        {
            /*
            .Join(
                  結合するテーブル,
                  結合する側の結合条件（TeamTable）,
                  結合される側の結合条件（PersonTable）,
                  (（結合する側を指す範囲変数）, （結合される側を指す範囲変数）)
　　　                                 => new
                  {
                     （結合後のテーブル）
                  }) 
            */

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

            //TDLとHDRを結合して取得
            var merged =
                    from h in hdt.AsEnumerable()
                    join d in tdlTable.AsEnumerable() on h.Field<string>("組合C").Trim() equals d.Field<string>("組合C").Trim()
                    where
                        h.Field<string>("健診基本情報管理番号").Trim() == d.Field<string>("健診基本情報管理番号").Trim()
                        && d.Field<string>("削除フラグ").Trim() == "0"
                        && d.Field<string>("未実施FLG").Trim() == "0"
                        && d.Field<string>("測定不能FLG").Trim() == "0"
                    select new MergedMap
                    {
                        //ヘッダー情報は、人事データ結合時に処理する。
                        KensakoumokuCode = d.Field<string>("検査項目コード").Trim(),
                        KensakoumokuName = d.Field<string>("検査項目名称").Trim(),
                        KenshinmeisaiNo = d.Field<string>("健診明細情報管理番号").Trim(),
                        Value = (d.Field<string>("結果値データタイプ").Trim() == "4") ? d.Field<string>("コメント").Trim() : d.Field<string>("結果値").Trim(),
                    };
            //UtilCsv csv = new UtilCsv();
            //csv.WriteFile(".\\merged_"+ hrow["個人番号"].ToString()+".csv", csv.CreateDataTable(merged));

            return merged;
        }



        /// <summary>
        /// 項目マッピングから該当する必須項目一覧を抽出
        /// </summary>
        /// <param name="itemSheet">シート「項目マッピング」</param>
        /// <param name="ItemMap">１ユーザー分の検査項目一覧</param>
        /// <returns>項目マッピングの一覧</returns>
        private IEnumerable<ItemMap> JoinItemMapWithMergedMap(DataTable itemSheet, IEnumerable<MergedMap> merged)
        {
            var itemMapped =
                    from m in merged.AsEnumerable()
                    join t in itemSheet.AsEnumerable() on m.KensakoumokuCode.ToString() equals t.Field<string>("検査項目コード").Trim()
                    select new ItemMap
                    {
                        KensakoumokuCode = m.KensakoumokuCode,      //検査項目コード
                        OutputHdrIndex = t.Field<string>("列順"),
                        ItemName = t.Field<string>("項目名"),
                        Attribute = t.Field<string>("属性"),
                        CodeID = t.Field<string>("コードID"),
                        Type = t.Field<string>("種別"),             //半角英数等
                        //Rate = t.Field<string>("倍率"),
                        //StringFormat = t.Field<string>("文字フォーマット"),
                        Value = m.Value,                            //検査値
                    };

            //UtilCsv csv = new UtilCsv();
            //csv.WriteFile(".\\項目.csv", csv.CreateDataTable(itemMapped));

            return itemMapped;
        }

 
        /// <summary>
        /// 文字列が符号ありの小数かどうかを判定します
        /// </summary>
        /// <param name="target">対象の文字列</param>
        /// <returns>文字列が符号ありの小数の場合はtrue、それ以外はfalse</returns>
        public static bool IsDecimal(string target)
        {
            return new Regex("^[-+]?[0-9]*\\.?[0-9]+$").IsMatch(target);
        }

        /// <summary>
        /// 種別と検査値の判定をします
        /// </summary>
        /// <param name="ItemMap">抽出した項目マッピングの行</param>
        /// <returns>検査値</returns>
        private bool CheckMappingType(string type, string value)
        {
            switch (type)
            {
                /*
                case "数字":
                    {
                        try
                        {
                            if (itemMap.Rate != "")
                            {
                                float v = float.Parse(itemMap.Value) * float.Parse(itemMap.Rate);
                                return string.Format(itemMap.StringFormat, v).ToString();
                            }
                        }
                        catch (Exception ex)
                        {
                            Dbg.ErrorWithView(Properties.Resources.E_STRING_FORMAT_FAILE, itemMap.ItemName, itemMap.Value);
                        }

                        ret = itemMap.Value;
                    }
                    break;
                */

                case "半角数字":
                case "数値":
                    {
                        int i = 0;
                        if(!int.TryParse(value, out i))
                        {
                            float f = 0.0f;
                            if (!float.TryParse(value, out f))
                            { 
                                //エラーの場合空白として出力
                                return false;
                            }
                        }
                    }
                    break;

                case "年月日":
                    {
                        DateTime d;

                        if (!DateTime.TryParseExact(value, "yyyyMMdd", null, DateTimeStyles.None, out d))
                        {
                            //エラーの場合空白として出力
                            return false;
                        }
                    }
                    break;
            }

            return true;
        }
    }
}
