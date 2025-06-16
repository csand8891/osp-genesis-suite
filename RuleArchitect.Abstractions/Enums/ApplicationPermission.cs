using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace RuleArchitect.Abstractions.Enums
{
    public enum ApplicationPermission
    {
        // User Management Permissions
        [Display(Name = "View Users", Description = "Allows viewing the list of users.")]
        ViewUsers,

        [Display(Name = "Manage Users", Description = "Allows creating, editing, and deleting users.")]
        ManageUsers, // Encompasses create, edit, delete, activate/deactivate roles

        [Display(Name = "Manage Roles", Description = "Allows creating, editing, and deleting roles and their permissions.")]
        ManageRoles,

        // Order Management Permissions
        [Display(Name = "View Orders", Description = "Allows viewing order details.")]
        ViewOrders,

        [Display(Name = "Create Orders", Description = "Allows creating new orders.")]
        CreateOrders,

        [Display(Name = "Edit Orders", Description = "Allows editing existing orders.")]
        EditOrders,

        [Display(Name = "Delete Orders", Description = "Allows deleting orders.")] // Be cautious with this one
        DeleteOrders,

        [Display(Name = "Submit Order For Production", Description = "Allows submitting an order to the production phase.")]
        SubmitOrderForProduction,

        [Display(Name = "Manage Order Production Status", Description = "Allows updating the order's production status (start, complete).")]
        ManageOrderProductionStatus,

        [Display(Name = "Manage Order Software Review Status", Description = "Allows updating the order's software review status.")]
        ManageOrderSoftwareReviewStatus,

        [Display(Name = "Put Order On Hold", Description = "Allows putting an order on hold.")]
        PutOrderOnHold,

        [Display(Name = "Cancel Order", Description = "Allows cancelling an order.")]
        CancelOrder,

        [Display(Name = "Reject Order", Description = "Allows rejecting an order.")]
        RejectOrder,

        [Display(Name = "Remove Software Option From Order", Description = "Allows removing a software option (line item) from an order.")]
        RemoveSoftwareOptionFromOrder,

        // Software Option (Rulesheet) Management Permissions
        [Display(Name = "View Software Options", Description = "Allows viewing software options/rulesheets.")]
        ViewSoftwareOptions,

        [Display(Name = "Manage Software Options", Description = "Allows creating, editing, and deleting software options/rulesheets.")]
        ManageSoftwareOptions,

        // Administrative/System Permissions
        [Display(Name = "View Admin Dashboard", Description = "Allows viewing the administrative dashboard.")]
        ViewAdminDashboard,

        [Display(Name = "Access System Settings", Description = "Allows accessing and modifying system-level settings.")]
        AccessSystemSettings,

        [Display(Name = "View System Logs", Description = "Allows viewing system logs and audit trails.")]
        ViewSystemLogs
    }
}
