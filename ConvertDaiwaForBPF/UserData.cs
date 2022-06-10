namespace ConvertDaiwaForBPF
{
    /// <summary>
    /// 1ユーザー分の検査項目コード一覧
    /// 健診ヘッダーと健診データの結合結果
    /// </summary>
    internal class UserData
    {
        /// <summary>
        /// 検査項目コード
        /// </summary>
        public string InspectionItemCode { get; set; } = null;

        /// <summary>
        /// 結果値
        /// </summary>
        public string Value { get; set; } = null;

        /// <summary>
        /// 健診データの行番号
        /// </summary>
        public int DLine { get; set; } = 0;

        /// <summary>
        /// 健診データの列番号
        /// </summary>
        public int DColumnIndex { get; set; } = 0;

    }
}
