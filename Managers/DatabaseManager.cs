using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Text;
using Zenject;

namespace BeatSaberHitDataStorage.Managers
{
    internal class DatabaseManager : IInitializable, IDisposable
    {
        private SQLiteConnection _dbConnection;
        private SQLiteTransaction _dbTransaction;
        private SQLiteCommand _dbCommand;

        private byte _writeCount = 0;

        private const byte MaxWritesPerTransaction = 100;

        public void Initialize()
        {
            string dbFileLocation = Path.Combine(Environment.CurrentDirectory, "UserData", "HitDatabase.sqlite");
            Plugin.Log.Debug($"Opening database located at: {dbFileLocation}");

            _dbConnection = new SQLiteConnection($"Data Source={dbFileLocation}");
            _dbConnection.Open();

            // prepare database tables
            using (var dbCommand = _dbConnection.CreateCommand())
            {
                foreach (string tableName in DatabaseSchemas.TableSchemas.Keys)
                {
                    if (!CheckDatabaseTableExists(dbCommand, tableName))
                    {
                        dbCommand.CommandText = DatabaseSchemas.CreateTableStatements[tableName];
                        dbCommand.ExecuteNonQuery();
                    }
                }
            }
        }

        public void Dispose()
        {
            if (_dbCommand != null)
                _dbCommand.Dispose();

            if (_dbTransaction != null)
            {
                _dbTransaction.Commit();
                _dbTransaction.Dispose();
            }

            _dbConnection.Dispose();
        }

        public long FindEntryID(string tableName, IEnumerable<(string, object)> identifyingValues)
        {
            PrepareAndCheckTransaction(false);

            StringBuilder sb = new StringBuilder("SELECT id FROM ");
            sb.Append(tableName);
            sb.Append(" WHERE ");

            _dbCommand.Parameters.Clear();
            foreach ((string columnName, object value) in identifyingValues)
            {
                sb.Append(columnName);
                sb.Append("=@");
                sb.Append(columnName);
                sb.Append(" AND ");

                _dbCommand.Parameters.AddWithValue($"@{columnName}", value);
            }

            // remove trailing ' AND'
            sb.Remove(sb.Length - 5, 5);

            _dbCommand.CommandText = sb.ToString();
            _dbCommand.Prepare();

            object result = _dbCommand.ExecuteScalar();
            return result != null ? (long)result : -1L;
        }

        public long InsertEntry(string tableName, IEnumerable<(string, object)> values)
        {
            PrepareAndCheckTransaction();

            StringBuilder sb = new StringBuilder("INSERT INTO ");
            sb.Append(tableName);
            sb.Append("(id");
            foreach ((string columnName, object value) in values)
            {
                sb.Append(',');
                sb.Append(columnName);
            }

            sb.Append(") VALUES(NULL");

            _dbCommand.Parameters.Clear();
            foreach ((string columnName, object value) in values)
            {
                sb.Append(",@");
                sb.Append(columnName);

                _dbCommand.Parameters.AddWithValue($"@{columnName}", value);
            }

            sb.Append(')');

            _dbCommand.CommandText = sb.ToString();
            _dbCommand.Prepare();

            _dbCommand.ExecuteNonQuery();

            _dbCommand.CommandText = "SELECT last_insert_rowid()";
            return (long)_dbCommand.ExecuteScalar();
        }

        public void UpdateEntry(string tableName, long entryID, IEnumerable<(string, object)> updateValues)
        {
            PrepareAndCheckTransaction();

            StringBuilder sb = new StringBuilder("UPDATE ");
            sb.Append(tableName);
            sb.Append(" SET ");

            _dbCommand.Parameters.Clear();
            foreach ((string columnName, object value) in updateValues)
            {
                sb.Append(columnName);
                sb.Append("=@");
                sb.Append(columnName);
                sb.Append(',');

                _dbCommand.Parameters.AddWithValue($"@{columnName}", value);
            }

            // remove trailing comma
            sb.Remove(sb.Length - 1, 1);

            sb.Append(" WHERE id=");
            sb.Append(entryID);

            _dbCommand.CommandText = sb.ToString();
            _dbCommand.Prepare();

            _dbCommand.ExecuteNonQuery();
        }

        private bool CheckDatabaseTableExists(SQLiteCommand dbCommand, string tableName)
        {
            dbCommand.CommandText = $"SELECT COUNT(name) FROM sqlite_master WHERE type='table' AND name='{tableName}'";
            return (long)dbCommand.ExecuteScalar() > 0;
        }

        private void PrepareAndCheckTransaction(bool isWriteAction = true)
        {
            if (_dbTransaction == null)
            {
                _dbTransaction = _dbConnection.BeginTransaction();
                _dbCommand = _dbConnection.CreateCommand();
            }

            if (isWriteAction && ++_writeCount > MaxWritesPerTransaction)
            {
                _writeCount = 0;

                if (_dbCommand != null)
                    _dbCommand.Dispose();

                _dbTransaction.Commit();
                _dbTransaction.Dispose();

                _dbTransaction = _dbConnection.BeginTransaction();
                _dbCommand = _dbConnection.CreateCommand();
            }
        }
    }
}
