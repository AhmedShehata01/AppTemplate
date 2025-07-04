using AppTemplate.BLL.Helper;
using Microsoft.AspNetCore.Mvc;

namespace AppTemplate.API.Controllers
{
    public class FilesController : BaseController
    {
        private readonly IWebHostEnvironment _env;

        public FilesController(IWebHostEnvironment env)
        {
            _env = env;
        }

        [HttpPost("upload")]
        public async Task<ActionResult<ApiResponse<object>>> UploadFile(IFormFile file, [FromQuery] string folder)
        {
            // تحقق من وجود الملف
            if (file == null || file.Length == 0)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Code = 400,
                    Status = "File is required.",
                    Result = null
                });
            }

            // تحقق من وجود اسم المجلد
            if (string.IsNullOrWhiteSpace(folder))
            {
                return BadRequest(new ApiResponse<object>
                {
                    Code = 400,
                    Status = "Folder name is required.",
                    Result = null
                });
            }

            // تنظيف اسم المجلد لحماية من المسارات الخبيثة
            folder = folder.Trim();
            folder = folder.Replace("..", "").Replace("\\", "").Replace("/", "");

            // إنشاء اسم فريد للملف
            var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

            // تحديد المسار النسبي والمطلق للمجلد
            var subFolder = Path.Combine("uploads", folder);
            var uploadsFolder = Path.Combine(_env.WebRootPath ?? "wwwroot", subFolder);

            // إنشاء المجلد إذا لم يكن موجودًا
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            // إنشاء المسار الكامل للملف
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // حفظ الملف فعليًا
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // إنشاء المسار النسبي للملف (لاستخدامه في عرض الصورة لاحقًا)
            var relativePath = $"/uploads/{folder}/{uniqueFileName}".Replace("\\", "/");

            // استجابة موحدة باستخدام ApiResponse
            return Ok(new ApiResponse<object>
            {
                Code = 200,
                Status = "Success",
                Result = new { filePath = relativePath }
            });
        }


    }
}
