using System.Threading;
using CancellationToken = System.Threading.CancellationToken;
using Cysharp.Threading.Tasks;
using Genies.Customization.Framework;
using Genies.Naf;
using Genies.Naf.Content;
using Genies.ServiceManagement;
using GnWrappers;

namespace Genies.Looks.Customization.Commands
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal class EquipNativeAvatarTattooCommand : ICommand
#else
    public class EquipNativeAvatarTattooCommand : ICommand
#endif
    {
        private readonly NativeUnifiedGenieController _controller;
        private readonly string                       _tattooGuid;
        private readonly MegaSkinTattooSlot           _slot;
        private readonly string                       _previousTattooGuid;
        private readonly IAssetIdConverter            _idConverter;

        public EquipNativeAvatarTattooCommand(string tattooGuid, MegaSkinTattooSlot slot, NativeUnifiedGenieController controller)
        {
            _controller         = controller;
            _tattooGuid         = tattooGuid;
            _slot               = slot;
            _previousTattooGuid = controller.GetEquippedTattoo(slot);
            _idConverter = this.GetService<IAssetIdConverter>();
        }

        public async UniTask ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var assetId = await _idConverter.ConvertToUniversalIdAsync(_tattooGuid);
            await _controller.EquipTattooAsync(_slot, assetId, await _controller.AssetParamsService.FetchParamsAsync(_tattooGuid));
        }

        public async UniTask UndoAsync(CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(_previousTattooGuid))
            {
                await _controller.UnequipTattooAsync(_slot);
            }

            var prevTatAssetId = await _idConverter.ConvertToUniversalIdAsync(_previousTattooGuid);
            await _controller.EquipTattooAsync(_slot, prevTatAssetId, await _controller.AssetParamsService.FetchParamsAsync(_previousTattooGuid));
        }
    }
}
