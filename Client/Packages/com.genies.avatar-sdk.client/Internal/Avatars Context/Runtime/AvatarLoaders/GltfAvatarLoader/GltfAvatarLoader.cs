using Cysharp.Threading.Tasks;
using Genies.Avatars;
using Genies.Refs;
using UnityEngine;

namespace Genies.Avatars.Context
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal sealed class GltfAvatarLoader : IAvatarLoader
#else
    public sealed class GltfAvatarLoader : IAvatarLoader
#endif
    {
        public string Url;
        public GenieGltfImporter.Settings Settings;

        public UniTask<IGenie> LoadAsync(Transform parent = null)
        {
            return GenieGltfImporter.ImportAsync(Url, parent, Settings);
        }

        public UniTask<Ref<IGeniePrefab>> LoadAsPrefabAsync()
        {
            return GenieGltfImporter.ImportAsPrefabAsync(Url, Settings);
        }

        public UniTask<ISpeciesGenieController> LoadControllerAsync(Transform parent = null)
        {
            Debug.LogError($"[{nameof(GltfAvatarLoader)}] glTF avatars are readonly so a controller cannot be instantiated from it");
            return UniTask.FromResult<ISpeciesGenieController>(null);
        }

        /// <summary>
        /// Forces the unload from memory of any genies loaded from the current URL, even if they are still in use.
        /// Don't use this method unless you know what you are doing.
        /// </summary>
        public void ForceUnload()
        {
            GenieGltfImporter.ForceUnload(Url);
        }
    }
}