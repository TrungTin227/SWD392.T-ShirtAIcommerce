using DTOs.Files;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FileUploadController : ControllerBase
    {
        private readonly IFileUploadService _fileUploadService;

        public FileUploadController(IFileUploadService fileUploadService)
        {
            _fileUploadService = fileUploadService;
        }

        [HttpPost("single")]
        [Authorize]
        public async Task<IActionResult> UploadSingleFile([FromForm] IFormFile file, [FromForm] FileUploadRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (file == null || file.Length == 0)
                return BadRequest("File is required");

            var result = await _fileUploadService.UploadFileAsync(file, request);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPost("multiple")]
        [Authorize]
        public async Task<IActionResult> UploadMultipleFiles([FromForm] List<IFormFile> files, [FromForm] FileUploadRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (files == null || !files.Any())
                return BadRequest("Files are required");

            var result = await _fileUploadService.UploadMultipleFilesAsync(files, request);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpGet("{fileId}")]
        public async Task<IActionResult> GetFileInfo(string fileId)
        {
            var result = await _fileUploadService.GetFileInfoAsync(fileId);
            return result.IsSuccess ? Ok(result) : NotFound(result);
        }

        [HttpGet("{fileId}/download")]
        public async Task<IActionResult> DownloadFile(string fileId)
        {
            var fileInfoResult = await _fileUploadService.GetFileInfoAsync(fileId);
            if (!fileInfoResult.IsSuccess || fileInfoResult.Data == null)
                return NotFound(fileInfoResult);

            var contentResult = await _fileUploadService.GetFileContentAsync(fileId);
            if (!contentResult.IsSuccess || contentResult.Data == null)
                return NotFound(contentResult);

            return File(contentResult.Data, fileInfoResult.Data.MimeType, fileInfoResult.Data.FileName);
        }

        [HttpDelete("{fileId}")]
        [Authorize]
        public async Task<IActionResult> DeleteFile(string fileId)
        {
            var result = await _fileUploadService.DeleteFileAsync(fileId);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPost("{fileId}/resize")]
        [Authorize]
        public async Task<IActionResult> ResizeImage(string fileId, [FromBody] ImageResizeOptions options)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _fileUploadService.ResizeImageAsync(fileId, options);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpGet("type/{fileType}")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetFilesByType(FileType fileType)
        {
            var result = await _fileUploadService.GetFilesByTypeAsync(fileType);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPost("validate")]
        public async Task<IActionResult> ValidateFile([FromForm] IFormFile file, [FromForm] FileType fileType)
        {
            if (file == null)
                return BadRequest("File is required");

            var result = await _fileUploadService.ValidateFileAsync(file, fileType);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPost("{fileId}/presigned-url")]
        [Authorize]
        public async Task<IActionResult> GeneratePreSignedUrl(string fileId, [FromQuery] int expirationHours = 1)
        {
            var expiration = TimeSpan.FromHours(expirationHours);
            var result = await _fileUploadService.GeneratePreSignedUrlAsync(fileId, expiration);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPost("{fileId}/move")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> MoveFile(string fileId, [FromBody] string newPath)
        {
            if (string.IsNullOrWhiteSpace(newPath))
                return BadRequest("New path is required");

            var result = await _fileUploadService.MoveFileAsync(fileId, newPath);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPost("product-images")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> UploadProductImages([FromForm] List<IFormFile> files, [FromForm] string? description)
        {
            if (files == null || !files.Any())
                return BadRequest("Files are required");

            var request = new FileUploadRequestDto
            {
                FileType = FileType.ProductImage,
                Description = description
            };

            var result = await _fileUploadService.UploadMultipleFilesAsync(files, request);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPost("design-images")]
        [Authorize]
        public async Task<IActionResult> UploadDesignImages([FromForm] List<IFormFile> files, [FromForm] string? description)
        {
            if (files == null || !files.Any())
                return BadRequest("Files are required");

            var request = new FileUploadRequestDto
            {
                FileType = FileType.DesignImage,
                Description = description
            };

            var result = await _fileUploadService.UploadMultipleFilesAsync(files, request);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPost("avatar")]
        [Authorize]
        public async Task<IActionResult> UploadUserAvatar([FromForm] IFormFile file, [FromForm] string? description)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is required");

            var request = new FileUploadRequestDto
            {
                FileType = FileType.UserAvatar,
                Description = description
            };

            var result = await _fileUploadService.UploadFileAsync(file, request);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpGet("health")]
        public IActionResult HealthCheck()
        {
            return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
        }
    }
}