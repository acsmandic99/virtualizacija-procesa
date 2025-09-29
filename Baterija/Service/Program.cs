using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Service
{
    internal class Program
    {
        public static BatteryEventPublisher EventPublisher { get; private set; }
        static void Main(string[] args)
        {
            EventPublisher = new BatteryEventPublisher();
            BatteryEventSubscriber subscriber = new BatteryEventSubscriber(EventPublisher);
            ServiceHost svc = new ServiceHost(typeof(BatteryService));
            svc.Open();

            Console.ReadKey();


        }
    }
}
