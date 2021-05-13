using System;
using System.Collections.Generic;
using System.Text;

namespace BeatSaberHitDataStorage
{
    internal static class DatabaseSchemas
    {
        public const string BeatmapsTableName = "beatmaps";
        public const string PlaysTableName = "plays";
        public const string NoteHitsTableName = "note_hits";
        public const string BombHitsTableName = "bomb_hits";

        public static readonly Dictionary<string, string> CreateTableStatements;

        public static readonly Dictionary<string, Dictionary<string, Type>> TableSchemas = new Dictionary<string, Dictionary<string, Type>>
        {
            { BeatmapsTableName, new Dictionary<string, Type>
                {
                    { "level_hash", typeof(string) },
                    { "song_name", typeof(string) },
                    { "song_author_name", typeof(string) },
                    { "level_author_name", typeof(string) },
                    { "length", typeof(float) },
                    { "characteristic", typeof(string) },
                    { "difficulty", typeof(string) },
                    { "note_count", typeof(int) }
                }
            },
            { PlaysTableName, new Dictionary<string, Type>
                {
                    { "beatmap_id", typeof(int) },
                    { "play_datetime", typeof(DateTime) }
                }
            },
            { NoteHitsTableName, new Dictionary<string, Type>
                {
                    { "play_id", typeof(int) },
                    { "time", typeof(float) },
                    { "valid_hit", typeof(int) },
                    { "is_miss", typeof(int) },
                    { "before_cut_score", typeof(int) },
                    { "after_cut_score", typeof(int) },
                    { "accuracy_score", typeof(int) },
                    { "time_deviation", typeof(float) },
                    { "dir_deviation", typeof(float) }
                }
            },
            { BombHitsTableName, new Dictionary<string, Type>
                {
                    { "play_id", typeof(int) },
                    { "time", typeof(float) }
                }
            }
        };

        public static readonly Dictionary<string, (string, string)> ForeignKeyConstraints = new Dictionary<string, (string, string)>
        {
            { PlaysTableName, ("beatmap_id", BeatmapsTableName) },
            { NoteHitsTableName, ("play_id", PlaysTableName) },
            { BombHitsTableName, ("play_id", PlaysTableName) }
        };

        private static readonly Dictionary<Type, string> TypeMapping = new Dictionary<Type, string>
        {
            { typeof(string), "TEXT" },
            { typeof(int), "INTEGER" },
            { typeof(float), "REAL" },
            { typeof(DateTime), "TEXT" }
        };

        static DatabaseSchemas()
        {
            CreateTableStatements = new Dictionary<string, string>(TableSchemas.Count);

            foreach (string tableName in TableSchemas.Keys)
                CreateTableStatements.Add(tableName, BuildCreateTableStatement(tableName));
        }

        private static string BuildCreateTableStatement(string tableName)
        {
            StringBuilder sb = new StringBuilder("CREATE TABLE ");
            sb.Append(tableName);
            sb.Append("(id INTEGER PRIMARY KEY");

            foreach ((string columnName, Type type) in TableSchemas[tableName])
            {
                sb.Append(", ");
                sb.Append(columnName);
                sb.Append(" ");
                sb.Append(TypeMapping[type]);
            }

            if (ForeignKeyConstraints.TryGetValue(tableName, out (string keyColumnName, string parentTableName) foreignKey))
            {
                sb.Append(", FOREIGN KEY(");
                sb.Append(foreignKey.keyColumnName);
                sb.Append(") REFERENCES ");
                sb.Append(foreignKey.parentTableName);
                sb.Append("(id))");
            }
            else
            {
                sb.Append(')');
            }

            return sb.ToString();
        }
    }
}
