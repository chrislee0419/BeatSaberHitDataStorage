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

        public static readonly Dictionary<string, Dictionary<string, string>> TableSchemas = new Dictionary<string, Dictionary<string, string>>
        {
            { BeatmapsTableName, new Dictionary<string, string>
                {
                    { "level_hash", "TEXT" },
                    { "song_name", "TEXT" },
                    { "song_author_name", "TEXT" },
                    { "level_author_name", "TEXT" },
                    { "length", "REAL" },
                    { "characteristic", "TEXT" },
                    { "difficulty", "TEXT" },
                    { "note_count", "INT" }
                }
            },

            { PlaysTableName, new Dictionary<string, string>
                {
                    { "beatmap_id", "INT" },
                    { "play_datetime", "TEXT" }
                }
            },

            { NoteHitsTableName, new Dictionary<string, string>
                {
                    { "play_id", "INT" },
                    { "time", "REAL" },
                    { "valid_hit", "INT" },
                    { "is_miss", "INT" },
                    { "before_cut_score", "INT" },
                    { "after_cut_score", "INT" },
                    { "accuracy_score", "INT" },
                    { "time_deviation", "REAL" },
                    { "dir_deviation", "REAL" }
                }
            },
            { BombHitsTableName, new Dictionary<string, string>
                {
                    { "play_id", "INT" },
                    { "time", "REAL" }
                }
            }
        };

        private static readonly Dictionary<string, (string, string)> ForeignKeyConstraints = new Dictionary<string, (string, string)>
        {
            { PlaysTableName, ("beatmap_id", BeatmapsTableName) },
            { NoteHitsTableName, ("play_id", PlaysTableName) },
            { BombHitsTableName, ("play_id", PlaysTableName) }
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

            foreach ((string columnName, string type) in TableSchemas[tableName])
            {
                sb.Append(", ");
                sb.Append(columnName);
                sb.Append(" ");
                sb.Append(type);
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
