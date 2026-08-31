using System.Text.Json;
using FluentAssertions;
using MerchForge.api.DTOs.BusinessDashboard;
using MerchForge.api.Enums;
using MerchForge.api.Exceptions.AI;
using MerchForge.api.Services.AI.Contracts;
using MerchForge.api.Services.AI.Interfaces;
using MerchForge.api.Services.BusinessDashboard.interfaces;
using MerchForge.api.Services.ImageSuggestion;
using MerchForge.api.Repositories.Interfaces;
using MerchForge.api.Services.Subscription.interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MerchForge.UnitTests.Services;

public class ImageSuggestionServiceTests
{
    private static readonly Guid BusinessId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private const string ImageUrl = "/uploads/products/main.jpg";

    private readonly Mock<IProductImageService> _imageService = new();
    private readonly Mock<IProductImageSuggestionClient> _suggestionClient = new();
    private readonly Mock<IBusinessDashboardService> _dashboardService = new();
    private readonly Mock<IBusinessDashboardRepository> _dashboardRepository = new();
    private readonly Mock<IFeatureCreditService> _featureCreditService = new();

    private readonly ImageSuggestionService _service;

    public ImageSuggestionServiceTests()
    {
        _imageService
            .Setup(s => s.ReadAsync(BusinessId, ImageUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(([1, 2, 3], "image/jpeg"));

        _dashboardService
            .Setup(s => s.GetProductFormAsync(BusinessId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductFormResponse
            {
                Categories = [new ProductFormCategoryResponse { Id = Guid.NewGuid(), Name = "Shirts" }],
                MetadataFields = [],
            });

        _dashboardRepository
            .Setup(r => r.GetBusinessSummaryAsync(BusinessId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(("Test Business", DateTime.UtcNow, (string?)null));

        _dashboardRepository
            .Setup(r => r.GetProductFormDataAsync(BusinessId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, []));

        _featureCreditService
            .Setup(s => s.TryConsumeAsync(BusinessId, FeatureKeys.AiImageEditing, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _service = new ImageSuggestionService(
            _imageService.Object,
            _suggestionClient.Object,
            _dashboardService.Object,
            _dashboardRepository.Object,
            _featureCreditService.Object,
            Mock.Of<ILogger<ImageSuggestionService>>());
    }

    [Fact]
    public async Task Spends_a_credit_only_after_a_successful_analysis()
    {
        _suggestionClient
            .Setup(c => c.SuggestAsync(It.IsAny<ImageEditInput>(), It.IsAny<ProductAiContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductAiDraft { Title = "A mug", Tags = [] });

        await _service.SuggestAsync(BusinessId, UserId, ImageUrl);

        _featureCreditService.Verify(
            s => s.TryConsumeAsync(BusinessId, FeatureKeys.AiImageEditing, It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Does_not_spend_a_credit_when_the_provider_call_fails()
    {
        _suggestionClient
            .Setup(c => c.SuggestAsync(It.IsAny<ImageEditInput>(), It.IsAny<ProductAiContext>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("provider down"));

        var act = () => _service.SuggestAsync(BusinessId, UserId, ImageUrl);

        // Every provider failure - a bad response, a dropped connection, a timeout -
        // is normalized to one clean, actionable exception rather than leaking
        // whatever the client happened to throw, the same way ImageEditingService
        // and ProductAiService both do.
        await act.Should().ThrowAsync<ImageEditingException>();

        _featureCreditService.Verify(
            s => s.TryConsumeAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Drops_a_hallucinated_category_id_that_does_not_belong_to_this_business()
    {
        var hallucinatedCategoryId = Guid.NewGuid();

        _suggestionClient
            .Setup(c => c.SuggestAsync(It.IsAny<ImageEditInput>(), It.IsAny<ProductAiContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductAiDraft { CategoryId = hallucinatedCategoryId, Tags = [] });

        _dashboardRepository
            .Setup(r => r.CanUseCategoryAsync(BusinessId, hallucinatedCategoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _service.SuggestAsync(BusinessId, UserId, ImageUrl);

        result.CategoryId.Should().BeNull();
        result.CategoryName.Should().BeNull();
    }

    [Fact]
    public async Task Keeps_and_resolves_a_category_id_that_belongs_to_this_business()
    {
        var categoryId = Guid.NewGuid();

        _suggestionClient
            .Setup(c => c.SuggestAsync(It.IsAny<ImageEditInput>(), It.IsAny<ProductAiContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductAiDraft { CategoryId = categoryId, Tags = [] });

        _dashboardService
            .Setup(s => s.GetProductFormAsync(BusinessId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductFormResponse
            {
                Categories = [new ProductFormCategoryResponse { Id = categoryId, Name = "Shoes" }],
                MetadataFields = [],
            });

        _dashboardRepository
            .Setup(r => r.CanUseCategoryAsync(BusinessId, categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _service.SuggestAsync(BusinessId, UserId, ImageUrl);

        result.CategoryId.Should().Be(categoryId);
        result.CategoryName.Should().Be("Shoes");
    }

    [Fact]
    public async Task Drops_a_metadata_key_whose_value_is_an_explicit_json_null()
    {
        var metadata = new Dictionary<string, JsonElement>
        {
            ["sizes"] = JsonSerializer.SerializeToElement<string?>(null),
        };

        _suggestionClient
            .Setup(c => c.SuggestAsync(It.IsAny<ImageEditInput>(), It.IsAny<ProductAiContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductAiDraft { Title = "A mug", Tags = [], Metadata = metadata });

        var result = await _service.SuggestAsync(BusinessId, UserId, ImageUrl);

        result.Metadata.Should().BeNull();
    }

    [Fact]
    public async Task Keeps_a_metadata_key_with_a_real_value_alongside_one_that_was_null()
    {
        var metadata = new Dictionary<string, JsonElement>
        {
            ["sizes"] = JsonSerializer.SerializeToElement<string?>(null),
            ["colors"] = JsonSerializer.SerializeToElement(new[] { "#FF0000" }),
        };

        _suggestionClient
            .Setup(c => c.SuggestAsync(It.IsAny<ImageEditInput>(), It.IsAny<ProductAiContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductAiDraft { Title = "A mug", Tags = [], Metadata = metadata });

        var result = await _service.SuggestAsync(BusinessId, UserId, ImageUrl);

        result.Metadata.Should().NotBeNull();
        result.Metadata!.RootElement.TryGetProperty("sizes", out _).Should().BeFalse();
        result.Metadata!.RootElement.TryGetProperty("colors", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Rejects_an_empty_image_url_before_calling_the_provider()
    {
        var act = () => _service.SuggestAsync(BusinessId, UserId, "");

        await act.Should().ThrowAsync<Exception>();

        _suggestionClient.Verify(
            c => c.SuggestAsync(It.IsAny<ImageEditInput>(), It.IsAny<ProductAiContext>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
