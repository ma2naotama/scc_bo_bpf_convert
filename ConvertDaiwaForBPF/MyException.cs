using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConvertDaiwaForBPF
{
    /// <summary>
    /// 例外処理のラッパー
    /// </summary>
    [Serializable()]
    public class MyException : System.Exception
    {
        public MyException() : base() 
        { 
        }

        public MyException(string message) : base(message) 
        { 
        }

        public MyException(string message, System.Exception inner) : base(message, inner) 
        { 
        }

        // 逆シリアル化コンストラクタ。このクラスの逆シリアル化のために必須。
        // アクセス修飾子をpublicにしないこと！
        protected MyException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
        {
        }         
    }
}
