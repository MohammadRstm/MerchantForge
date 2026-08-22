namespace MerchForge.api.Services.AI.Contracts;

/// <summary>One input image for an edit request, already read into memory.</summary>
public record ImageEditInput(byte[] Bytes, string MimeType);

/// <summary>The edited image the provider returned.</summary>
public record ImageEditResult(byte[] ImageBytes, string MimeType);
