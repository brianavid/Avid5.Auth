using Avid5.Auth.Controllers;
using NLog;
using NLog.Web;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

//app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

var logger = NLog.LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();

if (args.Length > 0 && File.Exists(args[0]))
{
    Config.Initialize(args[0]);
    AuthController.Initialize(app.Lifetime);

    logger.Info("Avid5 Auth Started");
    app.Run();
}
else
{
    if (args.Length > 0)
        logger.Info($"Avid5 Auth missing config XML file '{args[0]}'");
    else
        logger.Info("Avid5 Auth missing config XML file argument");
}
