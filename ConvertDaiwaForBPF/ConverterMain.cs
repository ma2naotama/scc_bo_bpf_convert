using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace ConvertDaiwaForBPF
{
    /// <summary>
    /// 変換処理メイン
    /// </summary>
    internal class ConverterMain : BaseThread
    {
        // 各種のパス
        /// <summary>
        /// 受領フォルダのパス
        /// </summary>
        private string mPathInput = null;

        /// <summary>
        /// 人事データのパス
        /// </summary>
        private string mPathHR = null;

        /// <summary>
        /// 出力先のパス
        /// </summary>
        private string mPathOutput = null;

        /// <summary>
        /// 設定ファイル
        /// </summary>
        private DataSet mMasterSheets = null;

        /// <summary>
        /// 項目マッピング
        /// </summary>
        private DataRow[] mItemMap = null;

        /// <summary>
        /// 団体名称
        /// </summary>
        private string mOrganizationName = null;

        /// <summary>
        /// コードマッピング
        /// </summary>
        private DataRow[] mCordMap = null;

        /// <summary>
        /// 人事データの結合カラム
        /// </summary>
        private string mHRJoinKey = null;

        /// <summary>
        /// 人事データ
        /// </summary>
        private DataRow[] mHRRows = null;

        /// <summary>
        /// 出力情報
        /// </summary>
        private DataTable mOutputCsv = null;

        /// <summary>
        /// 設定ファイルの読み込み
        /// </summary>
        /// <param name="path">読み込み先</param>
        /// <returns></returns>
        private DataSet ReadMasterFile(string path)
        {
            try
            {
                var excel = new UtilExcel();

                var optionarray = new ExcelOption[]
                {
                    new ExcelOption ( "各種設定", 2, 1),
                    new ExcelOption ( "項目マッピング", 4, 1),
                    new ExcelOption ( "コードマッピング", 3, 1),
                    new ExcelOption ( "項目マッピング複数読込", 3, 1),
                };

                excel.SetExcelOptionArray(optionarray);

                var master = excel.ReadAllSheets(path);

                return master;
            }
            catch (Exception ex)
            {
                Dbg.ErrorWithView(Properties.Resources.E_READFAILED_MASTER, path);

                throw ex;
            }
        }

        /// <summary>
        /// スレッドのキャンセル
        /// </summary>
        /// <returns>bool
        /// true    ;キャンセル処理正常
        /// false   :キャンセル処理異常
        /// </returns>
        public override bool MultiThreadCancel()
        {
            base.MultiThreadCancel();

            return true;
        }

        /// <summary>
        /// 各パスの設定（実行ボタン押下で呼ばれる）
        /// </summary>
        /// <param name="pathInput">受領フォルダのパス</param>
        /// <param name="pathHR">人事データのファイルパス</param>
        /// <param name="pathOutput">出力先フォルダのパス</param>
        public void InitConvert(string pathInput, string pathHR, string pathOutput)
        {
            mPathInput = pathInput;

            mPathHR = pathHR;

            mPathOutput = pathOutput;

            mItemMap = null;

            mOrganizationName = null;

            mCordMap = null;

            mHRRows = null;

            mHRJoinKey = null;

            mOutputCsv = null;

            // キャンセルフラグの初期化
            Cancel = false;

            Dbg.SetLogPath(mPathOutput);
        }

        // 団体IDの確認
        const string ITEMMAPPING_ORGANIZATIONID = "団体ID";

        /// <summary>
        /// スレッド内の処理
        /// これ自体をキャンセルはできない為cancelTokenで処理を中断させる
        /// </summary>
        /// <param name="cancelToken"></param>
        /// <returns>bool false:タスクのキャンセル、true：正常終了</returns>
        public override bool MultiThreadMethod(CancellationToken cancelToken)
        {
            Dbg.ViewLog(Properties.Resources.MSG_LABEL_INPUT_DAIWA_FILE);

            try
            {
                // 初期化と設定ファイルの読み込み
                // タスクキャンセル
                if (!Init() || cancelToken.IsCancellationRequested)
                {
                    return false;
                }

                // タスクキャンセル
                if (cancelToken.IsCancellationRequested)
                {
                    // キャンセルされたらTaskを終了する.
                    return false;
                }

                // 健診ヘッダーの読み込み
                var hdrTbl = ReadHelthHeder(mPathInput);

                // タスクキャンセル
                if (hdrTbl == null || cancelToken.IsCancellationRequested)
                {
                    return false;
                }

                // 健診データの読み込み
                var tdlTbl = ReadHelthData(mPathInput);

                // タスクキャンセル
                if (tdlTbl == null || cancelToken.IsCancellationRequested)
                {
                    return false;
                }

                // 人事データの読み込み
                Dbg.ViewLog(Properties.Resources.MSG_LABEL_INPUT_HR);

                mHRRows = ReadHumanResourceData(mPathHR);

                // タスクキャンセル
                if (mHRRows == null || cancelToken.IsCancellationRequested)
                {
                    return false;
                }

                // 団体IDの確認(固定)
                var itemrow = mItemMap.AsEnumerable()
                    .Where(x => x.Field<string>("項目名") == ITEMMAPPING_ORGANIZATIONID)
                    .FirstOrDefault();

                if (itemrow != null)
                {
                    var orgaid = itemrow.Field<string>("固定値").Trim();

                    //「参照人事」で指定した項目名で検索
                    try
                    {
                        var hrcolumn = itemrow.Field<string>("参照人事").Trim();

                        // 固定IDと人事データの確認、例外が発生しなければOK
                        var hr_id = mHRRows
                            .Where(x => x.Field<string>(hrcolumn) == orgaid)
                            .First();
                    }
                    catch (Exception ex)
                    {
                        Dbg.ErrorWithView(Properties.Resources.E_MISMATCHED_ORGANIZATION_ID, orgaid);

                        // 処理中断
                        throw ex;
                    }
                }

                // 健診ヘッダーから「削除フラグ=0」のユーザーのみ抽出
                var hdrUsers = GetActiveUsers(hdrTbl);

                // タスクキャンセル
                if (hdrUsers == null || cancelToken.IsCancellationRequested)
                {
                    return false;
                }

                // 出力先
                Dbg.ViewLog(Properties.Resources.MSG_LABEL_INPUT_OUTPUT);

                Dbg.ViewLog(mPathOutput);

                // 一ユーザー毎に処理する
                foreach (var hrow in hdrUsers)
                {
                    // タスクキャンセル
                    if (cancelToken.IsCancellationRequested)
                    {
                        // キャンセルされたらTaskを終了する
                        return false;
                    }

                    // 変換処理
                    if (!ConvertMain(hrow, ref tdlTbl, cancelToken))
                    {
                        // キャンセルされたらTaskを終了する
                        return false;
                    }

                    //テスト用、１ユーザー分でやめる
                    //break;
                }

                // 出力情報から全レコードの書き出し
                WriteCsv(ref mItemMap, ref mOutputCsv, mPathOutput);
            }
            catch (Exception ex)
            {
                MultiThreadCancel();

                // メッセージのみ、ログ画面に表示
                Dbg.ViewLog(ex.Message);

                // エラー内容全体は、ログファイルに書き出す
                Dbg.Error(ex.ToString());

                return false;
            }

            // 処理完了
            Completed = true;

            return true;
        }

        /// <summary>
        /// 初期化と設定ファイルの読み込み
        /// </summary>
        /// <returns></returns>
        private bool Init()
        {
            try
            {
                // 独自に設定した「appSettings」へのアクセス
                var appSettings = (NameValueCollection)ConfigurationManager.GetSection("appSettings");

                var path = appSettings["SettingPath"];

                // 設定ファイルの読み込み
                mMasterSheets = ReadMasterFile(path);

                // 出力用CSVの初期化
                mItemMap = mMasterSheets.Tables["項目マッピング"].AsEnumerable()
                    .Where(x => x["列順"].ToString() != "")
                    .ToArray();

                // 項目マッピングの順列の最大値と項目数（個数）の確認
                var max = mItemMap.Max(r => int.Parse(r["列順"].ToString()));

                if (mItemMap.Length != max)
                {
                    throw new MyException(Properties.Resources.E_ITEMMAPPING_INDEX_FAILE);
                }

                mOutputCsv = new DataTable();

                // 同じ列名（カラム名）はセットできないので、列順をセットしておく
                foreach (var row in mItemMap)
                {
                    mOutputCsv.Columns.Add("" + row["列順"], typeof(string));
                }

                // 団体名の取得
                mOrganizationName = mMasterSheets.Tables["項目マッピング"].AsEnumerable()
                    .Where(x => x["項目名"].ToString() == "団体名称")
                    .Select(x => x.Field<string>("固定値").ToString().Trim())
                    .FirstOrDefault();

                if (string.IsNullOrEmpty(mOrganizationName))
                {
                    // 処理中断
                    throw new MyException(Properties.Resources.E_NO_ORGANIZATION_NAME);
                }

                // コードマッピング初期化
                mCordMap = mMasterSheets.Tables["コードマッピング"].AsEnumerable()
                    .Where(x => x["コードID"].ToString().Trim() != "")
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
            }
            catch (Exception ex)
            {
                throw ex;
            }

            // 次の処理へ
            return true;
        }

        /// <summary>
        /// 健診ヘッダーCSVの読み込み
        /// </summary>
        /// <param name="path">受領フォルダのパス</param>
        /// <returns>DataTable</returns>
        private DataTable ReadHelthHeder(string path)
        {
            try
            {
                var filename = mMasterSheets.Tables["各種設定"].AsEnumerable()
                    .Where(x => x["名称"].ToString() == "健診ヘッダー")
                    .Select(x => x.Field<string>("設定値").ToString().Trim())
                    .First();

                var csv = new UtilCsv();

                var tbl = csv.ReadFile(path + "\\" + filename, ",", false, GlobalVariables.ENCORDTYPE.SJIS);

                SetColumnName(tbl, GlobalVariables.ColumnHDR);

                // 次の処理へ
                return tbl;
            }
            catch (Exception ex)
            {
                // 中断
                Dbg.ErrorWithView(Properties.Resources.E_READFAILED_HDR);

                throw ex;
            }
        }

        /// <summary>
        /// 健診データCSVの読み込み
        /// </summary>
        /// <param name="path">受領フォルダのパス</param>
        /// <returns>DataTable</returns>
        private DataTable ReadHelthData(string path)
        {
            try
            {
                var filename = mMasterSheets.Tables["各種設定"].AsEnumerable()
                    .Where(x => x["名称"].ToString() == "健診データ")
                    .Select(x => x.Field<string>("設定値").ToString().Trim())
                    .First();

                var csv = new UtilCsv();

                var tbl = csv.ReadFile(path + "\\" + filename, ",", false, GlobalVariables.ENCORDTYPE.SJIS);

                SetColumnName(tbl, GlobalVariables.ColumnTDL);

                return tbl;
            }
            catch (Exception ex)
            {
                // 中断
                Dbg.ErrorWithView(Properties.Resources.E_READFAILED_TDL);

                throw ex;
            }
        }

        /// <summary>
        /// 人事CSVの読み込み
        /// </summary>
        /// <param name="path">人事データのファイルパス</param>
        /// <returns>削除されている人事データを除いたDataRowの配列</returns>
        private DataRow[] ReadHumanResourceData(string path)
        {
            try
            {
                var csv = new UtilCsv();

                var hr = csv.ReadFile(path, ",", true, GlobalVariables.ENCORDTYPE.UTF8);

                // 健診ヘッダーの削除フラグが0だけ抽出
                var row = hr.AsEnumerable()
                    .Where(x => x["削除"].ToString() == "0")
                    .ToArray();

                return row;
            }
            catch (Exception ex)
            {
                // 中断
                Dbg.ErrorWithView(Properties.Resources.E_READFAILED_HR);

                throw ex;
            }
        }

        /// <summary>
        /// 有効なユーザーの一覧取得
        /// </summary>
        /// <param name="HdrTbl">読み込んだ健診ヘッダー</param>
        /// <returns>DataRowの配列  削除されている健診ヘッダーを除く</returns>
        private DataRow[] GetActiveUsers(DataTable HdrTbl)
        {
            DataRow[] hdrRows = null;

            try
            {
                // 健診ヘッダーの削除フラグが0だけ抽出
                hdrRows = HdrTbl.AsEnumerable()
                    .Where(x => x["削除フラグ"].ToString() == "0")
                    .ToArray();

                if (hdrRows.Length <= 0)
                {
                    throw new MyException(Properties.Resources.E_HDR_IS_EMPTY);
                }

                // 健診ヘッダーの重複の確認
                var dr_array =
                    from row in hdrRows.AsEnumerable()
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
                    Dbg.ErrorWithView(Properties.Resources.E_DUPLICATE_USERS_COUNT, overlapcount.ToString());

                    // 重複している行を表示
                    foreach (var row in dr_array)
                    {
                        Dbg.ErrorWithView(Properties.Resources.E_DUPLICATE_USERS_INFO,
                            row["個人番号"].ToString(),
                            row["健診実施日"].ToString(),
                            row["健診実施機関名称"].ToString().Trim());
                    }

                    // 重複したデータをそのままログに出力する
                }
            }
            catch (Exception ex)
            {
                Dbg.ErrorWithView(Properties.Resources.E_READFAILED_HR);

                throw ex;
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

        // 出力形式の値
        const string ITEMMAPPING_VALUE_OF_OUTPUTTYPE = "該当なし";

        // 属性の値
        const string ITEMMAPPING_VALUE_OF_ATTRIBUTE = "コード";

        /// <summary>
        /// 変換処理メイン
        /// </summary>
        /// <param name="hrow">１ユーザー分の健診ヘッダー</param>
        /// <param name="TdlTbl">健診データ</param>
        /// <param name="cancelToken">キャンセルトークン</param>
        /// <returns>bool
        /// true :次のユーザー
        /// false:処理キャンセル
        /// </returns>
        private bool ConvertMain(DataRow hrow, ref DataTable TdlTbl, CancellationToken cancelToken)
        {
            // 健診ヘッダーの個人番号取得
            var userID = hrow["個人番号"].ToString();

            // 健診ヘッダーと健診データを結合し、１ユーザー分の検査項目一覧を抽出する。
            var userdata = CreateUserData(ref hrow, ref TdlTbl);

            if (userdata.Count() <= 0)
            {
                // 結合した結果データが無い
                Dbg.ErrorWithView(Properties.Resources.E_MERGED_DATA_IS_EMPTY, userID);

                // 次のユーザーへ
                return true;
            }

            // 人事データ取得(健診ヘッダーの個人番号と「各種設定」で指定したキーで取得)
            var hr_row = GetHumanResorceRow(userID, mHRJoinKey);

            if (hr_row == null)
            {
                Dbg.ErrorWithView(Properties.Resources.E_NO_USERDATA, userID);

                // 存在しない場合はレコードを作成しないで次のユーザーへ
                return true;
            }

            //旧検査項目コードの書き換え
            userdata = ReplaceInspectItemCode(ref userdata, mMasterSheets.Tables["項目マッピング複数読込"]);

            // 出力情報の一行分作成
            // カラムは、0始まり
            var outputrow = mOutputCsv.NewRow();

            // 項目マッピング処理
            // 必要な検査項目コード分ループ
            foreach (var row in mItemMap)
            {
                // 処理キャンセル
                if (cancelToken.IsCancellationRequested)
                {
                    return false;
                }

                var outputtype = row.Field<string>("出力形式").Trim();

                if (outputtype == ITEMMAPPING_VALUE_OF_OUTPUTTYPE)
                {
                    continue;
                }

                // 列順は１始まり
                var outputindex = int.Parse(row.Field<string>("列順"));

                var value = "";

                // 固定値の取得
                var fixvalue = row.Field<string>("固定値").Trim();

                if (fixvalue != "")
                {
                    value = fixvalue;
                }

                // 参照健診ヘッダーの取得
                if (value == "")
                {
                    var inspectionHeader = row.Field<string>("参照健診ヘッダー").Trim();

                    if (inspectionHeader != "")
                    {
                        // 現状、健診実施日と健診実施機関番号のみ
                        try
                        {
                            value = hrow[inspectionHeader].ToString();
                        }
                        catch (Exception ex)
                        {
                            Dbg.ErrorWithView(Properties.Resources.E_NOT_EXIST_ITEM_IN_HDR, inspectionHeader);

                            // 処理中断
                            throw ex;
                        }
                    }
                }

                // 人事データの取得
                if (value == "")
                {
                    // 人事の指定列名
                    var hrcolumn = row.Field<string>("参照人事").Trim();

                    if (hrcolumn != "")
                    {
                        // 項目マッピングで指定した列名の値をセット
                        try
                        {
                            value = hr_row.Field<string>(hrcolumn).Trim();
                        }
                        catch (Exception ex)
                        {
                            Dbg.ErrorWithView(Properties.Resources.E_NOT_EXIST_ITEM_IN_HR, hrcolumn);

                            // 処理中断
                            throw ex;
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
                        if (!IsOnlyAlphaWithNumeric(inspectcord))
                        {
                            // 処理中断
                            throw new MyException(string.Format(Properties.Resources.E_MISMATCHED_INSPECTCORD_TYPE, inspectcord));
                        }

                        // ユーザーデータから検査値を抽出
                        var retvalueArray = userdata.AsEnumerable()
                            .Where(x => x.InspectionItemCode == inspectcord && x.Value != "")
                            .Select(x => x.Value)
                            .ToArray();

                        // 検査値がある
                        if (retvalueArray != null && retvalueArray.Count() > 0)
                        {
                            // 検査項目の重複確認
                            if (retvalueArray.Count() >= 2)
                            {
                                // 検査値が同じかどうか確認
                                // 同じ値なら1になる。
                                if (retvalueArray.Distinct().Count() >= 2)
                                {
                                    // 検査値が違う場合エラー
                                    foreach (var v in retvalueArray)
                                    {
                                        // 検査項目に重複があります。個人番号：{0}　検査項目コード：{1}　検査値：{2}
                                        Dbg.ErrorWithView(Properties.Resources.E_DUPLICATE_INSPECTCORD_INFO, userID, inspectcord, v);
                                    }

                                    throw new MyException(string.Format(Properties.Resources.E_DUPLICATE_INSPECTCORD));
                                }
                                else
                                {
                                    // 検査項目に重複があります。個人番号：{0}　検査項目コード：{1}　検査値：{2}
                                    Dbg.WarnWithView(Properties.Resources.WRN_DUPLICATE_INSPECTCORD, userID, inspectcord, retvalueArray[0]);
                                }
                            }

                            // 検査値
                            value = retvalueArray[0];
                        }
                    }
                }

                // コードマッピング（属性が「コード」の場合、値の置換）
                if (value != "" && row.Field<string>("属性") == ITEMMAPPING_VALUE_OF_ATTRIBUTE)
                {
                    var codeid = row.Field<string>("コードID").Trim();

                    // コードマッピング処理
                    value = GetCodeMapping(value, codeid, userID);
                }

                // 種別と値のチェック
                if (value != "")
                {
                    // 種別
                    var type = row.Field<string>("種別").Trim();

                    // 種別が数値を期待しているのに、数値以外の値の場合はエラーとする
                    value = CheckMappingType(type, value, userID, row.Field<string>("項目名"));
                }

                // 出力情報に指定列順で値をセット
                if (!string.IsNullOrEmpty(value))
                {
                    outputrow[outputindex - 1] = value;
                }
            }

            // CSV出力情報に追加
            mOutputCsv.Rows.Add(outputrow);

            return true;
        }

        // 出力ファイル名
        const string OUTPUTFILENAME = "Converted_{0}_{1}.csv";

        /// <summary>
        /// CSVの書き出し
        /// </summary>
        /// <param name="itemMap">項目マッピングの項目名欄</param>
        /// <param name="datatable">書き出すデータ</param>
        /// <param name="outputPath">出力先フォルダのパス</param>
        private void WriteCsv(ref DataRow[] itemMap, ref DataTable datatable, string outputPath)
        {
            Dbg.ViewLog(Properties.Resources.MSG_CREATE_OUTPUT, datatable.Rows.Count.ToString());

            try
            {
                var str_arry = new List<string>();

                // 初期カラム名
                foreach (var r in itemMap)
                {
                    str_arry.Add("-");
                }

                // 列順の項目を書き換え
                foreach (var r in itemMap)
                {
                    var itemname = r.Field<string>("項目名");

                    str_arry[int.Parse(r.Field<string>("列順")) - 1] = itemname;
                }

                var dt = DateTime.Now;

                // 出力ファイル名
                var outptfilename = ".\\" + String.Format(OUTPUTFILENAME, mOrganizationName, dt.ToString("yyyyMMdd"));

                var csv = new UtilCsv();

                csv.WriteFile(outputPath + outptfilename, datatable, str_arry);
            }
            catch (Exception ex)
            {
                Dbg.ErrorWithView(Properties.Resources.E_FAILED_CREATE_CSV);

                throw ex;
            }
        }

        /// <summary>
        /// 列名（カラム名）を付け加える
        /// </summary>
        /// <param name="dstTable">設定するDataTable</param>
        /// <param name="columns">列名のリスト</param>
        private void SetColumnName(DataTable dstTable, List<string> columns)
        {
            var n = columns.Count;

            for (var i = 0; i < columns.Count(); i++)
            {
                if (i < n)
                {
                    dstTable.Columns["" + (i + 1)].ColumnName = columns[i].ToString().Trim();
                }
                else
                {
                    dstTable.Columns.Add(columns[i].ToString().Trim());
                }
            }
        }

        // データタイプが4の場合、コメントを参照
        const string INSPECTION_DATA_TYPE = "4";

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

                    // コメントのTrimはしない
                    Value = (d.Field<string>("結果値データタイプ") == INSPECTION_DATA_TYPE) ? d.Field<string>("コメント") : d.Field<string>("結果値").Trim(),
                };

            return merged.ToList();
        }

        /// <summary>
        /// コードマッピング処理
        /// 指定のコードを置換する
        /// </summary>
        /// <param name="value">検査値</param>
        /// <param name="codeid">項目マッピングのコードID</param>
        /// <param name="userID">該当ユーザー</param>
        /// <returns>コード変換した値</returns>
        private string GetCodeMapping(string value, string codeid, string userID)
        {
            // コードマッピング（属性が「コード」の場合、値の置換）
            // コードマッピングから抽出
            var newvalue = mCordMap.AsEnumerable()
                .Where(x => x.Field<string>("コードID").Trim() == codeid && x.Field<string>("★コード").Trim() == value)
                .Select(x => x.Field<string>("コード").Trim())
                .FirstOrDefault();

            if (string.IsNullOrEmpty(newvalue))
            {
                // エラー表示
                Dbg.ErrorWithView(Properties.Resources.E_CORDMAPPING_FILED, userID, codeid);

                // エラーの場合空にする
                newvalue = "";
            }

            return newvalue;
        }

        /// <summary>
        /// 種別と検査値の判定をします
        /// </summary>
        /// <param name="type">項目マッピングの種別</param>
        /// <param name="value">チェックする検査値</param>
        /// <param name="userID">該当ユーザー</param>
        /// <param name="itemName">項目マッピングの項目名</param>
        /// <returns>検査値</returns>
        private string CheckMappingType(string type, string value, string userID, string itemName)
        {
            switch (type)
            {
                case "半角数字":
                case "数値":
                    {
                        if (!int.TryParse(value, out int _))
                        {
                            if (!float.TryParse(value, out float _))
                            {
                                Dbg.ErrorWithView(Properties.Resources.E_MISMATCHED_ITEM_TYPE, userID, itemName.Trim(), type, value);

                                // エラーの場合空白として出力
                                return "";
                            }
                        }
                    }
                    break;

                case "年月日":
                    {
                        // 年月日の変換
                        if (DateTime.TryParseExact(value, "yyyyMMdd", null, DateTimeStyles.None, out DateTime d))
                        {
                            // 日付
                            value = d.ToString("yyyy/MM/dd");
                        }
                        else
                        {
                            // エラー表示
                            Dbg.ErrorWithView(Properties.Resources.E_MISMATCHED_ITEM_TYPE, userID, itemName.Trim(), type, value);

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
        /// <param name="userID">該当ユーザー</param>
        /// <param name="hrcolumn">参照人事の列名</param>
        /// <returns>DataRow</returns>
        private DataRow GetHumanResorceRow(string userID, string hrcolumn)
        {
            DataRow row = null;

            // 最終的に残った項目で検索
            try
            {
                row = mHRRows
                    .Where(x => x.Field<string>(hrcolumn) == userID)
                    .First();
            }
            catch (ArgumentException)
            {
                // 処理中断
                throw new MyException(string.Format(Properties.Resources.E_NOT_EXIST_ITEM_IN_HR, hrcolumn));
            }
            catch (Exception ex)
            {
                Dbg.Error(ex.ToString());

                // 存在しない場合はレコードを作成しないで次のユーザーへ
                return null;
            }

            return row;
        }

        /// <summary>
        /// 旧検査項目コードを新検査項目コードに置換します。
        /// </summary>
        /// <param name="user">UserDataのList</param>
        /// <param name="replaceTable">設定ファイルで読み込んだ「項目マッピング複数読込」テーブル</param>
        /// <returns>検査項目コードを置換したUserDataのList</returns>
        private List<UserData> ReplaceInspectItemCode(ref List<UserData> user, DataTable replaceTable)
        {
            var ret = new List<UserData>();

            foreach (var m in user)
            {
                var newcode = replaceTable.AsEnumerable()
                    .Where(x => x.Field<string>("検査結果項目コード").Trim() == m.InspectionItemCode && x.Field<string>("置換実施対象").Trim() != "")
                    .Select(x => x.Field<string>("検査結果項目コード置換").Trim())
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
    }
}
