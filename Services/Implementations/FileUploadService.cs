using DTOs.Files;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Repositories.Commons;
using Services.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace Services.Implementations
{
    public class FileUploadService : IFileUploadService
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<FileUploadService> _logger;
        
        private readonly string _uploadPath;
        private readonly long _maxFileSize;
        private readonly string[] _allowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
        private readonly string[] _allowedDocumentExtensions = { ".pdf", ".doc", ".docx", ".txt" };
        private readonly Dictionary<FileType, string[]> _allowedExtensions;

        public FileUploadService(
            IConfiguration configuration, 
            IWebHostEnvironment environment,
            ILogger<FileUploadService> logger)
        {
            _configuration = configuration;
            _environment = environment;
            _logger = logger;
            
            _uploadPath = Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, "uploads");
            _maxFileSize = configuration.GetValue<long>("FileUpload:MaxFileSize", 10 * 1024 * 1024); // 10MB default

            _allowedExtensions = new Dictionary<FileType, string[]>
            {
                { FileType.ProductImage, _allowedImageExtensions },
                { FileType.DesignImage, _allowedImageExtensions },
                { FileType.UserAvatar, _allowedImageExtensions },
                { FileType.Document, _allowedDocumentExtensions },
                { FileType.Other, _allowedImageExtensions.Concat(_allowedDocumentExtensions).ToArray() }
            };

            // Ensure upload directory exists
            if (!Directory.Exists(_uploadPath))
            {
                Directory.CreateDirectory(_uploadPath);
            }
        }

        public async Task<ApiResult<FileResponseDto>> UploadFileAsync(IFormFile file, FileUploadRequestDto request)
        {
            try
            {
                var validationResult = await ValidateFileAsync(file, request.FileType);
                if (!validationResult.IsSuccess)
                {
                    return ApiResult<FileResponseDto>.Failure(validationResult.Message);
                }

                var fileId = GenerateFileId();
                var fileName = GenerateFileName(file.FileName, fileId);
                var typePath = GetTypeSpecificPath(request.FileType);
                var fullPath = Path.Combine(typePath, fileName);

                // Ensure type-specific directory exists
                if (!Directory.Exists(typePath))
                {
                    Directory.CreateDirectory(typePath);
                }

                // Save file
                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var fileResponse = new FileResponseDto
                {
                    FileId = fileId,
                    FileName = file.FileName,
                    FileUrl = GenerateFileUrl(request.FileType, fileName),
                    FileType = request.FileType,
                    FileSize = file.Length,
                    MimeType = file.ContentType,
                    UploadedAt = DateTime.UtcNow,
                    Description = request.Description,
                    AltText = request.AltText
                };

                _logger.LogInformation("File uploaded successfully: {FileId}", fileId);
                return ApiResult<FileResponseDto>.Success(fileResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file");
                return ApiResult<FileResponseDto>.Failure($"Error uploading file: {ex.Message}");
            }
        }

        public async Task<ApiResult<List<FileResponseDto>>> UploadMultipleFilesAsync(List<IFormFile> files, FileUploadRequestDto request)
        {
            try
            {
                var results = new List<FileResponseDto>();
                var errors = new List<string>();

                foreach (var file in files)
                {
                    var result = await UploadFileAsync(file, request);
                    if (result.IsSuccess && result.Data != null)
                    {
                        results.Add(result.Data);
                    }
                    else
                    {
                        errors.Add($"Failed to upload {file.FileName}: {result.Message}");
                    }
                }

                if (errors.Any() && !results.Any())
                {
                    return ApiResult<List<FileResponseDto>>.Failure($"All uploads failed: {string.Join(", ", errors)}");
                }

                if (errors.Any())
                {
                    _logger.LogWarning("Some files failed to upload: {Errors}", string.Join(", ", errors));
                }

                return ApiResult<List<FileResponseDto>>.Success(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading multiple files");
                return ApiResult<List<FileResponseDto>>.Failure($"Error uploading files: {ex.Message}");
            }
        }

        public async Task<ApiResult<bool>> DeleteFileAsync(string fileId)
        {
            try
            {
                var fileInfo = await GetFileInfoAsync(fileId);
                if (!fileInfo.IsSuccess || fileInfo.Data == null)
                {
                    return ApiResult<bool>.Failure("File not found");
                }

                var filePath = GetFilePathFromUrl(fileInfo.Data.FileUrl);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    _logger.LogInformation("File deleted successfully: {FileId}", fileId);
                    return ApiResult<bool>.Success(true);
                }

                return ApiResult<bool>.Failure("File not found on disk");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file: {FileId}", fileId);
                return ApiResult<bool>.Failure($"Error deleting file: {ex.Message}");
            }
        }

        public async Task<ApiResult<FileResponseDto>> GetFileInfoAsync(string fileId)
        {
            try
            {
                // This is a simplified implementation. In a real application,
                // you would store file metadata in a database
                var allFiles = await GetAllFilesFromDisk();
                var file = allFiles.FirstOrDefault(f => f.FileId == fileId);
                
                if (file == null)
                {
                    return ApiResult<FileResponseDto>.Failure("File not found");
                }

                return ApiResult<FileResponseDto>.Success(file);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting file info: {FileId}", fileId);
                return ApiResult<FileResponseDto>.Failure($"Error getting file info: {ex.Message}");
            }
        }

        public async Task<ApiResult<byte[]>> GetFileContentAsync(string fileId)
        {
            try
            {
                var fileInfo = await GetFileInfoAsync(fileId);
                if (!fileInfo.IsSuccess || fileInfo.Data == null)
                {
                    return ApiResult<byte[]>.Failure("File not found");
                }

                var filePath = GetFilePathFromUrl(fileInfo.Data.FileUrl);
                if (!File.Exists(filePath))
                {
                    return ApiResult<byte[]>.Failure("File not found on disk");
                }

                var content = await File.ReadAllBytesAsync(filePath);
                return ApiResult<byte[]>.Success(content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting file content: {FileId}", fileId);
                return ApiResult<byte[]>.Failure($"Error getting file content: {ex.Message}");
            }
        }

        public async Task<ApiResult<FileResponseDto>> ResizeImageAsync(string fileId, ImageResizeOptions options)
        {
            // For now, return a placeholder implementation
            return await Task.FromResult(ApiResult<FileResponseDto>.Failure("Image resizing not implemented in this version"));
        }

        public async Task<ApiResult<List<FileResponseDto>>> GetFilesByTypeAsync(FileType fileType)
        {
            try
            {
                var allFiles = await GetAllFilesFromDisk();
                var filesOfType = allFiles.Where(f => f.FileType == fileType).ToList();
                return ApiResult<List<FileResponseDto>>.Success(filesOfType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting files by type: {FileType}", fileType);
                return ApiResult<List<FileResponseDto>>.Failure($"Error getting files: {ex.Message}");
            }
        }

        public async Task<ApiResult<bool>> ValidateFileAsync(IFormFile file, FileType fileType)
        {
            await Task.CompletedTask; // Make async

            if (file == null || file.Length == 0)
            {
                return ApiResult<bool>.Failure("File is empty or null");
            }

            if (file.Length > _maxFileSize)
            {
                return ApiResult<bool>.Failure($"File size exceeds maximum allowed size of {_maxFileSize / (1024 * 1024)} MB");
            }

            var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            if (string.IsNullOrEmpty(extension))
            {
                return ApiResult<bool>.Failure("File has no extension");
            }

            if (!_allowedExtensions[fileType].Contains(extension))
            {
                return ApiResult<bool>.Failure($"File extension {extension} is not allowed for {fileType}");
            }

            // Additional MIME type validation
            var allowedMimeTypes = GetAllowedMimeTypes(fileType);
            if (!allowedMimeTypes.Contains(file.ContentType))
            {
                return ApiResult<bool>.Failure($"MIME type {file.ContentType} is not allowed for {fileType}");
            }

            return ApiResult<bool>.Success(true);
        }

        public async Task<ApiResult<string>> GeneratePreSignedUrlAsync(string fileId, TimeSpan expiration)
        {
            try
            {
                // This is a simplified implementation. In a real application with cloud storage,
                // you would generate a pre-signed URL from your cloud provider
                var fileInfo = await GetFileInfoAsync(fileId);
                if (!fileInfo.IsSuccess || fileInfo.Data == null)
                {
                    return ApiResult<string>.Failure("File not found");
                }

                var token = GenerateAccessToken(fileId, expiration);
                var preSignedUrl = $"{fileInfo.Data.FileUrl}?token={token}&expires={DateTimeOffset.UtcNow.Add(expiration).ToUnixTimeSeconds()}";
                
                return ApiResult<string>.Success(preSignedUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating pre-signed URL: {FileId}", fileId);
                return ApiResult<string>.Failure($"Error generating pre-signed URL: {ex.Message}");
            }
        }

        public async Task<ApiResult<bool>> MoveFileAsync(string fileId, string newPath)
        {
            try
            {
                var fileInfo = await GetFileInfoAsync(fileId);
                if (!fileInfo.IsSuccess || fileInfo.Data == null)
                {
                    return ApiResult<bool>.Failure("File not found");
                }

                var currentPath = GetFilePathFromUrl(fileInfo.Data.FileUrl);
                var targetPath = Path.Combine(_uploadPath, newPath);

                if (!File.Exists(currentPath))
                {
                    return ApiResult<bool>.Failure("Source file not found on disk");
                }

                // Ensure target directory exists
                var targetDir = Path.GetDirectoryName(targetPath);
                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir!);
                }

                File.Move(currentPath, targetPath);
                _logger.LogInformation("File moved successfully: {FileId} to {NewPath}", fileId, newPath);
                
                return ApiResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving file: {FileId}", fileId);
                return ApiResult<bool>.Failure($"Error moving file: {ex.Message}");
            }
        }

        #region Private Helper Methods

        private string GenerateFileId()
        {
            return Guid.NewGuid().ToString("N");
        }

        private string GenerateFileName(string originalFileName, string fileId)
        {
            var extension = Path.GetExtension(originalFileName);
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return $"{fileId}_{timestamp}{extension}";
        }

        private string GetTypeSpecificPath(FileType fileType)
        {
            var subFolder = fileType.ToString().ToLowerInvariant();
            return Path.Combine(_uploadPath, subFolder);
        }

        private string GenerateFileUrl(FileType fileType, string fileName)
        {
            var subFolder = fileType.ToString().ToLowerInvariant();
            return $"/uploads/{subFolder}/{fileName}";
        }

        private string GetFilePathFromUrl(string fileUrl)
        {
            var relativePath = fileUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            return Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, relativePath);
        }

        private async Task<List<FileResponseDto>> GetAllFilesFromDisk()
        {
            var files = new List<FileResponseDto>();
            
            foreach (FileType fileType in Enum.GetValues<FileType>())
            {
                var typePath = GetTypeSpecificPath(fileType);
                if (!Directory.Exists(typePath)) continue;

                var fileInfos = Directory.GetFiles(typePath)
                    .Select(f => new FileInfo(f))
                    .ToList();

                foreach (var fileInfo in fileInfos)
                {
                    var fileId = ExtractFileIdFromFileName(fileInfo.Name);
                    if (!string.IsNullOrEmpty(fileId))
                    {
                        files.Add(new FileResponseDto
                        {
                            FileId = fileId,
                            FileName = fileInfo.Name,
                            FileUrl = GenerateFileUrl(fileType, fileInfo.Name),
                            FileType = fileType,
                            FileSize = fileInfo.Length,
                            MimeType = GetMimeType(fileInfo.Extension),
                            UploadedAt = fileInfo.CreationTimeUtc
                        });
                    }
                }
            }

            return await Task.FromResult(files);
        }

        private string ExtractFileIdFromFileName(string fileName)
        {
            var parts = fileName.Split('_');
            return parts.Length > 0 ? parts[0] : string.Empty;
        }

        private string[] GetAllowedMimeTypes(FileType fileType)
        {
            return fileType switch
            {
                FileType.ProductImage or FileType.DesignImage or FileType.UserAvatar => 
                    new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/bmp", "image/webp" },
                FileType.Document => 
                    new[] { "application/pdf", "application/msword", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "text/plain" },
                _ => new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "application/pdf", "text/plain" }
            };
        }

        private string GetMimeType(string extension)
        {
            return extension.ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".webp" => "image/webp",
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".txt" => "text/plain",
                _ => "application/octet-stream"
            };
        }

        private string GenerateAccessToken(string fileId, TimeSpan expiration)
        {
            var payload = $"{fileId}:{DateTimeOffset.UtcNow.Add(expiration).ToUnixTimeSeconds()}";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_configuration["FileUpload:SecretKey"] ?? "default-secret"));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return Convert.ToBase64String(hash);
        }

        #endregion
    }
}