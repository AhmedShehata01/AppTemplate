using System.Security.Claims;
using System.Text;
using AppTemplate.BLL.Helper;
using AppTemplate.BLL.Hubs;
using AppTemplate.BLL.Mapper;
using AppTemplate.BLL.Middleware;
using AppTemplate.BLL.Services;
using AppTemplate.BLL.Services.AppSecurity;
using AppTemplate.BLL.Services.IdentityServices;
using AppTemplate.BLL.Services.SendEmail;
using AppTemplate.BLL.Services.UserSessionServices;
using AppTemplate.DAL.Database;
using AppTemplate.DAL.Extend;
using AppTemplate.DAL.Repository;
using AppTemplate.DAL.StaticData;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NLog;
using NLog.Web;


var logger = LogManager.Setup().LoadConfigurationFromAppSettings()
.GetCurrentClassLogger();


try
{


    var builder = WebApplication.CreateBuilder(args);

    #region Logger
    // NLog : setup NLog for dependency Injection
    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    #endregion

    #region MemoryCache Service
    builder.Services.AddMemoryCache();
    #endregion

    builder.Services.AddHttpContextAccessor();

    // Add services to the container.
    builder.Services.AddControllers();

    #region Connection String Decryption
    // Retrieve the encrypted connection string from appsettings.json
    var encryptedConnectionString = builder.Configuration.GetConnectionString("ApplicationConnection");

    // Define the new secret key for decryption
    var secretKey = "012345678901234567890123456789Aa"; // Ensure this matches your encryption key

    // Decrypt the connection string using the custom decryption logic
    var decryptor = new Decrypt(secretKey);
    var decryptedConnectionString = decryptor.DecryptConnectionString(encryptedConnectionString);

    // Log the decrypted connection string for debugging (remove in production)
    Console.WriteLine("Decrypted Connection String: " + decryptedConnectionString);

    string cleanedConnectionString = decryptedConnectionString.Replace(@"\\", @"\");

    // Use the decrypted connection string for the DbContext
    builder.Services.AddDbContext<ApplicationContext>(options =>
        options.UseSqlServer(cleanedConnectionString));
    #endregion

    #region User Claims
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("ViewRolePolicy", policy =>
            policy.RequireAssertion(context =>
                context.User.HasClaim(c => c.Type == "View Role" && c.Value == "true") ||
                context.User.IsInRole("Super Admin")
            ));

        options.AddPolicy("CreateRolePolicy", policy =>
            policy.RequireAssertion(context =>
                context.User.HasClaim(c => c.Type == "Create Role" && c.Value == "true") ||
                context.User.IsInRole("Super Admin")
            ));

        options.AddPolicy("EditRolePolicy", policy =>
            policy.RequireAssertion(context =>
                context.User.HasClaim(c => c.Type == "Edit Role" && c.Value == "true") ||
                context.User.IsInRole("Super Admin")
            ));

        options.AddPolicy("DeleteRolePolicy", policy =>
            policy.RequireAssertion(context =>
                context.User.HasClaim(c => c.Type == "Delete Role" && c.Value == "true") ||
                context.User.IsInRole("Super Admin")
            ));
    });


    #endregion

    #region Auto mapper Service

    builder.Services.AddAutoMapper(x => x.AddProfile(new DomainProfile()));

    #endregion

    #region Swagger
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(setup =>
    {

        setup.SwaggerDoc("v1", new OpenApiInfo { Title = "AppTemplate API", Version = "v1" });

        // Include 'SecurityScheme' to use JWT Authentication
        var jwtSecurityScheme = new OpenApiSecurityScheme
        {
            Scheme = "bearer",
            BearerFormat = "JWT",
            Name = "JWT Authentication",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Description = "Put *ONLY* your JWT Bearer token on textbox below!",

            Reference = new OpenApiReference
            {
                Id = JwtBearerDefaults.AuthenticationScheme,
                Type = ReferenceType.SecurityScheme
            }
        };

        setup.AddSecurityDefinition(jwtSecurityScheme.Reference.Id, jwtSecurityScheme);

        setup.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    { jwtSecurityScheme, Array.Empty<string>() }
                });

    });

    #endregion

    #region Microsoft Identity Configuration
    // إعداد الـ Identity الكامل (ApplicationUser + Roles + Token Providers)
    builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
    {
        // إعدادات اسم المستخدم
        options.User.RequireUniqueEmail = true;
        options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+ ";

        // إعدادات كلمة المرور
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequireUppercase = true;
        options.Password.RequiredLength = 6;
        options.Password.RequiredUniqueChars = 0;

        // إلغاء تأكيد البريد لو مش مطلوب
        options.SignIn.RequireConfirmedAccount = false;

    })
    .AddEntityFrameworkStores<ApplicationContext>()
    .AddDefaultTokenProviders();

    #endregion


    #region Redis
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = builder.Configuration["Redis:ConnectionString"];
    });
    #endregion

    #region SignalR + Redis Backplane
    builder.Services.AddSignalR()
        .AddStackExchangeRedis(builder.Configuration["Redis:ConnectionString"]);
    #endregion



    #region CORS

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAngularDev", builder =>
        {
            builder
                .WithOrigins("http://localhost:4200") // your frontend origin
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    });

    #endregion

    #region Bind the TimeZoneSettings to appsettings.json
    builder.Services.Configure<TimeZoneSettings>(builder.Configuration.GetSection("TimeZoneSettings"));

    #endregion

    #region AddScoped Services
    builder.Services.AddScoped(typeof(IGenericRepository<,>), typeof(GenericRepository<,>));

    builder.Services.AddScoped<IAuthService, AuthService>();

    builder.Services.AddScoped<IEmailService, EmailService>();



    builder.Services.AddScoped<IRoleManagementService, RoleManagementService>();
    builder.Services.AddScoped<IUserManagementService, UserManagementService>();
    builder.Services.AddScoped<IUserClaimManagementService, UserClaimManagementService>();
    builder.Services.AddScoped<IUserRoleManagementService, UserRoleManagementService>();

    builder.Services.AddScoped<ISecuredRouteService, SecuredRouteService>();
    builder.Services.AddScoped<ISidebarService, SidebarService>();
    builder.Services.AddScoped<IActivityLogService, ActivityLogService>();
    builder.Services.AddScoped<IOtpService, OtpService>();


    #region uesr session services
    builder.Services.AddScoped<IUserSessionRepository, UserSessionRepository>();
    builder.Services.AddScoped<IUserSessionCacheService, UserSessionCacheService>();
    builder.Services.AddScoped<IUserSessionService, UserSessionService>();
    #endregion

    #endregion

    #region JWT Configuration

    builder.Services.Configure<JWTHelper>(builder.Configuration.GetSection("JWT"));

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = builder.Configuration["JWT:Issuer"],
            ValidAudience = builder.Configuration["JWT:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Key"])),
            ClockSkew = TimeSpan.Zero,

            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                // ✅ تأكد إن ده نفس المسار عندك بالظبط
                if (!string.IsNullOrEmpty(accessToken) &&
                    path.StartsWithSegments("/presenceHub"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });



    #endregion

    #region Admin Setting in AppSettings
    builder.Services.Configure<AdminSettings>(
    builder.Configuration.GetSection("AdminSettings"));
    #endregion

    var app = builder.Build();

    #region Ensure roles and admin user are seeded
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        await SeedData.SeedRolesAndAdminUser(services, userManager, roleManager);
    }
    #endregion



    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseMiddleware<ExceptionMiddleware>();
    app.UseHttpsRedirection();

    // Use the named CORS policy here
    app.UseCors("AllowAngularDev");

    app.UseStaticFiles();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseMiddleware<SingleLoginMiddleware>();

    app.MapControllers();
    app.MapHub<PresenceHub>("/presenceHub");

    app.Run();
}
catch (Exception ex)
{
    logger.Error(ex);
}
finally
{
    LogManager.Shutdown();
}