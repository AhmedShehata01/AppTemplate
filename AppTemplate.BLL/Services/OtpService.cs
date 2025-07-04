using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using AppTemplate.BLL.Models;
using AppTemplate.BLL.Services.SendEmail;
using AppTemplate.DAL.Database;
using AppTemplate.DAL.Entity;
using AppTemplate.DAL.Extend;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AppTemplate.BLL.Services
{
    public class OtpService : IOtpService
    {
        #region Props
        private readonly ApplicationContext _db;
        private readonly IMapper _mapper;
        private readonly ILogger<OtpService> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly IEmailService _emailService;
        private readonly UserManager<ApplicationUser> _userManager;
        #endregion

        #region Constructor
        public OtpService(ApplicationContext db,
                          IMapper mapper,
                          ILogger<OtpService> logger,
                          IWebHostEnvironment env,
                          IEmailService emailService,
                          UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _mapper = mapper;
            _logger = logger;
            _env = env;
            _emailService = emailService;
            _userManager = userManager;
        }
        #endregion

        #region Methods

        /// <summary>
        /// Generates a new OTP code for the user and sends it to the user's email.
        /// </summary>
        public async Task<ActionResultDTO<OtpDTO>> GenerateAndSendOtpAsync(RequestOtpDTO dto)
        {
            // Find the user by email
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                return new ActionResultDTO<OtpDTO>
                {
                    Success = false,
                    Message = "User not found",
                    Data = null
                };
            }

            // Check if an OTP was already created for the same purpose within the last minute
            var lastOtp = await _db.Otp
                .Where(o => o.UserId == user.Id && o.Purpose == dto.Purpose)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (lastOtp != null && (DateTime.UtcNow - lastOtp.CreatedAt).TotalSeconds < 60)
            {
                return new ActionResultDTO<OtpDTO>
                {
                    Success = false,
                    Message = "Please wait a minute before requesting a new OTP.",
                    Data = null
                };
            }

            // 1. Generate a random 6-digit code
            var otpCode = new Random().Next(100000, 999999).ToString();

            // 2. Hash the code for secure storage
            string hash = HashOtp(otpCode);

            // 3. Create new OTP entity
            var otp = new Otp
            {
                UserId = user.Id,
                CodeHash = hash,
                Purpose = dto.Purpose,
                Expiry = DateTime.UtcNow.AddMinutes(1),
                CreatedAt = DateTime.UtcNow
            };

            await _db.Otp.AddAsync(otp);
            await _db.SaveChangesAsync();

            // 4. Send the OTP code via email
            string subject = "Verification Code";
            string body = $"Your verification code is: {otpCode}";
            await _emailService.SendEmailAsync(user.Email, subject, body);

            // 5. In development environment, log the OTP code for easier testing
            if (_env.IsDevelopment())
                _logger.LogInformation($"OTP (DEV): {otpCode} → Email: {user.Email}");

            // 6. Map the entity to DTO
            var otpDto = _mapper.Map<OtpDTO>(otp);

            return new ActionResultDTO<OtpDTO>
            {
                Success = true,
                Message = "Verification code sent to your email",
                Data = otpDto
            };
        }

        /// <summary>
        /// Verifies if the entered OTP is valid for the user and the specific purpose.
        /// </summary>
        public async Task<ActionResultDTO<OtpDTO>> VerifyOtpAsync(VerifyOtpDTO dto)
        {
            // Find user by email
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                return new ActionResultDTO<OtpDTO>
                {
                    Success = false,
                    Message = "Email not found",
                    Data = null
                };
            }

            // Get the most recent unused and valid OTP for the same purpose
            var otpList = await _db.Otp
                .Where(o => o.UserId == user.Id
                         && o.Purpose == dto.Purpose
                         && !o.IsUsed
                         && o.Expiry > DateTime.UtcNow)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            if (otpList == null || otpList.Count == 0)
            {
                return new ActionResultDTO<OtpDTO>
                {
                    Success = false,
                    Message = "No valid verification code found",
                    Data = null
                };
            }

            var otp = otpList.First();

            // Hash the entered code for comparison
            var codeHash = HashOtp(dto.Code);

            if (otp.CodeHash != codeHash)
            {
                otp.Attempts++;

                if (otp.Attempts >= 5)
                {
                    otp.IsUsed = true; // Disable OTP after 5 failed attempts
                }

                await _db.SaveChangesAsync();

                if (otp.IsUsed)
                {
                    return new ActionResultDTO<OtpDTO>
                    {
                        Success = false,
                        Message = "Maximum verification attempts exceeded",
                        Data = null
                    };
                }

                return new ActionResultDTO<OtpDTO>
                {
                    Success = false,
                    Message = $"Invalid verification code. Attempt {otp.Attempts} of 5",
                    Data = null
                };
            }

            // Check if OTP has expired
            if (otp.Expiry < DateTime.UtcNow)
            {
                otp.IsUsed = true;
                await _db.SaveChangesAsync();

                return new ActionResultDTO<OtpDTO>
                {
                    Success = false,
                    Message = "Verification code expired",
                    Data = null
                };
            }

            // OTP is valid, mark as used
            otp.IsUsed = true;
            await _db.SaveChangesAsync();

            var otpDto = _mapper.Map<OtpDTO>(otp);

            return new ActionResultDTO<OtpDTO>
            {
                Success = true,
                Message = "Verification successful",
                Data = otpDto
            };
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Hashes the OTP code using SHA256 for secure storage.
        /// </summary>
        public string HashOtp(string code)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(code);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        #endregion
    }

    public interface IOtpService
    {
        Task<ActionResultDTO<OtpDTO>> GenerateAndSendOtpAsync(RequestOtpDTO dto);
        Task<ActionResultDTO<OtpDTO>> VerifyOtpAsync(VerifyOtpDTO dto);
    }
}
