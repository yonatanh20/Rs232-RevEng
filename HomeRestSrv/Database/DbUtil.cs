using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Data.SQLite;
using System.Reflection;
using System.Configuration;

namespace HomeRestSrv
{
    public class DBUtil
    {
        SQLiteConnection _dbcon;

        public DBUtil()
        {
            var exePath = Assembly.GetExecutingAssembly().Location;
            var exeDir = new FileInfo(exePath).Directory.FullName;
            var dbName = System.Configuration.ConfigurationManager.AppSettings["dbName"];
            var dbPath = Path.GetFullPath(Path.Combine(exeDir, dbName));

            try
            {
                var cb = new SQLiteConnectionStringBuilder();

                cb.DataSource = dbPath;

                _dbcon = new SQLiteConnection(cb.ToString());
                _dbcon.Open();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to open database, {ex.Message}");
                Environment.Exit(1);
            }


        }
        public List<HistoryRecord> DeviceHistory(string wantedName, long startDate, uint entryCount)
        {
            var entries = new List<HistoryRecord>();

            wantedName = (null == wantedName) ? "" : $"AND [Name] LIKE '%{wantedName}%'";
            entryCount = (entryCount == 0) ? 25 : entryCount;

            string cmdString = $"SELECT * FROM [History] WHERE [TimeStamp] < '{startDate}' {wantedName} ORDER BY [TimeStamp] DESC LIMIT {entryCount}";

            //Todo: Handle query parameterization
            var prms = new SQLiteParameter[]
            {
                new SQLiteParameter("startDate", startDate),
                new SQLiteParameter("wantedName", wantedName),
                new SQLiteParameter("entryCount", entryCount)
            };
            try
            {

                using (var cmd = new SQLiteCommand(cmdString, _dbcon))
                {
                    var reader = cmd.ExecuteReader();
                    while (reader.Read() && entryCount > 0)
                    {
                        string id = (string)reader["Id"];
                        string name = (string)reader["Name"];
                        long timestampTicks = long.Parse(reader["TimeStamp"].ToString());
                        int newValue = int.Parse(reader["NewState"].ToString());
                        entries.Add(new HistoryRecord(id, name, timestampTicks, newValue));

                        entryCount--;
                    }
                    reader.Close();
                    return entries;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to fetch history, {ex.Message}");
                return entries;
            }
        }

        public List<Control> GetControl()
        {
            string cmdString = $"SELECT * FROM [Controls]";
            var controls = new List<Control>();

            try
            {

                using (var cmd = new SQLiteCommand(cmdString, _dbcon))
                {
                    var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        string id = (string)reader["Id"];
                        string name = (string)reader["Name"];
                        int fileNumber = int.Parse(reader["FileNumber"].ToString());
                        int element = int.Parse(reader["Element"].ToString());
                        int subElement = int.Parse(reader["SubElement"].ToString());
                        string fileType = (string)reader["FileType"];
                        int offset = int.Parse(reader["Offset"].ToString());
                        controls.Add(new Control(id , name , fileNumber , element, subElement, fileType, offset));
                        
                    }
                    reader.Close();
                    return controls;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to fetch Controls, {ex.Message}");
                return controls;
            }
        }
    }
}
