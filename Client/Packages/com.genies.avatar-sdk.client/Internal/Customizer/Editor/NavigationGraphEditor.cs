using System;
using Genies.Customization.Framework;
using Genies.Customization.MegaEditor;
using Genies.Customization.Framework.Navigation;
using UnityEngine;
using XNodeEditor;
using Object = UnityEngine.Object;

namespace Genies.Customization.Framework
{
    [CustomNodeGraphEditor(typeof(NavigationGraph))]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class NavigationGraphEditor : NodeGraphEditor
#else
    public class NavigationGraphEditor : NodeGraphEditor
#endif
    {
        public override string GetNodeMenuName(Type type)
        {
            if (typeof(BaseNavigationNode).IsAssignableFrom(type))
            {
                return base.GetNodeMenuName(type);
            }

            return null;
        }

        public override void OnDropObjects(Object[] objects)
        {
            var pos = NodeEditorWindow.current.WindowToGridPosition(Event.current.mousePosition);

            foreach (var obj in objects)
            {
                if (obj is CustomizationConfig controller)
                {
                    var node = CreateNode(typeof(NavigationNode), pos);
                    pos.y += 150;

                    var navNode = node as NavigationNode;
                    navNode.customizationConfig = controller;
                    navNode.name = controller.name;
                }
            }

            base.OnDropObjects(objects);
        }
    }
}
