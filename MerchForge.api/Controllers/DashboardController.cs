using System.Security.Claims;
using FluentValidation;
using MerchForge.api.Authorization;
using MerchForge.api.DTOs.BusinessDashboard;
using MerchForge.api.DTOs.Common;
using MerchForge.api.DTOs.Dashboard;
using MerchForge.api.DTOs.WebsiteTemplateRequests;
using MerchForge.api.Services.Dashboard.interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MerchForge.api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = AuthorizationPolicies.SystemSuperAdmin)]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;
        private readonly IValidator<UsersQueryRequest> _usersQueryValidator;
        private readonly IValidator<BusinessesQueryRequest> _businessesQueryValidator;
        private readonly IValidator<CustomersQueryRequest> _customersQueryValidator;
        private readonly IValidator<CreateWebsiteTemplateRequest> _createWebsiteTemplateValidator;
        private readonly IValidator<UpdateWebsiteTemplateRequest> _updateWebsiteTemplateValidator;
        private readonly IValidator<WebsiteTemplateRequestsQueryRequest> _websiteTemplateRequestsQueryValidator;
        private readonly IValidator<CloseWebsiteTemplateRequestRequest> _closeWebsiteTemplateRequestValidator;
        private readonly IValidator<UpdateMetadataShapeRequest> _updateMetadataShapeValidator;
        private readonly IValidator<CreateProductAttributeDefinitionRequest> _createAttributeDefinitionValidator;
        private readonly IValidator<UpdateProductAttributeDefinitionRequest> _updateAttributeDefinitionValidator;
        private readonly IValidator<CreateWebsiteTemplateCustomizableComponentRequest> _createCustomizableComponentValidator;
        private readonly IValidator<UpdateWebsiteTemplateCustomizableComponentRequest> _updateCustomizableComponentValidator;

        public DashboardController(
            IDashboardService dashboardService,
            IValidator<UsersQueryRequest> usersQueryValidator,
            IValidator<BusinessesQueryRequest> businessesQueryValidator,
            IValidator<CustomersQueryRequest> customersQueryValidator,
            IValidator<CreateWebsiteTemplateRequest> createWebsiteTemplateValidator,
            IValidator<UpdateWebsiteTemplateRequest> updateWebsiteTemplateValidator,
            IValidator<WebsiteTemplateRequestsQueryRequest> websiteTemplateRequestsQueryValidator,
            IValidator<CloseWebsiteTemplateRequestRequest> closeWebsiteTemplateRequestValidator,
            IValidator<UpdateMetadataShapeRequest> updateMetadataShapeValidator,
            IValidator<CreateProductAttributeDefinitionRequest> createAttributeDefinitionValidator,
            IValidator<UpdateProductAttributeDefinitionRequest> updateAttributeDefinitionValidator,
            IValidator<CreateWebsiteTemplateCustomizableComponentRequest> createCustomizableComponentValidator,
            IValidator<UpdateWebsiteTemplateCustomizableComponentRequest> updateCustomizableComponentValidator)
        {
            _dashboardService = dashboardService;
            _usersQueryValidator = usersQueryValidator;
            _businessesQueryValidator = businessesQueryValidator;
            _customersQueryValidator = customersQueryValidator;
            _updateWebsiteTemplateValidator = updateWebsiteTemplateValidator;
            _createWebsiteTemplateValidator = createWebsiteTemplateValidator;
            _websiteTemplateRequestsQueryValidator = websiteTemplateRequestsQueryValidator;
            _closeWebsiteTemplateRequestValidator = closeWebsiteTemplateRequestValidator;
            _updateMetadataShapeValidator = updateMetadataShapeValidator;
            _createAttributeDefinitionValidator = createAttributeDefinitionValidator;
            _updateAttributeDefinitionValidator = updateAttributeDefinitionValidator;
            _createCustomizableComponentValidator = createCustomizableComponentValidator;
            _updateCustomizableComponentValidator = updateCustomizableComponentValidator;
        }

        [HttpGet("stats")]
        public async Task<ActionResult<DashboardStatsResponse>> GetStats(
            CancellationToken cancellationToken)
        {
            var response = await _dashboardService.GetPlatformStatsAsync(cancellationToken);

            return Ok(response);
        }

        [HttpGet("users")]
        public async Task<ActionResult<PagedResult<DashboardUserResponse>>> GetUsers(
            [FromQuery] UsersQueryRequest query,
            CancellationToken cancellationToken)
        {
            await _usersQueryValidator.ValidateAndThrowAsync(query, cancellationToken);

            var response = await _dashboardService.GetUsersAsync(query, cancellationToken);

            return Ok(response);
        }

        [HttpPost("users/{userId:guid}/revoke-sessions")]
        public async Task<ActionResult<RevokeUserSessionsResponse>> RevokeUserSessions(
            Guid userId,
            CancellationToken cancellationToken)
        {
            var actingUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(actingUserId, out var parsedActingUserId))
            {
                return Unauthorized();
            }

            var response = await _dashboardService.RevokeUserSessionsAsync(
                userId,
                parsedActingUserId,
                cancellationToken);

            return Ok(response);
        }

        [HttpGet("businesses")]
        public async Task<ActionResult<PagedResult<DashboardBusinessResponse>>> GetBusinesses(
            [FromQuery] BusinessesQueryRequest query,
            CancellationToken cancellationToken)
        {
            await _businessesQueryValidator.ValidateAndThrowAsync(query, cancellationToken);

            var response = await _dashboardService.GetBusinessesAsync(query, cancellationToken);

            return Ok(response);
        }

        [HttpGet("businesses/{businessId:guid}")]
        public async Task<ActionResult<BusinessDetailResponse>> GetBusinessDetail(
            Guid businessId,
            CancellationToken cancellationToken)
        {
            var response = await _dashboardService.GetBusinessDetailAsync(businessId, cancellationToken);

            return Ok(response);
        }

        [HttpPost("businesses/{businessId:guid}/revoke-sessions")]
        public async Task<ActionResult<RevokeUserSessionsResponse>> RevokeBusinessSessions(
            Guid businessId,
            CancellationToken cancellationToken)
        {
            var response = await _dashboardService.RevokeBusinessSessionsAsync(businessId, cancellationToken);

            return Ok(response);
        }

        [HttpGet("businesses/{businessId:guid}/metadata-shape")]
        public async Task<ActionResult<List<ProductFormFieldResponse>>> GetBusinessMetadataShape(
            Guid businessId,
            CancellationToken cancellationToken)
        {
            var response = await _dashboardService.GetBusinessMetadataShapeAsync(businessId, cancellationToken);

            return Ok(response);
        }

        [HttpPut("businesses/{businessId:guid}/metadata-shape")]
        public async Task<ActionResult<List<ProductFormFieldResponse>>> UpdateBusinessMetadataShape(
            Guid businessId,
            [FromBody] UpdateMetadataShapeRequest request,
            CancellationToken cancellationToken)
        {
            await _updateMetadataShapeValidator.ValidateAndThrowAsync(request, cancellationToken);

            var response = await _dashboardService.UpdateBusinessMetadataShapeAsync(businessId, request, cancellationToken);

            return Ok(response);
        }

        // ---- product attribute definitions (domain field catalogue) ----

        [HttpGet("product-attributes")]
        public async Task<ActionResult<List<ProductAttributeDefinitionResponse>>> GetProductAttributeDefinitions(
            [FromQuery] Guid? businessDomainId,
            CancellationToken cancellationToken)
        {
            var response = await _dashboardService.GetAttributeDefinitionsAsync(businessDomainId, cancellationToken);

            return Ok(response);
        }

        [HttpPost("product-attributes")]
        public async Task<ActionResult<ProductAttributeDefinitionResponse>> CreateProductAttributeDefinition(
            [FromBody] CreateProductAttributeDefinitionRequest request,
            CancellationToken cancellationToken)
        {
            await _createAttributeDefinitionValidator.ValidateAndThrowAsync(request, cancellationToken);

            var response = await _dashboardService.CreateAttributeDefinitionAsync(request, cancellationToken);

            return Ok(response);
        }

        [HttpPut("product-attributes/{id:guid}")]
        public async Task<ActionResult<ProductAttributeDefinitionResponse>> UpdateProductAttributeDefinition(
            Guid id,
            [FromBody] UpdateProductAttributeDefinitionRequest request,
            CancellationToken cancellationToken)
        {
            await _updateAttributeDefinitionValidator.ValidateAndThrowAsync(request, cancellationToken);

            var response = await _dashboardService.UpdateAttributeDefinitionAsync(id, request, cancellationToken);

            return Ok(response);
        }

        [HttpPost("product-attributes/{id:guid}/deactivate")]
        public async Task<ActionResult<ProductAttributeDefinitionResponse>> DeactivateProductAttributeDefinition(
            Guid id,
            CancellationToken cancellationToken)
        {
            var response = await _dashboardService.SetAttributeDefinitionActiveAsync(id, false, cancellationToken);

            return Ok(response);
        }

        [HttpPost("product-attributes/{id:guid}/reactivate")]
        public async Task<ActionResult<ProductAttributeDefinitionResponse>> ReactivateProductAttributeDefinition(
            Guid id,
            CancellationToken cancellationToken)
        {
            var response = await _dashboardService.SetAttributeDefinitionActiveAsync(id, true, cancellationToken);

            return Ok(response);
        }

        // ---- website template customizable components (per-template capability catalogue) ----

        [HttpGet("website-templates/{websiteTemplateId:guid}/customizable-components")]
        public async Task<ActionResult<List<WebsiteTemplateCustomizableComponentResponse>>> GetCustomizableComponents(
            Guid websiteTemplateId,
            CancellationToken cancellationToken)
        {
            var response = await _dashboardService.GetCustomizableComponentsAsync(websiteTemplateId, cancellationToken);

            return Ok(response);
        }

        [HttpPost("website-templates/{websiteTemplateId:guid}/customizable-components")]
        public async Task<ActionResult<WebsiteTemplateCustomizableComponentResponse>> CreateCustomizableComponent(
            Guid websiteTemplateId,
            [FromBody] CreateWebsiteTemplateCustomizableComponentRequest request,
            CancellationToken cancellationToken)
        {
            request.WebsiteTemplateId = websiteTemplateId;

            await _createCustomizableComponentValidator.ValidateAndThrowAsync(request, cancellationToken);

            var response = await _dashboardService.CreateCustomizableComponentAsync(request, cancellationToken);

            return Ok(response);
        }

        [HttpPut("website-templates/{websiteTemplateId:guid}/customizable-components/{id:guid}")]
        public async Task<ActionResult<WebsiteTemplateCustomizableComponentResponse>> UpdateCustomizableComponent(
            Guid websiteTemplateId,
            Guid id,
            [FromBody] UpdateWebsiteTemplateCustomizableComponentRequest request,
            CancellationToken cancellationToken)
        {
            await _updateCustomizableComponentValidator.ValidateAndThrowAsync(request, cancellationToken);

            var response = await _dashboardService.UpdateCustomizableComponentAsync(id, request, cancellationToken);

            return Ok(response);
        }

        [HttpPost("website-templates/{websiteTemplateId:guid}/customizable-components/{id:guid}/deactivate")]
        public async Task<ActionResult<WebsiteTemplateCustomizableComponentResponse>> DeactivateCustomizableComponent(
            Guid websiteTemplateId,
            Guid id,
            CancellationToken cancellationToken)
        {
            var response = await _dashboardService.SetCustomizableComponentActiveAsync(id, false, cancellationToken);

            return Ok(response);
        }

        [HttpPost("website-templates/{websiteTemplateId:guid}/customizable-components/{id:guid}/reactivate")]
        public async Task<ActionResult<WebsiteTemplateCustomizableComponentResponse>> ReactivateCustomizableComponent(
            Guid websiteTemplateId,
            Guid id,
            CancellationToken cancellationToken)
        {
            var response = await _dashboardService.SetCustomizableComponentActiveAsync(id, true, cancellationToken);

            return Ok(response);
        }

        // ---- website templates ----

        [HttpGet("website-templates")]
        public async Task<ActionResult<List<WebsiteTemplateResponse>>> GetWebsiteTemplates(
            CancellationToken cancellationToken)
        {
            var response = await _dashboardService.GetWebsiteTemplatesAsync(cancellationToken);

            return Ok(response);
        }

        [HttpPost("website-templates")]
        public async Task<ActionResult<WebsiteTemplateResponse>> CreateWebsiteTemplate(
            [FromBody] CreateWebsiteTemplateRequest request,
            CancellationToken cancellationToken)
        {
            await _createWebsiteTemplateValidator.ValidateAndThrowAsync(request, cancellationToken);

            var response = await _dashboardService.CreateWebsiteTemplateAsync(request, cancellationToken);

            return Ok(response);
        }

        [HttpPost("website-templates/image")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<ActionResult<UploadWebsiteTemplateImageResponse>> UploadWebsiteTemplateImage(
            IFormFile file,
            CancellationToken cancellationToken)
        {
            var imageUrl = await _dashboardService.UploadWebsiteTemplateImageAsync(file, cancellationToken);

            return Ok(new UploadWebsiteTemplateImageResponse { ImageUrl = imageUrl });
        }

        [HttpGet("website-templates/{websiteTemplateId:guid}")]
        public async Task<ActionResult<WebsiteTemplateDetailResponse>> GetWebsiteTemplate(
            Guid websiteTemplateId,
            CancellationToken cancellationToken)
        {
            var response = await _dashboardService.GetWebsiteTemplateDetailAsync(websiteTemplateId, cancellationToken);

            return Ok(response);
        }

        [HttpPut("website-templates/{websiteTemplateId:guid}")]
        public async Task<ActionResult<WebsiteTemplateResponse>> UpdateWebsiteTemplate(
            Guid websiteTemplateId,
            [FromBody] UpdateWebsiteTemplateRequest request,
            CancellationToken cancellationToken)
        {
            await _updateWebsiteTemplateValidator.ValidateAndThrowAsync(request, cancellationToken);

            var response = await _dashboardService.UpdateWebsiteTemplateAsync(websiteTemplateId, request, cancellationToken);

            return Ok(response);
        }

        [HttpPost("website-templates/{websiteTemplateId:guid}/deactivate")]
        public async Task<ActionResult<WebsiteTemplateResponse>> DeactivateWebsiteTemplate(
            Guid websiteTemplateId,
            CancellationToken cancellationToken)
        {
            var response = await _dashboardService.DeactivateWebsiteTemplateAsync(websiteTemplateId, cancellationToken);

            return Ok(response);
        }

        // ---- customers ----

        [HttpGet("customers")]
        public async Task<ActionResult<PagedResult<DashboardCustomerResponse>>> GetCustomers(
            [FromQuery] CustomersQueryRequest query,
            CancellationToken cancellationToken)
        {
            await _customersQueryValidator.ValidateAndThrowAsync(query, cancellationToken);

            var response = await _dashboardService.GetCustomersAsync(query, cancellationToken);

            return Ok(response);
        }

        [HttpGet("customers/{customerId:guid}")]
        public async Task<ActionResult<DashboardCustomerDetailResponse>> GetCustomerDetail(
            Guid customerId,
            CancellationToken cancellationToken)
        {
            var response = await _dashboardService.GetCustomerDetailAsync(customerId, cancellationToken);

            return Ok(response);
        }

        // ---- website template requests ----

        [HttpGet("website-template-requests")]
        public async Task<ActionResult<PagedResult<WebsiteTemplateRequestSummaryResponse>>> GetWebsiteTemplateRequests(
            [FromQuery] WebsiteTemplateRequestsQueryRequest query,
            CancellationToken cancellationToken)
        {
            await _websiteTemplateRequestsQueryValidator.ValidateAndThrowAsync(query, cancellationToken);

            var response = await _dashboardService.GetWebsiteTemplateRequestsAsync(query, cancellationToken);

            return Ok(response);
        }

        [HttpGet("website-template-requests/{websiteTemplateRequestId:guid}")]
        public async Task<ActionResult<WebsiteTemplateRequestDetailResponse>> GetWebsiteTemplateRequest(
            Guid websiteTemplateRequestId,
            CancellationToken cancellationToken)
        {
            var response = await _dashboardService.GetWebsiteTemplateRequestAsync(websiteTemplateRequestId, cancellationToken);

            return Ok(response);
        }

        [HttpPost("website-template-requests/{websiteTemplateRequestId:guid}/start-build")]
        public async Task<ActionResult<WebsiteTemplateRequestDetailResponse>> StartWebsiteTemplateRequestBuild(
            Guid websiteTemplateRequestId,
            CancellationToken cancellationToken)
        {
            var response = await _dashboardService.StartWebsiteTemplateRequestBuildAsync(
                websiteTemplateRequestId, cancellationToken);

            return Ok(response);
        }

        [HttpPost("website-template-requests/{websiteTemplateRequestId:guid}/close")]
        public async Task<ActionResult<WebsiteTemplateRequestDetailResponse>> CloseWebsiteTemplateRequest(
            Guid websiteTemplateRequestId,
            [FromBody] CloseWebsiteTemplateRequestRequest request,
            CancellationToken cancellationToken)
        {
            await _closeWebsiteTemplateRequestValidator.ValidateAndThrowAsync(request, cancellationToken);

            var closedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(closedByUserId, out var parsedClosedByUserId))
            {
                return Unauthorized();
            }

            var response = await _dashboardService.CloseWebsiteTemplateRequestAsync(
                websiteTemplateRequestId,
                parsedClosedByUserId,
                request,
                cancellationToken);

            return Ok(response);
        }
    }
}
