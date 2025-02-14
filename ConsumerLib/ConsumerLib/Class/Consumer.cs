using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Transactions;
using ConsumerLib.AttributeFile;
using ConsumerLib.Interface;

namespace ConsumerLib.Class
{
    [RateLimit(5)]
    public class Consumer : IConsumer
    {
        private static readonly Random _random = new();

        public void Printer(object obj)
        {
            if (obj == null)
            {
                Console.WriteLine("Waiting for server...");
            }
            else if (obj is bool && (bool)obj == false)
            {
                Console.WriteLine("Queue is empty or Message is null");
            }
            else
            {
                Console.WriteLine(obj.ToString());
            }
        }
    }
}
