using FluentValidation;
using MerchForge.api.Authorization;
using MerchForge.api.DTOs.Subscriptions;
using MerchForge.api.Services.Subscription.interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MerchForge.api.Controllers
{
    [Route("api/subscription-plans")]
    [ApiController]
    [Authorize(Policy = AuthorizationPolicies.SystemSuperAdmin)]
    public class SubscriptionPlansController : ControllerBase
    {
        private readonly ISubscriptionPlanService _subscriptionPlanService;
        private readonly IValidator<CreateSubscriptionPlanRequest> _createValidator;
        private readonly IValidator<UpdateSubscriptionPlanRequest> _updateValidator;

        public SubscriptionPlansController(
            ISubscriptionPlanService subscriptionPlanService,
            IValidator<CreateSubscriptionPlanRequest> createValidator,
            IValidator<UpdateSubscriptionPlanRequest> updateValidator)
        {
            _subscriptionPlanService = subscriptionPlanService;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }

        /// <summary>Active plans only — no auth, for the public landing/billing pages.</summary>
        [HttpGet("public")]
        [AllowAnonymous]
        public async Task<ActionResult<List<SubscriptionPlanDetailResponse>>> GetPublicPlans(
            CancellationToken cancellationToken)
        {
            var response = await _subscriptionPlanService.GetPublicAsync(cancellationToken);

            return Ok(response);
        }

        [HttpGet]
        public async Task<ActionResult<List<SubscriptionPlanResponse>>> GetAll(
            CancellationToken cancellationToken)
        {
            var response = await _subscriptionPlanService.GetAllAsync(cancellationToken);

            return Ok(response);
        }

        [HttpGet("features")]
        public async Task<ActionResult<List<FeatureResponse>>> GetFeatures(
            CancellationToken cancellationToken)
        {
            var response = await _subscriptionPlanService.GetFeaturesAsync(cancellationToken);

            return Ok(response);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<SubscriptionPlanDetailResponse>> GetById(
            Guid id,
            CancellationToken cancellationToken)
        {
            var response = await _subscriptionPlanService.GetByIdAsync(id, cancellationToken);

            return Ok(response);
        }

        [HttpPost]
        public async Task<ActionResult<SubscriptionPlanResponse>> Create(
            [FromBody] CreateSubscriptionPlanRequest request,
            CancellationToken cancellationToken)
        {
            await _createValidator.ValidateAndThrowAsync(request, cancellationToken);

            var response = await _subscriptionPlanService.CreateAsync(request, cancellationToken);

            return Ok(response);
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<SubscriptionPlanResponse>> Update(
            Guid id,
            [FromBody] UpdateSubscriptionPlanRequest request,
            CancellationToken cancellationToken)
        {
            await _updateValidator.ValidateAndThrowAsync(request, cancellationToken);

            var response = await _subscriptionPlanService.UpdateAsync(id, request, cancellationToken);

            return Ok(response);
        }

        [HttpPost("{id:guid}/deactivate")]
        public async Task<ActionResult<SubscriptionPlanResponse>> Deactivate(
            Guid id,
            CancellationToken cancellationToken)
        {
            var response = await _subscriptionPlanService.SetActiveAsync(id, false, cancellationToken);

            return Ok(response);
        }

        [HttpPost("{id:guid}/reactivate")]
        public async Task<ActionResult<SubscriptionPlanResponse>> Reactivate(
            Guid id,
            CancellationToken cancellationToken)
        {
            var response = await _subscriptionPlanService.SetActiveAsync(id, true, cancellationToken);

            return Ok(response);
        }
    }
}
