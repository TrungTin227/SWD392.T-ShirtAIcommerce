using System.ComponentModel.DataAnnotations;

namespace DTOs.Files
{
    public enum FileType
    {
        ProductImage,
        DesignImage,
        UserAvatar,
        Document,
        Other
    }

    public class FileUploadRequestDto
    {
        [Required(ErrorMessage = "File type is required")]
        public FileType FileType { get; set; }

        public string? Description { get; set; }
        public string? AltText { get; set; }
    }

    public class FileResponseDto
    {
        public string FileName { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public string FileId { get; set; } = string.Empty;
        public FileType FileType { get; set; }
        public long FileSize { get; set; }
        public string MimeType { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }
        public string? Description { get; set; }
        public string? AltText { get; set; }
    }

    public class FileValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    public class ImageResizeOptions
    {
        public int? Width { get; set; }
        public int? Height { get; set; }
        public bool MaintainAspectRatio { get; set; } = true;
        public string Quality { get; set; } = "High"; // High, Medium, Low
    }

    public class FileMetadataDto
    {
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public string FileExtension { get; set; } = string.Empty;
    }
}