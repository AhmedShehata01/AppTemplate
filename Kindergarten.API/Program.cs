
using Kindergarten.BLL.Services.AppSecurity;
using Kindergarten.DAL.Database;
using Kindergarten.DAL.Extend;
using Kindergarten.DAL.StaticData;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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




    #region Microsoft IDentity Configuration
    builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme,
                        options =>
                        {
                            options.LoginPath = new PathString("/Account/Login");
                            options.AccessDeniedPath = new PathString("/Account/Login");
                        });

    builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
        .AddRoles<ApplicationRole>()
        .AddEntityFrameworkStores<ApplicationContext>()
        .AddTokenProvider<DataProtectorTokenProvider<ApplicationUser>>(TokenOptions.DefaultProvider);
    // User And Password Validations

    builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
    {

        options.User.RequireUniqueEmail = true;
        options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ ";

        // Default Password settings.
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequireUppercase = true;
        options.Password.RequiredLength = 6;
        options.Password.RequiredUniqueChars = 0;

    }).AddEntityFrameworkStores<ApplicationContext>();

    #endregion

    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

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

    app.UseHttpsRedirection();

    app.UseAuthorization();

    app.MapControllers();

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