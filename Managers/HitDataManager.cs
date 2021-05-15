using System;
using System.Collections.Generic;
using Zenject;

namespace BeatSaberHitDataStorage.Managers
{
    internal class HitDataManager : IInitializable, IDisposable
    {
        private IScoreController _scoreController;
        private PlayDataManager _playDataManager;

        private Stack<SwingRatingHandler> _swingRatingHandlerPool;

        [Inject]
        public HitDataManager(IScoreController scoreController, PlayDataManager playDataManager, IDifficultyBeatmap difficultyBeatmap)
        {
            _scoreController = scoreController;
            _playDataManager = playDataManager;

            int density = Convert.ToInt32(difficultyBeatmap.beatmapData.cuttableNotesType / difficultyBeatmap.level.songDuration);

            _swingRatingHandlerPool = new Stack<SwingRatingHandler>(density * 2);
            for (int i = 0; i < density; ++i)
                _swingRatingHandlerPool.Push(new SwingRatingHandler(this));
        }

        public void Initialize()
        {
            _scoreController.noteWasCutEvent += OnNoteWasCut;
            _scoreController.noteWasMissedEvent += OnNoteWasMissed;
        }

        public void Dispose()
        {
            if (_scoreController != null)
            {
                _scoreController.noteWasCutEvent -= OnNoteWasCut;
                _scoreController.noteWasMissedEvent -= OnNoteWasMissed;
            }
        }

        private void OnNoteWasCut(NoteData noteData, in NoteCutInfo noteCutInfo, int multiplier)
        {
            if (noteData.cutDirection == NoteCutDirection.None || noteData.colorType == ColorType.None)
            {
                // bomb hit
                if (PluginConfig.Instance.RecordBombHits)
                    _playDataManager.RecordBombHitData(noteData.time);
            }
            else if (noteCutInfo.allIsOK)
            {
                SwingRatingHandler handler;
                if (_swingRatingHandlerPool.Count == 0)
                    handler = new SwingRatingHandler(this);
                else
                    handler = _swingRatingHandlerPool.Pop();

                handler.Prepare(noteData.time, noteData, in noteCutInfo);
            }
            else
            {
                _playDataManager.RecordNoteHitData(
                    noteData.time,
                    0,
                    0,
                    IsRightHandedNote(noteData),
                    GetNoteCutDirectionString(noteData),
                    noteData.lineIndex,
                    (int)noteData.noteLineLayer,
                    0,
                    0,
                    0,
                    noteCutInfo.timeDeviation,
                    noteCutInfo.cutDirDeviation);
            }
        }

        private void OnNoteWasMissed(NoteData noteData, int multiplier)
        {
            // don't handle bombs
            if (noteData.cutDirection == NoteCutDirection.None || noteData.colorType == ColorType.None)
                return;

            _playDataManager.RecordNoteHitData(
                noteData.time,
                0,
                1,
                IsRightHandedNote(noteData),
                GetNoteCutDirectionString(noteData),
                noteData.lineIndex,
                (int)noteData.noteLineLayer,
                0,
                0,
                0,
                0,
                0);
        }

        private static int IsRightHandedNote(NoteData noteData) => noteData.colorType == ColorType.ColorB ? 1 : 0;

        private static string GetNoteCutDirectionString(NoteData noteData) => PlayDataManager.GetNoteCutDirectionString(noteData.cutDirection);

        private class SwingRatingHandler : ISaberSwingRatingCounterDidFinishReceiver
        {
            private HitDataManager _hitDataManager;

            private float _cutDistanceToCenter;
            private float _time;
            private int _isRightHandNote;
            private string _cutDirectionString;
            private int _lineIndex;
            private int _lineLayer;
            private float _timeDeviation;
            private float _directionDeviation;

            public SwingRatingHandler(HitDataManager hitDataManager)
            {
                _hitDataManager = hitDataManager;
            }

            public void HandleSaberSwingRatingCounterDidFinish(ISaberSwingRatingCounter ratingCounter)
            {
                ScoreModel.RawScoreWithoutMultiplier(
                    ratingCounter,
                    _cutDistanceToCenter,
                    out int beforeCutRawScore,
                    out int afterCutRawScore,
                    out int cutDistanceRawScore);

                _hitDataManager._playDataManager.RecordNoteHitData(
                    _time,
                    1,
                    0,
                    _isRightHandNote,
                    _cutDirectionString,
                    _lineIndex,
                    _lineLayer,
                    beforeCutRawScore,
                    afterCutRawScore,
                    cutDistanceRawScore,
                    _timeDeviation,
                    _directionDeviation);

                ratingCounter.UnregisterDidFinishReceiver(this);
                _hitDataManager._swingRatingHandlerPool.Push(this);
            }

            public void Prepare(float time, NoteData noteData, in NoteCutInfo noteCutInfo)
            {
                _cutDistanceToCenter = noteCutInfo.cutDistanceToCenter;
                _time = time;
                _isRightHandNote = IsRightHandedNote(noteData);
                _cutDirectionString = GetNoteCutDirectionString(noteData);
                _lineIndex = noteData.lineIndex;
                _lineLayer = (int)noteData.noteLineLayer;
                _timeDeviation = noteCutInfo.timeDeviation;
                _directionDeviation = noteCutInfo.cutDirDeviation;

                noteCutInfo.swingRatingCounter.RegisterDidFinishReceiver(this);
            }
        }
    }
}
