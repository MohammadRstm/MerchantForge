namespace MerchForge.api.Enums;

/// <summary>
/// Lifecycle of an AI-assisted product creation conversation.
///
/// A product is only written to the products table from <see cref="Completed"/>,
/// which is reachable solely through explicit owner confirmation — the agent
/// deciding a product looks finished moves the draft to
/// <see cref="WaitingForProductApproval"/> and no further.
/// </summary>
public enum ProductDraftStatus
{
    /// <summary>Gathering information; the agent may still be asking questions.</summary>
    CollectingInformation,

    /// <summary>The agent asked for something specific and is waiting on the owner.</summary>
    WaitingForMissingInformation,

    /// <summary>An image modification was requested and is being applied.</summary>
    ProcessingImage,

    /// <summary>A modified image is waiting for the owner to accept or reject it.</summary>
    WaitingForImageApproval,

    /// <summary>The product is complete and previewed; waiting for final confirmation.</summary>
    WaitingForProductApproval,

    /// <summary>Confirmed by the owner and written to the products table.</summary>
    Completed,

    /// <summary>Abandoned by the owner.</summary>
    Cancelled,

    /// <summary>The conversation could not continue (e.g. the AI provider failed).</summary>
    Failed
}
