using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ChannelFactory<IBatteryService> factory = new ChannelFactory<IBatteryService>("BatteryService");

            IBatteryService proxy = factory.CreateChannel();
            
            DataTransferManager transferManager = new DataTransferManager();
            Console.WriteLine("Pokrecem prenos podataka...");
            Console.WriteLine("=======================================");
            transferManager.TransferAllData("../../../SoC Estimation on Li-ion Batteries A New EIS-based Dataset for data-driven applications");
            Console.WriteLine("=======================================");
            Console.WriteLine("Prenos zavrsen");
            Console.ReadKey();
        }
    }
}
