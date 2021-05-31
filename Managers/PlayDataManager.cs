using System;
using System.Collections.Generic;
using Zenject;
using SongCore;
using BeatSaberHitDataStorage.Utilities;

namespace BeatSaberHitDataStorage.Managers
{
    internal class PlayDataManager : IInitializable, IDisposable
    {
        private GameplayCoreSceneSetupData _gameplayCoreSceneSetupData;
        private ILevelEndActions _levelEndActions;
        private IDifficultyBeatmap _difficultyBeatmap;

        private DatabaseManager _dbManager;

        private long _beatmapRowID = -1;
        private long _playRowID = -1;

        private List<(string, object)> _columnValues = new List<(string, object)>(DatabaseSchemas.TableSchemas[DatabaseSchemas.NoteHitsTableName].Count);

        private static Dictionary<(int, string, int, int), long> _noteInfosMapping = null;
        private static Dictionary<string, long> _gameplayModifiersMapping = null;

        [Inject]
        public PlayDataManager(
            GameplayCoreSceneSetupData gameplayCoreSceneSetupData,
            ILevelEndActions levelEndActions,
            IDifficultyBeatmap difficultyBeatmap,
            DatabaseManager databaseManager)
        {
            _gameplayCoreSceneSetupData = gameplayCoreSceneSetupData;
            _levelEndActions = levelEndActions;
            _difficultyBeatmap = difficultyBeatmap;
            _dbManager = databaseManager;
        }

        public void Initialize()
        {
            PrepareNoteInfosTable();
            PrepareModifiersTable();

            _beatmapRowID = PrepareDatabaseBeatmapEntry();
            _playRowID = PrepareDatabasePlaysEntry();
            PreparePlayModifiersEntries();

            _levelEndActions.levelFinishedEvent += OnLevelFinished;
            _levelEndActions.levelFailedEvent += OnLevelFailed;
        }

        public void Dispose()
        {
            if (_levelEndActions != null)
            {
                _levelEndActions.levelFinishedEvent -= OnLevelFinished;
                _levelEndActions.levelFailedEvent -= OnLevelFailed;
            }
        }

