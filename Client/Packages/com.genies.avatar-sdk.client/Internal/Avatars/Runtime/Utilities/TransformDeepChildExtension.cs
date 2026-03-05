using UnityEngine;
using System.Collections.Generic;

namespace Genies.Avatars
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal static class TransformDeepChildExtension
#else
    public static class TransformDeepChildExtension
#endif
    {
        //Breadth-first search
        public static Transform FindDeepChild(this Transform aParent, string aName)
        {
            Queue<Transform> queue = new Queue<Transform>();
            queue.Enqueue(aParent);
            while (queue.Count > 0)
            {
                var c = queue.Dequeue();
                if (c.name == aName)
                {
                    return c;
                }

                foreach (Transform t in c)
                {
                    queue.Enqueue(t);
                }
            }

            return null;
        }
    }
}