using MerchForge.api.Authorization;
using MerchForge.api.DTOs.Subscriptions;
using FluentValidation;
using MerchForge.api.Services.Subscription.interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MerchForge.api.Controllers
{
    /// <summary>
    /// Buying features independently of a subscription plan. Separate from
    /// SubscriptionsController's plan management: a plan changes what a business is
    /// entitled to as a whole, this changes one feature's credit balance without
    /// touching the plan at all.
    /// </summary>
    [Route("api/businesses/{businessId:guid}/dashboard/features")]
    [ApiController]
    [Authorize(Policy = AuthorizationPolicies.BusinessOwner)]
    public class FeaturesController : ControllerBase
    {
        private readonly IFeatureCreditService _featureCreditService;
        private readonly IValidator<PurchaseFeatureCreditsRequest> _purchaseValidator;

        public FeaturesController(
            IFeatureCreditService featureCreditService,
            IValidator<PurchaseFeatureCreditsRequest> purchaseValidator)
        {
            _featureCreditService = featureCreditService;
            _purchaseValidator = purchaseValidator;
        }

        [HttpGet]
        public async Task<ActionResult<List<FeatureCreditOverviewResponse>>> GetOverview(
            Guid businessId,
            CancellationToken cancellationToken)
        {
            var response = await _featureCreditService.GetOverviewAsync(businessId, cancellationToken);

            return Ok(response);
        }

        [HttpPost("purchases")]
        public async Task<ActionResult<BusinessFeatureCreditResponse>> Purchase(
            Guid businessId,
            [FromBody] PurchaseFeatureCreditsRequest request,
            CancellationToken cancellationToken)
        {
            await _purchaseValidator.ValidateAndThrowAsync(request, cancellationToken);

            var response = await _featureCreditService.PurchaseAsync(
                businessId, request.PackageId, cancellationToken);

            return Ok(response);
        }
    }
}
