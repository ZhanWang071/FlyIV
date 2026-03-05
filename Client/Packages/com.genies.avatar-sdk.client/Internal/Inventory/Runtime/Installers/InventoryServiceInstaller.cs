using Genies.FeatureFlags;
using Genies.ServiceManagement;
using UnityEngine;
using VContainer;

namespace Genies.Inventory.Installers
{
    [AutoResolve]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class InventoryServiceInstaller : IGeniesInstaller
#else
    public class InventoryServiceInstaller : IGeniesInstaller
#endif
    {
        [Header("Optional Override")]
        [SerializeField] private InventoryItemToCategory _inventoryItemToCategory;
        [SerializeField] private string _partyId;

        // 1 day default. Matches default in DefaultInventoryService constructor
        public int cacheExpirationOverride = 86400;
        public bool IncludeV1Inventory;
        public bool isDemoMode = false;
        public string DefaultInventoryOrgId;
        public string DefaultInventoryAppId;

        public InventoryItemToCategory InventoryItemToCategoryDep
        {
            get => _inventoryItemToCategory;
            set => _inventoryItemToCategory = value;
        }

        public string PartyId
        {
            get => _partyId;
            set => _partyId = value;
        }

        public InventoryServiceInstaller()
        {
        }

        public InventoryServiceInstaller(string partyId)
        {
            PartyId = partyId;
        }

        public void Install(IContainerBuilder builder)
        {
            if (IncludeV1Inventory)
            {
                builder.Register<IInventoryService, InventoryService>(Lifetime.Singleton)
                    .WithParameter(InventoryItemToCategoryDep)
                    .WithParameter(PartyId).AsSelf();
            }

            var defaultInventory = new DefaultInventoryService(
                DefaultInventoryOrgId ?? "ALL",
                DefaultInventoryAppId ?? "SDK_ALL",
                cacheExpirationSeconds: cacheExpirationOverride,
                isDemoMode: isDemoMode);

            defaultInventory.RegisterAs<DefaultInventoryService, IDefaultInventoryService>();
        }
    }
}
