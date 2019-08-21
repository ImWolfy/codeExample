using System.IO;

namespace AutoCallerWindowsService.Controllers.FileController
{
    class FileController
    {
        public static void SaveFile(string name, string content, string path)
        {
            var fullName = $"{path}\\{name}.call";
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            if (File.Exists(fullName))
                throw new System.Exception($"Файл {name}.call уже существует");
            using (var wr = new StreamWriter(fullName, false))
            {
                wr.Write(content);
                wr.Flush();
            }
        }
    }
}
