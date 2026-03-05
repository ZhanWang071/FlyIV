using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Genies.Customization.Framework;
using Genies.Naf;
using Genies.Naf.Content;
using Genies.ServiceManagement;

namespace Genies.Looks.Customization.Commands
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal class UnequipNativeAvatarAssetCommand : ICommand
#else
    public class UnequipNativeAvatarAssetCommand : ICommand
#endif
    {
        private readonly NativeUnifiedGenieController _controller;
        private readonly string                       _assetGuid;
        private readonly List<string>                 _previousEquippedAssetGuids;
        private readonly IAssetIdConverter            _idConverter;

        public UnequipNativeAvatarAssetCommand(string assetGuid, NativeUnifiedGenieController controller)
        {
            _idConverter = this.GetService<IAssetIdConverter>();
            _controller     = controller;
            _assetGuid      = assetGuid;
            _previousEquippedAssetGuids = controller.GetEquippedAssetIds();
        }

        public async UniTask ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var assetId = await _idConverter.ConvertToUniversalIdAsync(_assetGuid);
            await _controller.UnequipAssetAsync(assetId);
        }

        public UniTask UndoAsync(CancellationToken cancellationToken = default)
        {
            var prevIds = _previousEquippedAssetGuids.Select(id => (id, new Dictionary<string, string>())).ToList();
            return _controller.SetEquippedAssetsAsync(prevIds);
        }
    }
}
