using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsumerLib.AttributeFile
{
    [AttributeUsage(AttributeTargets.Class)]
    public class RateLimitAttribute : Attribute
    {
        public int RateLimit { get; set; }
        public RateLimitAttribute(int rateLimit)
        {
            RateLimit = rateLimit;
        }
    }
}
