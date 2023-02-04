using System;
using System.Drawing;
using System.Linq;

namespace iRacingDirector.Plugin.StandardOverlays
{
    public partial class MyPlugin
    {
        void DrawIntroFlashCard(int page)
        {
            var r = DrawFlashCardHeading("Qualifying Results");

            DrawFlashCardIntro(r, page);
        }

        void DrawFlashCardIntro(GraphicRect r, int page)
        {
            var totalWidth = FlashCardWidth;
            var left = FlashCardLeft;

            var thisPageOfQualifyingResults = EventData.QualifyingResults.Skip(page * DriversPerPage).Take(DriversPerPage);

            var offset = 5;
            Graphics.InRectangle(left, r.Rectangle.Top, totalWidth, 10)
                .WithPen(Styles.ThickBlackPen)
                .DrawLine(left + 8, r.Rectangle.Top - offset, left + totalWidth - 16, r.Rectangle.Top - offset);

            foreach (var qualifier in thisPageOfQualifyingResults)
            {
                var driver = EventData.GetCompetingDriverByIndex(qualifier.CarIdx);

                r.Center(cg => cg
                            .DrawText(qualifier.Position.ToString())
                            .AfterText(qualifier.Position.ToString())
                            .MoveRight(1)
                            .WithFont(Settings.FontName, 16, FontStyle.Bold)
                            .DrawText(qualifier.Position.Ordinal()))
                    .ToRight(width: 120, left: 30)
                    .DrawText(TimeSpan.FromSeconds(qualifier.FastestTime).ToString("mm\\:ss\\.ff"))
                    .ToRight(width: 60)
                    .DrawText(driver.CarNumber)
                    .ToRight(width: 300)
                    .DrawText(driver.UserName);

                r = r.ToBelow();

                Graphics.InRectangle(left, r.Rectangle.Top, totalWidth, 10)
                    .WithPen(Styles.ThickBlackPen)
                    .DrawLine(left + 8, r.Rectangle.Top - offset, left + totalWidth - 16, r.Rectangle.Top - offset);
            }
        }
    }
}
