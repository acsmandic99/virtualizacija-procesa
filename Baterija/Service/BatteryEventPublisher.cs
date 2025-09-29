using Common;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service
{
    public class BatteryEventPublisher
    {
        public delegate void TransferStartedEventHandler(object sender, TransferStartedEventArgs e);
        public delegate void SampleReceivedEventHandler(object sender, SampleReceivedEventArgs e);
        public delegate void TransferCompletedEventHandler(object sender, TransferCompletedEventArgs e);
        public delegate void WarningRaisedEventHandler(object sender, WarningRaisedEventArgs e);
        public delegate void OutOfBandWarningEventHandler(object sender, OutOfBandWarningRaisedEventArgs e);
        public delegate void VoltageSpikeEventHandler(object sender, VoltageSpikeEventArgs e);
        public delegate void ImpedanceJumpHandler(object sender, ImpedanceJumpEventArgs e);

        public event TransferStartedEventHandler OnTransferStarted;
        public event SampleReceivedEventHandler OnSampleReceived;
        public event TransferCompletedEventHandler OnTransferCompleted;
        public event WarningRaisedEventHandler OnWarningRaised;
        public event OutOfBandWarningEventHandler OnOutOfBandRaised;
        public event VoltageSpikeEventHandler OnVoltageSpike;
        public event ImpedanceJumpHandler OnImpedanceJump;


        public void RaiseOnOutOfBandWarning(string batteryId, string testId, int soc, string warningType, string message, double actualValue, double threshold, EisSample sample)
        {
            if (OnOutOfBandRaised != null)
            {
                OnOutOfBandRaised(this, new OutOfBandWarningRaisedEventArgs(batteryId, testId, soc, warningType, message, actualValue, threshold, sample));
            }
        }
        public void RaiseImpedanceJump(string batteryId,string testId,int soc,double newValue,double lastValue, string smer)
        {
            if(OnImpedanceJump != null)
            {
                OnImpedanceJump(this,new ImpedanceJumpEventArgs(batteryId, testId, soc, newValue, lastValue, smer));
            }
        }
        public void RaiseVoltageSpike(string batteryId,string testId,int soc,double newValue,double lastValue, string smer)
        {
            if(OnVoltageSpike != null)
            {
                OnVoltageSpike(this, new VoltageSpikeEventArgs(batteryId, testId, soc, newValue, lastValue,smer));
            }
        }


        public void RaiseTransferStarted(string batteryId,string testId,int soc,string sessionFile)
        {

            if (OnTransferStarted != null) 
            {
                OnTransferStarted(this, new TransferStartedEventArgs(batteryId, testId, soc, sessionFile));
            }
        }
        public void RaiseSampleReceived(string batteryId,string testId,int soc,EisSample sample,int count)
        {

            if(OnSampleReceived != null)
            {
                OnSampleReceived(this,new SampleReceivedEventArgs(batteryId,testId, soc, sample,count));
            }
            
        }
        public void RaiseTransferCompleted(string batteryId, string testId, int soc, int total, int valid, int rejected)
        {
            if (OnTransferCompleted != null)
            {
                OnTransferCompleted(this, new TransferCompletedEventArgs(batteryId, testId, soc, total, valid, rejected));
            }
        }

        public void RaiseWarning(string batteryId, string testId, int soc, string warningType, string message, double actualValue, double threshold,EisSample sample)
        {
            if (OnWarningRaised != null)
            {
                OnWarningRaised(this, new WarningRaisedEventArgs(batteryId, testId, soc, warningType, message, actualValue, threshold,sample));
            }
        }
    }
}
