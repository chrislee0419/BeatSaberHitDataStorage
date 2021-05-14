using System.Runtime.CompilerServices;
using IPA.Config.Stores;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]
namespace BeatSaberHitDataStorage
{
    internal class PluginConfig
    {
        public static PluginConfig Instance { get; set; }

        public virtual bool RecordDeviations { get; set; } = false;
    }
}
