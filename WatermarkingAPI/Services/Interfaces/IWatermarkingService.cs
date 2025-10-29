using SixLabors.ImageSharp;
using WatermarkingAPI.Helpers;

namespace WatermarkingAPI.Services.Interfaces
{
    public interface IWatermarkingService
    {
        Task<ImageResult> AddTextWatermarkAsync(string mainImageUrl, string watermarkText, string fontName, float angle,int? position);
        Task<ImageResult> AddImageWatermarkAsync(string mainImageUrl, string watermarkImageUrl,float angle, int? position);
    }
}
