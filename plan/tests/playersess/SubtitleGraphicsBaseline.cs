// Exact pre-repair method, PlayerControl SHA256 18fac7cc1afcdf38f5896364f1606dd05446358965f3aae51cff00a7929b1e4f.
namespace SubtitleGraphicsProbe {
using System;
using System.Threading.Tasks;
class ActualOriginal : FixtureOwner
{
 public Task<Grid> Render(string text) => GenerateSubtitleItem(text);
        private async Task<Grid> GenerateSubtitleItem(string text)
        {
            //行首行尾加空格，防止字体描边超出
            text = " " + text.Replace("\n", " \n ") + " ";

            var fontSize = (float)SubtitleSettingSize.Value;
            var color = Utils.ToColor((SubtitleSettingColor.SelectedItem as ComboBoxItem).Tag.ToString());
            var borderColor = Utils.ToColor((SubtitleSettingBorderColor.SelectedItem as ComboBoxItem).Tag.ToString());

            CanvasHorizontalAlignment canvasHorizontalAlignment = CanvasHorizontalAlignment.Center;
            TextAlignment textAlignment = TextAlignment.Center;
            if (SubtitleSettingAlign.SelectedIndex == 1)
            {
                canvasHorizontalAlignment = CanvasHorizontalAlignment.Left;
                textAlignment = TextAlignment.Left;
            }
            else if (SubtitleSettingAlign.SelectedIndex == 2)
            {
                canvasHorizontalAlignment = CanvasHorizontalAlignment.Right;
                textAlignment = TextAlignment.Right;
            }
            CanvasDevice device = CanvasDevice.GetSharedDevice();
            CanvasTextFormat fmt = new CanvasTextFormat() { FontSize = fontSize, HorizontalAlignment = canvasHorizontalAlignment, };
            var tb = new TextBlock { Text = text, FontSize = fontSize, TextAlignment = textAlignment };
            if (SubtitleSettingBold.IsOn)
            {
                fmt.FontWeight = FontWeights.Bold;
                tb.FontWeight = FontWeights.Bold;
            }

            tb.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            var myBitmap = new CanvasRenderTarget(device, (float)tb.DesiredSize.Width + 4, (float)tb.DesiredSize.Height, displayInformation.LogicalDpi);

            CanvasTextLayout canvasTextLayout = new CanvasTextLayout(device, text, fmt, (float)tb.DesiredSize.Width + 4, (float)tb.DesiredSize.Height);

            CanvasGeometry combinedGeometry = CanvasGeometry.CreateText(canvasTextLayout);

            using (var ds = myBitmap.CreateDrawingSession())
            {
                ds.Clear(Colors.Transparent);
                ds.DrawGeometry(combinedGeometry, borderColor, 4f, new CanvasStrokeStyle()
                {
                    DashStyle = CanvasDashStyle.Solid
                });
                ds.FillGeometry(combinedGeometry, color);
            }
            Image image = new Image();
            BitmapImage im = new BitmapImage();
            using (InMemoryRandomAccessStream oStream = new InMemoryRandomAccessStream())
            {
                await myBitmap.SaveAsync(oStream, CanvasBitmapFileFormat.Png, 1.0f);
                await im.SetSourceAsync(oStream);
            }
            image.Width = tb.DesiredSize.Width;
            image.Source = im;
            image.Stretch = Stretch.Uniform;
            Grid grid = new Grid();

            grid.Tag = text;
            grid.Children.Add(image);

            return grid;
        }
}

}