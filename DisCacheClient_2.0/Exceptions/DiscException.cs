using System;
using System.Collections.Generic;
using System.Text;

namespace DisCacheClient.Exceptions
{
    public class DiscException : System.Exception
    {
        public DiscException(string message, System.Exception innerException)
            : base(message, innerException)
        {
        }

    }
}
