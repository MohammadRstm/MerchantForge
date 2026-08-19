using MerchForge.api.DTOs.Onboarding;
using MerchForge.api.Services.Onboarding.interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MerchForge.api.Controllers
{
    /// <summary>
    /// Read-only platform reference data used by the registration form — which
    /// business verticals exist, and which categories are already available in each
    /// — so a new business owner can see what's there before deciding whether they
    /// need to add something custom.
    ///
    /// Anonymous by design: registration itself (Auth/businessOwner/registration) is
    /// anonymous, reached from an emailed invitation link before the invitee has any
    /// account, so the form that leads up to it must be too. Everything returned here
    /// is already public: the same domains and platform categories the storefront API
    /// exposes.
    /// </summary>
    [Route("api/domains")]
    [ApiController]
    [AllowAnonymous]
    public class DomainsController : ControllerBase
    {
        private readonly IDomainService _domainService;

        public DomainsController(IDomainService domainService)
        {
            _domainService = domainService;
        }

        [HttpGet]
        public async Task<ActionResult<List<OnboardingDomainResponse>>> GetDomains(
            CancellationToken cancellationToken)
        {
            var response = await _domainService.GetDomainsAsync(cancellationToken);

            return Ok(response);
        }

        [HttpGet("{domainId:guid}/categories")]
        public async Task<ActionResult<List<OnboardingCategoryResponse>>> GetCategories(
            Guid domainId,
            CancellationToken cancellationToken)
        {
            var response = await _domainService.GetCategoriesAsync(domainId, cancellationToken);

            return Ok(response);
        }

        /// <summary>
        /// The optional product fields businesses in this domain can opt into, shown
        /// as checkboxes during registration. The selection is snapshotted into the
        /// business's metadata shape and drives what its product form asks for.
        /// </summary>
        [HttpGet("{domainId:guid}/product-attributes")]
        public async Task<ActionResult<List<OnboardingProductAttributeResponse>>> GetProductAttributes(
            Guid domainId,
            CancellationToken cancellationToken)
        {
            var response = await _domainService.GetProductAttributesAsync(domainId, cancellationToken);

            return Ok(response);
        }
    }
}
