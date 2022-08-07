using System;
using System.Collections.Generic;
using Zenject;
using static BeatSaberHitDataStorage.Managers.PlayDataManager;

namespace BeatSaberHitDataStorage.Managers
{
    internal class HitDataManager : IInitializable, IDisposable
    {
        private ScoreController _scoreController;
        private BeatmapObjectManager _beatmapObjectManager;
        private PlayDataManager _playDataManager;

        private Stack<SwingRatingHandler> _swingRatingHandlerPool;

        [Inject]
        public HitDataManager(
            ScoreController scoreController,
            BeatmapObjectManager beatmapObjectManager,
            PlayDataManager playDataManager,
            IDifficultyBeatmap difficultyBeatmap,
            IReadonlyBeatmapData beatmapData)
        {
            _scoreController = scoreController;
            _beatmapObjectManager = beatmapObjectManager;
            _playDataManager = playDataManager;

            int density = Convert.ToInt32(beatmapData.cuttableNotesCount / difficultyBeatmap.level.songDuration);

            _swingRatingHandlerPool = new Stack<SwingRatingHandler>(density * 2);
            for (int i = 0; i < density; ++i)
                _swingRatingHandlerPool.Push(new SwingRatingHandler(this));
        }

        public void Initialize()
        {
            _scoreController.scoringForNoteStartedEvent += OnScoringForNoteStarted;
            _beatmapObjectManager.noteWasCutEvent += OnNoteWasCut;
            _beatmapObjectManager.noteWasMissedEvent += OnNoteWasMissed;
        }

        public void Dispose()
        {
            if (_scoreController != null)
                _scoreController.scoringForNoteStartedEvent -= OnScoringForNoteStarted;

            if (_beatmapObjectManager != null)
            {
                _beatmapObjectManager.noteWasCutEvent -= OnNoteWasCut;
                _beatmapObjectManager.noteWasMissedEvent -= OnNoteWasMissed;
            }
        }

        private void OnScoringForNoteStarted(ScoringElement scoringElement)
        {
            NoteData noteData = scoringElement.noteData;
            if (scoringElement is BadCutScoringElement badCutScoringElement)
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
                    0,
                    0);
            }
            else if (scoringElement is GoodCutScoringElement goodCutScoringElement)
            {
                SwingRatingHandler handler;
                if (_swingRatingHandlerPool.Count == 0)
                    handler = new SwingRatingHandler(this);
                else
                    handler = _swingRatingHandlerPool.Pop();

                NoteCutInfo noteCutInfo = goodCutScoringElement.cutScoreBuffer.noteCutInfo;
                handler.Prepare(noteData, noteCutInfo, goodCutScoringElement.cutScoreBuffer);
            }
        }

        private void OnNoteWasCut(NoteController noteController, in NoteCutInfo noteCutInfo)
        {
            NoteData noteData = noteCutInfo.noteData;
            if (noteData.cutDirection == NoteCutDirection.None || noteData.colorType == ColorType.None)
            {
                // bomb hit
                if (PluginConfig.Instance.RecordBombHits)
                    _playDataManager.RecordBombHitData(noteData.time);
            }
        }

        private void OnNoteWasMissed(NoteController noteController)
        {
            // don't handle bombs
            NoteData noteData = noteController.noteData;
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

        private class SwingRatingHandler : ICutScoreBufferDidFinishReceiver
        {
            private HitDataManager _hitDataManager;

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

            public void HandleCutScoreBufferDidFinish(CutScoreBuffer cutScoreBuffer)
            {
                _hitDataManager._playDataManager.RecordNoteHitData(
                    _time,
                    1,
                    0,
                    _isRightHandNote,
                    _cutDirectionString,
                    _lineIndex,
                    _lineLayer,
                    cutScoreBuffer.beforeCutScore,
                    cutScoreBuffer.afterCutScore,
                    cutScoreBuffer.centerDistanceCutScore,
                    _timeDeviation,
                    _directionDeviation);

                cutScoreBuffer.UnregisterDidFinishReceiver(this);
                _hitDataManager._swingRatingHandlerPool.Push(this);
            }

            public void Prepare(NoteData noteData, NoteCutInfo noteCutInfo, IReadonlyCutScoreBuffer cutScoreBuffer)
            {
                _time = noteData.time;
                _isRightHandNote = IsRightHandedNote(noteData);
                _cutDirectionString = GetNoteCutDirectionString(noteData);
                _lineIndex = noteData.lineIndex;
                _lineLayer = (int)noteData.noteLineLayer;
                _timeDeviation = noteCutInfo.timeDeviation;
                _directionDeviation = noteCutInfo.cutDirDeviation;

                cutScoreBuffer.RegisterDidFinishReceiver(this);
            }
        }
    }
}
