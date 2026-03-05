using System;
using System.Collections.Generic;
using System.Text;

namespace UMA
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal abstract partial class UMADna : UMADnaBase
#else
    public abstract partial class UMADna : UMADnaBase
#endif
    {

    }
}
