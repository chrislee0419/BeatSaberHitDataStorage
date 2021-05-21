using System.Collections.Generic;

namespace BeatSaberHitDataStorage.Utilities
{
    internal static class GameplayModifierHelperUtilities
    {
        private static List<string> _allModifiers;
        public static List<string> AllModifiers
        {
            get
            {
                if (_allModifiers == null)
                {
                    _allModifiers = new List<string>
                    {
                        NoFailString, OneLifeString, FourLivesString,
                        NoBombsString, NoWallsString, NoArrowsString,
                        GhostNotesString, DisappearingArrowsString, SmallNotesString,
                        ProModeString, StrictAnglesString, ZenModeString,
                        SlowerSongString, FasterSongString, SuperFastSongString
                    };
                }

                return _allModifiers;
            }
        }

        private const string NoFailString = "NoFail";
        private const string OneLifeString = "OneLife";
        private const string FourLivesString = "FourLives";
        private const string NoBombsString = "NoBombs";
        private const string NoWallsString = "NoWalls";
        private const string NoArrowsString = "NoArrows";
        private const string GhostNotesString = "GhostNotes";
        private const string DisappearingArrowsString = "DisappearingArrows";
        private const string SmallNotesString = "SmallNotes";
        private const string ProModeString = "ProMode";
        private const string StrictAnglesString = "StrictAngles";
        private const string ZenModeString = "ZenMode";
        private const string SlowerSongString = "SlowerSong";
        private const string FasterSongString = "FasterSong";
        private const string SuperFastSongString = "SuperFastSong";

        public static List<string> GetGameplayModifierStrings(this GameplayModifiers gameplayModifiers)
        {
            var result = new List<string>();

            if (gameplayModifiers.noFailOn0Energy)
                result.Add(NoFailString);

            if (gameplayModifiers.instaFail)
                result.Add(OneLifeString);

            if (gameplayModifiers.energyType == GameplayModifiers.EnergyType.Battery)
                result.Add(FourLivesString);

            if (gameplayModifiers.noBombs)
                result.Add(NoBombsString);

            if (gameplayModifiers.enabledObstacleType == GameplayModifiers.EnabledObstacleType.NoObstacles)
                result.Add(NoWallsString);

            if (gameplayModifiers.noArrows)
                result.Add(NoArrowsString);

            if (gameplayModifiers.ghostNotes)
                result.Add(GhostNotesString);
            else if (gameplayModifiers.disappearingArrows)
                result.Add(DisappearingArrowsString);

            if (gameplayModifiers.smallCubes)
                result.Add(SmallNotesString);

            if (gameplayModifiers.proMode)
                result.Add(ProModeString);

            if (gameplayModifiers.strictAngles)
                result.Add(StrictAnglesString);

            if (gameplayModifiers.zenMode)
                result.Add(ZenModeString);

            var songSpeed = gameplayModifiers.songSpeed;
            if (songSpeed == GameplayModifiers.SongSpeed.Slower)
                result.Add(SlowerSongString);
            else if (songSpeed == GameplayModifiers.SongSpeed.Faster)
                result.Add(FasterSongString);
            else if (songSpeed == GameplayModifiers.SongSpeed.SuperFast)
                result.Add(SuperFastSongString);

            return result;
        }
    }
}
