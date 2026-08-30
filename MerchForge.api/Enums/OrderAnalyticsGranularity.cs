namespace MerchForge.api.Enums;

/// <summary>
/// Never persisted — computed per-request from the requested date span (see
/// OrderRepository.GetOrderAnalyticsAsync) purely to tell the frontend how to label
/// each point on the analytics chart.
/// </summary>
public enum OrderAnalyticsGranularity
{
    Daily,
    Monthly,
}
