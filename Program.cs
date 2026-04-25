using SkiaSharp;

const int width = 400;
const int height = 400;

using var bitmap = new SKBitmap(width, height);
using var canvas = new SKCanvas(bitmap);

canvas.Clear(SKColors.White);

var center = new SKPoint(width / 2f, height / 2f);

using var outerPaint = new SKPaint
{
    Style = SKPaintStyle.Stroke,
    Color = SKColors.DodgerBlue,
    StrokeWidth = 8,
    IsAntialias = true
};

using var innerPaint = new SKPaint
{
    Style = SKPaintStyle.Stroke,
    Color = SKColors.OrangeRed,
    StrokeWidth = 6,
    IsAntialias = true
};

canvas.DrawCircle(center, 120, outerPaint);
canvas.DrawCircle(center, 55, innerPaint);

using var image = SKImage.FromBitmap(bitmap);
using var data = image.Encode(SKEncodedImageFormat.Png, 100);

var outputPath = Path.Combine(AppContext.BaseDirectory, "circles.png");
using var stream = File.OpenWrite(outputPath);
data.SaveTo(stream);

Console.WriteLine($"Saved drawing to: {outputPath}");
