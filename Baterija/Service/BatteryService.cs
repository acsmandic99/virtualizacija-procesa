using Common;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using EventInfo = Common.EventInfo;

namespace Service
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession,
                  ConcurrencyMode = ConcurrencyMode.Single,
                  IncludeExceptionDetailInFaults = true)]
    public class BatteryService : IBatteryService
    {
        private int _lastRowIndex = -1;
        private string currentSessionFile;
        string basePath = "Data";
        private string rejectPath;
        private EisMeta currentMeta;
        private int recivedSamples;
        private int rejectedSamples;
        private BatteryEventPublisher _eventPublisher => Program.EventPublisher;


        private readonly double _vThreshold;
        private readonly double DeltaV_threshold;
        private readonly double _zThreshold;
        private readonly double Delta_zThreshold;
        private readonly double _deviationThresholdPercent;

        private double _currentVoltageSum = 0;
        private double _currentImpedanceSum = 0;
        private int _currentSampleCount = 0;
        private double _previusVoltage = 0;
        private double _previusImpedance = 0;
        public BatteryService()
        {
            _vThreshold = double.Parse(ConfigurationManager.AppSettings["V_threshold"]);
            DeltaV_threshold = double.Parse(ConfigurationManager.AppSettings["DeltaV_threshold"]);
            _zThreshold = double.Parse(ConfigurationManager.AppSettings["Z_threshold"]);
            Delta_zThreshold = double.Parse(ConfigurationManager.AppSettings["DeltaZ_threshold"]);
            _deviationThresholdPercent = double.Parse(ConfigurationManager.AppSettings["DeviationThresholdPercent"]);
        }


        public Response EndSession()
        {
            currentSessionFile = null;
            _lastRowIndex = -1;
            _eventPublisher.RaiseTransferCompleted(currentMeta.BatteryId, currentMeta.TestId, currentMeta.SoC, recivedSamples, recivedSamples - rejectedSamples, rejectedSamples);
            List<EventInfo> events = new List<EventInfo>();
            events.Add(new EventInfo { Type = "Transfer zavrsen", Message = $"Transfer zavrsen: {currentMeta.BatteryId}/{currentMeta.TestId}/SoC{currentMeta.SoC}%\nUkupno merenja: {recivedSamples} od toga {recivedSamples - rejectedSamples} je validno a {rejectedSamples} je odbijeno" });
            return new Response { Success = true, Status = "ACK", Message = "Session Ended",Events = events };
        }


        public Response PushSample(EisSample sample)
        {
            recivedSamples++;
            List<Common.EventInfo> events = new List<EventInfo>();
            events.Add(new EventInfo { Type = "Sample recived",Message=$"Sample primljen {recivedSamples}",Timestamp=DateTime.Now});
            using (BatteryDataService dataService = new BatteryDataService(currentSessionFile, rejectPath))
            {
                try
                {
                    ValidateSample(sample);
                    dataService.SaveSample(sample);

                    _currentVoltageSum += sample.V;
                    double impedance = Math.Sqrt(sample.R_ohm * sample.R_ohm + sample.X_ohm * sample.X_ohm);
                    _currentImpedanceSum += impedance;
                    _currentSampleCount++;
                    string message = "";
                    
                    if(recivedSamples >1)
                    {
                        message  = CheckForVoltageSpike(_previusVoltage, sample.V,events);
                        message += CheckForImpedanceJump(_previusImpedance, impedance,events);
                    }
                    _previusVoltage = sample.V;
                    _previusImpedance = impedance;
                    message += CheckThresholds(currentMeta.BatteryId, currentMeta.TestId, currentMeta.SoC, sample, impedance,events);

                    _eventPublisher.RaiseSampleReceived(currentMeta.BatteryId, currentMeta.TestId, currentMeta.SoC, sample, recivedSamples);

                    return new Response { Success = true, Status = "ACK" ,Message = message,Events = events};
                }
                catch (FaultException<ValidationFault> ex)
                {
                    double impedance = Math.Sqrt(sample.R_ohm * sample.R_ohm + sample.X_ohm * sample.X_ohm);
                    _previusVoltage = sample.V;
                    _previusImpedance = impedance;
                    rejectedSamples++;
                    dataService.SaveRejectedSample(currentMeta, sample, ex.Detail.Message);
                    return new Response { Message = $"Validaciona greska: {ex.Message}", Status = "NACK", Success = false };
                }
                catch (FaultException<DataFormatFault> ex)
                {
                    double impedance = Math.Sqrt(sample.R_ohm * sample.R_ohm + sample.X_ohm * sample.X_ohm);
                    _previusVoltage = sample.V;
                    _previusImpedance = impedance;
                    rejectedSamples++;
                    dataService.SaveRejectedSample(currentMeta,sample, ex.Detail.Message);
                    return new Response { Message = $"Validaciona greska: {ex.Message}", Status = "NACK", Success = false };
                }
                catch (Exception ex)
                {
                    double impedance = Math.Sqrt(sample.R_ohm * sample.R_ohm + sample.X_ohm * sample.X_ohm);
                    _previusVoltage = sample.V;
                    _previusImpedance = impedance;
                    rejectedSamples++;
                    dataService.SaveRejectedSample(currentMeta, sample, ex.Message);
                    return new Response { Message = $"Interna greska: {ex.Message}", Status = "NACK", Success = false };
                }

            }
        }

        private string CheckForImpedanceJump(double previusImpedance, double NewImpedance,List<EventInfo> events)
        {
            double deltaImpedance = NewImpedance - previusImpedance;
            if(Math.Abs(deltaImpedance) > Delta_zThreshold)
            {
                string smer;
                if (deltaImpedance < 0)
                {
                    smer = "ISPOD OCEKIVANE VREDNOSTI";
                }
                else
                    smer = "IZNAD OCEKIVANE VREDNOSTI";
                _eventPublisher.RaiseImpedanceJump(currentMeta.BatteryId, currentMeta.TestId, currentMeta.SoC, NewImpedance, previusImpedance, smer);
                events.Add(new EventInfo { Type = "[IMPEDANCE JUMP]", Message = $"Impedansa je skocila sa: {previusImpedance} na {NewImpedance} sto je {smer}", Timestamp = DateTime.Now });
                return $"[IMPEDANCE JUMP] Impedansa je skocila sa: {previusImpedance} na {NewImpedance} sto je {smer}\n";
            }
            return "";
        }

        private string CheckForVoltageSpike(double previusVoltage, double newVoltage,List<EventInfo> events)
        {
            double deltaV = newVoltage - previusVoltage;
            if(Math.Abs(deltaV) > DeltaV_threshold)
            {
                string smer;
                if (deltaV < 0)
                    smer = "ISPOD OCEKIVANOG";
                else
                    smer = "IZNAD OCEKIVANOG";
                _eventPublisher.RaiseVoltageSpike(currentMeta.BatteryId,currentMeta.TestId,currentMeta.SoC,newVoltage,previusVoltage,smer);
                events.Add(new EventInfo { Type = "[SPIKE VOLTAGE]", Message = "[PROSLA VREDNOST NAPONA]: {previusVoltage} [NOVA VREDNOST NAPONA]: {newVoltage} | {smer}", Timestamp = DateTime.Now });
                return $"[SPIKE VOLTAGE] [PROSLA VREDNOST NAPONA]: {previusVoltage} [NOVA VREDNOST NAPONA]: {newVoltage} | {smer}\n";
            }
            return "";
        }

        public void ValidateSample(EisSample sample)
        {

            if (double.IsInfinity(sample.V) || double.IsNaN(sample.V))
                throw new FaultException<DataFormatFault>(new DataFormatFault { Message = "Napon mora biti realan broj", Field = "V" }, "Nevalidni podaci");

            if (double.IsNaN(sample.X_ohm) || double.IsInfinity(sample.X_ohm))
                throw new FaultException<DataFormatFault>(new DataFormatFault { Message = "X ohm mora biti realan broj", Field = "V" }, "Nevalidni podaci");

            if (double.IsNaN(sample.Range_ohm) || double.IsInfinity(sample.R_ohm))
                throw new FaultException<DataFormatFault>(new DataFormatFault { Message = "Vrednost Range ohm nije validna", Field = "Range_ohm" }, "Nevalidni podaci");

            if (double.IsNaN(sample.T_degC) || double.IsInfinity(sample.T_degC))
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault { Message = "Temperatura mora biti validan broj", Field = "T_degC" }, "Format greške");

            if (sample.FrequencyHz <= 0)
                throw new FaultException<ValidationFault>(new ValidationFault { Message = "Frekvencija mora biti veca od 0", Field = "FrequencyHz" }, "Nevalidni podaci");

            if (sample.Range_ohm <= 0)
                throw new FaultException<ValidationFault>(new ValidationFault { Message = "Otpornost mora biti pozitivan broj", Field = "V" }, "Nevalidni podaci");

            if (sample.V < 0)
                throw new FaultException<ValidationFault>(new ValidationFault { Message = "Napon ne moze biti negativan", Field = "V" }, "Nevalidni podaci");

            if (sample.RowIndex <= _lastRowIndex)
                throw new FaultException<ValidationFault>(new ValidationFault { Message = "RowIndex mora biti monotono rastuci", Field = "RowIndex" }, "Nevalidni podaci");
            _lastRowIndex = sample.RowIndex;

            if (sample.T_degC < -20 || sample.T_degC > 60)
                throw new FaultException<ValidationFault>(
                    new ValidationFault { Message = $"Temperatura {sample.T_degC}C van dozvoljenog opsega", Field = "T_degC" }, "Nevalidni podaci");
        }
        public Response StartSession(EisMeta header)
        {
            _currentVoltageSum = 0;
            _currentImpedanceSum = 0;
            _currentSampleCount = 0;
            currentMeta = header;
            recivedSamples = 0;
            rejectedSamples = 0;

            string currentSessionPath = Path.Combine(basePath, header.BatteryId, header.TestId, header.SoC.ToString());
            string sessionFile = Path.Combine(currentSessionPath, "session.csv");
            if (File.Exists(sessionFile))
                File.Delete(sessionFile);

            Directory.CreateDirectory(currentSessionPath);
            currentSessionFile = sessionFile;

            using (StreamWriter writer = new StreamWriter(sessionFile, append: true))
            {
                writer.WriteLine("RowIndex,Frequency(Hz),R(ohm),X(ohm),V(V),T(deg C),Range(Ohm),Timestamp");
            }

            string rejectsFile = Path.Combine(basePath, header.BatteryId, header.TestId, "rejects.csv");
            if (!File.Exists(rejectsFile))
            {
                using (StreamWriter writer = new StreamWriter(rejectsFile, append: true))
                {
                    writer.WriteLine("BatteryId,TestId,SoC,Reason,RowIndex,Frequency(Hz),R(ohm),X(ohm),V(V),T(deg C),Range(Ohm),Timestamp");
                }

            }
            rejectPath = rejectsFile;
            Directory.CreateDirectory(currentSessionPath);
            currentSessionFile = sessionFile;
            _eventPublisher.RaiseTransferStarted(currentMeta.BatteryId, currentMeta.TestId, currentMeta.SoC, sessionFile);
            List<EventInfo> events = new List<EventInfo>();
            events.Add(new EventInfo { Type = "Transfer started", Message = $"TRANSFER POCEO: {currentMeta.BatteryId}/{currentMeta.TestId}/SoC{currentMeta.SoC}%" });
            return new Response { Success = true, Status = "ACK", Message = "Session Started",Events = events };
        }

        private string CheckThresholds(string batteryId, string testId, int soc, EisSample sample, double impedance,List<EventInfo> events)
        {
            string message = "";
            if (Math.Abs(impedance) > _zThreshold)
            {
                message += $"Impedansa probila prag {impedance}\n";
                _eventPublisher.RaiseOnOutOfBandWarning(batteryId, testId, soc, "Z_Threshold", $"Impedansa probila prag {impedance}", impedance, _zThreshold, sample);
                events.Add(new EventInfo { Type = "[WARNING]: [Z_Threshold]", Message = $"Impedansa probila prag {impedance}", Timestamp = DateTime.Now });
            }
            if (sample.V > _vThreshold)
            {
                message += $"Napon je probio prag {sample.V}\n";
                events.Add(new EventInfo { Type = "[WARNING]: [V_Threshold]", Message = $"Napon je probio prag {sample.V}", Timestamp = DateTime.Now });
                _eventPublisher.RaiseOnOutOfBandWarning(batteryId, testId, soc, "V_Threshold", $"Napon je probio prag {sample.V}", sample.V, _vThreshold, sample);

            }
            if (_currentSampleCount > 1)
            {
                double avgImpedance = _currentImpedanceSum / _currentSampleCount;
                double deviation = Math.Abs((impedance - avgImpedance) / avgImpedance) * 100;
                if (deviation > _deviationThresholdPercent)
                {
                    message += $"Impedansa odstupila od proseka:{deviation:F5}%\n";
                    events.Add(new EventInfo { Type = "[Out Of Band Warning]: [DEVIJACIJA]", Message = $"Impedansa odstupila od proseka:{deviation:F5}%", Timestamp = DateTime.Now });
                    _eventPublisher.RaiseOnOutOfBandWarning(batteryId, testId, soc, "DEVIJACIJA", $"Impedansa odstupila od proseka:{deviation:F5}%", deviation, _deviationThresholdPercent, sample);
                }
                double avgVoltage = _currentVoltageSum / _currentSampleCount;
                deviation = Math.Abs((sample.V - avgVoltage) / avgVoltage) * 100;
                if (deviation > _deviationThresholdPercent)
                {
                    message += $"Napon odstupio od proseka: {deviation:F5}%\n";
                    events.Add(new EventInfo { Type = "[Out Of Band Warning]: [DEVIJACIJA]", Message = $"Napon odstupio od proseka: {deviation:F5}%", Timestamp = DateTime.Now });
                    _eventPublisher.RaiseOnOutOfBandWarning(batteryId, testId, soc, "DEVIJACIJA", $"Napon odstupio od proseka: {deviation:F5}%", deviation, _deviationThresholdPercent, sample);
                }
            }
            return message;
        }
    }
}
