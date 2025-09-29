using Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service
{
    public class BatteryEventSubscriber
    {
        public BatteryEventSubscriber(BatteryEventPublisher publisher)
        {
            publisher.OnTransferStarted += OnTransferStarted;
            publisher.OnSampleReceived += OnSampleReceived;
            publisher.OnTransferCompleted += OnTransferCompleted;
            publisher.OnWarningRaised += OnWarningRaised;
            publisher.OnOutOfBandRaised += OutOfBandRaised;
            publisher.OnVoltageSpike += OnVoltageSpike;
            publisher.OnImpedanceJump += OnImpedanceJump;
        }

        private void OnVoltageSpike(object sender, VoltageSpikeEventArgs e)
        {
            Console.WriteLine($"[VOLTAGE SPIKE][{e.Smer}]: {e.BatteryId}/{e.TestId}/{e.SoC}% prethodna vrednost {e.PreviusValue} a nova vrednost {e.NewValue}");
            LogSpikes(e);
        }
        private void OnImpedanceJump(object sender, ImpedanceJumpEventArgs e)
        {
            Console.WriteLine($"[IMPEDANCE JUMP][{e.Smer}]: {e.BatteryId}/{e.TestId}/{e.SoC}% prethodna vrednost {e.PreviusValue} a nova vrednost {e.NewValue}");
            LogSpikes(e);
        }
        private void LogSpikes(SpikeEventArgs e)
        {
            try
            {
                string line = $"{e.BatteryId}/{e.TestId}/SoC{e.SoC}% | [VOLTAGE SPIKE] | {e.PreviusValue} -> {e.NewValue}]| [{e.Smer}]\n";
                File.AppendAllText("warnings.txt", line);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greska pri logovanju: {ex.Message}");
            }
        }
        private void OnWarningRaised(object sender, WarningRaisedEventArgs e)
        {
            Console.WriteLine($"Warning:[{e.WarningType}]: {e.Message}");
            Console.WriteLine($"Vrednost: {e.ActualValue},Prag {e.Threshold}");

            LogWarning(e);
        }
        private void OutOfBandRaised(object sender, WarningRaisedEventArgs e)
        {
            Console.WriteLine($"Out of Band Warning:[{e.WarningType}]: {e.Message}");
            Console.WriteLine($"Vrednost: {e.ActualValue},Prag {e.Threshold}");

            LogWarning(e);
        }
        private void LogWarning(WarningRaisedEventArgs e)
        {
            try
            {
                string line = $"{e.BatteryId}/{e.TestId}/SoC{e.SoC}% | {e.WarningType} | {e.Message} | [{e.ActualValue}] | [{e.Threshold}]\n";
                File.AppendAllText("warnings.txt", line);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greska pri logovanju: {ex.Message}");
            }
        }

        private void OnTransferCompleted(object sender, TransferCompletedEventArgs e)
        {
            Console.WriteLine($"Transfer zavrsen: {e.BatteryId}/{e.TestId}/SoC{e.SoC}%");
            Console.WriteLine($"Ukupno merenja: {e.TotalSamples} od toga {e.ValidSamples} je validno a {e.RejectedSamples} je odbijeno");
        }

        private void OnSampleReceived(object sender, SampleReceivedEventArgs e)
        {
            Console.WriteLine($"Sample #{e.TotalSamplesRecived}: Red{e.Sample.RowIndex}");
        }

        private void OnTransferStarted(object sender, TransferStartedEventArgs e)
        {
            Console.WriteLine($"TRANSFER POCEO: {e.BatteryId}/{e.TestId}/SoC{e.SoC}%");
        }
    }
}
