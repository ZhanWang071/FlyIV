using Cysharp.Threading.Tasks;
using Genies.CrashReporting;
using Newtonsoft.Json;
using System;
using Genies.ServiceManagement;

namespace Genies.Avatars.Services
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal static class AvatarCreatorUtil
#else
    public static class AvatarCreatorUtil
#endif
    {
        /// <summary>
        /// Resources file path for the old default avatar definition.
        /// Maintained for reference only.
        ///
        /// To load:
        /// var json = UnityEngine.Resources.Load<TextAsset>(DefaultAvatarDefPath)
        /// </summary>
        private const string DefaultAvatarDefPath = "DefaultAvatarDefinition";
        private static IAvatarService AvatarService => ServiceManager.Get<IAvatarService>();

        /// <summary>
        /// Syncs the passed avatar JSON definition to the logged-in user.
        /// If the user doesn't already have an avatar saved, spawns a local avatar with the given definition.
        /// Pass 'saveOnCreate = true' to save the created avatar to the logged-in user's account.
        /// </summary>
        /// <param name="avatarDefinition">JSON avatar definition to sync.</param>
        /// <param name="saveOnCreate">If true, saves the avatar with passed definition to the logged-in user's account if a new avatar is created.</param>
        public static async UniTask CreateOrUpdateAvatar(string avatarDefinition, bool saveOnCreate = true)
        {
            Naf.AvatarDefinition avatarDef = null;

            try
            {
                avatarDef = JsonConvert.DeserializeObject<Naf.AvatarDefinition>(avatarDefinition);
            }
            catch (AggregateException ae)
            {
                CrashReporter.Log("Cannot load, invalid avatar definition.", LogSeverity.Error);
                CrashReporter.LogHandledException(ae);

                avatarDef = null;
            }

            try
            {
                (bool IsSyncedDef, Naf.AvatarDefinition _) getAvatarResult = await AvatarService.GetUserAvatarDefinitionOrDefaultAsync();

                if (getAvatarResult.IsSyncedDef)
                {
                    if (avatarDef == null)
                    {
                        throw new ArgumentException("Cannot update user avatar with an invalid avatar definition.");
                    }

                    await AvatarService.UpdateAvatarAsync(avatarDef);
                }
                else
                {
                    await AvatarService.CreateAvatarAsync(avatarDef);

                    if (saveOnCreate)
                    {
                        await AvatarService.UpdateAvatarAsync(avatarDef);
                    }
                }
            }
            catch (Exception ex)
            {
                CrashReporter.Log($"Failed to create or update the user avatar with provided definition. {ex.Message}", LogSeverity.Error);
                CrashReporter.LogHandledException(ex);

                throw;
            }
        }

        public static async UniTask<string> GetPersistentAvatarDefinition()
        {
            Naf.AvatarDefinition unifiedDefinition = await AvatarService.GetAvatarDefinitionAsync();
            unifiedDefinition ??= NafAvatarExtensions.DefaultDefinition();
            //AvatarDefinitionFilter.FilterNonPersistentAttributes(unifiedDefinition);
            var persistentDefinition = JsonConvert.SerializeObject(unifiedDefinition);
            return persistentDefinition;
        }

    }
}
