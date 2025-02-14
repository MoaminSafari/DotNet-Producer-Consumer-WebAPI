namespace ProducerLib.Interface
{
    public interface IProducer
    {
        private static readonly Random _random;
        private static readonly int _failLimit;
        private static readonly int _retryLimit;
        private static readonly HttpClient _httpClient;
        object RandomObjectProducer();


    }
}
