using Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Service;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BatteryService.Tests
{
    [TestClass]
    public class DisposePatternTests
    {
        [TestMethod]
        public void TestDispose_ClosesFileResources()
        {
            string testFile = Path.GetTempFileName();
            string reject = Path.GetTempFileName();
            using (var service = new BatteryDataService(testFile,reject))
            {
                service.SaveSample(new EisSample { RowIndex = 1, FrequencyHz = 1.0 });

            }//Tu se poziva Dispose iz destruktora
            try
            {
                File.Delete(testFile);
                Assert.IsFalse(File.Exists(testFile));
            }
            catch (Exception ex)
            {
                Assert.Fail($"Fajl nije pravilno zatvoren: {ex.Message}");
            }
        }
        [TestMethod]
        [ExpectedException(typeof(ObjectDisposedException))]
        public void TestDisposedObject_ThrowsException()
        {
            string testFilePath = Path.GetTempFileName();
            string reject = Path.GetTempFileName();
            var service = new BatteryDataService(testFilePath,reject);

            service.Dispose();
            service.SaveSample(new EisSample { RowIndex = 1, FrequencyHz = 1.0 });
        }

        [TestMethod]
        public void TestDispose_Simulation()
        {
            string testFile = Path.GetTempFileName();
            string reject = Path.GetTempFileName();
            bool exceptionThrown = false;
            BatteryDataService service = null;

            try
            {
                using (service = new BatteryDataService(testFile, reject))
                {
                    service.SaveSample(new EisSample { RowIndex = 1, FrequencyHz = 1.0 });
                    throw new IOException("Prekid veze");
                }
            }
            catch (IOException ex)
            {
                exceptionThrown = true;
            }
            finally
            {
                Assert.IsTrue(exceptionThrown);
                try
                {
                    service.SaveSample(new EisSample { RowIndex = 2, FrequencyHz = 2.0 });
                    Assert.Fail();
                }
                catch(ObjectDisposedException)
                {

                }
            }
        }
    }
}
