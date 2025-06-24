using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Google.Apis.Auth;
using Kindergarten.BLL.Helper;
using Kindergarten.BLL.Models.Auth;
using Kindergarten.DAL.Extend;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Kindergarten.BLL.Services
{
    public class AuthService : IAuthService
    {
        #region Prop
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _config;


        private readonly string _googleClientId;
        private readonly string _facebookAppId;
        private readonly string _facebookAppSecret;
        #endregion

        #region CTOR
        public AuthService(UserManager<ApplicationUser> userManager, IConfiguration config)
        {
            _userManager = userManager;
            _config = config;

            // قراءة القيم من ملف appsettings.json
            _googleClientId = _config["Google:ClientId"];
            _facebookAppId = _config["Facebook:AppId"];
            _facebookAppSecret = _config["Facebook:AppSecret"];
        }
        #endregion

        #region Actions

        public async Task<ApiResponse<string>> RegisterAsync(RegisterDTO model)
        {
            var user = new ApplicationUser
            {
                UserName = model.UserName,
                Email = model.Email,
                EmailConfirmed = true,
                // UserType = (DAL.Enum.UserType)model.UserType
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return new ApiResponse<string> { Code = 400, Status = "Failed", Result = errors };
            }

            return new ApiResponse<string> { Code = 200, Status = "Success", Result = "User registered successfully" };
        }

        public async Task<ApiResponse<string>> LoginAsync(LoginDTO model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
            {
                return new ApiResponse<string> { Code = 401, Status = "Failed", Result = "Invalid credentials" };
            }

            // Get user roles
            var roles = await _userManager.GetRolesAsync(user);

            // Generate JWT using the new method
            var jwtToken = GenerateJwtToken(user, roles);

            return new ApiResponse<string> { Code = 200, Status = "Success", Result = jwtToken };
        }

        public string GenerateJwtToken(ApplicationUser user, IList<string> roles)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_config["JWT:Key"]);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                // new Claim("UserType", user.UserType.ToString())
                // إضافة IsAgree كـ Claim نصي (true / false)
                new Claim("IsAgree", user.IsAgree ? "true" : "false")
            };
            // ✅ فقط أضف Claim "IsFirstLogin" إذا كانت true
            if (user.IsFirstLogin)
            {
                claims.Add(new Claim("IsFirstLogin", "true"));
            }

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(int.Parse(_config["JWT:DurationInMinutes"])),
                Issuer = _config["JWT:Issuer"],
                Audience = _config["JWT:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public async Task<ApiResponse<string>> ChangePasswordAsync(string userId, ChangePasswordDTO model)
        {
            if (model.NewPassword == model.CurrentPassword)
            {
                return new ApiResponse<string>
                {
                    Code = 400,
                    Status = "Failed",
                    Result = "New password must be different from current password"
                };
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return new ApiResponse<string>
                {
                    Code = 404,
                    Status = "Failed",
                    Result = "User not found"
                };
            }

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return new ApiResponse<string>
                {
                    Code = 400,
                    Status = "Failed",
                    Result = errors
                };
            }

            return new ApiResponse<string>
            {
                Code = 200,
                Status = "Success",
                Result = "Password changed successfully"
            };
        }


        #endregion


        #region External Login 
        public async Task<ExternalUserInfoDto?> VerifyGoogleTokenAsync(string idToken)
        {
            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings()
                {
                    Audience = new List<string>() { _googleClientId }
                };

                var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);

                return new ExternalUserInfoDto
                {
                    Provider = "Google",
                    ProviderUserId = payload.Subject,
                    Email = payload.Email,
                    FullName = payload.Name
                };
            }
            catch (Exception ex)
            {
                // للتنقيح
                Console.WriteLine("Google Token Validation Failed:");
                Console.WriteLine(ex.ToString()); // مهم: يطبع الـ stack trace
                return null;
            }
        }



        public async Task<ExternalUserInfoDto?> VerifyFacebookTokenAsync(string accessToken)
        {
            var client = new HttpClient();
            var appAccessToken = $"{_facebookAppId}|{_facebookAppSecret}";

            var verifyTokenUrl = $"https://graph.facebook.com/debug_token?input_token={accessToken}&access_token={appAccessToken}";
            var verifyResponse = await client.GetAsync(verifyTokenUrl);

            if (!verifyResponse.IsSuccessStatusCode)
                return null;

            var userInfoUrl = $"https://graph.facebook.com/me?fields=id,name,email&access_token={accessToken}";
            var userInfoResponse = await client.GetAsync(userInfoUrl);

            if (!userInfoResponse.IsSuccessStatusCode)
                return null;

            var content = await userInfoResponse.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(content).RootElement;

            return new ExternalUserInfoDto
            {
                Provider = "Facebook",
                ProviderUserId = json.GetProperty("id").GetString(),
                Email = json.TryGetProperty("email", out var emailProp) ? emailProp.GetString() : null,
                FullName = json.GetProperty("name").GetString()
            };
        }


        public async Task<AuthResponseDto> HandleExternalUserAsync(ExternalUserInfoDto externalUser)
        {
            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.Provider == externalUser.Provider && u.ProviderUserId == externalUser.ProviderUserId);

            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = externalUser.Email ?? Guid.NewGuid().ToString(),
                    Email = externalUser.Email,
                    FullName = externalUser.FullName,
                    Provider = externalUser.Provider,
                    ProviderUserId = externalUser.ProviderUserId,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new Exception($"Failed to create user: {errors}");
                }
            }

            var roles = await _userManager.GetRolesAsync(user);

            var token = GenerateJwtToken(user, roles);

            return new AuthResponseDto
            {
                IsAuthenticated = true,
                Token = token,
                Email = user.Email,
                UserName = user.UserName,
                FullName = user.FullName,
                Provider = user.Provider,
                //ExpireOn = /* إذا عندك تاريخ انتهاء التوكن، ضعه هنا */
};
        }






        #endregion

    }

    public interface IAuthService
    {
        Task<ApiResponse<string>> RegisterAsync(RegisterDTO model);
        Task<ApiResponse<string>> LoginAsync(LoginDTO model);
        string GenerateJwtToken(ApplicationUser user, IList<string> roles);
        Task<ApiResponse<string>> ChangePasswordAsync(string userId, ChangePasswordDTO model);


        #region External Login
        Task<ExternalUserInfoDto?> VerifyGoogleTokenAsync(string accessToken);
        Task<ExternalUserInfoDto?> VerifyFacebookTokenAsync(string accessToken);
        Task<AuthResponseDto> HandleExternalUserAsync(ExternalUserInfoDto externalUser);
        #endregion

    }

}
