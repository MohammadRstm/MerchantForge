namespace MerchForge.api.DTOs.Dashboard;

public class CustomerStatsResponse
{
    /// <summary>Registered accounts only - guest checkouts (Order.CustomerId == null) are not counted anywhere on this page.</summary>
    public int TotalCustomers { get; set; }

    /// <summary>Registered in the requested recent window (see the query's NewCustomersPeriodDays).</summary>
    public int NewCustomers { get; set; }

    public int CustomersWithOrders { get; set; }

    public int CustomersWithoutOrders { get; set; }

    /// <summary>Excludes Cancelled orders and guest orders (CustomerId == null).</summary>
    public int TotalCustomerOrders { get; set; }

    /// <summary>Customers with 2 or more non-cancelled orders.</summary>
    public int RepeatCustomers { get; set; }

    /// <summary>RepeatCustomers / CustomersWithOrders. Null when no customer has ordered yet.</summary>
    public double? RepeatCustomerRate { get; set; }

    /// <summary>TotalCustomerOrders / CustomersWithOrders. Zero when no customer has ordered yet.</summary>
    public double AverageOrdersPerCustomer { get; set; }

    /// <summary>Recorded order totals, not money actually collected - no payment gateway exists.</summary>
    public List<CustomerCurrencyTotalResponse> RevenueByCurrency { get; set; } = [];
}
