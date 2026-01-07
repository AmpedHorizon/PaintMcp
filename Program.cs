using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;

namespace PaintMcp
{
    public class Program
    {
        [McpServerToolType]
        public static class paint_tools
        {
            private static Bitmap? _canvas;
            private static Graphics? _g;
            private static bool _saveMode = false;

            private static string DesktopPath =>
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "generated.png");

            private static void AutoSave()
            {
                if (_saveMode && _canvas != null)
                {
                    _canvas.Save(DesktopPath, ImageFormat.Png);
                }
            }

            private static void EnsureCanvas()
            {
                if (_canvas == null)
                {
                    create_new_canvas(500, 500, "#FFFFFF");
                }
            }

            private static void ApplyQualitySettings()
            {
                if (_g == null) return;

                _g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                _g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                _g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                _g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            }

            // -------------------------------
            // SAVE MODE
            // -------------------------------
            [McpServerTool, Description("Enables or disables automatic saving after each draw operation.")]
            public static void set_save_mode(
                [Description("If true, saves the canvas after each operation as generated.png on the Desktop.")]
        bool enabled)
            {
                _saveMode = enabled;
                AutoSave();
            }

            // -------------------------------
            // CREATE NEW CANVAS
            // -------------------------------
            [McpServerTool, Description("Creates a new canvas in pixels with a background color.")]
            public static void create_new_canvas(
                [Description("Canvas width in pixels")] int width,
                [Description("Canvas height in pixels")] int height,
                [Description("Background color (e.g. Red, #FF0000), leave empty for transparent")] string backgroundHtmlColor)
            {
                _canvas?.Dispose();
                _g?.Dispose();

                _canvas = new Bitmap(width, height);
                _g = Graphics.FromImage(_canvas);
                ApplyQualitySettings();
                _g.Clear(ColorTranslator.FromHtml(backgroundHtmlColor));

                AutoSave();
            }

            // -------------------------------
            // EXPORT CURRENT CANVAS AS BASE64
            // -------------------------------
            [McpServerTool, Description("Exports the current in-memory canvas as a Base64-encoded PNG.")]
            public static string get_image_as_base64()
            {
                if (_canvas == null)
                    return string.Empty;

                using var ms = new MemoryStream();
                _canvas.Save(ms, ImageFormat.Png);
                return "data:image/png;base64," + Convert.ToBase64String(ms.ToArray());
            }

            // -------------------------------
            // DRAW PIXEL
            // -------------------------------
            [McpServerTool, Description("Draws a single pixel (or dot) on the canvas.")]
            public static void draw_pixel(
                [Description("X coordinate in pixels")] int x,
                [Description("Y coordinate in pixels")] int y,
                [Description("Pixel thickness (diameter in pixels)")] int thickness,
                [Description("Pixel color in HTML format (e.g. #FF0000 or Red)")] string htmlColor)
            {
                EnsureCanvas();
                using var brush = new SolidBrush(ColorTranslator.FromHtml(htmlColor));
                _g.FillEllipse(brush, x, y, thickness, thickness);
                AutoSave();
            }

            // -------------------------------
            // DRAW LINE (WITH START & END POINT)
            // -------------------------------
            [McpServerTool, Description("Draws a line between two points.")]
            public static void draw_line(
                [Description("Start X")] int x1,
                [Description("Start Y")] int y1,
                [Description("End X")] int x2,
                [Description("End Y")] int y2,
                [Description("Line thickness")] int thickness,
                [Description("Line color (e.g. #FF0000 or Red)")] string color)
            {
                EnsureCanvas();

                using var pen = new Pen(ColorTranslator.FromHtml(color), thickness);
                _g.DrawLine(pen, x1, y1, x2, y2);

                AutoSave();
            }

