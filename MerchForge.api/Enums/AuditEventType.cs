namespace MerchForge.api.Enums;

// The category filter set on the Security tab's audit log (Part 17 of the
// Users & Security spec). Kept to exactly the categories that have real
// events writing into them today - no placeholder categories.
public enum AuditEventType
{
    Authentication,
    UserManagement,
    BusinessManagement,
    Subscription,
    Template,
    ProductFields,
    Security
}
