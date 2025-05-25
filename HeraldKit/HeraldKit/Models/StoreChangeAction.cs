using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HeraldKit.Models
{
    /// <summary>
    /// Describes the type of change that occurred in the notification store.
    /// </summary>
    public enum StoreChangeAction
    {
        Added,
        Removed,
        Updated, // e.g., Marked as read
        Cleared
    }
}
