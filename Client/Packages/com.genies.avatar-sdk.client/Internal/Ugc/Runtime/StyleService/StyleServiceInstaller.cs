using Genies.CloudSave;
using Genies.DataRepositoryFramework;
using Genies.ServiceManagement;
using Genies.Services.Model;
using VContainer;

namespace Genies.Ugc
{
    [AutoResolve]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class StyleServiceInstaller : IGeniesInstaller
#else
    public class StyleServiceInstaller : IGeniesInstaller
#endif
    {
        public void Install(IContainerBuilder builder)
        {
            RegisterCustomStyles(builder);
        }

        private static void RegisterCustomStyles(IContainerBuilder builder)
        {
            //Styles
            builder.Register
                    (
                     _ =>
                     {
                         return new CloudFeatureSaveService<Style>
                             (
                              GameFeature.GameFeatureTypeEnum.UgcStyles,
                              new StyleCloudSaveJsonSerializer(),
                              null,
                              data => data.Id
                             );
                     },
                     Lifetime.Singleton
                    )
                   .As<ICloudFeatureSaveService<Style>>();

            builder.Register<IDataRepository<Style>, RemoteStyleDataRepository>(Lifetime.Singleton)
                   .WithParameter(StyleServiceStates.CustomStyle);
        }

    }
}
