using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConvertDaiwaForBPF
{
    /// <summary>
    /// 健診ヘッダーと健診データの結合用
    /// </summary>
    internal class UserData
    {
        /// <summary>
        ///検査項目コード
        /// </summary>
        public string InspectionItemCode { get; set; }

        /// <summary>
        ///検査項目名称
        /// </summary>
        public string InspectionItemName { get; set; }

        /// <summary>
        ///健診明細情報管理番号
        /// </summary>
        public string InspectionDetailID { get; set; }

        /// <summary>
        /// 結果値
        /// </summary>
        public string Value { get; set; }
    }

}
