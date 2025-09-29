using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Baterija
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ChannelFactory<IBatteryService> factory = new ChannelFactory<IBatteryService>("BatteryService");

            IBatteryService proxy = factory.CreateChannel();

            string result = proxy.Ping();
            Console.WriteLine(result);

        }
    }
}
