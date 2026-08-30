namespace MerchForge.api.DTOs.BusinessDashboard;

/// <summary>
/// Distinct buyers, counted by Order.CustomerEmail (always populated, guest or
/// authenticated) rather than Order.CustomerId (only ever set for a logged-in
/// Customer account) — this answers "how many distinct people have bought from me",
/// a different question from the per-order repeat-buyer lookup used in the order
/// detail drawer, which deliberately only counts orders tied to a real account.
/// </summary>
public class CustomerSnapshotResponse
{
    public int TotalCustomers { get; set; }

    /// <summary>Customers whose first-ever order falls inside the requested [From, To] range.</summary>
    public int NewCustomersInPeriod { get; set; }
}
