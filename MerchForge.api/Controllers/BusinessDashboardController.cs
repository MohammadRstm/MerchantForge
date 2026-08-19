using FluentValidation;
using MerchForge.api.Authorization;
using MerchForge.api.DTOs.BusinessDashboard;
using MerchForge.api.DTOs.Common;
using MerchForge.api.Services.BusinessDashboard.interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MerchForge.api.Controllers
{
    [Route("api/businesses/{businessId:guid}/dashboard")]
    [ApiController]
    [Authorize(Policy = AuthorizationPolicies.BusinessOwner)]
    public class BusinessDashboardController : ControllerBase
    {
        private readonly IBusinessDashboardService _businessDashboardService;
        private readonly IValidator<ProductsQueryRequest> _productsQueryValidator;

        public BusinessDashboardController(
            IBusinessDashboardService businessDashboardService,
            IValidator<ProductsQueryRequest> productsQueryValidator)
        {
            _businessDashboardService = businessDashboardService;
            _productsQueryValidator = productsQueryValidator;
        }

        [HttpGet("stats")]
        public async Task<ActionResult<BusinessDashboardStatsResponse>> GetStats(
            Guid businessId,
            CancellationToken cancellationToken)
        {
            var response = await _businessDashboardService.GetStatsAsync(businessId, cancellationToken);

            return Ok(response);
        }

        [HttpGet("products")]
        public async Task<ActionResult<PagedResult<BusinessProductResponse>>> GetProducts(
            Guid businessId,
            [FromQuery] ProductsQueryRequest query,
            CancellationToken cancellationToken)
        {
            await _productsQueryValidator.ValidateAndThrowAsync(query, cancellationToken);

            var response = await _businessDashboardService.GetProductsAsync(businessId, query, cancellationToken);

            return Ok(response);
        }

        [HttpGet("members")]
        public async Task<ActionResult<List<BusinessMemberResponse>>> GetMembers(
            Guid businessId,
            CancellationToken cancellationToken)
        {
            var response = await _businessDashboardService.GetMembersAsync(businessId, cancellationToken);

            return Ok(response);
        }

        [HttpGet("subscription")]
        public async Task<ActionResult<BusinessSubscriptionResponse?>> GetSubscription(
            Guid businessId,
            CancellationToken cancellationToken)
        {
            var response = await _businessDashboardService.GetSubscriptionAsync(businessId, cancellationToken);

            return Ok(response);
        }
    }
}
