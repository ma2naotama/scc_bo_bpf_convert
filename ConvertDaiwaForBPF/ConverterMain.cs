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
        private DataRow[] mOrderMap = null;

        //コードマッピング
        private DataRow[] mCordMap = null;

        //出力情報
        private DataTable mOutputCsv = null;


        //健診ヘッダーと健診データの結合用
        private class MergedMap// : object
        {
            //public string userId { get; set; }                  //個人番号

            //public string date_of_consult { get; set; }         //受診日

            public string KensakoumokuCode { get; set; }

            public string KensakoumokuName { get; set; }

            public string KenshinmeisaiNo { get; set; }

            public string Value { get; set; }
        }

        /*
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
        */

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
            mOrderMap = null;
            mCordMap  = null;

            mOutputCsv = null;

            Cancel = false;
        }


        //スレッド内の処理（これ自体をキャンセルはできない）
        public override int MultiThreadMethod()
        {
            Dbg.ViewLog("変換中...");
            try
            {
                //初期化と設定ファイルの読み込み
                if (!Init())
                {
                    return 0;
                }

                //健診ヘッダーの読み込み
                DataTable hdrTbl = ReadHelthHeder();
                if (hdrTbl == null)
                {
                    return 0;
                }

                //健診データの読み込み
                DataTable tdlTbl = ReadHelthData();
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

                    //変換処理
                    Dbg.ViewLog("{0} 個人番号:{1}", i.ToString(), hrow["個人番号"].ToString());

                    if (!ConvertMain(hrow, tdlTbl))
                    {
                        return 0;
                    }

                    i++;

                    //テスト用、１ユーザー分でやめる
                    break;
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
                Dbg.ErrorWithView(ex.ToString());
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


            //オーダーマッピング初期化
            mOrderMap = mMasterSheets.Tables["オーダーマッピング"].AsEnumerable()
                  .Where(x => x["検査項目コード"].ToString() != "")
                  .ToArray();


            //コードマッピング初期化
            mCordMap = mMasterSheets.Tables["コードマッピング"].AsEnumerable()
                  .Where(x => x["コードID"].ToString() != "")
                  .ToArray();

            //次の処理へ
            return true;
        }

        /// <summary>
        /// 健診ヘッダーの読み込み
        /// </summary>
        /// <returns></returns>
        DataTable ReadHelthHeder()
        {
            DataRow[] rows =
                mMasterSheets.Tables["config"].AsEnumerable()
                  .Where(x => x["受信ファイル名"].ToString() != "")
                  .ToArray();

            UtilCsv　csv = new UtilCsv();
            DataTable tbl = csv.ReadFile(mPathInput + "\\" + rows[0][0], ",", GlobalVariables.ENCORDTYPE.SJIS);
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

            SetColumnName(tbl, mMasterSheets.Tables["DHPTV001HED"]);

            //次の処理へ
            return tbl;
        }

        /// <summary>
        /// 健診データの読み込み
        /// </summary>
        /// <returns></returns>
        DataTable ReadHelthData()
        {
            DataRow[] rows =
                mMasterSheets.Tables["config"].AsEnumerable()
                    .Where(x => x["受信ファイル名"].ToString() != "")
                    .ToArray();

            UtilCsv csv = new UtilCsv();

            DataTable tbl = csv.ReadFile(mPathInput + "\\" + rows[1][0], ",", GlobalVariables.ENCORDTYPE.SJIS);
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

            SetColumnName(tbl, mMasterSheets.Tables["DHPTV001DTL"]);
            return tbl;
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

            //DataTableが大きすぎるとここで処理が終わらない事がある。
            //※現在ユーザー毎に処理する様に変更した為問題は起きないはず。
            int overlapcount = dr_array.Count();

            if (overlapcount > 0)
            {
                Dbg.Warn("受診者の重複件数：" + overlapcount);
                foreach (var row in dr_array)
                {
                    Dbg.Warn("重複個人番号：{0} 健診実施日:{1} 健診実施機関名称:{2}"
                        , row["個人番号"].ToString()
                        , row["健診実施日"].ToString()
                        , row["健診実施機関名称"].ToString());
                }
            }

            //次の処理へ
            return hdrRows;
        }

        /// <summary>
        /// 変換処理メイン
        /// </summary>
        /// <param name="hrow"></param>
        /// <param name="TdlTbl"></param>
        /// <returns></returns>
        bool ConvertMain(DataRow hrow, DataTable TdlTbl)
        {
            var userID = hrow["個人番号"].ToString();

            //健診ヘッダーと健診データを結合し、１ユーザー分の検査項目一覧を抽出する。
            var merged = JoinHdrWithTdl(hrow, TdlTbl)
                        .ToArray();

            if (merged.Count() <= 0)
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
            outputrow[5] = hrow["個人番号"].ToString();    //仮

            //項目マッピング処理

            //オーダーマッピング（特定の検査項目コードの絞込）
            var newmerged = OrderMapping(ref merged, mOrderMap, userID);

            //項目マッピングから該当する検査項目コード一覧を抽出（複数の検査項目コードも抽出される）

            //必要な検査項目コード分ループ
            foreach (var row in mItemMap)
            {
                string request = row.Field<string>("必須").Trim();
                if (request == "入力禁止")
                {
                    continue;
                }

                int index = int.Parse(row.Field<string>("列順"));     //列順は１始まり

                string value = null;

                //固定値
                string fixvalue = row.Field<string>("固定値").Trim();
                if (fixvalue != "")
                {
                    value = fixvalue;
                }

                //13列目は、必ず受診日が入る
                if (index == 13)
                {
                    value = hrow["健診実施日"].ToString();
                }

                //検査項目コードの検索
                if (value == null)
                {
                    if (row.Field<string>("検査項目コード") == "")
                    {
                        continue;
                    }

                    //ユーザーデータから抽出
                    var userdataArray = merged.AsEnumerable()
                            .Where(x => x.KensakoumokuCode == row.Field<string>("検査項目コード"));
                            //.ToArray();

                    if (userdataArray == null)
                    {
                        continue;
                    }

                    if (userdataArray.Count() == 0)
                    {
                        continue;
                    }

                    var useritem = userdataArray.First();

                    //検査値
                    value = useritem.Value;
                    //Dbg.ViewLog("value:" + value + " " + row.Field<string>("項目名"));
                }


                //種別のチェック
                string type = row.Field<string>("種別").Trim();

                if (value != null && !CheckMappingType(type, value))
                {
                    Dbg.ErrorWithView(Properties.Resources.E_ITEM_TYPE_MISMATCH, row.Field<string>("項目名"), type, value);

                    //エラーの場合空にする
                    value = "";
                }

                //日付の変更
                if (value != null && type == "年月日")
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
                        Dbg.ErrorWithView(Properties.Resources.E_ITEM_TYPE_MISMATCH, row.Field<string>("項目名"), type, value);

                        //エラーの場合空にする
                        value = "";
                    }
                }

                //TODO:コードマッピング（属性が「コード」の場合、値の置換）
                //CodeMapping(itemMapped,itemSheet);

                //必須項目確認
                if (request == "〇")
                {
                    if (value == null)
                    {
                        //必須項目に値が無い場合は、そのデータを作成しない。
                        Dbg.ErrorWithView(Properties.Resources.E_NOT_REQUIRED_FIELD, row.Field<string>("項目名"));
                        outputrow = null;

                        //次のユーザー
                        return true;
                    }
                }

                //出力情報に指定列順で値をセット
                outputrow[index - 1] = value;
            }

            // CSV出力情報に追加
            mOutputCsv.Rows.Add(outputrow);

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

        private MergedMap[] OrderMapping(ref MergedMap[] merged, DataRow[] ordermap, string userID)
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

            //上記、カテゴリー別のstring 配列を動的生成 
            var inOrder = ordermap.AsEnumerable()
                    .Where(x => x.Field<string>("検査項目コード") != "")
                    .GroupBy( x => new
                    {
                        category = x.Field<string>("カテゴリー"),
                    })
                    .Select(x => new {
                        category = x.Key.category,
                        code = x.Select(y => y.Field<string>("検査項目コード")).ToArray() 
                    });

            foreach(var order in inOrder)
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
                var inCause = order.code;

                //ユーザーデータから抽出
                try
                {
                    //Dbg.ViewLog("category:" + order.category);

                    var userdataArray = merged.AsEnumerable()
                        .Where(x => inCause.Contains(x.KensakoumokuCode))
                        .OrderBy(x => x.KensakoumokuCode)
                        .ToArray();

                    var  remove = new List<MergedMap>();

                    int i =0;
                    foreach (var o in userdataArray)
                    {
                        if (i>=1)
                        {
                            //Dbg.ViewLog("code:" + o.KensakoumokuCode + " v:" + o.Value);
                            remove.Add(o);
                        }
                        i++;
                    }

                    //ユーザーデータから優先度の低いものを削除
                    merged = merged.Except(remove).ToArray();
                }
                catch (Exception ex)
                {
                    Dbg.ErrorWithView(Properties.Resources.E_ORDERMAPPING_FILED, userID);
                    throw ex;
                }
            }


            return merged;
        }

        private IEnumerable CodeMapping(IEnumerable<MergedMap> merged, DataRow[] cordmap)
        {
            return null;
        }


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
                    select new MergedMap
                    {
                        //ヘッダー情報は、人事データ結合時に処理する。
                        KensakoumokuCode = d.Field<string>("検査項目コード").Trim(),
                        KensakoumokuName = d.Field<string>("検査項目名称").Trim(),
                        KenshinmeisaiNo = d.Field<string>("健診明細情報管理番号").Trim(),
                        Value = (d.Field<string>("結果値データタイプ") == "4") ? d.Field<string>("コメント").Trim() : d.Field<string>("結果値").Trim(),
                    };

            //UtilCsv csv = new UtilCsv();
            //csv.WriteFile(".\\merged_"+ hrow["個人番号"].ToString()+".csv", csv.CreateDataTable(merged));

            return merged;
        }


        /*
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
        */
 

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
            }

            return true;
        }
    }
}
