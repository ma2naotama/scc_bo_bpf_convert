using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConvertDaiwaForBPF
{
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

        protected MyException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
        {
        }
    }
}
