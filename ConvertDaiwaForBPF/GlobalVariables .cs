﻿using System.Collections.Generic;

namespace ConvertDaiwaForBPF
{
    /// <summary>
    /// グローバル変数
    /// </summary>
    internal class GlobalVariables
    {
        /// <summary>
        /// エンコードタイプ
        /// </summary>
        public enum ENCORDTYPE
        {
            SJIS,
            UTF8
        };

        /// <summary>
        /// 健診ヘッダーのカラム
        /// </summary>
        public static List<string> ColumnHDR = new List<string>()
        {
            "組合C",
            "健診基本情報管理番号",
            "本支部C",
            "個人番号",
            "報告区分",
            "報告区分名称",
            "受診時年度",
            "健診実施日",
            "健診プログラム種別",
            "健診プログラム種別名称",
            "健診実施機関種別",
            "健診実施機関種別名称",
            "健診実施機関番号",
            "健診実施機関名称",
            "受診券保険者番号",
            "受診券整理番号（印刷）",
            "受診券有効期限",
            "メタボリックシンドローム判定",
            "メタボリックシンドローム判定名称",
            "保健指導レベル",
            "保健指導レベル名称",
            "服薬の有無（ＸＭＬ）",
            "服薬の有無名称",
            "服薬１（血圧）",
            "服薬１名称",
            "服薬２（血糖）",
            "服薬２名称",
            "服薬３（脂質）",
            "服薬３名称",
            "生活習慣の改善",
            "生活習慣の改善名称",
            "保健指導の希望",
            "保健指導の希望名称",
            "受診勧奨基準該当",
            "ファイル作成日",
            "保険者番号",
            "記号",
            "番号",
            "カナ氏名（全角）",
            "受診者氏名漢字",
            "性別C",
            "生年月日",
            "健診個人郵便番号",
            "健診個人住所",
            "文書作成日",
            "ファイル作成機関種別",
            "ファイル作成機関種別名称",
            "ファイル作成機関番号",
            "ファイル作成機関名称",
            "ファイル作成機関電話番号",
            "ファイル作成機関郵便番号",
            "ファイル作成機関所在地",
            "健診実施機関電話番号",
            "健診実施機関郵便番号",
            "健診実施機関所在地",
            "送付種別コード",
            "送付元機関種別",
            "送付元機関番号",
            "削除フラグ"
        };

        /// <summary>
        /// 健診データのカラム
        /// </summary>
        public static List<string> ColumnTDL = new List<string>()
        {
            "組合C",
            "健診基本情報管理番号",
            "健診明細情報管理番号",
            "検査項目コード",
            "検査項目名称",
            "健診CDAセクション",
            "健診CDAセクション名称",
            "未実施FLG",
            "未実施FLG名称",
            "測定不能FLG",
            "測定不能FLG名称",
            "入力範囲FLG",
            "入力範囲FLG名称",
            "結果値データタイプ",
            "健診結果コード体系",
            "結果値",
            "健診結果コード名称",
            "結果値単位",
            "結果値単位名称",
            "結果解釈コード",
            "結果解釈名称",
            "検査方法コード",
            "検査方法名称",
            "基準値依存種別コード",
            "基準値下限値",
            "基準値上限値",
            "コメント",
            "削除フラグ",
        };
    }
}
