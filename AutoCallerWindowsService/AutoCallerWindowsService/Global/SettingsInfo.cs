namespace AutoCallerWindowsService.Global
{
    class SettingsInfo
    {
        /// Name базы данных
        public string AutoCallerDatabaseName { get; set; }
        /// IP базы данных
        public string AutoCallerDatabaseIPAddress { get; set; }

        /// Порт базы
        public string AutoCallerDatabasePort { get; set; }

        /// IP Api
        public string AutoCallerAPIIPAddress { get; set; }

        /// Порт Api
        public string AutoCallerAPIPort { get; set; }

        public string BasePath { get; set; }
    }
}
