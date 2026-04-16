using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace iRacingDirector.Plugin.StandardOverlays
{
    public static class MyPluginExtensions
    {
        public static string FormattedForLeaderboard(this string shortName)
        {
            var length = Math.Min(4, shortName.Length);
            return shortName.Substring(0, length).ToUpper();
        }

        public static GraphicRect DrawGrayBackground(this GraphicRect rr)
        {
            return rr
                .WithBrush(Styles.TransparentLightGray)
                .WithPen(Styles.BlackPen)
                .DrawRectangleWithBorder();
        }

        public static GraphicRect WithFontSizeOf(this GraphicRect rr, int fontSize)
        {
            return rr
                .WithFont(Settings.FontName, fontSize, FontStyle.Bold);
        }

        public static GraphicRect DrawWhiteGradiantBox(this GraphicRect rr)
        {
            return rr
                .WithLinearGradientBrush(Color.DarkGray, Color.White, LinearGradientMode.Vertical)
                .WithPen(Styles.BlackPen)
                .DrawRectangleWithBorder()
                .WithBrush(Styles.BlackBrush);
        }

        public static GraphicRect DrawBlueGradientBox(this GraphicRect rr)
        {
            return rr
                .WithHeight(rr.Rectangle.Height + 3)
                .MoveUp(3)
                .WithLinearGradientBrush(Styles.BlueFlagBlueDark, Styles.BlueFlagBlue, LinearGradientMode.Vertical)
                .DrawRoundRectangle(5)
                .WithBrush(Styles.WhiteBrush);
        }
    }
}
