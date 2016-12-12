using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SQLite;
using System.IO;
using System.Threading.Tasks;
using BuffPanel.Logging;

namespace BuffPanel
{
    /**
     * Every public method in this class except Initialize is thread safe
     */
    internal class Db
    {
        private object synchronizer = new Object();
        private Logger logger;
        private string fileName;
        private SQLiteConnection dbConnection;

        public Db(Logger logger, string workingDirectory)
        {
            this.logger = logger;
            fileName = Path.Combine((workingDirectory == null) ? "" : workingDirectory, Constants.DATABASE_NAME);
        }

        public void Initialize()
        {
            if (!File.Exists(fileName))
            {
                try
                {
                    SQLiteConnection.CreateFile(fileName);
                }
                catch (Exception e)
                {
                    if (logger.IsLevelEnabled(Level.Error)) logger.Log(Level.Error, e.Message);
                }
            }
            CreateConnection();
            CreateDatabase();
        }

        private void CreateConnection()
        {
            try
            {
                dbConnection = new SQLiteConnection("Data Source=" + fileName + ";Version=3;");
                dbConnection.Open();
            }
            catch (Exception e)
            {
                if (logger.IsLevelEnabled(Level.Error)) logger.Log(Level.Error, e.Message);
            }
        }

        private void CreateDatabase()
        {
            try
            {
                // table where we will keep scheduled commands
                String commandsTable = "create table if not exists commands " +
                    "(" +
                    "id integer primary key autoincrement, " +
                    "data text not null, " +
                    "retries integer not null default 0" +
                    ")";
                SQLiteCommand command = new SQLiteCommand(commandsTable, dbConnection);
                command.ExecuteNonQuery();

                // table to keep identity
                String identityTable = "create table if not exists identity " +
                    "(" +
                    "id text not null primary key, " +
                    "value text not null" +
                    ")";
                command = new SQLiteCommand(identityTable, dbConnection);
                command.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                if (logger.IsLevelEnabled(Level.Error)) logger.Log(Level.Error, e.Message);
            }
        }

        public bool AddCommand(string serializedCommand)
        {
            lock (synchronizer)
            {
                try
                {
                    SQLiteCommand sqlCommand = new SQLiteCommand("insert into commands (data) values (@data)", dbConnection);
                    sqlCommand.Parameters.AddWithValue("@data", serializedCommand);
                    sqlCommand.ExecuteNonQuery();
                    return true;
                }
                catch (Exception e)
                {
                    if (logger.IsLevelEnabled(Level.Error)) logger.Log(Level.Error, e.Message);
                    return false;
                }
            }
        }

        public void SetIdentifiers(Dictionary<string, string> customerIds)
        {
            lock (synchronizer)
            {
                foreach (KeyValuePair<string, string> pair in customerIds)
                {
                    SetIdentifierUnsafe(pair.Key, pair.Value);
                }
            }
        }

        public void SetIdentifier(string id, string idValue)
        {
            lock (synchronizer)
            {
                SetIdentifierUnsafe(id, idValue);
            }
        }

        private void SetIdentifierUnsafe(string id, string idValue)
        {
            try
            {
                SQLiteCommand sqlCommand = new SQLiteCommand("insert or replace into identity (id, value) values (@id, @value)", dbConnection);
                sqlCommand.Parameters.AddWithValue("@id", id);
                sqlCommand.Parameters.AddWithValue("@value", idValue);
                sqlCommand.ExecuteNonQuery();

            }
            catch (Exception e)
            {
                if (logger.IsLevelEnabled(Level.Error)) logger.Log(Level.Error, e.Message);
            }
        }

        public Dictionary<string, string> GetIdentifiers()
        {
            lock (synchronizer)
            {
                Dictionary<string, string> result = new Dictionary<string, string>();
                SQLiteDataReader reader = null;
                try
                {
                    SQLiteCommand sqlCommand = new SQLiteCommand("select * from identity", dbConnection);
                    reader = sqlCommand.ExecuteReader();
                    while (reader.Read())
                    {
                        result[(string) reader["id"]] = (string) reader["value"];
                    }
                }
                catch (Exception e)
                {
                    if (logger.IsLevelEnabled(Level.Error)) logger.Log(Level.Error, e.Message);
                }
                finally
                {
                    if (reader != null) reader.Close();
                }
                return result;
            }
        }

        public void RemoveIdentifiers()
        {
            lock (synchronizer)
            {
                try
                {
                    SQLiteCommand command = new SQLiteCommand("delete from identity", dbConnection);
                    command.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    if (logger.IsLevelEnabled(Level.Error)) logger.Log(Level.Error, e.Message);
                }
            }
        }

        public List<CommandRequest> ReadFirst()
        {
            return ReadFirst(Constants.BULK_LIMIT);
        }

        public List<CommandRequest> ReadFirst(int limit)
        {
            lock (synchronizer)
            {
                List<CommandRequest> result = new List<CommandRequest>();
                SQLiteDataReader reader = null;
                try
                {
                    SQLiteCommand sqlCommand = new SQLiteCommand("select * from commands limit @limit", dbConnection);
                    sqlCommand.Parameters.AddWithValue("@limit", Constants.BULK_LIMIT);
                    reader = sqlCommand.ExecuteReader();
                    while (reader.Read())
                    {
                        result.Add(new CommandRequest(
                            Convert.ToInt32(reader["id"]),
                            Json.Deserialize((string) reader["data"]) as Dictionary<string, object>,
                            Convert.ToInt32(reader["retries"])
                        ));
                    }
                }
                catch (Exception e)
                {
                    if (logger.IsLevelEnabled(Level.Error)) logger.Log(Level.Error, e.Message);
                }
                finally
                {
                    if (reader != null) reader.Close();
                }
                return result;
            }
        }

        public void RemoveCommands(List<int> ids)
        {
            lock (synchronizer)
            {
                if (ids.Count == 0) return;
                try
                {
                    ExecuteListParametrizedQuery("delete from commands where id in ({0})", ids);
                }
                catch (Exception e)
                {
                    if (logger.IsLevelEnabled(Level.Error)) logger.Log(Level.Error, e.Message);
                }
            }
        }

        public void IncrementRetries(List<int> ids)
        {
            lock (synchronizer)
            {
                try
                {
                    ExecuteListParametrizedQuery("update commands set retries = retries + 1 where id in ({0})", ids);
                    SQLiteCommand command = new SQLiteCommand("delete from commands where retries > @max", dbConnection);
                    command.Parameters.AddWithValue("@max", Constants.BULK_MAX_RETRIES);
                    command.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    if (logger.IsLevelEnabled(Level.Error)) logger.Log(Level.Error, e.Message);
                }
            }
        }

        private void ExecuteListParametrizedQuery(string sqlTemplate, List<int> values)
        {
            SQLiteCommand command = new SQLiteCommand();
            string[] parameters = new string[values.Count];

            for (int i = 0; i < values.Count; i++)
            {
                parameters[i] = string.Format("@p{0}", i);
                command.Parameters.AddWithValue(parameters[i], values[i]);
            }

            command.CommandText = string.Format(sqlTemplate, string.Join(", ", parameters));
            command.Connection = dbConnection;
            command.ExecuteNonQuery();
        }
    }
}
