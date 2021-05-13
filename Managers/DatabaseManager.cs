﻿using System;
using System.IO;
using System.Data.SQLite;
using Zenject;
using SongCore;

namespace BeatSaberHitDataStorage.Managers
{
    internal class DatabaseManager : IInitializable, IDisposable
    {
        private IDifficultyBeatmap _difficultyBeatmap;

        private SQLiteConnection _dbConnection;
        private SQLiteTransaction _dbTransaction;
        private SQLiteCommand _dbCommand;

        private long _beatmapRowID = -1;
        private long _playRowID = -1;
        private byte _actionCount = 0;
        private bool _errorOccurred = false;

        private const string BeatmapsTableName = "beatmaps";
        private const string PlaysTableName = "plays";
        private const string NoteHitsTableName = "note_hits";
        private const string BombHitsTableName = "bomb_hits";

        private const byte MaxActionsPerTransaction = 100;

        [Inject]
        public DatabaseManager(IDifficultyBeatmap difficultyBeatmap)
        {
            _difficultyBeatmap = difficultyBeatmap;
        }

        public void Initialize()
        {
            string dbFileLocation = Path.Combine(Environment.CurrentDirectory, "UserData", "HitDatabase.sqlite");
            Plugin.Log.Info($"Opening database located at: {dbFileLocation}");

            _dbConnection = new SQLiteConnection($"Data Source={dbFileLocation}");
            _dbConnection.Open();

            using (var dbCommand = _dbConnection.CreateCommand())
            {
                PrepareDatabaseTables(dbCommand);
                _beatmapRowID = PrepareDatabaseBeatmapEntry(dbCommand);
                _playRowID = PrepareDatabasePlaysEntry(dbCommand);
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

        public void RecordNoteHitData(float time, int validHit, int isMiss, int beforeCutScore, int afterCutScore, int accuracyScore, float timeDeviation, float directionDeviation)
        {
            if (_beatmapRowID < 0 || _playRowID < 0)
            {
                if (!_errorOccurred)
                    Plugin.Log.Debug("Unable to store note hit data to SQLite database");

                _errorOccurred = true;
                return;
            }

            PrepareAndCheckTransaction();

            _dbCommand.Parameters.Clear();

            _dbCommand.CommandText = $@"
INSERT INTO {NoteHitsTableName}
VALUES(NULL, @play_id, @time, @valid, @miss, @before, @after, @accuracy, @time_dev, @dir_dev)";

            _dbCommand.Parameters.AddWithValue("@play_id", _playRowID);
            _dbCommand.Parameters.AddWithValue("@time", time);
            _dbCommand.Parameters.AddWithValue("@valid", validHit);
            _dbCommand.Parameters.AddWithValue("@miss", isMiss);
            _dbCommand.Parameters.AddWithValue("@before", beforeCutScore);
            _dbCommand.Parameters.AddWithValue("@after", afterCutScore);
            _dbCommand.Parameters.AddWithValue("@accuracy", accuracyScore);
            _dbCommand.Parameters.AddWithValue("@time_dev", timeDeviation);
            _dbCommand.Parameters.AddWithValue("@dir_dev", directionDeviation);
            _dbCommand.Prepare();

            _dbCommand.ExecuteNonQuery();
        }

        public void RecordBombHitData(float time)
        {
            if (_beatmapRowID < 0 || _playRowID < 0)
            {
                if (!_errorOccurred)
                    Plugin.Log.Debug("Unable to store bomb hit data to SQLite database");

                _errorOccurred = true;
                return;
            }

            PrepareAndCheckTransaction();

            _dbCommand.Parameters.Clear();

            _dbCommand.CommandText = $@"
INSERT INTO {BombHitsTableName}
VALUES(NULL, @play_id, @time)";

            _dbCommand.Parameters.AddWithValue("@play_id", _playRowID);
            _dbCommand.Parameters.AddWithValue("@time", time);
            _dbCommand.Prepare();

            _dbCommand.ExecuteNonQuery();
        }

        private void PrepareDatabaseTables(SQLiteCommand dbCommand)
        {
            if (CheckDatabaseTableExists(dbCommand, BeatmapsTableName))
            {
                dbCommand.CommandText = $@"
CREATE TABLE {BeatmapsTableName}(id INTEGER PRIMARY KEY,
                                 level_hash TEXT,
                                 song_name TEXT,
                                 song_author_name TEXT,
                                 level_author_name TEXT,
                                 length REAL,
                                 characteristic TEXT,
                                 difficulty TEXT,
                                 note_count INT)";

                dbCommand.ExecuteNonQuery();
            }

            if (CheckDatabaseTableExists(dbCommand, PlaysTableName))
            {
                dbCommand.CommandText = $@"
CREATE TABLE {PlaysTableName}(id INTEGER PRIMARY KEY,
                              beatmap_id INT,
                              play_datetime TEXT,
                              FOREIGN KEY(beatmap_id) REFERENCES {BeatmapsTableName}(id))";

                dbCommand.ExecuteNonQuery();
            }

            if (CheckDatabaseTableExists(dbCommand, NoteHitsTableName))
            {
                dbCommand.CommandText = $@"
CREATE TABLE {NoteHitsTableName}(id INTEGER PRIMARY KEY,
                                 play_id INT,
                                 time REAL,
                                 valid_hit INT,
                                 is_miss INT,
                                 before_cut_score INT,
                                 after_cut_score INT,
                                 accuracy_score INT,
                                 time_deviation REAL,
                                 dir_deviation REAL,
                                 FOREIGN KEY(play_id) REFERENCES {PlaysTableName}(id))";

                dbCommand.ExecuteNonQuery();
            }

            if (CheckDatabaseTableExists(dbCommand, BombHitsTableName))
            {
                dbCommand.CommandText = $@"
CREATE TABLE {BombHitsTableName}(id INTEGER PRIMARY KEY,
                                 play_id INT,
                                 time REAL,
                                 FOREIGN KEY(play_id) REFERENCES {PlaysTableName}(id))";

                dbCommand.ExecuteNonQuery();
            }
        }

        private long PrepareDatabaseBeatmapEntry(SQLiteCommand dbCommand)
        {
            dbCommand.CommandText = $@"
SELECT id
FROM {BeatmapsTableName}
WHERE level_hash=@hash AND
      characteristic=@characteristic AND
      difficulty=@difficulty";

            dbCommand.Parameters.AddWithValue("@hash", Collections.hashForLevelID(_difficultyBeatmap.level.levelID));
            dbCommand.Parameters.AddWithValue("@characteristic", _difficultyBeatmap.parentDifficultyBeatmapSet.beatmapCharacteristic.serializedName);
            dbCommand.Parameters.AddWithValue("@difficulty", _difficultyBeatmap.difficulty.SerializedName());
            dbCommand.Prepare();

            var result = dbCommand.ExecuteScalar();

            if (result == null)
            {
                dbCommand.CommandText = $@"
INSERT INTO {BeatmapsTableName}
VALUES(NULL, @hash, @song_name, @song_author, @level_author, @length, @characteristic, @difficulty, @note_count)";

                dbCommand.Parameters.AddWithValue("@song_name", _difficultyBeatmap.level.songName);
                dbCommand.Parameters.AddWithValue("@song_author", _difficultyBeatmap.level.songAuthorName);
                dbCommand.Parameters.AddWithValue("@level_author", _difficultyBeatmap.level.levelAuthorName);
                dbCommand.Parameters.AddWithValue("@length", _difficultyBeatmap.level.songDuration);
                dbCommand.Parameters.AddWithValue("@note_count", _difficultyBeatmap.beatmapData.cuttableNotesType);
                dbCommand.Prepare();

                dbCommand.ExecuteNonQuery();

                dbCommand.Parameters.Clear();

                dbCommand.CommandText = "SELECT last_insert_rowid()";
                return (long)dbCommand.ExecuteScalar();

            }
            else
            {
                dbCommand.Parameters.Clear();
                return (long)result;
            }
        }

        private long PrepareDatabasePlaysEntry(SQLiteCommand dbCommand)
        {
            dbCommand.CommandText = $@"
INSERT INTO {PlaysTableName}
VALUES(NULL, @beatmap_id, @play_datetime)";

            dbCommand.Parameters.AddWithValue("@beatmap_id", _beatmapRowID);
            dbCommand.Parameters.AddWithValue("@play_datetime", DateTime.Now);
            dbCommand.Prepare();

            dbCommand.ExecuteNonQuery();

            dbCommand.Parameters.Clear();

            dbCommand.CommandText = "SELECT last_insert_rowid()";
            return (long)dbCommand.ExecuteScalar();
        }

        private void PrepareAndCheckTransaction()
        {
            if (_dbTransaction == null)
            {
                _dbTransaction = _dbConnection.BeginTransaction();
                _dbCommand = _dbConnection.CreateCommand();
            }
            else if (++_actionCount > MaxActionsPerTransaction)
            {
                _actionCount = 0;

                if (_dbCommand != null)
                    _dbCommand.Dispose();

                _dbTransaction.Commit();
                _dbTransaction.Dispose();

                _dbTransaction = _dbConnection.BeginTransaction();
                _dbCommand = _dbConnection.CreateCommand();
            }
        }

        private bool CheckDatabaseTableExists(SQLiteCommand dbCommand, string tableName)
        {
            dbCommand.CommandText = $"SELECT COUNT(name) FROM sqlite_master WHERE type='table' AND name='{tableName}'";
            return (long)dbCommand.ExecuteScalar() == 0;
        }
    }
}