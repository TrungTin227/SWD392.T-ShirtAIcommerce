using DTOs.Files;
using Microsoft.AspNetCore.Http;
using Repositories.Commons;

namespace Services.Interfaces
{
    public interface IFileUploadService
    {
        Task<ApiResult<FileResponseDto>> UploadFileAsync(IFormFile file, FileUploadRequestDto request);
        Task<ApiResult<List<FileResponseDto>>> UploadMultipleFilesAsync(List<IFormFile> files, FileUploadRequestDto request);
        Task<ApiResult<bool>> DeleteFileAsync(string fileId);
        Task<ApiResult<FileResponseDto>> GetFileInfoAsync(string fileId);
        Task<ApiResult<byte[]>> GetFileContentAsync(string fileId);
        Task<ApiResult<FileResponseDto>> ResizeImageAsync(string fileId, ImageResizeOptions options);
        Task<ApiResult<List<FileResponseDto>>> GetFilesByTypeAsync(FileType fileType);
        Task<ApiResult<bool>> ValidateFileAsync(IFormFile file, FileType fileType);
        Task<ApiResult<string>> GeneratePreSignedUrlAsync(string fileId, TimeSpan expiration);
        Task<ApiResult<bool>> MoveFileAsync(string fileId, string newPath);
    }
}