        public long RecordNoteHitData(float time, int validHit, int isMiss, int isRightHand, string noteDirection, int lineIndex, int lineLayer, int beforeCutScore, int afterCutScore, int accuracyScore, float timeDeviation, float directionDeviation)
        {
            long noteInfoID;
            if (!_noteInfosMapping.TryGetValue((isRightHand, noteDirection, lineIndex, lineLayer), out noteInfoID))
            {
                _columnValues.Clear();
                _columnValues.Add(("is_right_hand", isRightHand));
                _columnValues.Add(("note_direction", noteDirection));
                _columnValues.Add(("line_index", lineIndex));
                _columnValues.Add(("line_layer", lineLayer));

                noteInfoID = _dbManager.InsertEntry(DatabaseSchemas.NoteInfosTableName, _columnValues);
                _noteInfosMapping.Add((isRightHand, noteDirection, lineIndex, lineLayer), noteInfoID);
            }

            _columnValues.Clear();
            _columnValues.Add(("play_id", _playRowID));
            _columnValues.Add(("note_info_id", noteInfoID));
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

        private void PrepareNoteInfosTable()
        {
            if (_noteInfosMapping == null)
            {
                _noteInfosMapping = new Dictionary<(int, string, int, int), long>();

                // check database for existing mappings
                var noteInfos = _dbManager.GetRowsFromTable(DatabaseSchemas.NoteInfosTableName);
                foreach (var noteInfo in noteInfos)
                {
                    var tuple = (
                        (int)noteInfo["is_right_hand"],
                        (string)noteInfo["note_direction"],
                        (int)noteInfo["line_index"],
                        (int)noteInfo["line_layer"]);

                    _noteInfosMapping[tuple] = (long)noteInfo["id"];
                }
            }

            // add info from all notes in the map if necessary
            foreach (var lineData in _difficultyBeatmap.beatmapData.beatmapLinesData)
            {
                foreach (var objData in lineData.beatmapObjectsData)
                {
                    if (objData.beatmapObjectType != BeatmapObjectType.Note)
                        continue;

                    var noteData = (NoteData)objData;
                    var tuple = (IsRightHandedNote(noteData), GetNoteCutDirectionString(noteData), noteData.lineIndex, (int)noteData.noteLineLayer);

                    if (_noteInfosMapping.ContainsKey(tuple))
                        continue;

                    _columnValues.Clear();
                    _columnValues.Add(("is_right_hand", tuple.Item1));
                    _columnValues.Add(("note_direction", tuple.Item2));
                    _columnValues.Add(("line_index", tuple.Item3));
                    _columnValues.Add(("line_layer", tuple.Item4));

                    _noteInfosMapping[tuple] = _dbManager.InsertEntry(DatabaseSchemas.NoteInfosTableName, _columnValues);
                }
            }
        }

        private long PrepareDatabaseBeatmapEntry()
        {
            string hash = Collections.hashForLevelID(_difficultyBeatmap.level.levelID);
            string characteristic = _difficultyBeatmap.parentDifficultyBeatmapSet.beatmapCharacteristic.serializedName;
            string difficulty = _difficultyBeatmap.difficulty.SerializedName();

            if (string.IsNullOrWhiteSpace(hash))
                hash = _difficultyBeatmap.level.levelID;

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

        private void PrepareModifiersTable()
        {
            if (_gameplayModifiersMapping == null)
            {
                var modifiers = _dbManager.GetRowsFromTable(DatabaseSchemas.ModifiersTableName);
                var allModifiers = GameplayModifierHelperUtilities.AllModifiers;

                if (modifiers.Count < allModifiers.Count)
                {
                    _gameplayModifiersMapping = new Dictionary<string, long>(allModifiers.Count);

                    foreach (var modifier in GameplayModifierHelperUtilities.AllModifiers)
                    {
                        _columnValues.Clear();
                        _columnValues.Add(("modifier_name", modifier));
                        _gameplayModifiersMapping[modifier] = _dbManager.InsertEntry(DatabaseSchemas.ModifiersTableName, _columnValues, true);
                    }
                }
                else
                {
                    _gameplayModifiersMapping = new Dictionary<string, long>(modifiers.Count);

                    foreach (var row in modifiers)
                        _gameplayModifiersMapping[(string)row["modifier_name"]] = (long)row["id"];
                }
            }
        }

        private long PrepareDatabasePlaysEntry()
        {
            _columnValues.Clear();
            _columnValues.Add(("beatmap_id", _beatmapRowID));
            _columnValues.Add(("is_practice", _gameplayCoreSceneSetupData.practiceSettings != null ? 1 : 0));
            _columnValues.Add(("play_datetime", DateTime.Now));
            _columnValues.Add(("completed", 0));
            _columnValues.Add(("failed", 0));

            return _dbManager.InsertEntry(DatabaseSchemas.PlaysTableName, _columnValues);
        }

        private void PreparePlayModifiersEntries()
        {
            foreach (var modifier in GameplayModifierHelperUtilities.GetGameplayModifierStrings(_gameplayCoreSceneSetupData.gameplayModifiers))
            {
                _columnValues.Clear();

                if (_gameplayModifiersMapping.TryGetValue(modifier, out long modifierID))
                {
                    _columnValues.Add(("modifier_id", modifierID));
                }
                else
                {
                    _columnValues.Add(("modifier_name", modifier));
                    modifierID = _dbManager.InsertEntry(DatabaseSchemas.ModifiersTableName, _columnValues);

                    _columnValues.Clear();
                    _columnValues.Add(("modifier_id", modifierID));
                }

                _columnValues.Add(("play_id", _playRowID));

                _dbManager.InsertEntry(DatabaseSchemas.PlayModifiersTableName, _columnValues);
            }
        }

        private void OnLevelFinished()
        {
            _columnValues.Clear();
            _columnValues.Add(("completed", 1));

            _dbManager.UpdateEntry(DatabaseSchemas.PlaysTableName, _playRowID, _columnValues);
        }

        private void OnLevelFailed()
        {
            _columnValues.Clear();
            _columnValues.Add(("failed", 1));

            _dbManager.UpdateEntry(DatabaseSchemas.PlaysTableName, _playRowID, _columnValues);
        }

        public static int IsRightHandedNote(NoteData noteData) => noteData.colorType == ColorType.ColorB ? 1 : 0;

        public static string GetNoteCutDirectionString(NoteData noteData) => GetNoteCutDirectionString(noteData.cutDirection);

        public static string GetNoteCutDirectionString(NoteCutDirection cutDirection)
        {
            switch (cutDirection)
            {
                case NoteCutDirection.Any:
                    return "a";

                case NoteCutDirection.Down:
                    return "d";
                case NoteCutDirection.DownLeft:
                    return "dl";
                case NoteCutDirection.DownRight:
                    return "dr";

                case NoteCutDirection.Up:
                    return "u";
                case NoteCutDirection.UpLeft:
                    return "ul";
                case NoteCutDirection.UpRight:
                    return "ur";

                case NoteCutDirection.Left:
                    return "l";
                case NoteCutDirection.Right:
                    return "r";

                default:
                    return "?";
            }
        }
    }
}
