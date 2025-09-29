using Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    public class BatteryDataScanner
    {
        public List<EisMeta> ScanedDataset(string root,string batteryid,string testid)
        {
            List<EisMeta> results = new List<EisMeta>();
            DirectoryInfo di;
            DirectoryInfo batteryID = null;
            DirectoryInfo testID = null;
            DirectoryInfo hioki = null;
            if (Directory.Exists(root))
            {

                di = new DirectoryInfo(root);
            }
            else
            {
                Console.WriteLine($"Folder ${root} ne postoji");
                return results;
            }
            batteryID = di.GetDirectories().FirstOrDefault(d => d.Name == batteryid);
            if (batteryID == null)
            {
                Console.WriteLine($@"Putanja {root}\{batteryid} ne postoji");
                return results;
            }
            testID = batteryID.GetDirectories().FirstOrDefault(d => d.Name == "EIS measurements").
                GetDirectories().FirstOrDefault(d => d.Name == testid);
            if(testID == null )
            {
                Console.WriteLine($@"{testid} folder ne postoji u putanji {root}\{batteryid}\EIS measurements");
                return results;
            }
            hioki = testID.GetDirectories().FirstOrDefault(d => d.Name == "Hioki");
            if (hioki == null)
                return results;
            foreach (var file in hioki.GetFiles("*.csv"))
            {
                int SoC = GetSoC(file.Name);
                if (SoC == -1)
                {
                    continue;
                }

                using (FileStream fs = file.Open(FileMode.Open))
                {
                    using (StreamReader sr = new StreamReader(fs))
                    {
                        int rows = 0;
                        string line;
                        sr.ReadLine();
                        while ((line = sr.ReadLine()) != null)
                        {
                            rows++;
                        }
                        if(rows == 28)
                        {
                            
                            results.Add(new EisMeta { BatteryId = batteryid, TestId = testid,FileName = fs.Name,FilePath=file.FullName,SoC = SoC,TotalRows = rows });
                        }
                        else
                        {
                            results.Add(new EisMeta { BatteryId = batteryid, TestId = testid, FileName = fs.Name, FilePath = file.FullName, SoC = SoC, TotalRows = rows });
                            Console.WriteLine($"Fajl {file.Name} ima {rows} redova umesto ocekivanih 28");
                            Logger.Log("Logs","scan_errors.txt",$"Fajl {file.Name} ima {rows} redova umesto ocekivanih 28");
                        }
                    }
                }
            }
            return results;
        }
        public int GetSoC(string s)
        {
            string[] strings = s.Split('_');
            int SoC = Int32.Parse(strings[3]);
            if (!strings[2].Equals("SoC"))
            {
                Logger.Log("Logs","scan_errors.txt",$"Nepravilno ime fajla: {s}");
                return -1;
            }
            if(SoC>=5 &&  SoC<=100 && SoC%5 ==0) 
                return SoC;
            else
            {
                Logger.Log("Logs", "scan_errors.txt", $"Fajl: {s} - Nepravilna vrednost za SoC {SoC}");
                return -1;
            }
        }
    }
}
