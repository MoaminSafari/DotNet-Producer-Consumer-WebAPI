using System.Text.Json;
using System.Text;
using System.Transactions;
using ProducerLib.AttributeFile;
using ProducerLib.Interface;

namespace ProducerLib.Class
{
    [RateLimit(3)]
    [RetryLimit(3)]
    public class Producer : IProducer
    {
        private static readonly Random _random = new();

        public object RandomObjectProducer()
        {
            int typeChoise = _random.Next(0, 3);
            object message;
            switch (typeChoise)
            {
                case 0:
                    {
                        message = _random.Next(1, 100);
                        break;
                    }
                case 1:
                    {
                        message = Guid.NewGuid().ToString().Substring(0, 5);
                        break;
                    }
                case 2:
                    {
                        message = (char)_random.Next(65, 90);
                        break;
                    }
                //case 3:
                //    {
                //        message = new User()
                //        {
                //            Id = _random.Next(1, 100),
                //            Name = Guid.NewGuid().ToString(),
                //            Email = Guid.NewGuid().ToString()
                //        };
                //        break;
                //    }
                default:
                    {
                        throw new InvalidOperationException();
                    };
            }

            return message;
        }
    }
}
