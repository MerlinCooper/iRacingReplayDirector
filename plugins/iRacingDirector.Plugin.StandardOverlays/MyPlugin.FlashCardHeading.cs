using System.Drawing;

namespace iRacingDirector.Plugin.StandardOverlays
{
    public partial class MyPlugin
    {
        GraphicRect DrawFlashCardHeading(string title)
        {
            var displayName = EventData.WeekendInfo.TrackDisplayName.ToUpper();

            Graphics.InRectangle(FlashCardLeft, 250, FlashCardWidth, 575)
                .DrawGrayBackground();

            Graphics.InRectangle(FlashCardLeft - 10, 311 - 2, FlashCardWidth - 100, 48)
                .DrawBlueGradientBox()
                .MoveDown(7)
                .MoveRight(20)
                .WithFontSizeOf(23)
                .WithStringFormat(StringAlignment.Near)
                .DrawText(title, topOffset: 4);

            Graphics.InRectangle(FlashCardLeft - 10, 240, FlashCardWidth - 100, 72)
                .DrawWhiteGradiantBox()
                .MoveDown(7)
                .MoveRight(20)
                .WithFontSizeOf(23)
                .WithStringFormat(StringAlignment.Near)
                .DrawText(displayName)
                .MoveDown(32)
                .WithFontSizeOf(17)
                .WithStringFormat(StringAlignment.Near)
                .DrawText(EventData.WeekendInfo.TrackCity.ToUpper() + ", " + EventData.WeekendInfo.TrackCountry.ToUpper());

            return Graphics.InRectangle(FlashCardLeft + 30, 400, 60, 40)
                .WithPen(Styles.BlackPen)
                .WithBrush(Styles.BlackBrush)
                .WithFontSizeOf(20)
                .WithStringFormat(StringAlignment.Near);
        }
    }
}
