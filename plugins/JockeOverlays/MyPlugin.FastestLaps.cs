using iRacingDirector.Plugin;
using System;
using System.Drawing;

namespace JockeOverlays
{
    public partial class MyPlugin
    {
        public FastLap FastLap;

        void DrawFastestLap()
        {
            if (FastLap == null)
                return;

            const int left = 1920 - 80 - 450;
            const int top = 900;

            Graphics.InRectangle(left, top + 34, 400, 34)
                .DrawBlueGradientBox();

            Graphics.InRectangle(left, top + 34, 250, 34)
                .WithBrush(Styles.WhiteBrush)
                .WithFontSizeOf(19)
                .WithStringFormat(StringAlignment.Center)
                .DrawText(FastLap.Driver.UserName, topOffset: 5)
                .ToRight(width: 150)
                .DrawText(TimeSpan.FromSeconds(FastLap.Time).ToString(@"mm\:ss\.fff"), topOffset: 5);

            Graphics.InRectangle(left, top, 400, 34)
               .DrawWhiteGradiantBox()
                .WithBrush(Styles.BlackBrush)
                .WithFontSizeOf(18)
                .WithStringFormat(StringAlignment.Center)
                .DrawText("New Fast Lap", topOffset: 5);
        }
    }
}
