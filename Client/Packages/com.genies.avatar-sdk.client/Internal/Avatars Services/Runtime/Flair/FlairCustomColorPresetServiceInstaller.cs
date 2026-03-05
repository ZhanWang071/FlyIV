using Genies.CloudSave;
using Genies.DataRepositoryFramework;
using Genies.ServiceManagement;
using Genies.Services.Model;
using VContainer;

namespace Genies.Avatars.Services.Flair
{
    [AutoResolve]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class FlairCustomColorPresetServiceInstaller : IGeniesInstaller
#else
    public class FlairCustomColorPresetServiceInstaller : IGeniesInstaller
#endif
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register
                (
                    _ =>
                    {
                        return new CloudFeatureSaveService<FlairColorPreset>
                        (
                            GameFeature.GameFeatureTypeEnum.CustomFlairColors,
                            new FlairColorPresetCloudSaveJsonSerializer(),
                            (data, id) => data.Id = id,
                            data => data.Id
                        );
                    },
                    Lifetime.Singleton
                )
                .As<IDataRepository<FlairColorPreset>>();
            builder.Register<IFlairCustomColorPresetService, FlairCustomColorPresetService>(Lifetime.Singleton);
        }

    }
}
