using System;
using System.Collections.Generic;
using Zenject;
using SongCore;

namespace BeatSaberHitDataStorage.Managers
{
    internal class PlayDataManager : IInitializable
    {
        private IDifficultyBeatmap _difficultyBeatmap;
        private DatabaseManager _dbManager;

        private long _beatmapRowID = -1;
        private long _playRowID = -1;

        private List<(string, object)> _columnValues = new List<(string, object)>(DatabaseSchemas.TableSchemas[DatabaseSchemas.BeatmapsTableName].Count);

        [Inject]
        public PlayDataManager(IDifficultyBeatmap difficultyBeatmap, DatabaseManager databaseManager)
        {
            _difficultyBeatmap = difficultyBeatmap;
            _dbManager = databaseManager;
        }

        public void Initialize()
        {
            _beatmapRowID = PrepareDatabaseBeatmapEntry();
            _playRowID = PrepareDatabasePlaysEntry();
        }

        private long PrepareDatabaseBeatmapEntry()
        {
            string hash = Collections.hashForLevelID(_difficultyBeatmap.level.levelID);
            string characteristic = _difficultyBeatmap.parentDifficultyBeatmapSet.beatmapCharacteristic.serializedName;
            string difficulty = _difficultyBeatmap.difficulty.SerializedName();

            _columnValues.Clear();
            _columnValues.Add(("level_hash", hash));
            _columnValues.Add(("characteristic", characteristic));
            _columnValues.Add(("difficulty", difficulty));

            long id = _dbManager.FindEntryID(DatabaseSchemas.BeatmapsTableName, _columnValues);

            if (id < 0)
            {
                _columnValues.Add(("song_name", _difficultyBeatmap.level.songName));
                _columnValues.Add(("song_author_name", _difficultyBeatmap.level.songAuthorName));
                _columnValues.Add(("level_author_name", _difficultyBeatmap.level.levelAuthorName));
                _columnValues.Add(("length", _difficultyBeatmap.level.songDuration));
                _columnValues.Add(("note_count", _difficultyBeatmap.beatmapData.cuttableNotesType));

                id = _dbManager.InsertEntry(DatabaseSchemas.BeatmapsTableName, _columnValues);
            }

            return id;
        }

        private long PrepareDatabasePlaysEntry()
        {
            _columnValues.Clear();
            _columnValues.Add(("beatmap_id", _beatmapRowID));
            _columnValues.Add(("play_datetime", DateTime.Now));

            return _dbManager.InsertEntry(DatabaseSchemas.PlaysTableName, _columnValues);
        }

        public long RecordNoteHitData(float time, int validHit, int isMiss, int beforeCutScore, int afterCutScore, int accuracyScore, float timeDeviation, float directionDeviation)
        {
            _columnValues.Clear();
            _columnValues.Add(("play_id", _playRowID));
            _columnValues.Add(("time", time));
            _columnValues.Add(("valid_hit", validHit));
            _columnValues.Add(("is_miss", isMiss));
            _columnValues.Add(("before_cut_score", beforeCutScore));
            _columnValues.Add(("after_cut_score", afterCutScore));
            _columnValues.Add(("accuracy_score", accuracyScore));

            long hitID = _dbManager.InsertEntry(DatabaseSchemas.NoteHitsTableName, _columnValues);

            if (PluginConfig.Instance.RecordDeviations)
            {
                _columnValues.Clear();
                _columnValues.Add(("hit_id", hitID));
                _columnValues.Add(("time_deviation", timeDeviation));
                _columnValues.Add(("dir_deviation", directionDeviation));

                _dbManager.InsertEntry(DatabaseSchemas.HitDeviationsTableName, _columnValues);
            }

            return hitID;
        }

        public long RecordBombHitData(float time)
        {
            _columnValues.Clear();
            _columnValues.Add(("play_id", _playRowID));
            _columnValues.Add(("time", time));

            return _dbManager.InsertEntry(DatabaseSchemas.BombHitsTableName, _columnValues);
        }
    }
}
