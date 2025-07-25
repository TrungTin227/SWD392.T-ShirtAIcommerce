﻿using System.ComponentModel.DataAnnotations;

namespace Services.Commons.Gmail.Implementations
{
    public class EmailSettings
    {
        public string SmtpHost { get; set; } = string.Empty;
        public int SmtpPort { get; set; } = 587;
        public string SmtpUsername { get; set; } = string.Empty;
        public string SmtpPassword { get; set; } = string.Empty;
        public bool EnableSsl { get; set; } = true;
        [Required, EmailAddress]
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string SupportEmail { get; set; } = string.Empty;
        public int MaxRetryAttempts { get; set; } = 3;
    }
}