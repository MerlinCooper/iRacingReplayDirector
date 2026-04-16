using System.Drawing;
using System.Drawing.Drawing2D;

namespace iRacingDirector.Plugin.StandardOverlays
{
    public partial class MyPlugin
    {
        public Driver CamDriver;

        void DrawCurrentDriverRow()
        {
            var position = CamDriver.Position != null ? CamDriver.Position.Value.ToString() : "";
            var indicator = CamDriver.Position != null ? CamDriver.Position.Value.Ordinal() : "";

            var offset = 5;

            Graphics.InRectangle(1920 / 2 - 440 / 2, 980, 70, 40)
                .WithBrush(Styles.BlueFlagYellowBrush)
                .WithPen(Styles.BlackPen)
                .DrawRectangleWithBorder()
                .WithFontSizeOf(24)
                .WithBrush(Styles.BlackBrush)
                .WithStringFormat(StringAlignment.Near)
                .Center(cg => cg
                            .DrawText(position, topOffset: offset)
                            .AfterText(position)
                            .MoveRight(3)
                            .WithFont(Settings.FontName, 18, FontStyle.Bold)
                            .DrawText(indicator, topOffset: offset)
                )

                .ToRight(width: 70)
                .WithLinearGradientBrush(Styles.White, Styles.WhiteSmoke, LinearGradientMode.BackwardDiagonal)
                .DrawRectangleWithBorder()
                .WithStringFormat(StringAlignment.Center)
                .WithBrush(Styles.BlackBrush)
                .DrawText(CamDriver.CarNumber, topOffset: offset)

                .ToRight(width: 300)
                .WithLinearGradientBrush(Styles.White, Styles.WhiteSmoke, LinearGradientMode.BackwardDiagonal)
                .DrawRectangleWithBorder()
                .WithStringFormat(StringAlignment.Center)
                .WithBrush(Styles.BlackBrush)
                .DrawText(CamDriver.UserName, topOffset: offset);
        }
    }
}
