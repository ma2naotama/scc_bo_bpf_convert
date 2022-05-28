using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace ConvertDaiwaForBPF
{
    /// <summary>
    /// 変換処理メイン
    /// </summary>
    internal class ConverterMain : BaseThread
    {
        // 各種のパス
        private string mPathInput;
        private string mPathHR;
        private string mPathOutput;

        // 設定ファイル
        private DataSet mMasterSheets = null;

        // 項目マッピング
        private DataRow[] mItemMap = null;

        // コードマッピング
        private DataRow[] mCordMap = null;

        // 人事データ
        private string mHRJoinKey = null;
        private DataRow[] mHRRows = null;

        // 出力情報
        private DataTable mOutputCsv = null;


        /// <summary>
        /// コンストラクタ
        /// </summary>
        public ConverterMain()
        {
        }

        /// <summary>
        /// 設定ファイルの読み込み
        /// </summary>
        /// <param name="path">読み込み先</param>
        /// <returns></returns>
        private DataSet ReadMasterFile(string path)
        {
            //Dbg.Log("master.xlsx 読み込み中...");

            var excel = new UtilExcel();

            var optionarray = new ExcelOption[]
            {
                new ExcelOption ( "各種設定",           2, 1),
                new ExcelOption ( "項目マッピング",     4, 1),
                new ExcelOption ( "コードマッピング",   3, 1),
            };

            excel.SetExcelOptionArray(optionarray);

            var master = excel.ReadAllSheets(path);            
            return master;
        }

        /// <summary>
        /// スレッドのキャンセル
        /// </summary>
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
            mCordMap  = null;

            mHRRows = null;

            mOutputCsv = null;

            Cancel = false;

            Dbg.SetLogPath(mPathOutput);
        }

        /// <summary>
        /// スレッド内の処理（これ自体をキャンセルはできない）
        /// </summary>
        /// <returns></returns>
        public override int MultiThreadMethod()
        {
            Dbg.ViewLog("変換中...");
            Dbg.Debug("開始");

            try
            {
                // 初期化と設定ファイルの読み込み
                if (!Init())
                {
                    return 0;
                }

                // 人事データの読み込み
                mHRRows = ReadHumanResourceData(mPathHR);
                if (mHRRows == null)
                {
                    return 0;
                }

                // 健診ヘッダーの読み込み
                var hdrTbl = ReadHelthHeder(mPathInput);
                if (hdrTbl == null)
                {
                    return 0;
                }

                // 健診データの読み込み
                var tdlTbl = ReadHelthData(mPathInput);
                if (tdlTbl == null)
                {
                    return 0;
                }

                // 健診ヘッダーから「削除フラグ=0」のユーザーのみ抽出
                var hdrUsers = GetActiveUsers(hdrTbl);
                if (hdrUsers == null)
                {
                    return 0;
                }

                // 一ユーザー毎に処理する
                int i = 0;
                foreach (var hrow in hdrUsers)
                {
                    // キャンセル
                    if (Cancel)
                    {
                        return 0;
                    }

                    // 変換処理
                    var h = hrow;
                    if (!ConvertMain(ref h, ref tdlTbl))
                    {
                        return 0;
                    }

                    i++;

                    //テスト用、１ユーザー分でやめる
                    //break;
                }

                // 出力情報から全レコードの書き出し
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
            // 独自に設定した「appSettings」へのアクセス
            var appSettings = (NameValueCollection)ConfigurationManager.GetSection("appSettings");

            var path = appSettings["SettingPath"];
            Dbg.ViewLog(Properties.Resources.MSG_READ_SETTINGFILE, path);

            mMasterSheets = ReadMasterFile(path);

            // 出力用CSVの初期化
            mItemMap = mMasterSheets.Tables["項目マッピング"].AsEnumerable()
                  .Where(x => x["列順"].ToString() != "")
                  .ToArray();

            // 項目マッピングの順列の最大値と項目数（個数）の確認
            if (mItemMap.Length != mItemMap.Max(r => int.Parse(r["列順"].ToString())))
            {
                Dbg.ErrorWithView(Properties.Resources.E_ITEMMAPPING_INDEX_FAILE);
                return false;
            }

            mOutputCsv = new DataTable();

            // 同じ列名（カラム名）はセットできないので、列順をセットしておく
            foreach (var row in mItemMap)
            {
                mOutputCsv.Columns.Add("" + row["列順"], typeof(string));
            }

            // コードマッピング初期化
            mCordMap = mMasterSheets.Tables["コードマッピング"].AsEnumerable()
                  .Where(x => x["コードID"].ToString() != "")
                  .ToArray();


            // 人事データの結合用のキー（テレビ朝日とその他の団体で結合するキーが違う為）
            mHRJoinKey = mMasterSheets.Tables["各種設定"].AsEnumerable()
                    .Where(x => x["名称"].ToString() == "人事データ結合列名")
                    .Select(x => x.Field<string>("設定値").ToString().Trim())
                    .FirstOrDefault();

            if (string.IsNullOrEmpty(mHRJoinKey))
            {
                // 処理中断
                throw new MyException(Properties.Resources.E_MISMATCHED_HR_KEY);
            }


            // 次の処理へ
            return true;
        }

        /// <summary>
        /// 健診ヘッダーCSVの読み込み
        /// </summary>
        /// <returns>DataTable</returns>
        DataTable ReadHelthHeder(string path)
        {
            var filename=
                mMasterSheets.Tables["各種設定"].AsEnumerable()
                  .Where(x => x["名称"].ToString() == "健診ヘッダー")
                  .Select(x => x.Field<string>("設定値").ToString().Trim())
                  .First();

            var csv = new UtilCsv();
            var tbl = csv.ReadFile(path + "\\" + filename, ",", false, GlobalVariables.ENCORDTYPE.SJIS);
            if (tbl == null)
            {
                // 中断
                Dbg.ErrorWithView(Properties.Resources.E_READFAILED_HDR);
                return null;
            }

            if (tbl.Rows.Count == 0)
            {
                // 中断
                Dbg.ErrorWithView(Properties.Resources.E_READFAILED_HDR);
                return null;
            }

            SetColumnName(tbl, GlobalVariables.ColumnHDR);

            // 次の処理へ
            return tbl;
        }

        /// <summary>
        /// 健診データCSVの読み込み
        /// </summary>
        /// <returns>DataTable</returns>
        DataTable ReadHelthData(string path)
        {
            var filename =
                mMasterSheets.Tables["各種設定"].AsEnumerable()
                  .Where(x => x["名称"].ToString() == "健診データ")
                  .Select(x => x.Field<string>("設定値").ToString().Trim())
                  .First();

            var csv = new UtilCsv();
            var tbl = csv.ReadFile(path + "\\" + filename, ",", false, GlobalVariables.ENCORDTYPE.SJIS);
            if (tbl == null)
            {
                // 中断
                Dbg.ErrorWithView(Properties.Resources.E_READFAILED_TDL);
                return null;
            }

            if (tbl.Rows.Count == 0)
            {
                // 中断
                Dbg.ErrorWithView(Properties.Resources.E_READFAILED_TDL);
                return null;
            }

            SetColumnName(tbl, GlobalVariables.ColumnTDL);
            return tbl;
        }

        /// <summary>
        /// 人事CSVの読み込み
        /// </summary>
        /// <returns>DataRowの配列  削除されている人事を除く</returns>
        DataRow[] ReadHumanResourceData(string path)
        {
            var csv = new UtilCsv();
            var tbl = csv.ReadFile(path, ",", true, GlobalVariables.ENCORDTYPE.SJIS);
            if (tbl == null)
            {
                // 中断
                Dbg.ErrorWithView(Properties.Resources.E_READFAILED_TDL);
                return null;
            }

            if (tbl.Rows.Count == 0)
            {
                // 中断
                Dbg.ErrorWithView(Properties.Resources.E_READFAILED_TDL);
                return null;
            }

            // 健診ヘッダーの削除フラグが0だけ抽出
            var row =
                tbl.AsEnumerable()
                .Where(x => x["削除"].ToString() == "0")
                .ToArray();

            return row;
        }


        /// <summary>
        /// 有効なユーザーの一覧取得
        /// </summary>
        /// <param name="HdrTbl"></param>
        /// <returns>DataRowの配列  削除されている検診ヘッダーを除く</returns>
        DataRow[] GetActiveUsers(DataTable HdrTbl)
        {
            // 健診ヘッダーの削除フラグが0だけ抽出
            var hdrRows =
                HdrTbl.AsEnumerable()
                .Where(x => x["削除フラグ"].ToString() == "0")
                .ToArray();

            if (hdrRows.Length <= 0)
            {
                Dbg.ErrorWithView(Properties.Resources.E_HDR_IS_EMPTY);
                return null;
            }

            // 健診ヘッダーの重複の確認
            var dr_array = from row in hdrRows.AsEnumerable()
                           where (
                               from _row in hdrRows.AsEnumerable()
                               where
                               row["個人番号"].ToString() == _row["個人番号"].ToString()
                               && row["健診実施日"].ToString() == _row["健診実施日"].ToString()
                               && row["健診実施機関名称"].ToString() == _row["健診実施機関名称"].ToString()
                               select _row["個人番号"]
                           ).Count() > 1 // 重複していたら、２つ以上見つかる
                           select row;

            // DataTableが大きすぎるとここで処理が終わらない事がある。※現在ユーザー毎に処理する様に変更した為問題は起きないはず。
            var overlapcount = dr_array.Count();
            if (overlapcount > 0)
            {
                // 重複件数の表示
                Dbg.ErrorWithView(Properties.Resources.E_DUPLICATE_USERS_COUNT
                        , overlapcount.ToString());

                // 重複している行を表示
                foreach (var row in dr_array)
                {
                    Dbg.ErrorWithView(Properties.Resources.E_DUPLICATE_USERS_INFO
                        , row["個人番号"].ToString()
                        , row["健診実施日"].ToString()
                        , row["健診実施機関名称"].ToString().Trim());
                }

                // 重複したデータをそのまま出力する
            }

            // 次の処理へ
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


        // 団体IDの列順
        const int INDEX_OF_ORGANIZATION_ID = 4;

        /// <summary>
        /// 変換処理メイン
        /// </summary>
        /// <param name="hrow"></param>
        /// <param name="TdlTbl"></param>
        /// <returns></returns>
        bool ConvertMain(ref DataRow hrow, ref DataTable TdlTbl)
        {
            var userID = hrow["個人番号"].ToString();

            // 健診ヘッダーと健診データを結合し、１ユーザー分の検査項目一覧を抽出する。
            var userdata = CreateUserData(
                        ref hrow
                        , ref TdlTbl);

            if (userdata.Count() <= 0)
            {
                // 結合した結果データが無い
                Dbg.ErrorWithView(Properties.Resources.E_MERGED_DATA_IS_EMPTY);

                // 次のユーザーへ
                return true;
            }

            // 人事データ取得(健診ヘッダーの個人番号と「各種設定」で指定したキーで取得)
            var hr_row = GetHumanResorceRow(userID, mHRJoinKey);
            if (hr_row == null)
            {
                Dbg.ErrorWithView(Properties.Resources.E_NO_USERDATA
                    , userID);

                // 存在しない場合はレコードを作成しないで次のユーザーへ
                return true;
            }

            // 出力情報の一行分作成
            var outputrow = mOutputCsv.NewRow();        // カラムは、0始まり

            // 必須項目のエラーフラグ
            var requestFiledError = false;

            // 項目マッピング処理
            // 必要な検査項目コード分ループ
            foreach (var row in mItemMap)
            {
                string outputtype = row.Field<string>("出力形式").Trim();
                if (outputtype == "該当なし")
                {
                    continue;
                }

                var outputindex = int.Parse(row.Field<string>("列順"));     //列順は１始まり
                var value = "";

                // 固定値
                var fixvalue = row.Field<string>("固定値").Trim();
                if (fixvalue != "")
                {
                    value = fixvalue;
                }

                // 団体IDの確認(固定)
                if(outputindex == INDEX_OF_ORGANIZATION_ID)
                {
                    //「参照人事」で指定した項目名で検索
                    try
                    {
                        string hrcolumn = row.Field<string>("参照人事").Trim();

                        // 固定IDと人事データの確認、例外が発生しなければOK
                        var hr_id = mHRRows
                            .Where(x => x.Field<string>(hrcolumn) == value)
                            .First();
                    }
                    catch (Exception ex)
                    {
                        Dbg.ErrorWithView(Properties.Resources.E_MISMATCHED_ORGANIZATION_ID
                                , value);

                        Dbg.Error(ex.ToString());

                        // 処理中断
                        throw new MyException(Properties.Resources.E_PROCESSING_ABORTED);
                    }
                }


                // 人事データ結合
                if(value == "")
                {
                    var hrcolumn = row.Field<string>("参照人事");
                    if(hrcolumn != "")
                    {
                        // 人事の指定列名
                        hrcolumn = hrcolumn.Trim();

                        // 項目マッピングで指定した列名の値をセット
                        try
                        {
                            value = hr_row.Field<string>(hrcolumn).Trim();
                        }
                        catch (Exception ex)
                        {
                            Dbg.ErrorWithView(Properties.Resources.E_NOT_EXIST_ITEM_IN_HR
                                    , hrcolumn);

                            Dbg.Error(ex.ToString());

                            // 処理中断
                            throw new MyException(Properties.Resources.E_PROCESSING_ABORTED);
                        }
                    }
                }

                // 参照健診ヘッダーの取得
                if (value == "")
                {
                    var inspectionHeader = row.Field<string>("参照健診ヘッダー").Trim();
                    if(inspectionHeader != "")
                    {
                        // 現状、健診実施日と健診実施機関番号のみ
                        try
                        {
                            value = hrow[inspectionHeader].ToString();
                        }
                        catch (Exception ex)
                        {
                            Dbg.ErrorWithView(Properties.Resources.E_NOT_EXIST_ITEM_IN_HDR
                                    , inspectionHeader);

                            Dbg.Error(ex.ToString());

                            // 処理中断
                            throw new MyException(Properties.Resources.E_PROCESSING_ABORTED);
                        }
                    }
                }


                // 検査項目コードの検索
                if (value == "")
                {
                    var inspectcord = row.Field<string>("検査項目コード").Trim();
                    if (inspectcord != "")
                    {
                        // 検査項目コードに半角英数以外が使われているか確認
                        if(!IsOnlyAlphaWithNumeric(inspectcord))
                        {
                            Dbg.ViewLog(Properties.Resources.E_MISMATCHED_INSPECTCORD_TYPE
                                , inspectcord);

                            // 処理中断
                            throw new MyException(Properties.Resources.E_PROCESSING_ABORTED);
                        }

                        // ユーザーデータから検査値を抽出
                        var retvalue =  userdata.AsEnumerable()
                                .Where(x => x.InspectionItemCode == inspectcord)
                                .Select(x => x.Value)
                                .FirstOrDefault();

                        // 検査値
                        if (!string.IsNullOrEmpty(retvalue))
                        {
                            value = retvalue;
                        }
                    }
                }

                // コードマッピング（属性が「コード」の場合、値の置換）
                if (value != "" && row.Field<string>("属性") == "コード")
                {
                    var codeid = row.Field<string>("コードID").Trim();

                    // コードマッピング処理
                    value = GetCodeMapping(value, codeid, userID);
                }

                // 種別と値のチェック
                if (value != "")
                {
                    //種別
                    var type = row.Field<string>("種別").Trim();

                    // 種別が数値を期待しているのに、数値以外の値の場合はエラーとする
                    value = CheckMappingType(type, value, userID, row.Field<string>("項目名"));
                }


                // 必須項目確認
                var request = row.Field<string>("必須").Trim();
                if (request == "○" && value == "")
                {
                    // 必須項目に値が無い場合は、そのデータを作成しない。
                    Dbg.ErrorWithView(Properties.Resources.E_NO_VALUE_REQUIRED_FIELD
                        ,userID
                        ,row.Field<string>("項目名"));

                    // 必須項目でエラーの場合はフラグを立てる
                    requestFiledError = true;
                }

                // 出力情報に指定列順で値をセット
                outputrow[outputindex - 1] = value;
            }

            // 全ての必須項目で一つでもエラーがあれば、レコードを作成しない
            if (!requestFiledError)
            {
                // CSV出力情報に追加
                mOutputCsv.Rows.Add(outputrow);
            }

            outputrow = null;

            // 次のユーザー
            return true;
        }

        /// <summary>
        /// CSVの書き出し
        /// </summary>
        /// <returns></returns>
        bool WriteCsv()
        {
            Dbg.ViewLog(Properties.Resources.MSG_CREATE_OUTPUT, mOutputCsv.Rows.Count.ToString());

            var str_arry = new List<string>();

            // 初期カラム名
            foreach (var r in mItemMap)
            {
                str_arry.Add("-");
            }

            // 列順の項目を書き換え
            foreach (var r in mItemMap)
            {
                str_arry[int.Parse(r.Field<string>("列順")) - 1] = r.Field<string>("項目名");
            }

            var dt = DateTime.Now;
            var outptfilename  = ".\\" + String.Format("Converted_{0}.csv", dt.ToString("yyyyMMdd"));       // デフォルトファイル名

            var csv = new UtilCsv();
            csv.WriteFile(mPathOutput+ outptfilename, mOutputCsv, str_arry);

            return true;
        }

        /// <summary>
        /// 列名（カラム名）を付け加える
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="sheet"></param>
        void SetColumnName(DataTable dt, List<string> sheet)
        {
            var n = sheet.Count;
            for (int i=0; i< sheet.Count(); i++)
            {
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

            var hdt = new DataTable();
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

            // TDLとHDRを結合して取得
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
                        // ヘッダー情報は、人事データ結合時に処理する。
                        InspectionItemCode = d.Field<string>("検査項目コード").Trim(),
                        InspectionItemName = d.Field<string>("検査項目名称").Trim(),
                        InspectionDetailID = d.Field<string>("健診明細情報管理番号").Trim(),

                        // コメントのTrimはしない
                        Value = (d.Field<string>("結果値データタイプ") == "4") ? d.Field<string>("コメント") : d.Field<string>("結果値").Trim(),
                    };

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
            var ret = new List<UserData>();

            foreach (var m in user)
            {
                var newcode = jlacTable.AsEnumerable()
                                .Where(x => x.Field<string>("旧検査項目コード") == m.InspectionItemCode && x.Field<string>("置換対象") == "〇")
                                .Select(x => x.Field<string>("新検査項目コード"))
                                .FirstOrDefault();

                if (!string.IsNullOrEmpty(newcode))
                {
                    m.InspectionItemCode = newcode.Trim();
                }

                // refが使えない為、値を書き換えて別に保存
                ret.Add(m);
            }

            return ret.ToList();
        }

        /// <summary>
        /// コードマッピング処理
        /// 指定のコードを置換する
        /// </summary>
        /// <param name="value"></param>
        /// <param name="codeid"></param>
        /// <param name="userID"></param>
        /// <returns>コード変換した値</returns>
        string GetCodeMapping(string value, string codeid, string userID)
        { 
            // コードマッピング（属性が「コード」の場合、値の置換）
            // コードマッピングから抽出
            var newvalue = mCordMap.AsEnumerable()
                .Where(x => x.Field<string>("コードID").Trim() == codeid && x.Field<string>("★コード").Trim() == value)
                .Select(x => x.Field<string>("コード").Trim())
                .FirstOrDefault();

            if(string.IsNullOrEmpty(newvalue))
            {
                // エラー表示
                Dbg.ErrorWithView(Properties.Resources.E_CORDMAPPING_FILED
                    , userID
                    , codeid);

                // エラーの場合空にする
                newvalue = "";
            }

            return newvalue;
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
                        if (!int.TryParse(value, out int i))
                        {
                            if (!float.TryParse(value, out float f))
                            {
                                Dbg.ErrorWithView(Properties.Resources.E_MISMATCHED_ITEM_TYPE
                                    , userID
                                    , itenName.Trim()
                                    , type
                                    , value);

                                // エラーの場合空白として出力
                                return "";
                            }
                        }
                    }
                    break;

                case "年月日":
                    {
                        // 年月日の変換
                        DateTime d;
                        if (DateTime.TryParseExact(value, "yyyyMMdd", null, DateTimeStyles.None, out d))
                        {
                            // 日付
                            value = d.ToString("yyyy/MM/dd");
                        }
                        else
                        {
                            // エラー表示
                            Dbg.ErrorWithView(Properties.Resources.E_MISMATCHED_ITEM_TYPE
                                , userID
                                , itenName.Trim()
                                , type
                                , value);

                            // エラーの場合空にする
                            value = "";
                        }
                    }
                    break;
            }

            return value;
        }

        /// <summary>
        /// 人事データの取得
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="hrcolumn"></param>
        /// <returns>DataRow</returns>
        DataRow GetHumanResorceRow(string userID, string hrcolumn)
        {
            DataRow row = null;

            // 最終的に残った項目で検索
            try
            {
                row = mHRRows
                    .Where(x => x.Field<string>(hrcolumn) == userID)
                    .First();
            }
            catch (Exception ex)
            {
                Dbg.Error(ex.ToString());
                 
                // 存在しない場合はレコードを作成しないで次のユーザーへ
                return null;
            }

            return row;
        }


    }
}
