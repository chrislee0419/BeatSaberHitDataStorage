using Zenject;
using BeatSaberHitDataStorage.Managers;

namespace BeatSaberHitDataStorage.Installers
{
    public class RecorderInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<DatabaseManager>().AsSingle();
            Container.BindInterfacesAndSelfTo<PlayDataManager>().AsSingle();
            Container.BindInterfacesAndSelfTo<HitDataManager>().AsSingle();
        }
    }
}
