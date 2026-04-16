using System;
using System.Drawing;
using System.Linq;

namespace iRacingDirector.Plugin.StandardOverlays
{
    public partial class MyPlugin
    {
        public LeaderBoard LeaderBoard;
        const int leaderBoardWidth = 189;
        const int headerFontSize = 12;
        const int fontSize = 16;
        const int offset = 4;

        void DrawLeaderboard(TimeSpan timeInSeconds)
        {
            var maxRows = LeaderBoard.LapCounter == null ? 22 : 21;
            var showPitStopCount = timeInSeconds.Minutes % 3 == 0 && timeInSeconds.Seconds < 30 && LeaderBoard.Drivers.Take(maxRows).Any(d => d.PitStopCount > 0);
            var numberOfDrivers = LeaderBoard.Drivers.Take(maxRows).Count();

            var width = showPitStopCount ? leaderBoardWidth + 30 : leaderBoardWidth;
            var height = 35 + numberOfDrivers * 32 + 23;
            if (LeaderBoard.LapCounter != null)
                height += 35;

            Graphics.InRectangle(80, 80, width, height)
                .DrawGrayBackground();

            var r = Graphics.InRectangle(80, 80, width, 35)
                .WithBrush(Styles.BlackBrush)
                .WithFontSizeOf(20)
                .WithStringFormat(StringAlignment.Center);

            var headR = Graphics.InRectangle(60, 60, leaderBoardWidth, 35);

            if (LeaderBoard.LapCounter != null)
            {
                r = r.ToBelow();

                headR.ToBelow()
                    .DrawBlueGradientBox()
                    .WithFont(Settings.FontName, 18, FontStyle.Bold)
                    .WithStringFormat(StringAlignment.Near)
                    .DrawText(LeaderBoard.LapCounter, topOffset: 8, leftOffset: 20);
            }

            headR
               .DrawWhiteGradiantBox()
               .WithFontSizeOf(23)
               .WithStringFormat(StringAlignment.Center)
               .DrawText(LeaderBoard.RacePosition);

            r = r
                .ToBelow(width: 36, height: 23)
                .MoveRight(8);

            var n1 = r
                .WithFontSize(headerFontSize)
                .DrawText("Pos", topOffset: offset)
                .ToRight(width: 58)
                .WithFontSize(headerFontSize)
                .DrawText("Num", topOffset: offset);

            if (showPitStopCount)
                n1 = n1.ToRight(width: 30)
                    .WithFontSize(headerFontSize)
                    .DrawText("Pit", topOffset: offset);

            n1.ToRight(width: 95)
                .WithFontSize(headerFontSize)
                .WithStringFormat(StringAlignment.Near)
                .DrawText("Name", leftOffset: 10, topOffset: offset);

            foreach (var d in LeaderBoard.Drivers.Take(maxRows))
            {
                r = r.ToBelow(width: 36, height: 32);

                var position = d.Position != null ? d.Position.Value.ToString() : "";

                var n = r
                    .WithFontSize(fontSize)
                    .DrawText(position, topOffset: offset)
                    .ToRight(width: 58)
                    .DrawText(d.CarNumber, topOffset: offset);

                var pitStopCount = d.PitStopCount != 0 ? d.PitStopCount.ToString() : " ";
                if (showPitStopCount)
                    n = n.ToRight(width: 30)
                    .WithFontSize(fontSize)
                    .DrawText(pitStopCount, topOffset: offset);

                n.ToRight(width: 95)
                    .WithFontSize(fontSize)
                    .WithStringFormat(StringAlignment.Near)
                    .DrawText(d.ShortName.FormattedForLeaderboard(), leftOffset: 10, topOffset: offset)

                    .WithPen(Styles.ThickBlackPen)
                    .DrawLine(r.Rectangle.Left, r.Rectangle.Top, r.Rectangle.Left + width - 16, n.Rectangle.Top);
            }
        }
    }
}
