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
        private string mPathInput;
        private string mPathHR;
        private string mPathOutput;

        //設定ファイル
        private DataSet mMasterSheets = null;

        //項目マッピング
        private DataRow[] mItemMap = null;

        //オーダーマッピング
        //private DataRow[] mOrderMap = null;

        //コードマッピング
        private DataRow[] mCordMap = null;

        //人事データ
        private string mHRJoinKey = null;
        private DataRow[] mHRRows = null;

        //出力情報
        private DataTable mOutputCsv = null;


        //健診ヘッダーと健診データの結合用
        private class UserData
        {
            //public string userId { get; set; }                  //個人番号

            //public string date_of_consult { get; set; }         //受診日

            //検査項目コード
            public string InspectionItemCode { get; set; }

            //検査項目名称
            public string InspectionItemName { get; set; }

            //健診明細情報管理番号
            public string InspectionDetailID { get; set; }

            //結果値
            public string Value { get; set; }
        }

        //オーダーマッピング処理で使用
        private class OrderArray
        {
            public string Category { get; set; }
            public string[] InspectionItemCodeArray { get; set; }
        }

        private OrderArray[] mOrderArray = null;


        public ConverterMain()
        {
        }

        private DataSet ReadMasterFile(string path)
        {
            //Dbg.Log("master.xlsx 読み込み中...");

            UtilExcel excel = new UtilExcel();

            ExcelOption[] optionarray = new ExcelOption[]
            {
                new ExcelOption ( "各種設定",           2, 1, true),
                new ExcelOption ( "項目マッピング",     4, 1, true),
                new ExcelOption ( "コードマッピング",   3, 1, true),
                //new ExcelOption ( "JLAC10変換",         2, 1, true),
                //new ExcelOption ( "オーダーマッピング", 2, 1, true),
            };

            excel.SetExcelOptionArray(optionarray);

            DataSet master = excel.ReadAllSheets(path); 
            if(master == null)
            {
                return null;
            }

            return master;
        }

        public override void MultiThreadCancel()
        {
            base.MultiThreadCancel();
        }

        /// <summary>
        /// 各パスの設定（実行ボタン押下で呼ばれる）
        /// </summary>
        /// <param name="pathInput"></param>
        /// <param name="pathHR"></param>
        /// <param name="pathOutput"></param>
        public void InitConvert(string pathInput, string pathHR, string pathOutput)
        {
            mPathInput = pathInput;
            mPathHR = pathHR;
            mPathOutput = pathOutput;

            mItemMap  = null;
            //mOrderMap = null;
            mCordMap  = null;

            mHRRows = null;

            mOrderArray = null;

            mOutputCsv = null;

            Cancel = false;
        }


        //スレッド内の処理（これ自体をキャンセルはできない）
        public override int MultiThreadMethod()
        {
            Dbg.ViewLog("変換中...");
            Dbg.Debug("開始");

            try
            {
                //初期化と設定ファイルの読み込み
                if (!Init())
                {
                    return 0;
                }

                //人事データの読み込み
                mHRRows = ReadHumanResourceData(mPathHR);
                if (mHRRows == null)
                {
                    return 0;
                }

                //健診ヘッダーの読み込み
                DataTable hdrTbl = ReadHelthHeder(mPathInput);
                if (hdrTbl == null)
                {
                    return 0;
                }

                //健診データの読み込み
                DataTable tdlTbl = ReadHelthData(mPathInput);
                if (tdlTbl == null)
                {
                    return 0;
                }

                //健診ヘッダーから「削除フラグ=0」のユーザーのみ抽出
                DataRow[] hdrUsers = GetActiveUsers(hdrTbl);
                if (hdrUsers == null)
                {
                    return 0;
                }

                //一ユーザー毎に処理する
                int i = 0;
                foreach (var hrow in hdrUsers)
                {
                    //キャンセル
                    if (Cancel)
                    {
                        return 0;
                    }

                    //Dbg.ViewLog("{0} 個人番号:{1}", i.ToString(), hrow["個人番号"].ToString());

                    //変換処理
                    var h = hrow;
                    if (!ConvertMain(ref h, ref tdlTbl))
                    {
                        return 0;
                    }

                    i++;

                    //テスト用、１ユーザー分でやめる
                    //break;
                }

                //出力情報から全レコードの書き出し
                if (!WriteCsv())
                {
                    return 0;
                }
            }
            catch (Exception ex)
            {
                MultiThreadCancel();
                Dbg.ViewLog(ex.Message);    //メッセージのみ、ログ画面に表示
                Dbg.Error(ex.ToString());   //エラー内容全体は、ログファイルに書き出す
                return 0;
            }

            //処理完了
            Completed = true;
            return 1;
        }


        /// <summary>
        /// 初期化と設定ファイルの読み込み
        /// </summary>
        /// <returns></returns>
        bool Init()
        {
            string filename = "設定ファイル.xlsx";

            // 独自に設定した「appSettings」へのアクセス
            NameValueCollection appSettings = (NameValueCollection)ConfigurationManager.GetSection("appSettings");

            string path = appSettings["SettingPath"] + filename;
            Dbg.ViewLog("設定ファイルの読み込み:" + path);

            mMasterSheets = ReadMasterFile(path);
            if (mMasterSheets == null)
            {
                Dbg.ErrorWithView(Properties.Resources.E_READFAILED_MASTER, path);
                return false;
            }

            //出力用CSVの初期化
            mItemMap = mMasterSheets.Tables["項目マッピング"].AsEnumerable()
                  .Where(x => x["列順"].ToString() != "")
                  .ToArray();

            //項目マッピングの順列の最大値と項目数（個数）の確認
            if (mItemMap.Length != mItemMap.Max(r => int.Parse(r["列順"].ToString())))
            {
                Dbg.ErrorWithView(Properties.Resources.E_ITEMMAPPING_INDEX_FAILE);
                return false;
            }

            mOutputCsv = new DataTable();

            //同じ列名（カラム名）はセットできないので、列順をセットしておく
            foreach (var row in mItemMap)
            {
                //Dbg.Log("" + row["列順"]);
                mOutputCsv.Columns.Add("" + row["列順"], typeof(string));
            }


            /*
            //オーダーマッピング初期化
            mOrderMap = mMasterSheets.Tables["オーダーマッピング"].AsEnumerable()
                  .Where(x => x["検査項目コード"].ToString() != "")
                  .ToArray();
            */
            /*
            var inOrder = new string[][] {
                    new string[]{
                    "9A751000000000001",    //血圧 収縮期 1回目
                    "9A752000000000001",    //血圧 収縮期 2回目
                    "9A755000000000001"     //血圧 収縮期 3回目
                    }
                ,
                new string[] {
                    "9A761000000000001",    //血圧 拡張期 1回目
                    "9A762000000000001",    //血圧 拡張期 2回目
                    "9A765000000000001"     //血圧 拡張期 3回目
                    }
            };
            */
            /*
            //上記、カテゴリー別のstring 配列を動的生成 
            mOrderArray = mOrderMap.AsEnumerable()
                    .Where(x => x.Field<string>("検査項目コード") != "")
                    .GroupBy(x => new
                    {
                        category = x.Field<string>("カテゴリー"),
                    })
                    .Select(x => new OrderArray
                    {
                        Category = x.Key.category,
                        InspectionItemCodeArray = x.Select(y => y.Field<string>("検査項目コード")).ToArray()
                    })
                    .ToArray();
            */


            //コードマッピング初期化
            mCordMap = mMasterSheets.Tables["コードマッピング"].AsEnumerable()
                  .Where(x => x["コードID"].ToString() != "")
                  .ToArray();


            //人事データの結合用のキー（テレビ朝日とその他の団体で結合するキーが違う為）
            try
            {
                mHRJoinKey =
                  mMasterSheets.Tables["各種設定"].AsEnumerable()
                    .Where(x => x["名称"].ToString() == "人事データ結合列名")
                    .Select(x => x.Field<string>("設定値").ToString().Trim())
                    .First();
            }
            catch (Exception ex)
            {
                //処理中断
                Dbg.Error(ex.ToString());

                throw new MyException(Properties.Resources.E_MISMATCHED_HR_KEY);
            }


            //次の処理へ
            return true;
        }

        /// <summary>
        /// 健診ヘッダーの読み込み
        /// </summary>
        /// <returns></returns>
        DataTable ReadHelthHeder(string path)
        {
            var filename=
                mMasterSheets.Tables["各種設定"].AsEnumerable()
                  .Where(x => x["名称"].ToString() == "健診ヘッダー")
                  .Select(x => x.Field<string>("設定値").ToString().Trim())
                  .First();

            UtilCsv　csv = new UtilCsv();
            DataTable tbl = csv.ReadFile(path + "\\" + filename, ",", false, GlobalVariables.ENCORDTYPE.SJIS);
            if (tbl == null)
            {
                //中断
                Dbg.ErrorWithView(Properties.Resources.E_READFAILED_HDR);
                return null;
            }

            if (tbl.Rows.Count == 0)
            {
                //中断
                Dbg.ErrorWithView(Properties.Resources.E_READFAILED_HDR);
                return null;
            }

            SetColumnName(tbl, GlobalVariables.ColumnHDR);

            //次の処理へ
            return tbl;
        }

        /// <summary>
        /// 健診データの読み込み
        /// </summary>
        /// <returns></returns>
        DataTable ReadHelthData(string path)
        {
            var filename =
                mMasterSheets.Tables["各種設定"].AsEnumerable()
                  .Where(x => x["名称"].ToString() == "健診データ")
                  .Select(x => x.Field<string>("設定値").ToString().Trim())
                  .First();

            UtilCsv csv = new UtilCsv();
            DataTable tbl = csv.ReadFile(path + "\\" + filename, ",", false, GlobalVariables.ENCORDTYPE.SJIS);
            if (tbl == null)
            {
                //中断
                Dbg.ErrorWithView(Properties.Resources.E_READFAILED_TDL);
                return null;
            }

            if (tbl.Rows.Count == 0)
            {
                //中断
                Dbg.ErrorWithView(Properties.Resources.E_READFAILED_TDL);
                return null;
            }

            SetColumnName(tbl, GlobalVariables.ColumnTDL);
            return tbl;
        }

        /// <summary>
        /// 人事の読み込み
        /// </summary>
        /// <returns></returns>
        DataRow[] ReadHumanResourceData(string path)
        {
            UtilCsv csv = new UtilCsv();

            DataTable tbl = csv.ReadFile(path, ",", true, GlobalVariables.ENCORDTYPE.SJIS);
            if (tbl == null)
            {
                //中断
                Dbg.ErrorWithView(Properties.Resources.E_READFAILED_TDL);
                return null;
            }

            if (tbl.Rows.Count == 0)
            {
                //中断
                Dbg.ErrorWithView(Properties.Resources.E_READFAILED_TDL);
                return null;
            }

            //健診ヘッダーの削除フラグが0だけ抽出
            DataRow[] row =
                tbl.AsEnumerable()
                .Where(x => x["削除"].ToString() == "0")
                .ToArray();

            /*
            if (hrTabl.Columns.Contains("健康ポイントID"))
            { 
                 foreach (var h in hdrRows)
                {
                    Dbg.ViewLog(""+ h.Field<string>("健康ポイントID"));
                }
            }
            */

            return row;
        }


        /// <summary>
        /// 有効なユーザーの一覧取得
        /// </summary>
        /// <param name="HdrTbl"></param>
        /// <returns></returns>
        DataRow[] GetActiveUsers(DataTable HdrTbl)
        {
            //健診ヘッダーの削除フラグが0だけ抽出
            DataRow[] hdrRows =
                HdrTbl.AsEnumerable()
                .Where(x => x["削除フラグ"].ToString() == "0")
                .ToArray();

            if (hdrRows.Length <= 0)
            {
                Dbg.ErrorWithView(Properties.Resources.E_HDR_IS_EMPTY);
                return null;
            }

            //健診ヘッダーの重複の確認(何をもって重複とするか検討)
            var dr_array = from row in hdrRows.AsEnumerable()
                           where (
                               from _row in hdrRows.AsEnumerable()
                               where
                               row["個人番号"].ToString() == _row["個人番号"].ToString()
                               && row["健診実施日"].ToString() == _row["健診実施日"].ToString()
                               && row["健診実施機関名称"].ToString() == _row["健診実施機関名称"].ToString()
                               select _row["個人番号"]
                           ).Count() > 1 //重複していたら、２つ以上見つかる
                           select row;

            //DataTableが大きすぎるとここで処理が終わらない事がある。※現在ユーザー毎に処理する様に変更した為問題は起きないはず。
            int overlapcount = dr_array.Count();
            if (overlapcount > 0)
            {
                //重複件数の表示
                Dbg.ErrorWithView(Properties.Resources.E_DUPLICATE_USERS_COUNT
                        , overlapcount.ToString());

                //重複している行を表示
                foreach (var row in dr_array)
                {
                    Dbg.ErrorWithView(Properties.Resources.E_DUPLICATE_USERS_INFO
                        , row["個人番号"].ToString()
                        , row["健診実施日"].ToString()
                        , row["健診実施機関名称"].ToString().Trim());
                }

                //重複したデータをそのまま出力する
            }

            //次の処理へ
            return hdrRows;
        }


        /// <summary>
        /// 引数の文字列が半角英数字のみで構成されているかを調べる。
        /// </summary>
        /// <param name="text">チェック対象の文字列。</param>
        /// <returns>引数が英数字のみで構成されていればtrue、そうでなければfalseを返す。</returns>
        public static bool IsOnlyAlphaWithNumeric(string text)
        {
            // 文字列の先頭から末尾までが、英数字のみとマッチするかを調べる。
            return (Regex.IsMatch(text, @"^[0-9a-zA-Z]+$"));
        }


        /// <summary>
        /// 変換処理メイン
        /// </summary>
        /// <param name="hrow"></param>
        /// <param name="TdlTbl"></param>
        /// <returns></returns>
        bool ConvertMain(ref DataRow hrow, ref DataTable TdlTbl)
        {
            var userID = hrow["個人番号"].ToString();

            //健診ヘッダーと健診データを結合し、１ユーザー分の検査項目一覧を抽出する。
            var userdata = CreateUserData(
                        ref hrow
                        , ref TdlTbl);

            if (userdata.Count() <= 0)
            {
                //結合した結果データが無い
                Dbg.ErrorWithView(Properties.Resources.E_MERGED_DATA_IS_EMPTY);

                //次のユーザーへ
                return true;
            }

            //人事データ取得(健診ヘッダーの個人番号と「各種設定」で指定したキーで取得)
            DataRow hr_row = GetHumanResorceRow(userID, mHRJoinKey);
            if (hr_row == null)
            {
                Dbg.ErrorWithView(Properties.Resources.E_NO_USERDATA
                    , userID);

                //存在しない場合はレコードを作成しないで次のユーザーへ
                return true;
            }

            //旧検査項目コードの書き換え
            //userdata = ReplaceInspectItemCode(ref userdata,  mMasterSheets.Tables["JLAC10変換"], userID, hrow["健診実施日"].ToString());

            //オーダーマッピング（特定の検査項目コードの絞込）
            //userdata = OrderMapping(ref userdata, ref mOrderMap, userID);

            //出力情報の一行分作成
            DataRow outputrow = mOutputCsv.NewRow();        //カラムは、0始まり

            bool requestFiledError = false;

            //項目マッピング処理
            //必要な検査項目コード分ループ
            foreach (var row in mItemMap)
            {
                string outputtype = row.Field<string>("出力形式").Trim();
                if (outputtype == "該当なし")
                {
                    continue;
                }

                int index = int.Parse(row.Field<string>("列順"));     //列順は１始まり

                string value = "";

                //固定値
                string fixvalue = row.Field<string>("固定値").Trim();
                if (fixvalue != "")
                {
                    value = fixvalue;
                }

                //団体IDの確認(固定)
                if(index == 4)
                {
                    //「参照人事」で指定した項目名で検索
                    try
                    {
                        string hrcolumn = row.Field<string>("参照人事").Trim();

                        var hr_id = mHRRows
                            .Where(x => x.Field<string>(hrcolumn) == value)
                            .First();
                    }
                    catch (Exception ex)
                    {
                        Dbg.ErrorWithView(Properties.Resources.E_MISMATCHED_ORGANIZATION_ID
                                , value);

                        Dbg.Error(ex.ToString());

                        //処理中断
                        throw new MyException(Properties.Resources.E_PROCESSING_ABORTED);
                    }
                }


                //人事データ結合
                if(value == "")
                {
                    string hrcolumn = row.Field<string>("参照人事");
                    if(hrcolumn != "")
                    {
                        //人事の指定列名
                        hrcolumn = hrcolumn.Trim();

                        //項目マッピングで指定した列名の値をセット
                        try
                        {
                            value = hr_row.Field<string>(hrcolumn).Trim();
                        }
                        catch (Exception ex)
                        {
                            Dbg.ErrorWithView(Properties.Resources.E_NOT_EXIST_ITEM_IN_HR
                                    , hrcolumn);

                            Dbg.Error(ex.ToString());

                            //処理中断
                            throw new MyException(Properties.Resources.E_PROCESSING_ABORTED);
                        }
                    }
                }

                //参照健診ヘッダーの取得
                if (value == "")
                {
                    string inspectionHeader = row.Field<string>("参照健診ヘッダー").Trim();
                    if(inspectionHeader != "")
                    {
                        //現状、健診実施日と健診実施機関番号のみ
                        //Dbg.ViewLog(inspectionHeader);
                        try
                        {
                            value = hrow[inspectionHeader].ToString();
                        }
                        catch (Exception ex)
                        {
                            Dbg.Error(ex.ToString());

                            //処理中断
                            throw new MyException(Properties.Resources.E_PROCESSING_ABORTED);
                        }
                    }
                }


                //検査項目コードの検索
                if (value == "")
                {
                    string inspectcord = row.Field<string>("検査項目コード").Trim();
                    if (inspectcord != "")
                    {
                        //検査項目コードに半角英数以外が使われているか確認
                        if(!IsOnlyAlphaWithNumeric(inspectcord))
                        {
                            Dbg.ViewLog(Properties.Resources.E_MISMATCHED_INSPECTCORD_TYPE
                                , inspectcord);

                            //処理中断
                            throw new MyException(Properties.Resources.E_PROCESSING_ABORTED);
                        }

                        //ユーザーデータから検査値を抽出
                        var retvalue =  userdata.AsEnumerable()
                                .Where(x => x.InspectionItemCode == inspectcord)
                                .Select(x => x.Value)
                                .FirstOrDefault();

                        //検査値
                        if (!string.IsNullOrEmpty(retvalue))
                        {
                            value = retvalue;
                        }
                    }
                }

                //コードマッピング（属性が「コード」の場合、値の置換）
                if (value != "" && row.Field<string>("属性") == "コード")
                {
                    var codeid = row.Field<string>("コードID").Trim();

                    //コードマッピング処理
                    value = GetCodeMapping(value, codeid, userID);
                }

                //種別と値のチェック
                if (value != "")
                {
                    //種別
                    string type = row.Field<string>("種別").Trim();

                    //種別が数値を期待しているのに、数値以外の値の場合はエラーとする
                    value = CheckMappingType(type, value, userID, row.Field<string>("項目名"));
                }


                //必須項目確認
                string request = row.Field<string>("必須").Trim();
                if (request == "○" && value == "")
                {
                    //必須項目に値が無い場合は、そのデータを作成しない。
                    Dbg.ErrorWithView(Properties.Resources.E_NO_VALUE_REQUIRED_FIELD
                        ,userID
                        ,row.Field<string>("項目名"));

                    //必須項目でエラーの場合はフラグを立てる
                    requestFiledError = true;
                }

                //出力情報に指定列順で値をセット
                outputrow[index - 1] = value;
            }

            //全ての必須項目で一つでもエラーがあれば、レコードを作成しない
            if (!requestFiledError)
            {
                // CSV出力情報に追加
                mOutputCsv.Rows.Add(outputrow);
            }

            outputrow = null;

            //次のユーザー
            return true;
        }

        /// <summary>
        /// CSVの書き出し
        /// </summary>
        /// <returns></returns>
        bool WriteCsv()
        {
            Dbg.ViewLog("CSV作成中...（件数{0})", mOutputCsv.Rows.Count.ToString());

            //出力用CSVのカラム名をDataRowの配列で取得（3018行分）
            //int max = rows.Max(r => int.Parse(r["列順"].ToString()));
            //int max = rows.Length;

            //最適化できそう
            List<string> str_arry = new List<string>();

            //初期
            foreach (var r in mItemMap)
            {
                str_arry.Add("-");
            }

            //列順の項目を書き換え
            foreach (var r in mItemMap)
            {
                str_arry[int.Parse(r.Field<string>("列順")) - 1] = r.Field<string>("項目名");
            }

            UtilCsv csv = new UtilCsv();
            csv.WriteFile(mPathOutput, mOutputCsv, str_arry);

            return true;
        }

        /// <summary>
        /// 列名（カラム名）を付け加える
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="sheet"></param>
        void SetColumnName(DataTable dt, List<string> sheet)
        {
            int n = sheet.Count;
            for (int i=0; i< sheet.Count(); i++)
            {
                //Dbg.Log(rows[i][0].ToString());
                if(i<n)
                {
                    dt.Columns[""+(i+1)].ColumnName = sheet[i].ToString().Trim();
                }
                else
                {
                    dt.Columns.Add(sheet[i].ToString().Trim());
                }
            }

        }

        /// <summary>
        /// 健診ヘッダーと健診データを結合し、１ユーザー分の検査項目一覧を作成する
        /// </summary>
        /// <param name="DataRow">１ユーザー分の健診ヘッダー</param>
        /// <param name="DataTable">健診データ</param>
        /// <returns>１ユーザー分の検査項目一覧</returns>
        private List<UserData> CreateUserData(ref DataRow hrow, ref DataTable tdlTable)
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
                    hrow["組合C"].ToString(),
                    hrow["健診基本情報管理番号"].ToString(),
                    hrow["健診実施日"].ToString(),
                    hrow["個人番号"].ToString()
                );

            //TDLとHDRを結合して取得
            var merged =
                    from h in hdt.AsEnumerable()
                    join d in tdlTable.AsEnumerable() on h.Field<string>("組合C") equals d.Field<string>("組合C")
                    where
                        h.Field<string>("健診基本情報管理番号") == d.Field<string>("健診基本情報管理番号")
                        && d.Field<string>("削除フラグ") == "0"
                        && d.Field<string>("未実施FLG") == "0"
                        && d.Field<string>("測定不能FLG") == "0"
                    select new UserData
                    {
                        //ヘッダー情報は、人事データ結合時に処理する。
                        InspectionItemCode = d.Field<string>("検査項目コード").Trim(),
                        InspectionItemName = d.Field<string>("検査項目名称").Trim(),
                        InspectionDetailID = d.Field<string>("健診明細情報管理番号").Trim(),

                        //コメントのTrimはしない
                        Value = (d.Field<string>("結果値データタイプ") == "4") ? d.Field<string>("コメント") : d.Field<string>("結果値").Trim(),
                    };


            // 外部結合を行うメソッド式
            /*
             * 同じ項目が増えるの使えない
            var outerJoin =
                merged.GroupJoin(jlacTable.AsEnumerable(), p => p.InspectionItemCode , j => j.Field<string>("旧検査項目コード"), (p, j) => new
                {
                    InspectionItemCode = p.InspectionItemCode,
                    InspectionItemName = p.InspectionItemName,
                    InspectionDetailID = p.InspectionDetailID,
                    Value = p.Value,
                    NewInspectionItemCode = j.DefaultIfEmpty()
                })
                .SelectMany(x => x.NewInspectionItemCode, (x, j) => new 
                {
                    InspectionItemCode = x.InspectionItemCode,
                    InspectionItemName = x.InspectionItemName,
                    InspectionDetailID = x.InspectionDetailID,
                    Value = x.Value,
                    NewInspectionItemCode = j != null ? j.Field<string>("新検査項目コード") : ""
                });
            */

            //return merged.ToList();

            /*
            if (ret.Count() > 0)
            {
                UtilCsv csv = new UtilCsv();
                csv.WriteFile(".\\out\\UserData_" + hrow["個人番号"].ToString() + "a.csv", csv.CreateDataTable(merged));
                csv.WriteFile(".\\out\\UserData_" + hrow["個人番号"].ToString() + "b.csv", csv.CreateDataTable(ret));
            }
            */

            return merged.ToList();
        }

        /// <summary>
        /// 旧検査項目コードを新検査項目コードに置換します。
        /// </summary>
        /// <param name="user"></param>
        /// <param name="jlacTable"></param>
        /// <returns></returns>
        private List<UserData> ReplaceInspectItemCode(ref List<UserData> user, DataTable jlacTable, string userID, string date)
        {
            List<UserData> ret = new List<UserData>();

            foreach (var m in user)
            {
                var newcode = jlacTable.AsEnumerable()
                                .Where(x => x.Field<string>("旧検査項目コード") == m.InspectionItemCode && x.Field<string>("置換対象") == "〇")
                                .Select(x => x.Field<string>("新検査項目コード"))
                                .FirstOrDefault();

                if (!string.IsNullOrEmpty(newcode))
                {
                    m.InspectionItemCode = newcode.Trim();
                    //Dbg.Debug("個人番号：{0} 健診実施日:{1} newcode：{2}", userID, date, m.InspectionItemCode);
                }

                //refが使えない為、値を書き換えて別に保存
                ret.Add(m);
            }

            /*
            if (ret.Count() > 0)
            {
                UtilCsv csv = new UtilCsv();
                csv.WriteFile(".\\out\\UserData_a.csv", csv.CreateDataTable(user));
                csv.WriteFile(".\\out\\UserData_b.csv", csv.CreateDataTable(ret));
            }
            */

            return ret.ToList();
        }


        /*
        /// <summary>
        /// 項目マッピングから該当する必須項目一覧を抽出
        /// </summary>
        /// <param name="itemSheet">シート「項目マッピング」</param>
        /// <param name="ItemMap">１ユーザー分の検査項目一覧</param>
        /// <returns>項目マッピングの一覧</returns>
        private IEnumerable<ItemMap> GetItemMapByUserData(DataTable itemSheet, IEnumerable<UserData> merged)
        {
            var itemMapped =
                    from m in merged.AsEnumerable()
                    join t in itemSheet.AsEnumerable() on m.InspectionItemCode.ToString() equals t.Field<string>("検査項目コード").Trim()
                    select new ItemMap
                    {
                        InspectionItemCode = m.InspectionItemCode,      //検査項目コード
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
        */

        /// <summary>
        /// オーダーマッピング処理
        /// 優先度が高い検査項目コードだけ残す
        /// </summary>
        /// <param name="merged"></param>
        /// <param name="ordermap"></param>
        /// <param name="userID"></param>
        /// <returns></returns>
        private UserData[] OrderMapping(ref UserData[] merged, ref DataRow[] ordermap, string userID)
        {
            /* -------
             * 元のSQL
             * -------
                select
                    top 1 値 
                from
                    10_検査結果 
                where
                    組合C = h.組合C 
                    and 健診基本情報管理番号 = h.健診基本情報管理番号 
                    and JLAC10 in ( 
                        '9A751000000000001'
                        , '9A752000000000001'
                        , '9A755000000000001'
                    ) 
                order by
                    JLAC10
             */

            foreach (var order in mOrderArray)
            {
                // IN句の条件
                /*
                var inCause = new string[] {
                    "9A751000000000001",    //血圧 収縮期 1回目
                    "9A752000000000001",    //血圧 収縮期 2回目
                    "9A755000000000001"     //血圧 収縮期 3回目
                    };
                */

                //IN句を動的生成
                var inCause = order.InspectionItemCodeArray;

                //ユーザーデータから抽出
                try
                {
                    //Dbg.ViewLog("category:" + order.category);

                    var userdataArray = merged.AsEnumerable()
                        .Where(x => inCause.Contains(x.InspectionItemCode))
                        .OrderBy(x => x.InspectionItemCode)
                        .ToArray();

                    var remove = new List<UserData>();

                    int i = 0;
                    foreach (var o in userdataArray)
                    {
                        if (i >= 1)
                        {
                            //Dbg.ViewLog("code:" + o.InspectionItemCode + " v:" + o.Value);
                            remove.Add(o);
                        }
                        i++;
                    }

                    //ユーザーデータから優先度の低いものを削除
                    if (remove.Count > 0)
                    {
                        //書き換え
                        merged = merged.Except(remove).ToArray();

                        //確認
                        var resultArray = merged.AsEnumerable()
                            .Where(x => inCause.Contains(x.InspectionItemCode))
                            .OrderBy(x => x.InspectionItemCode)
                            .ToArray();

                        //上記処理により、１つしか残らないはず
                        if (resultArray.Count() != 1)
                        {
                            throw new MyException(Properties.Resources.E_ORDERMAPPING_ABORTED);
                        }

                        //残った優先度を表示
                        var top = resultArray[0];
                        Dbg.ViewLog(Properties.Resources.MSG_RESULT_ORDER_MAPPING
                            , top.InspectionItemCode
                            , top.Value
                            , userID);
                    }
                }
                catch (Exception ex)
                {
                    Dbg.ErrorWithView(Properties.Resources.E_ORDERMAPPING_FILED
                        , userID);
                    throw ex;
                }
            }


            return merged;
        }

        /// <summary>
        /// コードマッピング処理
        /// 指定のコードを置換する
        /// </summary>
        /// <param name="value"></param>
        /// <param name="codeid"></param>
        /// <param name="userID"></param>
        /// <returns></returns>
        string GetCodeMapping(string value, string codeid, string userID)
        { 
            //コードマッピング（属性が「コード」の場合、値の置換）
            try
            {
                //コードマッピングから抽出
                value = mCordMap.AsEnumerable()
                    .Where(x => x.Field<string>("コードID").Trim() == codeid && x.Field<string>("★コード").Trim() == value)
                    .Select(x => x.Field<string>("コード").Trim())
                    .First();
            }
            catch(Exception ex)
            {
                //エラー表示
                Dbg.ErrorWithView(Properties.Resources.E_CORDMAPPING_FILED
                    , userID
                    , codeid);

                Dbg.Error(ex.ToString());

                //エラーの場合空にする
                value = "";
            }

            return value;
        }


        /// <summary>
        /// 種別と検査値の判定をします
        /// </summary>
        /// <param name="ItemMap">抽出した項目マッピングの行</param>
        /// <returns>検査値</returns>
        private string CheckMappingType(string type, string value, string userID, string itenName)
        {
            switch (type)
            {
                case "半角数字":
                case "数値":
                    {
                        int i = 0;
                        if(!int.TryParse(value, out i))
                        {
                            float f = 0.0f;
                            if (!float.TryParse(value, out f))
                            {
                                Dbg.ErrorWithView(Properties.Resources.E_MISMATCHED_ITEM_TYPE
                                    , userID
                                    , itenName.Trim()
                                    , type
                                    , value);

                                //エラーの場合空白として出力
                                return "";
                            }
                        }
                    }
                    break;

                case "年月日":
                    {
                        //年月日の変換
                        DateTime d;
                        if (DateTime.TryParseExact(value, "yyyyMMdd", null, DateTimeStyles.None, out d))
                        {
                            //日付
                            value = d.ToString("yyyy/MM/dd");
                        }
                        else
                        {
                            //エラー表示
                            Dbg.ErrorWithView(Properties.Resources.E_MISMATCHED_ITEM_TYPE
                                , userID
                                , itenName.Trim()
                                , type
                                , value);

                            //エラーの場合空にする
                            value = "";
                        }
                    }
                    break;
            }

            return value;
        }

        DataRow GetHumanResorceRow(string userID, string hrcolumn)
        {
            DataRow row = null;

            /*
            if (!mHRRows[0].Table.Columns.Contains("健康ポイントID"))
            {
                //健康ポイントIDが無い場合は、指定の列名（カラム名）で検索
                if (!mHRRows[0].Table.Columns.Contains(hrcolumn))
                {
                    //存在しない場合はレコードを作成しないで次のユーザーへ
                    return null;
                }
            }
            else
            {
                hrcolumn = "健康ポイントID";
            }
            */


            //最終的に残った項目で検索
            try
            {
                row = mHRRows
                    .Where(x => x.Field<string>(hrcolumn) == userID)
                    .First();
            }
            catch (Exception ex)
            {
                Dbg.Error(ex.ToString());

                //存在しない場合はレコードを作成しないで次のユーザーへ
                return null;
            }

            return row;            
        }

        /*
        bool TestConvertMain(ref DataRow hrow, ref DataTable TdlTbl)
        {
            var userID = hrow["個人番号"].ToString();

            //健診ヘッダーと健診データを結合し、１ユーザー分の検査項目一覧を抽出する。
            var userdata = CreateUserData(ref hrow, ref TdlTbl)
                        .ToArray();

            if (userdata.Count() <= 0)
            {
                //結合した結果データが無い
                Dbg.ErrorWithView(Properties.Resources.E_MERGED_DATA_IS_EMPTY);

                //次のユーザーへ
                return true;
            }

            //出力情報の一行分作成
            DataRow outputrow = mOutputCsv.NewRow();        //カラムは、0始まり

            //TODO:人事データ結合(ここで結合できない人事をワーニングとして出力する)

            //TODO:個人番号をセット
            outputrow[5] = userID;    //仮

            // CSV出力情報に追加
            mOutputCsv.Rows.Add(outputrow);

            outputrow = null;

            //次のユーザー
            return true;
        }
        */

    }
}
