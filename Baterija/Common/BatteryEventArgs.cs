using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class BatteryEventArgs : EventArgs
    {
        public string BatteryId { get; set; }
        public string TestId { get; set; }
        public int SoC { get; set; }
        public DateTime TimeStamp { get; set; }

        protected BatteryEventArgs(string batteryId,string testId,int soc) { 
            BatteryId = batteryId;
            TestId = testId;
            SoC = soc;
            TimeStamp = DateTime.Now;
        }
    }
    public class TransferStartedEventArgs : BatteryEventArgs
    {
        public string SessionFile { get; set; }
        public  TransferStartedEventArgs(string batteryId, string testId, int soc, string sessionFile) : base(batteryId, testId, soc)
        {
            SessionFile = sessionFile;
        }
    }
    public class SampleReceivedEventArgs : BatteryEventArgs
    {
        public SampleReceivedEventArgs(string batteryId, string testId, int soc,EisSample sample,int totalSamples) : base(batteryId, testId, soc)
        {
            Sample = sample;
            TotalSamplesRecived = totalSamples;
        }

        public EisSample Sample { get; set; }
        public int TotalSamplesRecived { get; set; }
    }
    public class TransferCompletedEventArgs : BatteryEventArgs
    {
        public TransferCompletedEventArgs(string batteryId, string testId, int soc,int total,int valid,int rejected) : base(batteryId, testId, soc)
        {
            TotalSamples = total;
            ValidSamples = valid;
            RejectedSamples = rejected;
        }

        public int TotalSamples { get; set; }
        public int ValidSamples { get; set; }
        public int RejectedSamples { get; set; }
    }
    public class WarningRaisedEventArgs : BatteryEventArgs
    {
        public WarningRaisedEventArgs(string batteryId, string testId, int soc,string warning,string message,double actualValue,double threshold,EisSample sample) : base(batteryId, testId, soc)
        {
            WarningType = warning;
            Message = message;
            ActualValue = actualValue;
            Threshold = threshold;
            Sample = sample;
        }

        public string WarningType { get; set; }
        public string Message { get; set; }
        public double ActualValue { get; set; }
        public double Threshold { get; set; }
        public EisSample Sample { get; set; }
    }
    public class OutOfBandWarningRaisedEventArgs : WarningRaisedEventArgs
    {
        public OutOfBandWarningRaisedEventArgs(string batteryId, string testId, int soc, string warning, string message, double actualValue, double threshold, EisSample sample) : base(batteryId, testId, soc, warning, message, actualValue, threshold, sample)
        {
        }
    }

    public class SpikeEventArgs : BatteryEventArgs
    {
        public SpikeEventArgs(string batteryId, string testId, int soc, double newValue, double previusValue, string smer) : base(batteryId, testId, soc)
        {
            NewValue = newValue;
            PreviusValue = previusValue;
            Smer = smer;
        }
        public double NewValue { get; set; }
        public double PreviusValue { get; set; }
        public string Smer { get; set; }
    }
    public class VoltageSpikeEventArgs : SpikeEventArgs
    {
        public VoltageSpikeEventArgs(string batteryId, string testId, int soc, double newValue, double previusValue, string smer) : base(batteryId, testId, soc, newValue, previusValue, smer)
        {
        }
    }

    public class ImpedanceJumpEventArgs : SpikeEventArgs
    {
        public ImpedanceJumpEventArgs(string batteryId, string testId, int soc, double newValue, double previusValue, string smer) : base(batteryId, testId, soc, newValue, previusValue, smer)
        {
        }
    }
}
