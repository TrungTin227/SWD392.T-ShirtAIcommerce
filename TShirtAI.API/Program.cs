// Program.cs

using WebAPI.Extensions;

var builder = WebApplication.CreateBuilder(args);

// 1) Chỉ một lần gọi AddInfrastructure, mọi thứ sẽ được register ở đó
builder.Services
       .AddInfrastructure(builder.Configuration)
       .AddSwaggerServices()
       .AddCustomServices();

// 2) Nếu cần dùng UserSecrets (chứa JWT key, SMTP password…), gọi trước khi Build()
//    (nếu để trong extension, cũng đảm bảo gọi AddUserSecrets trước AddInfrastructure)
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

var app = builder.Build();

// 3) Thêm Session middleware ngay sau Build và trước routing
app.UseSession();

// 4) Tiếp tục pipeline chung (migrate DB, UseCors, Authentication, Authorization…) 
//    Nếu bạn đã gom trong UseApplicationPipeline(), hãy đảm bảo nó chứa UseSession()/UseCors()/UseAuthentication()…
await app.UseApplicationPipeline();

app.UseSwaggerPipeline();

app.Run();
