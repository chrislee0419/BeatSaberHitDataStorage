using IPA;
using SiraUtil.Zenject;
using BeatSaberHitDataStorage.Installers;
using IPALogger = IPA.Logging.Logger;

namespace BeatSaberHitDataStorage
{
    [Plugin(RuntimeOptions.DynamicInit)]
    public class Plugin
    {
        internal static Plugin Instance { get; private set; }
        internal static IPALogger Log { get; private set; }

        [Init]
        public Plugin(IPALogger logger, Zenjector zenjector)
        {
            Instance = this;
            Plugin.Log = logger;
            Plugin.Log?.Debug("Logger initialized.");

            zenjector.OnGame<RecorderInstaller>();
        }

        [OnEnable]
        public void OnEnable()
        {
        }

        [OnDisable]
        public void OnDisable()
        {
        }
    }
}
