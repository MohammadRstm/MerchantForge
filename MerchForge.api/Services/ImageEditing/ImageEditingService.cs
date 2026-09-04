using System.Text.Json;
using MerchForge.api.DTOs.ImageEditing;
using MerchForge.api.Enums;
using MerchForge.api.Exceptions.AI;
using MerchForge.api.Models;
using MerchForge.api.Repositories.Interfaces;
using MerchForge.api.Services.AI.Contracts;
using MerchForge.api.Services.AI.Interfaces;
using MerchForge.api.Services.BusinessDashboard.interfaces;
using MerchForge.api.Services.ImageEditing.Interfaces;
using MerchForge.api.Services.Subscription.interfaces;
using MerchForge.api.Services.Storage.interfaces;
using Microsoft.Extensions.Logging;

namespace MerchForge.api.Services.ImageEditing;

public class ImageEditingService : IImageEditingService
{
    /// <summary>Well inside the provider's 14-image limit; matches the cap a manual product listing already has.</summary>
    private const int MaxImages = 5;

    private readonly IImageEditJobRepository _jobRepository;
    private readonly IProductImageService _imageService;
    private readonly IProductImageEditingClient _editingClient;
    private readonly IAiTranscriptionService _transcription;
    private readonly IFeatureCreditService _featureCreditService;
    private readonly ILogger<ImageEditingService> _logger;
    private readonly IStoredImageUrlResolver _productImageUrlResolver;

    public ImageEditingService(
        IImageEditJobRepository jobRepository,
        IProductImageService imageService,
        IProductImageEditingClient editingClient,
        IAiTranscriptionService transcription,
        IFeatureCreditService featureCreditService,
        ILogger<ImageEditingService> logger,
        IStoredImageUrlResolver productImageUrlResolver)
    {
        _productImageUrlResolver = productImageUrlResolver;
        _jobRepository = jobRepository;
        _imageService = imageService;
        _editingClient = editingClient;
        _transcription = transcription;
        _featureCreditService = featureCreditService;
        _logger = logger;
    }

    public async Task<ImageEditJobResponse> EditAsync(
        Guid businessId,
        Guid userId,
        Guid productId,
        List<string> imageUrls,
        string? prompt,
        IFormFile? audioPrompt,
        CancellationToken cancellationToken = default)
    {
        if (imageUrls.Count == 0)
        {
            throw new InvalidImageEditRequestException("Select at least one image to edit.");
        }

        if (imageUrls.Count > MaxImages)
        {
            throw new InvalidImageEditRequestException($"Select {MaxImages} images or fewer.");
        }

        var resolvedPrompt = await ResolvePromptAsync(prompt, audioPrompt, cancellationToken);

        // Read back rather than trusted as-sent: these are urls the frontend already
        // holds from a prior upload, and ReadAsync is what confirms they actually
        // belong to this business before anything is sent to a third party.
        var inputs = new List<ImageEditInput>(imageUrls.Count);

        foreach (var url in imageUrls)
        {
            var (bytes, contentType) = await _imageService.ReadAsync(businessId, url, cancellationToken);
            inputs.Add(new ImageEditInput(bytes, contentType));
        }

        var jobId = Guid.NewGuid();

        ImageEditResult result;

        try
        {
            result = await _editingClient.EditAsync(inputs, resolvedPrompt, cancellationToken);
        }
        catch (Exception ex)
        {
            await _jobRepository.CreateAsync(
                BuildJob(jobId, businessId, userId, resolvedPrompt, imageUrls, outputUrl: null, error: ex.Message),
                cancellationToken);

            // Normalizes every failure - a provider timeout, a dropped connection, a
            // bad response - to one clean, actionable message, the same way
            // ProductAiService does for the conversation flow. Without this, a
            // network-level failure (nothing the GeminiImageEditingClient's own
            // status-code check ever sees) would reach the client as a generic 500
            // instead of "try again".
            throw new ImageEditingException("The image editing provider is unavailable right now. Please try again.", ex);
        }

        var outputUrl = await _imageService.SaveAsync(
            businessId, productId, result.ImageBytes, result.MimeType, cancellationToken);

        // Charged after a successful call, never before - the same reasoning as AI
        // product generation: a failed provider call already returned above without
        // reaching here, so the owner is never billed for an edit that produced
        // nothing. Authorization was already checked before this request arrived, so
        // a false result here (credits ran out in the narrow window between the two)
        // is logged rather than turned into an error the owner can't act on - the
        // edit already happened, and the next request's authorization check catches it.
        var creditSpent = await _featureCreditService.TryConsumeAsync(
            businessId, FeatureKeys.AiImageEditing, jobId.ToString(), cancellationToken);

        if (!creditSpent)
        {
            _logger.LogWarning(
                "Image edit {JobId} for business {BusinessId} completed with no credit available to spend.",
                jobId, businessId);
        }

        var job = BuildJob(jobId, businessId, userId, resolvedPrompt, imageUrls, outputUrl, error: null);

        await _jobRepository.CreateAsync(job, cancellationToken);

        return ToResponse(job);
    }

    public async Task<ImageEditJobResponse> GetAsync(
        Guid businessId,
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        var job = await _jobRepository.GetForBusinessAsync(businessId, jobId, cancellationToken)
            ?? throw new ImageEditJobNotFoundException();

        return ToResponse(job);
    }

    private async Task<string> ResolvePromptAsync(
        string? prompt,
        IFormFile? audioPrompt,
        CancellationToken cancellationToken)
    {
        if (audioPrompt is not null)
        {
            if (audioPrompt.Length == 0)
            {
                throw new InvalidImageEditRequestException("The voice message was empty.");
            }

            await using var stream = audioPrompt.OpenReadStream();

            var transcript = await _transcription.TranscribeAsync(
                stream,
                audioPrompt.FileName,
                audioPrompt.ContentType ?? "application/octet-stream",
                cancellationToken);

            if (string.IsNullOrWhiteSpace(transcript))
            {
                throw new InvalidImageEditRequestException("The voice message could not be understood.");
            }

            return transcript;
        }

        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new InvalidImageEditRequestException("Describe what you want changed.");
        }

        return prompt;
    }

    private static ImageEditJob BuildJob(
        Guid id,
        Guid businessId,
        Guid userId,
        string prompt,
        List<string> inputUrls,
        string? outputUrl,
        string? error) => new()
        {
            Id = id,
            BusinessId = businessId,
            CreatedByUserId = userId,
            Prompt = prompt,
            InputImageUrls = JsonSerializer.SerializeToDocument(inputUrls),
            OutputImageUrl = outputUrl,
            Status = error is null ? ImageEditJobStatus.Completed : ImageEditJobStatus.Failed,
            ErrorMessage = error,
        };

    /// <summary>
    /// Inputs and output are both stored as object keys, so both need resolving before
    /// the dashboard can render them.
    /// </summary>
    private ImageEditJobResponse ToResponse(ImageEditJob job) => new()
    {
        Id = job.Id,
        Status = job.Status.ToString(),
        Prompt = job.Prompt,
        InputImageUrls = job.InputImageUrls.RootElement
            .EnumerateArray()
            .Select(e => _productImageUrlResolver.ToPublicUrl(e.GetString()!))
            .ToList(),
        OutputImageUrl = _productImageUrlResolver.ToPublicUrl(job.OutputImageUrl),
        ErrorMessage = job.ErrorMessage,
        CreatedAt = job.CreatedAt,
    };
}
