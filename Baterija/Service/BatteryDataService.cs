using Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service
{
    public class BatteryDataService : IDisposable
    {
        private bool disposed = false;
        private StreamWriter writer;
        private StreamWriter rejectWriter;

        public BatteryDataService(string filePathSession,string filePathRejects)
        {
            writer = new StreamWriter(filePathSession, append: true);
            rejectWriter = new StreamWriter(filePathRejects, append: true);

        }
        public void SaveSample(EisSample sample)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(BatteryDataService));
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var line = $"{sample.RowIndex.ToString(CultureInfo.InvariantCulture)},{sample.FrequencyHz.ToString(CultureInfo.InvariantCulture)},{sample.R_ohm.ToString(CultureInfo.InvariantCulture)}," +
                $"{sample.X_ohm.ToString(CultureInfo.InvariantCulture)},{sample.V.ToString(CultureInfo.InvariantCulture)},{sample.T_degC.ToString(CultureInfo.InvariantCulture)}," +
                $"{sample.Range_ohm.ToString(CultureInfo.InvariantCulture)},{timestamp}";
            writer.WriteLine(line);
            writer.Flush();
        }
        public void SaveRejectedSample(EisMeta meta,EisSample sample,string reason)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(BatteryDataService));
            var line = $"{meta.BatteryId},{meta.TestId},{meta.SoC},{reason},{sample.RowIndex.ToString(CultureInfo.InvariantCulture)},{sample.FrequencyHz.ToString(CultureInfo.InvariantCulture)}," +
                $"{sample.R_ohm.ToString(CultureInfo.InvariantCulture)},{sample.X_ohm.ToString(CultureInfo.InvariantCulture)},{sample.V},{sample.T_degC.ToString(CultureInfo.InvariantCulture)}," +
                $"{sample.Range_ohm.ToString(CultureInfo.InvariantCulture)},{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}";
            rejectWriter.WriteLine(line);
            rejectWriter.Flush();
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {

                if (disposing)
                {
                    writer?.Dispose();
                    rejectWriter?.Dispose();
                    writer = null;
                    rejectWriter = null;
                }
                disposed = true;
            }

        }
        ~BatteryDataService()
        {
            Dispose(false);
        }
    }
}
