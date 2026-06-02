using Microsoft.AspNetCore.Mvc;

namespace Chat.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FileController : ControllerBase
{
    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile(
        IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

        var uploadsFolder =
            Path.Combine(
                Directory.GetCurrentDirectory(),
                "Uploads");

        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var uniqueFileName =
            Guid.NewGuid() + "_" + file.FileName;

        var filePath =
            Path.Combine(
                uploadsFolder,
                uniqueFileName);

        using var stream =
            new FileStream(filePath, FileMode.Create);

        await file.CopyToAsync(stream);

        var fileUrl =
            $"{Request.Scheme}://{Request.Host}/Uploads/{uniqueFileName}";

        return Ok(new
        {
            FileName = file.FileName,
            FileUrl = fileUrl,
            FileType = file.ContentType
        });
    }
}