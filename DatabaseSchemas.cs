using System.Collections.Generic;
using System.Text;

namespace BeatSaberHitDataStorage
{
    internal static class DatabaseSchemas
    {
        public const string BeatmapsTableName = "beatmaps";
        public const string ModifiersTableName = "modifiers";
        public const string PlaysTableName = "plays";
        public const string PlayModifiersTableName = "play_modifiers";
        public const string NoteInfosTableName = "note_infos";
        public const string NoteHitsTableName = "note_hits";
        public const string BombHitsTableName = "bomb_hits";
        public const string HitDeviationsTableName = "hit_deviations";

        public static readonly Dictionary<string, List<(string, string)>> TableSchemas = new Dictionary<string, List<(string, string)>>
        {
            { BeatmapsTableName, new List<(string, string)>
                {
                    ( "level_hash", "TEXT" ),
                    ( "song_name", "TEXT" ),
                    ( "song_author_name", "TEXT" ),
                    ( "level_author_name", "TEXT" ),
                    ( "length", "REAL" ),
                    ( "characteristic", "TEXT" ),
                    ( "difficulty", "TEXT" ),
                    ( "note_count", "INT" )
                }
            },

            { ModifiersTableName, new List<(string, string)>
                {
                    ( "modifier_name", "TEXT" )
                }
            },

            { PlaysTableName, new List<(string, string)>
                {
                    ( "beatmap_id", "INT" ),
                    ( "play_datetime", "TEXT" ),
                    ( "is_practice", "INT" ),
                    ( "completed", "INT" )
                }
            },

            { PlayModifiersTableName, new List<(string, string)>
                {
                    ( "play_id", "INT" ),
                    ( "modifier_id", "INT" )
                }
            },

            { NoteInfosTableName, new List<(string, string)>
                {
                    ( "is_right_hand", "INT" ),
                    ( "note_direction", "TEXT" ),
                    ( "line_index", "INT" ),
                    ( "line_layer", "INT" )
                }
            },

            { NoteHitsTableName, new List<(string, string)>
                {
                    ( "play_id", "INT" ),
                    ( "time", "REAL" ),
                    ( "valid_hit", "INT" ),
                    ( "is_miss", "INT" ),
                    ( "is_right_hand", "INT" ),
                    ( "note_info_id", "TEXT" ),
                    ( "before_cut_score", "INT" ),
                    ( "after_cut_score", "INT" ),
                    ( "accuracy_score", "INT" )
                }
            },
            { BombHitsTableName, new List<(string, string)>
                {
                    ( "play_id", "INT" ),
                    ( "time", "REAL" )
                }
            },

            { HitDeviationsTableName, new List<(string, string)>
                {
                    ( "hit_id", "INT" ),
                    ( "time_deviation", "REAL" ),
                    ( "dir_deviation", "REAL" )
                }
            }
        };

        private static readonly Dictionary<(string, string), string> ReferenceConstraints = new Dictionary<(string, string), string>
        {
            { (PlaysTableName, "beatmap_id"), BeatmapsTableName },
            { (PlayModifiersTableName, "play_id"), PlaysTableName },
            { (PlayModifiersTableName, "modifier_id"), ModifiersTableName },
            { (NoteHitsTableName, "play_id"), PlaysTableName },
            { (NoteHitsTableName, "note_info_id"), NoteInfosTableName },
            { (BombHitsTableName, "play_id"), PlaysTableName },
            { (HitDeviationsTableName, "hit_id"), NoteHitsTableName }
        };

        private static readonly Dictionary<string, string[]> UniqueConstraints = new Dictionary<string, string[]>
        {
            { ModifiersTableName, new string[] { "modifier_name" } },
            { NoteInfosTableName, new string[] { "is_right_hand", "note_direction", "line_index", "line_layer" } }
        };

        public static string BuildCreateTableStatement(string tableName)
        {
            StringBuilder sb = new StringBuilder("CREATE TABLE ");
            sb.Append(tableName);
            sb.Append("(id INTEGER PRIMARY KEY");

            foreach ((string columnName, string type) in TableSchemas[tableName])
            {
                sb.Append(",");
                sb.Append(columnName);
                sb.Append(" ");
                sb.Append(type);

                if (ReferenceConstraints.TryGetValue((tableName, columnName), out string parentTableName))
                {
                    sb.Append(" REFERENCES ");
                    sb.Append(parentTableName);
                    sb.Append("(id)");
                }
            }

            if (UniqueConstraints.TryGetValue(tableName, out var uniqueColumns))
            {
                sb.Append(",UNIQUE(");

                foreach (var uniqueColumn in uniqueColumns)
                {
                    sb.Append(uniqueColumn);
                    sb.Append(',');
                }

                // remove trailing comma
                sb.Remove(sb.Length - 1, 1);
                sb.Append("))");
            }
            else
            {
                sb.Append(')');
            }

            return sb.ToString();
        }
    }
}
