using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Data.SQLite;
using System.Reflection;
using System.Configuration;

namespace Df1ProtocolAnalyzer
{
    public class DBUtil
    {
        SQLiteConnection _dbcon;

        public DBUtil()
        {
            var exePath = Assembly.GetExecutingAssembly().Location;
            var exeDir = new FileInfo(exePath).Directory.FullName;
            var dbName = System.Configuration.ConfigurationManager.AppSettings["dbName"];
            var dbPath = Path.Combine(exeDir, dbName);

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

        public string LookupControl(PlcControl plcControl)
        {
            try
            {
                using (var cmd = new SQLiteCommand($"select Name from [Controls] where [Id] = '{plcControl.Key}'", _dbcon))
                {
                    return cmd.ExecuteScalar() as string;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to lookup control '{plcControl.Key}', {ex.Message}");
                return null;
            }
        }

        public void AddControl(PlcControl plcControl)
        {
            try
            {
                using (var cmd = new SQLiteCommand($"insert into [Controls] (Id, Name, FileNumber, Element, SubElement, FileType, Offset) values (" +
                    $"'{plcControl.Key}','{plcControl.Key}',{plcControl.FileNumber},{plcControl.ElementNumber}," +
                    $"{plcControl.SubElementNumber},'{plcControl.FileType}',{plcControl.Offset})", _dbcon))
                {
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to add control '{plcControl.Key}', {ex.Message}");
            }
        }

        
    }
}
