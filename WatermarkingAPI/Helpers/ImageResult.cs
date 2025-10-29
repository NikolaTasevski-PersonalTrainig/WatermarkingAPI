using SixLabors.ImageSharp;

namespace WatermarkingAPI.Helpers
{
    public class ImageResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public int? StatusCode { get; set; }
        public Image? Image { get; set; }
    }
}
