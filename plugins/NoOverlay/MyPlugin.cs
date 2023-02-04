using iRacingDirector.Plugin;
//using iRacingSDK.Support;
using System.Drawing;

namespace NoOverlay
{
    public partial class MyPlugin
    {
        public Graphics Graphics;
        public EventData EventData;

        const int FlashCardWidth = 900;
        const int FlashCardLeft = (1920 / 2) - FlashCardWidth / 2;
        const int DriversPerPage = 10;
                
        public void IntroFlashCard(long duration, long timestamp)
        {
            //Do Nothing - below you can find a code example of JockeyOverlay

            /*var page = FlashCardPagingCalculator.GetPageNumber(EventData, DriversPerPage, duration, timestamp);

           DrawIntroFlashCard(page);*/
        }

        public void RaceOverlay(long timestamp)
        {
            //Do Nothing - below you can find a code example of JockeyOverlay

            /*var timeInSeconds = timestamp.FromNanoToSeconds();

            DrawLeaderboard(timeInSeconds.Seconds());
            DrawCurrentDriverRow();
            DrawRaceMessages(timeInSeconds);
            DrawFastestLap();*/
        }

        public void OutroFlashCard(long duration, long timestamp)
        {
            //Do Nothing - below you can find a code example of JockeyOverlay

            /*var page = FlashCardPagingCalculator.GetPageNumber(EventData, DriversPerPage, duration, timestamp);

            DrawOutroFlashCard(page);*/
        }
    }
}
