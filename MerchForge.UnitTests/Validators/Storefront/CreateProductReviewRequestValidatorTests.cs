using FluentAssertions;
using MerchForge.api.DTOs.Storefront;
using MerchForge.api.Validators.Storefront;

namespace MerchForge.UnitTests.Validators.Storefront;

public class CreateProductReviewRequestValidatorTests
{
    private readonly CreateProductReviewRequestValidator _validator = new();

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void Accepts_every_rating_in_range(int rating)
    {
        var result = _validator.Validate(Request(rating));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    public void Rejects_a_rating_outside_one_to_five(int rating)
    {
        var result = _validator.Validate(Request(rating));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateProductReviewRequest.Rating));
    }

    [Fact]
    public void Rejects_a_request_with_no_rating_at_all()
    {
        // An omitted Rating deserialises to 0, which is what makes the range rule double
        // as a required check — there is no separate NotEmpty for it.
        var result = _validator.Validate(new CreateProductReviewRequest { Comment = "Words but no stars." });

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Accepts_a_rating_with_no_comment()
    {
        var result = _validator.Validate(Request(4, comment: null));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Rejects_a_comment_longer_than_the_column_allows()
    {
        var result = _validator.Validate(Request(4, new string('x', 2001)));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateProductReviewRequest.Comment));
    }

    [Fact]
    public void Accepts_a_comment_at_exactly_the_maximum_length()
    {
        var result = _validator.Validate(Request(4, new string('x', 2000)));

        result.IsValid.Should().BeTrue();
    }

    private static CreateProductReviewRequest Request(int rating, string? comment = null) =>
        new() { Rating = rating, Comment = comment };
}
