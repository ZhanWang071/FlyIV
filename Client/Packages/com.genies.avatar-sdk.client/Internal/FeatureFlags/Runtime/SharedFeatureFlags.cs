using System.Collections.Generic;

namespace Genies.FeatureFlags
{

    [FeatureFlagsContainer(-1000)]
#if GENIES_SDK && !GENIES_INTERNAL
    internal static class SharedFeatureFlags
#else
    public static class SharedFeatureFlags
#endif
    {
        public const string NONE = "none";
        public const string BypassAuth = "bypass_auth";
        public const string ForceUpgrade = "force_upgrade";
        public const string FreeTexturePlacement = "free-texture-placement";
        public const string DynamicContent = "composer_dynamic_content";
        public const string GearContent = "gear_content";
        public const string ExternalGearContent = "external_gear_content";
        public const string ExternalThingsContent = "external_things_content";
        public const string ExternalSubSpeciesContent = "external_subspecies_content";
        public const string SilverStudioAssetFiltering = "silver_studio_asset_filtering";
        public const string DisableNfts = "disable_nfts";
        public const string BaserowCms = "baserow_cms";
        public const string AddressablesCmsLocationService = "addressables_cms_location_service";
        public const string AddressablesInventoryLocations = "addressables_inventory_locations";
        public const string UniversalContentLocations = "universal_content_locations";
        public const string LanguageSupport = "language_support";
        public const string DynamicConfigsFromApi = "dynamic_configs_from_api";
        public const string ExperiencesLauncher = "experiences_launcher";
        public const string Bugsee = "bugsee";
        public const string Friends = "friends";
        public const string FriendsFeed = "friends_feed";
        public const string SpotifyService = "spotify_service";
        public const string ContactsSync = "contacts_sync";
        public const string GameLauncher = "game_launcher";
        public const string NonUmaAvatar = "non_uma_avatar";
        public const string AIGCFlow = "aigc_flow";
        public const string DailyQuests = "daily_quests";
        public const string Marketplace = "marketplace";
        public const string GapAvatars = "gap_avatars";
        public const string LLMPhotoPersona = "llm_photo_persona";
        public const string LLMPhotoPersonaV2 = "llm_photo_persona_v2";
        public const string InventoryClient = "inventory_client";
        public const string SmartAvatar = "smart_avatar";
        public const string InAppPurchases = "in_app_purchases";
        public const string FirebasePushNotification = "firebase_push_notification";
        public const string PrePromptAigcDecor = "preprompt_aigc_decor";
        public const string GeniesCameraDeepLink = "is_gc_deeplink_enabled";
        public const string SpacesWallEditing = "spaces_wall_editing";
        public const string RecommendationSystem = "is_preprompt_recommendation_enabled";
        public const string Onboarding_v1_5 = "onboarding_v1.5";
        public const string IsFeedHidden = "is_feed_hidden";
        public const string IsVoiceUIUXEnabled = "is_voice_uiux_enabled";
        public const string MassPhotoUpload = "mass_photo_upload";

        private static List<string> _featureFlagIds = new List<string>()
        {
            BypassAuth,
            ForceUpgrade,
            FreeTexturePlacement,
            DynamicContent,
            GearContent,
            ExternalGearContent,
            ExternalSubSpeciesContent,
            SilverStudioAssetFiltering,
            DisableNfts,
            BaserowCms,
            AddressablesCmsLocationService,
            AddressablesInventoryLocations,
            LanguageSupport,
            DynamicConfigsFromApi,
            Bugsee,
            NonUmaAvatar,
            AIGCFlow,
            DailyQuests,
            Marketplace,
            GapAvatars,
            LLMPhotoPersona,
            LLMPhotoPersonaV2,
            ExperiencesLauncher,
            InventoryClient,
            SmartAvatar,
            InAppPurchases,
            FirebasePushNotification,
            PrePromptAigcDecor,
            GeniesCameraDeepLink,
            SpacesWallEditing,
            RecommendationSystem,
            Onboarding_v1_5,
            IsFeedHidden,
            IsVoiceUIUXEnabled,
            MassPhotoUpload,
        };

        public static List<string> GetList()
        {
            return _featureFlagIds;
        }
    }
}
