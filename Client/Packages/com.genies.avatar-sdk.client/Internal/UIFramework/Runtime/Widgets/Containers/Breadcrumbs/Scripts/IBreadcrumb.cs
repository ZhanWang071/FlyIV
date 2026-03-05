using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Genies.UI.Widgets
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal interface IBreadcrumb
#else
    public interface IBreadcrumb
#endif
    {
        string BreadcrumbId { get; }
        string Title { get; set; }

        void BreadcumbAction();
    }
}
