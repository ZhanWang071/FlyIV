using System.Collections.Generic;
using Genies.Avatars;

namespace Genies.Ugc
{
    /// <summary>
    /// Contains template metadata about a UGC wearable. This is not intended to contain any assets
    /// like UMA related assets or textures.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal sealed class UgcTemplateData
#else
    public sealed class UgcTemplateData
#endif
    {
        /// <summary>
        /// The template ID.
        /// </summary>
        public string TemplateId { get; }

        /// <summary>
        /// The wearable slot.
        /// </summary>
        public string Slot { get; }

        /// <summary>
        /// The wearable subcategory.
        /// </summary>
        public string Subcategory { get; }

        /// <summary>
        /// Whether or not this template represents a basic or a generative UGC wearable.
        /// </summary>
        public bool IsBasic { get; }

        /// <summary>
        /// The collision data of the wearable.
        /// </summary>
        public OutfitCollisionData CollisionData { get; }

        /// <summary>
        /// The data of the splits available for this template.
        /// </summary>
        public IReadOnlyList<UgcTemplateSplitData> Splits { get; }

        public UgcTemplateData(
            string templateId,
            string slot,
            string subcategory,
            bool isBasic,
            OutfitCollisionData collisionData,
            IEnumerable<UgcTemplateSplitData> splits)
        {
            TemplateId = templateId;
            Slot = slot;
            Subcategory = subcategory;
            IsBasic = isBasic;
            CollisionData = collisionData;

            var splitsList = new List<UgcTemplateSplitData>(splits);
            Splits = splitsList.AsReadOnly();
        }
    }
}
