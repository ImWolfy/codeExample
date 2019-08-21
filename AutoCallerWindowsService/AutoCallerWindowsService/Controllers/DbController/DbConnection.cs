using AutoCallerWindowsService.Global;
using MySql.Data.MySqlClient;
using System;

namespace AutoCallerWindowsService.Controllers.DbController
{
    class DbConnection
    {
        private DbConnectionData connectionData;
        private MySqlConnection connection;

        public MySqlConnection Connection
        {
            get
            {
                if (connection == null)
                    connection = new MySqlConnection(connectionData.ConnectionString);
                return connection;
            }
        }

        public DbConnection()
        {
            connectionData = new DbConnectionData();
            connection = new MySqlConnection(connectionData.ConnectionString);
        }

        public DbConnection(string database)
        {
            connectionData = new DbConnectionData(database);
            connection = new MySqlConnection(connectionData.ConnectionString);
        }

        public void OpenConnection()
        {
            if (connection.State != System.Data.ConnectionState.Open)
                try
                {
                    connection.Open();
                }
                catch (Exception exc)
                {
                    throw new Exception("Не удалось подключиться к базе данных", exc);
                }
        }

        public void CloseConnection()
        {
            if (connection.State != System.Data.ConnectionState.Closed)
                    connection.Close();
        }
    }

    class DbConnectionData
    {
        public string Database { get; private set; }
        public string DataSource { get; private set; }
        public string Port { get; private set; }
        public string UserID { get; } = "user";
        public string Password { get; } = "password";
        public string Charset { get; } = "utf8";
        public int ConnectionTimeOut { get; } = 10;
        public int MaxPoolSize { get; } = 150;
        public int MinPoolSize { get; } = 1;
        public bool Pooling { get; } = true;

        public DbConnectionData()
        {
            Settings settings = Settings.Instance;
            Database = settings.AutoCallerDatabaseName;
            DataSource = settings.AutoCallerDatabaseIPAddress;
            Port = settings.AutoCallerDatabasePort;
        }
        public DbConnectionData(string database)
            : this()
        {
            Database = database;
        }

        private string connectionString;

        public string ConnectionString
        {
            get
            {
                if (connectionString == null)
                {
                    MySqlConnectionStringBuilder stringBuilder = new MySqlConnectionStringBuilder
                    {
                        Database = Database,
                        Server = DataSource,
                        Port = uint.Parse(Port),
                        UserID = UserID,
                        Password = Password,
                        CharacterSet = Charset,
                        ConnectionTimeout = (uint)ConnectionTimeOut,
                        MaximumPoolSize = (uint)MaxPoolSize,
                        MinimumPoolSize = (uint)MinPoolSize,
                        Pooling = Pooling
                    };
                    connectionString = stringBuilder.ConnectionString;
                }
                return connectionString;
            }
        }
    }
}
