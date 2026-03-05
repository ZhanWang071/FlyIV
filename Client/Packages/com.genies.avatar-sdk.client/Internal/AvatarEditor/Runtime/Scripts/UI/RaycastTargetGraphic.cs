using UnityEngine;
using UnityEngine.UI;

namespace Genies.AvatarEditor
{
    [RequireComponent(typeof(CanvasRenderer))]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class RaycastTargetGraphic : Graphic
#else
    public class RaycastTargetGraphic : Graphic
#endif
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
        }
    }
}