﻿//------------------------------------------------------------------------------
// <auto-generated>
//     このコードはツールによって生成されました。
//     ランタイム バージョン:4.0.30319.42000
//
//     このファイルへの変更は、以下の状況下で不正な動作の原因になったり、
//     コードが再生成されるときに損失したりします。
// </auto-generated>
//------------------------------------------------------------------------------

namespace ConvertDaiwaForBPF.Properties {
    using System;
    
    
    /// <summary>
    ///   ローカライズされた文字列などを検索するための、厳密に型指定されたリソース クラスです。
    /// </summary>
    // このクラスは StronglyTypedResourceBuilder クラスが ResGen
    // または Visual Studio のようなツールを使用して自動生成されました。
    // メンバーを追加または削除するには、.ResX ファイルを編集して、/str オプションと共に
    // ResGen を実行し直すか、または VS プロジェクトをビルドし直します。
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   このクラスで使用されているキャッシュされた ResourceManager インスタンスを返します。
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("ConvertDaiwaForBPF.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   すべてについて、現在のスレッドの CurrentUICulture プロパティをオーバーライドします
        ///   現在のスレッドの CurrentUICulture プロパティをオーバーライドします。
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   コードマッピングの変換に失敗しました。個人番号：{0}　コードID：{1} に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string E_CORDMAPPING_FILED {
            get {
                return ResourceManager.GetString("E_CORDMAPPING_FILED", resourceCulture);
            }
        }
        
        /// <summary>
        ///   受診者の重複があります。重複件数：{0} に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string E_DUPLICATE_USERS_COUNT {
            get {
                return ResourceManager.GetString("E_DUPLICATE_USERS_COUNT", resourceCulture);
            }
        }
        
