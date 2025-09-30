using Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Client
{
    public class DataTransferManager
    {
        public void TransferAllData(string datasetPath)
        {
            BatteryDataScanner scanner = new BatteryDataScanner();
            List<EisMeta> allMetaData = scanner.ScanedDataset(datasetPath, "B01", "Test_1");
            Console.WriteLine($"Pronadjeno {allMetaData.Count} SoC nivoa za prenos");

            foreach (var meta in allMetaData)
            {
                using (ChannelFactory<IBatteryService> client = new ChannelFactory<IBatteryService>("BatteryService"))
                {

                    TransferSingleFile(meta, client);

                    Thread.Sleep(1000);
                }
            }
            Console.WriteLine("Svi podaci uspesno poslati");
        }

        private void TransferSingleFile(EisMeta meta, ChannelFactory<IBatteryService> client)
        {
            try
            {
                IBatteryService proxy = client.CreateChannel();
                Console.WriteLine($"\nZapocinjem prenos: SoC {meta.SoC}%");
                Response startResponse = proxy.StartSession(meta);
                if (!startResponse.Success)
                {
                    Console.WriteLine($"Greska pri startu sesije: {startResponse.Message} ");
                    Logger.Log("Logs", "unknown_errors.txt", $"Greska pri startu sesije: {startResponse.Message} ");

                    return;
                }
                Console.WriteLine($"Prenos u toku: SoC {meta.SoC}%");


                int sentSamples = SendSampleRow(meta, meta.FilePath, proxy);
                Response endResponse = proxy.EndSession();
                if (!endResponse.Success)
                {
                    Console.WriteLine($"Greska pri zatvaranju sesije: {endResponse.Message}");
                    Logger.Log("Logs", "unknown_errors.txt", $"Greska pri zatvaranju sesije: {endResponse.Message}");
                }
                Console.WriteLine($"Zavrsen prenos: SoC {meta.SoC}%");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greska pri prenosu SoC {meta.SoC}% {ex.Message}");
                Logger.Log("Logs", "unknown_errors.txt", $"Greska pri prenosu SoC {meta.SoC}% {ex.Message}");
            }
        }

        private int SendSampleRow(EisMeta meta, string filePath, IBatteryService proxy)
        {
            int sentCount = 0;

            try
            {
                using (StreamReader sr = new StreamReader(filePath))
                {
                    sr.ReadLine();//header
                    string line;
                    int rowIndex = 1;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (string.IsNullOrWhiteSpace(line))
                        {
                            Logger.Log("Logs", "unvalid_rows.txt", $"{meta.BatteryId}/{meta.TestId}/{meta.SoC},{rowIndex}: " +
                                                         $"Red je prazan");
                            rowIndex++;

                            continue;
                        }
                        EisSample sample = ParseCsvLine(meta, line, rowIndex);
                        if (sample != null)
                        {
                            if (rowIndex <= 28)
                            {
                                try
                                {
                                    Response response = proxy.PushSample(sample);
                                    if (response.Success)
                                    {
                                        sentCount++;
                                        Console.WriteLine($"Poslat sample {rowIndex}/28");
                                        if (response.Message != null)
                                        {
                                            Console.WriteLine($"{response.Message}");
                                        }
                                        foreach (var ev in response.Events)
                                        {
                                            Logger.Log("Logs", "event_logs.txt", $"{ev.Type}  {ev.Message}  {ev.Timestamp.ToString("yyyy-MM-dd HH-mm-ss")}");
                                        }
                                    }

                                    else
                                    {
                                        Console.WriteLine($"Odbijen sample  {rowIndex}: {response.Message}");
                                        Logger.Log("Logs", "unvalid_rows.txt", $"Odbijen sample  {rowIndex}: {response.Message}");
                                    }
                                }
                                catch (FaultException<DataFormatFault> ex)
                                {
                                    Console.WriteLine($"DataFormat Exception {ex.Detail.Field} {ex.Detail.Message}");
                                    Logger.Log("Logs", "exceptions.txt", $"DataFormat Exception {ex.Detail.Field} {ex.Detail.Message}");

                                }
                                catch (FaultException<ValidationFault> ex)
                                {
                                    Console.WriteLine($"ValidationFault Exception {ex.Detail.Field} {ex.Detail.Message}");
                                    Logger.Log("Logs", "exceptions.txt", $"ValidationFault Exception {ex.Detail.Field} {ex.Detail.Message}");
                                }

                                Thread.Sleep(100);
                            }
                            else
                            {
                                Logger.Log("Logs", "leftover_rows.txt", $"{meta.BatteryId}/{meta.TestId}/{meta.SoC}," +
                                                                        $"{sample.RowIndex.ToString(CultureInfo.InvariantCulture)},{sample.FrequencyHz.ToString(CultureInfo.InvariantCulture)}," +
                                                                        $"{sample.R_ohm.ToString(CultureInfo.InvariantCulture)}," +
                                                                        $"{sample.X_ohm.ToString(CultureInfo.InvariantCulture)},{sample.V.ToString(CultureInfo.InvariantCulture)},{sample.T_degC.ToString(CultureInfo.InvariantCulture)}," +
                                                                        $"{sample.Range_ohm.ToString(CultureInfo.InvariantCulture)},{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
                            }
                        }
                        rowIndex++;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greska pri slanju sampla: {ex.Message}");
                Logger.Log("Logs", "unknown_errors.txt", $"Greska pri slanju sampla: {ex.Message}");

            }
            return sentCount;
        }
        private EisSample ParseCsvLine(EisMeta meta, string line, int rowIndex)
        {
            try
            {
                string[] parts = line.Split(',');
                if (parts.Length != 6)
                {
                    Logger.Log("Logs", "unvalid_rows.txt", $"{meta.BatteryId}/{meta.TestId}/{meta.SoC},{rowIndex}: " +
                                                        $"Ima previse kolona: {parts.Length}");
                    return null;
                }
                return new EisSample
                {
                    RowIndex = rowIndex,
                    FrequencyHz = double.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture),
                    R_ohm = double.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture),
                    X_ohm = double.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture),
                    V = double.Parse(parts[3], System.Globalization.CultureInfo.InvariantCulture),
                    T_degC = Int32.Parse(parts[4], System.Globalization.CultureInfo.InvariantCulture),
                    Range_ohm = double.Parse(parts[5], System.Globalization.CultureInfo.InvariantCulture)
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greska pri parsiranju reda {rowIndex}: {ex.Message}");
                Console.WriteLine(" dsa" + line);
                Logger.Log("Logs", "unknown_errors.txt", $"{meta.BatteryId}/{meta.TestId}/{meta.SoC},{rowIndex}: " +
                                                         $"Greska pri parsiranju reda: {ex.Message}");
                return null;
            }
        }

    }
}
