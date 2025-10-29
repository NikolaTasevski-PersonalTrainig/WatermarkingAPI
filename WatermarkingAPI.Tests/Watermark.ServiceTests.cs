using NUnit.Framework;
using Moq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Threading.Tasks;
using Serilog;
using Serilog.Events;
using WatermarkingAPI.Tests.Helpers;

namespace WatermarkingAPI.Tests;

[TestFixture]
public class Tests
{
    private Serilog.ILogger _logger;
    private WatermarkingAPI.Services.WatermarkingService _watermarkingService;
    private SerilogTestHelper _serilogHelper;
    private List<string> _logCapture;

    [SetUp]
    public void Setup()
    {
        _logCapture = new List<string>();
        _logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Console()
            .WriteTo.Sink(new ListSink(_logCapture))
            .WriteTo.File("test-logs.txt"
                , rollingInterval: RollingInterval.Day,
                shared: true)
            .CreateLogger();
        _watermarkingService = new WatermarkingAPI.Services.WatermarkingService(_logger);
        _serilogHelper = new SerilogTestHelper();


    }

    [TearDown]
    public void Teardown()
    {
        Log.CloseAndFlush();

        if (_logger is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    [Test]
    [TestCase("https://picsum.photos/800/600.jpg", "https://i1.wp.com/static.free-logo-design.net/uploads/2020/06/free-falcon-logo-design.jpg", 45f, 1, TestName = "ValidImages_45Degree_TopLeft")]
    public async Task AddImageWatermarkAsync(string mainImageUrl, string watermarkImageUrl, float angle, int? position)
    {
        DeleteTestLogFiles();
        var imageResult = await _watermarkingService.AddImageWatermarkAsync(mainImageUrl, watermarkImageUrl, angle, position);

        var logFiles = GetLogFiles();

        var hasWatermarkLog = _logCapture.Any(log => log.Contains("Text watermarking took"));
        Assert.That(hasWatermarkLog, Is.True, "Should log information message with execution time");

        Assert.That(imageResult.Success, Is.True);
    }

    private void DeleteTestLogFiles()
    {
        try
        {
            var logFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), "test-logs*.txt");
            foreach (var logFile in logFiles)
            {
                File.Delete(logFile);
                TestContext.WriteLine($"Deleted: {logFile}");
            }
            TestContext.WriteLine($"Cleaned up {logFiles.Length} log files");
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Warning: Could not delete log files: {ex.Message}");
        }
    }

    private string[] GetLogFiles() {
        var logFiles = Directory.GetFiles(".", "test-logs*.txt");
        foreach (var logFile in logFiles)
        {
            string fileContent;
            using (var stream = new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(stream))
            {
                fileContent = reader.ReadToEnd();
            }

            if (fileContent.Contains("Text watermarking took"))
            {
                var lines = fileContent.Split('\n');
                var watermarkLine = lines.First(line => line.Contains("Text watermarking took"));
                _logCapture.AddRange(lines.Where(line => !string.IsNullOrWhiteSpace(line)));

                break;
            }
        }

        return logFiles;
    }
}
