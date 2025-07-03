using Microsoft.AspNetCore.Mvc;
using DTOs.Product;
using DTOs.Common;
using Services.Interfaces;

namespace Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpGet]
        public async Task<IActionResult> GetPaged([FromQuery] ProductFilterDto filter)
        {
            var result = await _productService.GetPagedAsync(filter);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _productService.GetByIdAsync(id);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpGet("sku/{sku}")]
        public async Task<IActionResult> GetBySku(string sku)
        {
            var result = await _productService.GetBySkuAsync(sku);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        //[HttpGet("bestsellers")]
        //public async Task<IActionResult> GetBestSellers([FromQuery] int count = 10)
        //{
        //    var result = await _productService.GetBestSellersAsync(count);
        //    return result.IsSuccess ? Ok(result) : BadRequest(result);
        //}

        //[HttpGet("featured")]
        //public async Task<IActionResult> GetFeatured([FromQuery] int count = 10)
        //{
        //    var result = await _productService.GetFeaturedAsync(count);
        //    return result.IsSuccess ? Ok(result) : BadRequest(result);
        //}

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateProductDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _productService.CreateAsync(dto);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _productService.UpdateAsync(id, dto);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPost("bulk-delete")]
        public async Task<IActionResult> BulkDelete([FromBody] BatchIdsRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _productService.BulkDeleteAsync(request);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPost("bulk-restore")]
        public async Task<IActionResult> BulkRestore([FromBody] BatchIdsRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _productService.BulkRestoreAsync(request);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
    }
}