using System;

namespace ConvertDaiwaForBPF
{
    /// <summary>
    /// 例外処理のラッパー
    /// </summary>
    [Serializable()]
    public class MyException : System.Exception
    {
        /// <summary>
        /// 例外引数無し
        /// </summary>
        public MyException() : base()
        {
        }

        /// <summary>
        /// 例外メッセージ有り
        /// </summary>
        /// <param name="message"></param>
        public MyException(string message) : base(message)
        {
        }

        /// <summary>
        /// 例外メッセージと例外オブジェクト
        /// </summary>
        /// <param name="message"></param>
        /// <param name="inner"></param>
        public MyException(string message, System.Exception inner) : base(message, inner)
        {
        }


        /// <summary>
        /// 逆シリアル化コンストラクタ。このクラスの逆シリアル化のために必須。
        /// アクセス修飾子をpublicにしないこと！
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected MyException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
        {
        }
    }
}