        /// <summary>
        ///   重複があります。　個人番号：{0}　健診実施日：{1}　健診実施機関名称：{2} に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string E_DUPLICATE_USERS_INFO {
            get {
                return ResourceManager.GetString("E_DUPLICATE_USERS_INFO", resourceCulture);
            }
        }
        
        /// <summary>
        ///   シートが空です。シート名：{0} に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string E_EMPTY_SHEET {
            get {
                return ResourceManager.GetString("E_EMPTY_SHEET", resourceCulture);
            }
        }
        
        /// <summary>
        ///   CSVの作成で失敗しました。  に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string E_FAILED_CREATE_CSV {
            get {
                return ResourceManager.GetString("E_FAILED_CREATE_CSV", resourceCulture);
            }
        }
        
        /// <summary>
        ///   健診ヘッダーが空です。 に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string E_HDR_IS_EMPTY {
            get {
                return ResourceManager.GetString("E_HDR_IS_EMPTY", resourceCulture);
            }
        }
        
        /// <summary>
        ///   項目マッピングの順列の最大値と項目数（個数）が合っていません。 に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string E_ITEMMAPPING_INDEX_FAILE {
            get {
                return ResourceManager.GetString("E_ITEMMAPPING_INDEX_FAILE", resourceCulture);
            }
        }
        
        /// <summary>
        ///   結合したデータが空です。個人番号：{0} に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string E_MERGED_DATA_IS_EMPTY {
            get {
                return ResourceManager.GetString("E_MERGED_DATA_IS_EMPTY", resourceCulture);
            }
        }
        
        /// <summary>
        ///   ヘッダーの数が合っていません。 に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string E_MISMATCHED_HDR_COUNT {
            get {
                return ResourceManager.GetString("E_MISMATCHED_HDR_COUNT", resourceCulture);
            }
        }
        
        /// <summary>
        ///   人事データの結合キーがありません。 に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string E_MISMATCHED_HR_KEY {
            get {
                return ResourceManager.GetString("E_MISMATCHED_HR_KEY", resourceCulture);
            }
        }
        
        /// <summary>
        ///   検査項目コードに、半角英数以外がつかわれています。検査項目コード：{0} に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string E_MISMATCHED_INSPECTCORD_TYPE {
            get {
                return ResourceManager.GetString("E_MISMATCHED_INSPECTCORD_TYPE", resourceCulture);
            }
        }
        
        /// <summary>
        ///   検査値が種別と合っていません。 個人番号：{0}　項目名：{1}　種別：{2}　 検査値：{3} に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string E_MISMATCHED_ITEM_TYPE {
            get {
                return ResourceManager.GetString("E_MISMATCHED_ITEM_TYPE", resourceCulture);
            }
        }
        
        /// <summary>
        ///   人事データの団体IDと項目マッピングの団体IDと合っていません。項目マッピングの団体ID：{0} に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string E_MISMATCHED_ORGANIZATION_ID {
            get {
                return ResourceManager.GetString("E_MISMATCHED_ORGANIZATION_ID", resourceCulture);
            }
        }
        
        /// <summary>
        ///   健診データに該当ユーザーがいません。個人番号：{0} に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string E_NO_TDLDATA {
            get {
                return ResourceManager.GetString("E_NO_TDLDATA", resourceCulture);
            }
        }
        
        /// <summary>
        ///   人事データに該当ユーザーがいません。個人番号：{0} に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string E_NO_USERDATA {
            get {
                return ResourceManager.GetString("E_NO_USERDATA", resourceCulture);
            }
        }
        
        /// <summary>
        ///   検診ヘッダーの参照項目がありません。指定項目：{0} に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string E_NOT_EXIST_ITEM_IN_HDR {
            get {
                return ResourceManager.GetString("E_NOT_EXIST_ITEM_IN_HDR", resourceCulture);
            }
        }
        
        /// <summary>
        ///   人事データの参照項目がありません。指定項目：{0} に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string E_NOT_EXIST_ITEM_IN_HR {
            get {
                return ResourceManager.GetString("E_NOT_EXIST_ITEM_IN_HR", resourceCulture);
            }
        }
        
        /// <summary>
        ///   中断しました。 に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string E_PROCESSING_ABORTED {
            get {
                return ResourceManager.GetString("E_PROCESSING ABORTED", resourceCulture);
            }
        }
        
        /// <summary>
        ///   健診ヘッダーが読めませんでした。 に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string E_READFAILED_HDR {
            get {
                return ResourceManager.GetString("E_READFAILED_HDR", resourceCulture);
            }
        }
        
        /// <summary>
        ///   人事データが読めませんでした。 に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string E_READFAILED_HR {
            get {
                return ResourceManager.GetString("E_READFAILED_HR", resourceCulture);
            }
        }
        
        /// <summary>
        ///   設定ファイルが読めませんでした。 に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string E_READFAILED_MASTER {
            get {
                return ResourceManager.GetString("E_READFAILED_MASTER", resourceCulture);
            }
        }
        
        /// <summary>
        ///   健診データが読めませんでした。 に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string E_READFAILED_TDL {
            get {
                return ResourceManager.GetString("E_READFAILED_TDL", resourceCulture);
            }
        }
        
        /// <summary>
        ///   タスク処理のキャンセルでエラーがありました。 に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string E_TASK_CANCEL_ERROR {
            get {
                return ResourceManager.GetString("E_TASK_CANCEL_ERROR", resourceCulture);
            }
        }
        
        /// <summary>
        ///   タスク処理でエラーがおきました。 に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string E_TASK_ERROR {
            get {
                return ResourceManager.GetString("E_TASK_ERROR", resourceCulture);
            }
        }
        
        /// <summary>
        ///   既に別の値が設定されています。個人番号：{0}　項目名：{1}　元値：{2}　置き換え値：{3} に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string E_VALUE_IS_ALREADY_EXIST {
            get {
                return ResourceManager.GetString("E_VALUE_IS_ALREADY_EXIST", resourceCulture);
            }
        }
        
        /// <summary>
        ///   変換キャンセル に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string MSG_CONVERT_CANCEL {
            get {
                return ResourceManager.GetString("MSG_CONVERT_CANCEL", resourceCulture);
            }
        }
        
        /// <summary>
        ///   完了しました。 に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string MSG_CONVERT_FINISHED {
            get {
                return ResourceManager.GetString("MSG_CONVERT_FINISHED", resourceCulture);
            }
        }
        
        /// <summary>
        ///   CSV作成中...（件数{0}) に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string MSG_CREATE_OUTPUT {
            get {
                return ResourceManager.GetString("MSG_CREATE_OUTPUT", resourceCulture);
            }
        }
        
        /// <summary>
        ///   ・受領ファイル に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string MSG_LABEL_INPUT_DAIWA_FILE {
            get {
                return ResourceManager.GetString("MSG_LABEL_INPUT_DAIWA_FILE", resourceCulture);
            }
        }
        
        /// <summary>
        ///   ・人事データ に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string MSG_LABEL_INPUT_HR {
            get {
                return ResourceManager.GetString("MSG_LABEL_INPUT_HR", resourceCulture);
            }
        }
        
        /// <summary>
        ///   ・出力先 に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string MSG_LABEL_INPUT_OUTPUT {
            get {
                return ResourceManager.GetString("MSG_LABEL_INPUT_OUTPUT", resourceCulture);
            }
        }
    }
}
