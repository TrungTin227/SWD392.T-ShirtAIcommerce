using DTOs.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProductVariantController : ControllerBase
    {
        private readonly IProductVariantService _productVariantService;

        public ProductVariantController(IProductVariantService productVariantService)
        {
            _productVariantService = productVariantService;
        }

        /// <summary>
        /// Create a new product variant
        /// </summary>
        /// <param name="dto">Product variant creation data</param>
        /// <returns>Created product variant</returns>
        [HttpPost]
        public async Task<IActionResult> CreateAsync([FromBody] ProductVariantCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _productVariantService.CreateAsync(dto);

            if (result.IsSuccess)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }


        /// <summary>
        /// Update an existing product variant
        /// </summary>
        /// <param name="id">Product variant ID</param>
        /// <param name="dto">Product variant update data</param>
        /// <returns>Updated product variant</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAsync(Guid id, [FromBody] ProductVariantUpdateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != dto.Id)
            {
                return BadRequest("ID mismatch");
            }

            var result = await _productVariantService.UpdateAsync(dto);

            if (result.IsSuccess)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }

        /// <summary>
        /// Delete a product variant
        /// </summary>
        /// <param name="id">Product variant ID</param>
        /// <returns>Deletion result</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAsync(Guid id)
        {
            var result = await _productVariantService.DeleteAsync(id);

            if (result.IsSuccess)
            {
                return Ok(result);
            }

            return NotFound(result);
        }

        /// <summary>
        /// Get a product variant by ID
        /// </summary>
        /// <param name="id">Product variant ID</param>
        /// <returns>Product variant details</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetByIdAsync(Guid id)
        {
            var result = await _productVariantService.GetByIdAsync(id);

            if (result.IsSuccess)
            {
                return Ok(result);
            }

            return NotFound(result);
        }

        /// <summary>
        /// Get all variants for a specific product
        /// </summary>
        /// <param name="productId">Product ID</param>
        /// <returns>List of product variants</returns>
        [HttpGet("product/{productId}")]
        public async Task<IActionResult> GetVariantsByProductIdAsync(Guid productId)
        {
            var result = await _productVariantService.GetVariantsByProductIdAsync(productId);

            if (result.IsSuccess)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }

        /// <summary>
        /// Get paged variants for a specific product
        /// </summary>
        /// <param name="productId">Product ID</param>
        /// <param name="pageNumber">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 10)</param>
        /// <returns>Paged list of product variants</returns>
        [HttpGet("product/{productId}/paged")]
        public async Task<IActionResult> GetPagedVariantsByProductIdAsync(
            Guid productId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            if (pageNumber < 1 || pageSize < 1)
            {
                return BadRequest("Page number and page size must be greater than 0");
            }

            var result = await _productVariantService.GetPagedVariantsByProductIdAsync(productId, pageNumber, pageSize);

            if (result.IsSuccess)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }

        /// <summary>
        /// Get paged list of all product variants
        /// </summary>
        /// <param name="pageNumber">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 10)</param>
        /// <returns>Paged list of product variants</returns>
        [HttpGet("paged")]
        public async Task<IActionResult> GetPagedAsync(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            if (pageNumber < 1 || pageSize < 1)
            {
                return BadRequest("Page number and page size must be greater than 0");
            }

            var result = await _productVariantService.GetPagedAsync(pageNumber, pageSize);

            if (result.IsSuccess)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }

        /// <summary>
        /// Bulk create product variants
        /// </summary>
        /// <param name="dtos">List of product variant creation data</param>
        /// <returns>Bulk creation result</returns>
        [HttpPost("bulk")]
        public async Task<IActionResult> BulkCreateAsync([FromBody] IEnumerable<ProductVariantCreateDto> dtos)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!dtos.Any())
            {
                return BadRequest("At least one product variant is required");
            }

            var result = await _productVariantService.BulkCreateAsync(dtos);

            if (result.IsSuccess)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }

        /// <summary>
        /// Bulk update product variants
        /// </summary>
        /// <param name="dtos">List of product variant update data</param>
        /// <returns>Bulk update result</returns>
        [HttpPut("bulk")]
        public async Task<IActionResult> BulkUpdateAsync([FromBody] IEnumerable<ProductVariantUpdateDto> dtos)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!dtos.Any())
            {
                return BadRequest("At least one product variant is required");
            }

            var result = await _productVariantService.BulkUpdateAsync(dtos);

            if (result.IsSuccess)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }

        /// <summary>
        /// Bulk delete product variants
        /// </summary>
        /// <param name="ids">List of product variant IDs to delete</param>
        /// <returns>Bulk deletion result</returns>
        [HttpDelete("bulk")]
        public async Task<IActionResult> BulkDeleteAsync([FromBody] IEnumerable<Guid> ids)
        {
            if (!ids.Any())
            {
                return BadRequest("At least one product variant ID is required");
            }

            var result = await _productVariantService.BulkDeleteAsync(ids);

            if (result.IsSuccess)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }
    }
}