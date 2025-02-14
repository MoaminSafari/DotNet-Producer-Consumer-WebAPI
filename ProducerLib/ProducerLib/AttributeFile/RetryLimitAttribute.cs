namespace ProducerLib.AttributeFile
{
    [AttributeUsage(AttributeTargets.Class)]
    public class RetryLimitAttribute : Attribute
    {
        public int RetryLimit { get; set; }
        public RetryLimitAttribute(int retryLimit)
        {
            RetryLimit = retryLimit;
        }
    }
}
