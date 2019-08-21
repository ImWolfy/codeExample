using AutoCallerWindowsService.Entities;
using MySql.Data.MySqlClient;
using System.Collections.Generic;

namespace AutoCallerWindowsService.Controllers.DbController
{
    class DbRequest
    {
        DbConnection dbConnection;

        public DbRequest()
        {
            dbConnection = new DbConnection();
        }

        public void InsertNewFileInfo(string centerID, string fileName)
        {
            var sql = $"INSERT INTO `auto_caller`(`center_id`, `file_name`)  VALUES(@centerID, @fileName)";
            using (dbConnection.Connection)
            {
                dbConnection.OpenConnection();
                using (var command = new MySqlCommand(sql, dbConnection.Connection))
                {
                    command.Parameters.AddWithValue("@centerID", centerID);
                    command.Parameters.AddWithValue("@fileName", fileName);
                    command.ExecuteNonQuery();
                }
            }
        }

        public List<CallState> GetCallStates(List<string> files)
        {
            var calls = new List<CallState>();
            var sql = "SELECT `status`, `result` FROM `auto_caller` WHERE `file_name` = @fileName";
            using (dbConnection.Connection)
            {
                dbConnection.OpenConnection();
                foreach (var file in files)
                    using (var command = new MySqlCommand(sql, dbConnection.Connection))
                    {
                        command.Parameters.AddWithValue("@fileName", file);
                        using (var reader = command.ExecuteReader())
                            while (reader.Read())
                            {
                                var callState = new CallState()
                                {
                                    FileName = file,
                                    Status = reader["Status"].ToString(),
                                    Result = reader["Result"].ToString()
                                };
                                calls.Add(callState);
                            }
                    }
            }
            return calls;
        }
    }
}
