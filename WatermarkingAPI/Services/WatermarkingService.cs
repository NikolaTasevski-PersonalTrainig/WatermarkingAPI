using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;
using WatermarkingAPI.Services.Interfaces;
using WatermarkingAPI.Helpers;
using System.Numerics;

namespace WatermarkingAPI.Services
{
    public class WatermarkingService : IWatermarkingService
    {
        const int OFFSET_X = 200;
        const int OFFSET_Y = 200;
        const int FONT_SIZE = 108;
        private readonly Serilog.ILogger _logger;

        public WatermarkingService(Serilog.ILogger logger) { 
            _logger = logger;
        }
        public async Task<ImageResult> AddImageWatermarkAsync(string mainImageUrl, string watermarkImageUrl, float angle, int? position)
        {
            var imageResult = await GetImage(mainImageUrl);

            if (!imageResult.Success) return imageResult;
            var image = imageResult.Image;

            var watermarkImageResult = await GetImage(watermarkImageUrl);
            if (!watermarkImageResult.Success) return watermarkImageResult;
            var watermarkImage = watermarkImageResult.Image;

            if (watermarkImage.Width > image.Width || watermarkImage.Height > image.Height)
            {
                return new ImageResult
                {
                    Success = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    ErrorMessage = "The watermark image is larger than the main image. Please use a smaller watermark."
                };
            }


            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                watermarkImage.Mutate(x => x.Rotate(angle));
                stopwatch.Stop();
                _logger.Information($"Text watermarking took {stopwatch.ElapsedMilliseconds} ms");
            }
            catch (Exception ex) {
                stopwatch.Stop();
                _logger.Error(ex, $"Image failed after {stopwatch.ElapsedMilliseconds} ms");
            }

            var watermarkPosition = DetermineWatermarkPosition(image,
                Enum.IsDefined(typeof(WatermarkPosition), position) ? (WatermarkPosition)position : WatermarkPosition.Center,
                OFFSET_X,
                OFFSET_Y);

            image.Mutate(x => x.DrawImage(watermarkImage, (Point)watermarkPosition, 0.5f));
            return new ImageResult { 
                Success = true,
                Image = image
            };
        }

        public async Task<ImageResult> AddTextWatermarkAsync(string mainImageUrl, string watermarkText, string fontName, float angle, int? position)
        {
            var imageResult = await GetImage(mainImageUrl);
            if (!imageResult.Success) return imageResult;
            var image = imageResult.Image;

            var watermarkPosition = DetermineWatermarkPosition(image,
                Enum.IsDefined(typeof(WatermarkPosition), position) ? (WatermarkPosition)position : WatermarkPosition.Center,
                OFFSET_X,
                OFFSET_Y);

            var textOptions = new RichTextOptions(SystemFonts.CreateFont(fontName, FONT_SIZE))
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Origin = watermarkPosition
            };
            var textColor = Color.Green.WithAlpha(0.5f);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                image.Mutate(x =>
                {
                    //x.SetDrawingTransform(Matrix3x2.CreateRotation((float)Math.PI * angle / 180.0f));
                    x.DrawText(textOptions, watermarkText, textColor);
                });
                stopwatch.Stop();
                _logger.Information($"Text watermarking took {stopwatch.ElapsedMilliseconds} ms");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.Error(ex, $"Image failed after {stopwatch.ElapsedMilliseconds} ms");
            }


            return new ImageResult
            {
                Success = true,
                Image = image
            };
        }

        async Task<ImageResult> GetImage(string mainImageUrl)
        {
            using var httpClient = new HttpClient();
            using var response = await httpClient.GetAsync(mainImageUrl, HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode)
            {
                return new ImageResult
                {
                    Success = false,
                    ErrorMessage = $"Failed to download main image from {mainImageUrl}. Status code: {response.StatusCode}",
                    StatusCode = (int)response.StatusCode
                };
            }

            var contentType = response.Content.Headers.ContentType?.MediaType;
            if (string.IsNullOrEmpty(contentType) || !contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)) {
                return new ImageResult
                {
                    Success = false,
                    ErrorMessage = $"Unsupported content type: {contentType}",
                    StatusCode = 415
                };
            }
              
            await using var stream = await response.Content.ReadAsStreamAsync();
            await using var networkStream = await response.Content.ReadAsStreamAsync();
            using var memoryStream = new MemoryStream();
            await networkStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            var format = await Image.DetectFormatAsync(memoryStream);
            var imageBytes = await new HttpClient().GetByteArrayAsync(mainImageUrl);
            memoryStream.Position = 0;
            var image = await Image.LoadAsync(memoryStream);

            return new ImageResult
            { 
                Success = true,
                Image = image
            };
        }

        PointF DetermineWatermarkPosition(Image image, WatermarkPosition position, int offsetX, int offsetY) {
            return position switch
            {
                WatermarkPosition.TopLeft => new PointF(offsetX, offsetY),
                WatermarkPosition.TopRight => new PointF(image.Width - offsetX, offsetY),
                WatermarkPosition.BottomLeft => new PointF(offsetX, image.Height - offsetY),
                WatermarkPosition.BottomRight => new PointF(image.Width - offsetX, image.Height - offsetY),
                WatermarkPosition.Center => new PointF(image.Width / 2f + offsetX, image.Height / 2f + offsetY),
                WatermarkPosition.TopCenter => new PointF(image.Width / 2f + offsetX, offsetY),
                WatermarkPosition.BottomCenter => new PointF(image.Width / 2f + offsetX, image.Height - offsetY),
                WatermarkPosition.LeftCenter => new PointF(offsetX, image.Height / 2f + offsetY),
                WatermarkPosition.RightCenter => new PointF(image.Width - offsetX, image.Height / 2f + offsetY),
            };
        }

        public enum WatermarkPosition
        {
            TopLeft = 1,
            TopCenter = 2,
            TopRight = 3,
            LeftCenter = 4,
            Center = 5,
            RightCenter = 6,
            BottomLeft = 7,
            BottomCenter = 8,
            BottomRight = 9
        }
    }
}
