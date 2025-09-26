using LibrarySystem.BLL.Extensions;
using LibrarySystem.BLL.Interfaces;
using LibrarySystem.BLL.Services;
using LibrarySystem.Common.Exceptions;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// =================================================================
// 1. CONFIGURE SERVICES (DEPENDENCY INJECTION)
// =================================================================

// --- Add services required by the framework ---
builder.Services.AddRazorPages(options =>
{
    // Configure authorization for specific folders.
    // Any page inside the /Admin folder will require the "ManagerOnly" policy.
    options.Conventions.AuthorizeFolder("/Admin", "ManagerOnly");
    // Any page inside the /Member folder will require the user to be authenticated.
    options.Conventions.AuthorizeFolder("/Member");
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// --- Register Data Access Layer (DAL) Services ---
// NOTE: This also registers Business Logic Layer (BLL) Services.
builder.Services.AddBllServices();
// =================================================================
// 2. CONFIGURE AUTHENTICATION & AUTHORIZATION
// =================================================================

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";                       // Redirect here if user is not authenticated.
        options.AccessDeniedPath = "/Account/AccessDenied";         // Redirect here if user lacks the required role.
        options.ExpireTimeSpan = TimeSpan.FromDays(7);        // The login cookie will be valid for 60 minutes.
        options.SlidingExpiration = true;                           // Resets the expiration time if the user is active.
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("ManagerOnly", policy => policy.RequireRole("Manager"));

// =================================================================
// 3. BUILD AND CONFIGURE THE HTTP REQUEST PIPELINE
// =================================================================
var app = builder.Build();

// =================================================================
// 3. Initialize Data Access Layer (DAL) Services
// =================================================================
await app.Services.InitializeBllServicesAsync();

// =================================================================
// 4. CONFIGURE THE HTTP REQUEST PIPELINE (The Middleware)
// =================================================================
// The pipeline configures how the application responds to web requests.
// Middleware is executed in the order it is added.
if (!app.Environment.IsDevelopment())
{
    // Use a friendly error page in production.
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
            var exception = exceptionHandlerPathFeature?.Error;

            var logger = app.Services.GetRequiredService<ILogger<Program>>();
            logger.LogError(exception, "Unhandled exception occurred.");

            if (exception is DataAccessConnectionException)
            {
                context.Response.Redirect("/DatabaseError");
            }
            else
            {
                context.Response.Redirect("/Error");
            }

            await Task.CompletedTask;
        });
    });

    // Enforce HTTPS in production.
    app.UseHsts();
}
else
{
    // Use a more detailed developer exception page in development.
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection(); // Redirect HTTP requests to HTTPS.
app.UseStaticFiles();      // Serve static files like CSS, JavaScript, and images from the wwwroot folder.

app.UseRouting();          // Enables routing to determine which page/endpoint to handle the request.

// IMPORTANT: Authentication must come before Authorization.
app.UseAuthentication();   // Identifies who the user is based on the cookie.
app.UseAuthorization();    // Determines if the identified user is permitted to access the requested resource.

app.MapRazorPages();       // Executes the Razor Page endpoint that was selected by routing.

// =================================================================
// 5. RUN THE APPLICATION
// =================================================================
app.Run();                 // Starts the web server and begins listening for requests.
