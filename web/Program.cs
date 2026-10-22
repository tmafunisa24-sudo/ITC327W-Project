using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using ServiceHub_IT.Filters;
using ServiceHub_IT.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ServiceHub_IT.Models.SupabaseSettings>(builder.Configuration.GetSection(ServiceHub_IT.Models.SupabaseSettings.SectionName));
builder.Services.AddHttpClient<ServiceHub_IT.Interfaces.ISupabaseService, ServiceHub_IT.Services.SupabaseService>();
builder.Services.AddHttpClient<ImageStorageService>();
builder.Services.AddHttpClient<DashboardService>();
builder.Services.AddHttpClient<AssetService>();
builder.Services.AddHttpClient<TicketService>();
builder.Services.AddHttpClient<DepartmentService>();
builder.Services.AddHttpClient<ReportService>();
builder.Services.AddHttpClient<PdfProcessingService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddSingleton<ServiceHub_IT.Services.Ai.TicketPredictionModel>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.Cookie.Name = "ServiceHubITAuth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/Login";
        options.ReturnUrlParameter = "returnUrl";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;

        options.Events.OnRedirectToLogin = context =>
        {
            if (context.Request.Path.StartsWithSegments("/Notifications") ||
                context.Request.Headers["Accept"].ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }

            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };

        options.Events.OnRedirectToAccessDenied = context =>
        {
            if (context.Request.Path.StartsWithSegments("/Notifications") ||
                context.Request.Headers["Accept"].ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            }

            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddScoped<StatusAuthorizationFilter>();
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AuthorizeFilter());
    options.Filters.AddService<StatusAuthorizationFilter>();
});
builder.Services.AddRouting(options => options.LowercaseUrls = true);

var app = builder.Build();

var supabaseSection = builder.Configuration.GetSection(ServiceHub_IT.Models.SupabaseSettings.SectionName);
var supabaseUrl = supabaseSection["Url"] ?? string.Empty;
var supabaseApiKey = supabaseSection["ApiKey"] ?? string.Empty;
var ticketEndpointUrl = string.IsNullOrWhiteSpace(supabaseUrl)
    ? string.Empty
    : $"{supabaseUrl.TrimEnd('/')}/rest/v1/tickets";
var maskedApiKey = string.IsNullOrWhiteSpace(supabaseApiKey)
    ? "<missing>"
    : supabaseApiKey.Length <= 8
        ? "<masked>"
        : $"{supabaseApiKey[..4]}...{supabaseApiKey[^4..]}";

app.Logger.LogInformation("Startup: Supabase.Url = {SupabaseUrl}", supabaseUrl);
app.Logger.LogInformation("Startup: Supabase.ApiKey loaded = {HasApiKey} (masked={MaskedApiKey})", !string.IsNullOrWhiteSpace(supabaseApiKey), maskedApiKey);
app.Logger.LogInformation("Startup: Ticket endpoint URL = {TicketEndpointUrl}", ticketEndpointUrl);
app.Logger.LogInformation("Startup: TicketsBucket = {TicketsBucket}", supabaseSection["TicketScreenshotBucketName"] ?? string.Empty);
app.Logger.LogInformation("Startup: AssetsBucket = {AssetsBucket}", supabaseSection["AssetImageBucketName"] ?? string.Empty);

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.Run();
