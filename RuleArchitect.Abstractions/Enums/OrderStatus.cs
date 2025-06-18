using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RuleArchitect.Abstractions.Enums
{
    /// <summary>
    /// Represents the detailed workflow statuses for an Order.
    /// </summary>
    public enum OrderStatus
    {
        /// <summary>
        /// Order created but not yet submitted for review.
        /// </summary>
        Draft,

        /// <summary>
        /// Submitted by Order Reviewer, awaiting Production Tech.
        /// </summary>
        ReadyForProduction,

        /// <summary>
        /// Production Tech is actively working on the software.
        /// </summary>
        ProductionInProgress,

        /// <summary>
        /// Production Tech has completed their work, awaiting Software Reviewer.
        /// </summary>
        ReadyForSoftwareReview,

        /// <summary>
        /// Software Reviewer is actively reviewing.
        /// </summary>
        SoftwareReviewInProgress,

        /// <summary>
        /// All steps completed and approved.
        /// </summary>
        Completed,

        /// <summary>
        /// Order was rejected at some stage (Order or Software review).
        /// Check Notes/History for details. May need to revert to a previous state.
        /// </summary>
        Rejected,

        /// <summary>
        /// Order was cancelled.
        /// </summary>
        Cancelled,

        /// <summary>
        /// Order is temporarily on hold.
        /// </summary>
        OnHold
    }
}
