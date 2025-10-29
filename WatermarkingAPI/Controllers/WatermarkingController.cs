using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;
using WatermarkingAPI.Services.Interfaces;
using SixLabors.ImageSharp.Formats.Jpeg;


namespace WatermarkingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WatermarkingController : ControllerBase
    {
        private readonly IWatermarkingService _watermarkingService;


        public WatermarkingController(IWatermarkingService watermarkingService)
        {
            _watermarkingService = watermarkingService;
        }

        [HttpPost]
        public async Task<IActionResult> Waternarking(string imageUrl, string fontName, float angle, string watermarkImageUrl = null, int? position = null, string watermarkText = null) {

            if (string.IsNullOrEmpty(imageUrl))
            {
                return BadRequest("ImageUrl is required.");
            }

            if (!Uri.IsWellFormedUriString(imageUrl, UriKind.Absolute))
            {
                return BadRequest("Invalid ImageUrl format.");
            }

            if (watermarkImageUrl != null && !Uri.IsWellFormedUriString(watermarkImageUrl, UriKind.Absolute)) {
                return BadRequest("Invalid watermarkImageUrl format.");
            }

            if (string.IsNullOrWhiteSpace(watermarkImageUrl) && string.IsNullOrWhiteSpace(watermarkText))
            {
                return BadRequest("Either watermarkImageUrl or watermarkText must be provided.");
            }


            var imageResult = watermarkImageUrl != null ? 
                await _watermarkingService.AddImageWatermarkAsync(imageUrl, watermarkImageUrl, angle, position) :
                await _watermarkingService.AddTextWatermarkAsync(imageUrl, watermarkText, fontName, angle, position);

            if (!imageResult.Success) return StatusCode(imageResult.StatusCode ?? 500, imageResult.ErrorMessage);

            using var outputStream = new MemoryStream();
            await imageResult.Image.SaveAsync(outputStream, new JpegEncoder());
            outputStream.Position = 0;

            return File(outputStream.ToArray(), "image/jpeg");
        }
    }
}
