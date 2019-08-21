using System;
using System.IO;

namespace AutoCallerWindowsService.Global
{
    class LogWriter
    {
        static object obj = new object();
        static string path = @"C:\medhelp";

        /// <summary>
        /// Добавляет запись в файл
        /// </summary>
        /// <param name="info">Запись</param>
        public static void Add(string info)
        {
            lock (obj)
            {
                string result = $"[{DateTime.Now}] : [AUTOCALLER] : [{info}]";
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                using (StreamWriter wr = new StreamWriter(path + "\\" + "logs.txt", true))
                {
                    wr.WriteLine(result);
                    wr.Flush();
                }
            }
        }
    }
}
