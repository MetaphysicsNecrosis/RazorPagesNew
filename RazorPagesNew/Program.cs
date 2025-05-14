using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RazorPagesNew.Data;
using RazorPagesNew.Services.Implementation;
using RazorPagesNew.Services.Interfaces;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ���������� ������������ �� appsettings.json
var configuration = builder.Configuration;

// ��������� ��������� ���� ������
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure()
    )
);
builder.Services.AddDbContext<MyIdentityDbContext>(options =>
    options.UseSqlServer(
        configuration.GetConnectionString("DefaultConnectionIdentity"),
        sqlOptions => sqlOptions.EnableRetryOnFailure()
    )
);
// ���������� Identity
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options => {
    // ��������� ���������� ������
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;

    // ��������� ����������
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;

    // ��������� ������������
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<MyIdentityDbContext>()
.AddDefaultTokenProviders();

// ��������� �������� ��������������
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(24);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = configuration["JWT:Issuer"],
        ValidAudience = configuration["JWT:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:Secret"]))
    };
});

// ��������� �����������
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
    options.AddPolicy("RequireHRRole", policy => policy.RequireRole("HR"));
    options.AddPolicy("RequireEvaluatorRole", policy => policy.RequireRole("Evaluator"));
    options.AddPolicy("RequireUserRole", policy => policy.RequireRole("User"));
});

// ��������� ������
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ����������� ��������
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IEvaluationService, EvaluationService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();

// ��������� �������� CORS
var allowedOrigins = configuration.GetSection("AllowedOrigins").Get<string[]>();

if (allowedOrigins == null || !allowedOrigins.Any())
{
    throw new InvalidOperationException("AllowedOrigins is not configured in appsettings.json.");
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins",
        builder => builder
            .WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
    );
});

// ��������� Razor Pages
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Admin", "RequireAdminRole");
    options.Conventions.AuthorizeFolder("/HR", "RequireHRRole");
    options.Conventions.AuthorizeFolder("/Evaluations", "RequireEvaluatorRole");
    options.Conventions.AuthorizePage("/Profile", "RequireUserRole");
});
/*.AddRazorRuntimeCompilation(); // ��������� ��������� ������� ������������ ��� ����������*/

// ��������� Anti-forgery token
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN";
    options.SuppressXFrameOptionsHeader = false;
});

// ��������� HTTP ���������
builder.Services.AddHttpContextAccessor();

// �����������: ��������� �������� �����������
builder.Services.AddMemoryCache();
builder.Services.AddDistributedMemoryCache();

var app = builder.Build();

// ��������� ��������� HTTP
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// ��������� �������������
app.UseRouting();

// ���������� CORS ��������
app.UseCors("AllowSpecificOrigins");

// ��������� �������������� � �����������
app.UseAuthentication();
app.UseAuthorization();

// ������������� ������
app.UseSession();

// ���������� ���������� ������
app.UseStatusCodePagesWithReExecute("/Error/{0}");

// ������������ �������� �����
app.MapRazorPages();

// �����������: API ���������
app.MapControllers();
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<ApplicationDbContext>();
        dbContext.Database.Migrate();
        DbInitializer.Initialize(dbContext);

        var identityDbContext = services.GetRequiredService<MyIdentityDbContext>();
        identityDbContext.Database.Migrate();

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

        IdentityDataInitializer.SeedData(userManager, roleManager);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}
// ������������� ���� ������ 
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<ApplicationDbContext>();
        DbInitializer.Initialize(dbContext);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

app.Run();