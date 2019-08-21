using System.IO;

namespace AutoCallerWindowsService.Global
{
    sealed class Settings
    {
        private readonly SettingsInfo _settings;

        private Settings()
        {
            try
            {
                if (!Directory.Exists(@"C:\medhelp"))
                    Directory.CreateDirectory(@"C:\medhelp");

                string jsonString = File.ReadAllText(@"C:\medhelp\settings.json");
                _settings = Editors.JsonEditor.Serializer.Deserialize<SettingsInfo>(jsonString);
                Init(_settings);
                LogWriter.Add("Инициализированы настройки из json");
            }
            catch
            {
                LogWriter.Add("Ошибка в загрузке файла с настройками");
                throw;
            }
        }

        private void Init(SettingsInfo setting)
        {
            AutoCallerAPIIPAddress = setting.AutoCallerAPIIPAddress;
            AutoCallerAPIPort = setting.AutoCallerAPIPort;
            AutoCallerDatabaseName = setting.AutoCallerDatabaseName;
            AutoCallerDatabasePort = setting.AutoCallerDatabasePort;
            AutoCallerDatabaseIPAddress = setting.AutoCallerDatabaseIPAddress;
            BasePath = setting.BasePath;
        }

        /// <summary>
        /// Объект класса Settings
        /// </summary>
        public static Settings Instance { get; set; } = new Settings();

        public string AutoCallerAPIIPAddress { get; private set; }

        public string AutoCallerAPIPort { get; private set; }

        /// Порт базы
        public string AutoCallerDatabasePort { get; private set; }

        /// Name базы данных
        public string AutoCallerDatabaseName { get; private set; }
        /// IP базы данных
        public string AutoCallerDatabaseIPAddress { get; private set; }

        public string BasePath { get; private set; }
    }
}
