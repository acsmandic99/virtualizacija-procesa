using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    public static class Logger
    {
        public static void Log(string folderName,string fileName,string message)
        {
            Directory.CreateDirectory(folderName);
            string path = Path.Combine(folderName, fileName);
            File.AppendAllText(path,message+"\n");
        }
    }
}
