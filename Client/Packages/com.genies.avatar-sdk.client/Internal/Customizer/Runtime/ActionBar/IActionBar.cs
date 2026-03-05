using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Genies.Customization.Framework.Actions
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal interface IActionBar
#else
    public interface IActionBar
#endif
    {
        public event Action UndoRequested;
        public event Action RedoRequested;
        public event Action SaveRequested;
        public event Action ExitRequested;
        public event Action ResetAllRequested;
        public event Action SubmitRequested;

        void ToggleUndoRedoActivity(bool hasUndo, bool hasRedo);
        void SetActionFlags(ActionBarFlags barFlags);
    }
}