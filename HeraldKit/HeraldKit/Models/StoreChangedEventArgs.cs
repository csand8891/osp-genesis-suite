using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HeraldKit.Models
{
    /// <summary>
    /// Provides data for the StoreChanged event.
    /// </summary>
    public class StoreChangedEventArgs : EventArgs
    {
        /// <summary>
        /// The type of action that occurred.
        /// </summary>
        public StoreChangeAction Action { get; private set; }

        /// <summary>
        /// The list of notification IDs affected by the change.
        /// May be empty for 'Cleared'.
        /// </summary>
        public IReadOnlyList<Guid> AffectedIds { get; private set; }

        public StoreChangedEventArgs(StoreChangeAction action, Guid affectedId)
            : this(action, new List<Guid> { affectedId })
        {
        }

        public StoreChangedEventArgs(StoreChangeAction action, IEnumerable<Guid> affectedIds = null)
        {
            Action = action;
            AffectedIds = affectedIds != null ? new List<Guid>(affectedIds).AsReadOnly() : new List<Guid>().AsReadOnly();
        }
    }
}