            [McpServerTool, Description("Draws an ellipse using center coordinates and radii.")]
            public static void draw_ellipse(
    [Description("Center X coordinate of the ellipse")] int centerX,
    [Description("Center Y coordinate of the ellipse")] int centerY,
    [Description("Horizontal radius (half-width)")] int radiusX,
    [Description("Vertical radius (half-height)")] int radiusY,
    [Description("Outline thickness in pixels (ignored if fill is true)")] int thickness,
    [Description("Ellipse color in HTML format")] string color,
    [Description("If true, the ellipse is filled; otherwise only the outline")] bool fill)
            {
                EnsureCanvas();

                int x = centerX - radiusX;
                int y = centerY - radiusY;
                int width = radiusX * 2;
                int height = radiusY * 2;

                var rect = new Rectangle(x, y, width, height);
                var c = ColorTranslator.FromHtml(color);

                if (fill)
                {
                    using var brush = new SolidBrush(c);
                    _g.FillEllipse(brush, rect);
                }
                else
                {
                    using var pen = new Pen(c, thickness);
                    _g.DrawEllipse(pen, rect);
                }

                AutoSave();
            }

            // -------------------------------
            // DRAW RECTANGLE
            // -------------------------------
            [McpServerTool, Description("Draws a rectangle on the canvas using start and end coordinates.")]
            public static void draw_rect(
                [Description("Top-left X coordinate of the rectangle in pixels")] int x1,
                [Description("Top-left Y coordinate of the rectangle in pixels")] int y1,
                [Description("Bottom-right X coordinate of the rectangle in pixels")] int x2,
                [Description("Bottom-right Y coordinate of the rectangle in pixels")] int y2,
                [Description("Outline thickness in pixels (ignored if fill is true)")] int thickness,
                [Description("Rectangle color in HTML format (e.g. #FF0000 or Green)")] string color,
                [Description("If true, the rectangle is filled; if false, only the outline is drawn")] bool fill)
            {
                EnsureCanvas();

                // Calculate width and height from coordinates
                int width = x2 - x1;
                int height = y2 - y1;

                var rect = new Rectangle(x1, y1, width, height);
                var c = ColorTranslator.FromHtml(color);

                if (fill)
                {
                    using var brush = new SolidBrush(c);
                    _g.FillRectangle(brush, rect);
                }
                else
                {
                    using var pen = new Pen(c, thickness);
                    _g.DrawRectangle(pen, rect);
                }
                AutoSave();
            }

            // -------------------------------
            // DRAW TRIANGLE
            // -------------------------------
            [McpServerTool, Description("Draws a triangle on the canvas using three explicit vertex coordinates.")]
            public static void draw_triangle(
                [Description("X coordinate of the first vertex")] int x1,
                [Description("Y coordinate of the first vertex")] int y1,
                [Description("X coordinate of the second vertex")] int x2,
                [Description("Y coordinate of the second vertex")] int y2,
                [Description("X coordinate of the third vertex")] int x3,
                [Description("Y coordinate of the third vertex")] int y3,
                [Description("Outline thickness in pixels (ignored if fill is true)")] int thickness,
                [Description("Triangle color in HTML format (e.g. #FF00FF or Blue)")] string color,
                [Description("If true, the triangle is filled; if false, only the outline is drawn")] bool fill)
            {
                EnsureCanvas();

                Point[] points =
                {
        new Point(x1, y1),
        new Point(x2, y2),
        new Point(x3, y3)
    };

                var c = ColorTranslator.FromHtml(color);

                if (fill)
                {
                    using var brush = new SolidBrush(c);
                    _g.FillPolygon(brush, points);
                }
                else
                {
                    using var pen = new Pen(c, thickness);
                    _g.DrawPolygon(pen, points);
                }

                AutoSave();
            }

        }

        public static async Task Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);
            builder.Logging.AddConsole(consoleLogOptions =>
            {
                // Configure all logs to go to stderr
                consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
            });
            //Task.Run(async () =>
            //{
            //    await Task.Delay(1000);
            //    paint_tools.create_new_canvas(500, 500, "");
            //    paint_tools.set_save_mode(true);
            //    paint_tools.draw_rect(0, 0, 100, 100, 10, "#ff0000", true);
            //});
            builder.Services
                .AddMcpServer()
                .WithStdioServerTransport()
                .WithToolsFromAssembly();
            await builder.Build().RunAsync();
        }
    }
}
