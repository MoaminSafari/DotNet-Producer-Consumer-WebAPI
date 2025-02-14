namespace ProducerLib.AttributeFile
{
    [AttributeUsage(AttributeTargets.Class)]
    public class RateLimitAttribute : Attribute
    {
        public int RateLimit { get; set; }
        public RateLimitAttribute (int rateLimit)
        {
            RateLimit = rateLimit;
        }
    }
}
