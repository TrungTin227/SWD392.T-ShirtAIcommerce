using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Services.Commons.Gmail.Interfaces;
namespace Services.Commons.Gmail.Implementations
{
    public static class EmailServiceExtensions
    {
        public static IServiceCollection AddEmailServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));
            services.AddSingleton<EmailQueue>();
            services.AddTransient<IEmailService, EmailService>();
            services.AddTransient<IEmailQueueService, EmailQueueService>();
            services.AddTransient<SendEmailJob>();
            services.AddHostedService<EmailBackgroundService>();
            services.AddHostedService<EmailReminderService>();
            services.AddTransient<EmailTemplateService>();
            services.AddQuartz(q => { });
            services.AddQuartzHostedService(options =>
            {
                options.WaitForJobsToComplete = true;
            });

            return services;
        }
    }
}