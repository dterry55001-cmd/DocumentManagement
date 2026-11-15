using DocumentManagement.Data;
using DocumentManagement.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddHealthChecks();
builder.Services.AddDbContext<DocumentDbContext>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("Documents")));
builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection("Storage"));
builder.Services.AddScoped<IDocumentStorage, LocalDocumentStorage>();

var app = builder.Build();
var dataRoot = app.Configuration["Storage:RootPath"] ?? "./data";
Directory.CreateDirectory(Path.Combine(dataRoot, "documents"));
Directory.CreateDirectory(Path.Combine(dataRoot, "temp"));

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DocumentDbContext>();
    await db.Database.EnsureCreatedAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready");
app.MapControllerRoute("default", "{controller=Documents}/{action=Index}/{id?}");
app.Run();
