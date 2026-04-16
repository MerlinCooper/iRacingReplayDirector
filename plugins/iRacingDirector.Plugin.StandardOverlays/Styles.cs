using System.Drawing;

namespace iRacingDirector.Plugin.StandardOverlays
{
    public static class Styles
    {
        public const int AlphaLevel = 120;
        public static readonly Color White = Color.FromArgb(AlphaLevel, Color.White);
        public static readonly Color WhiteSmoke = Color.FromArgb(AlphaLevel, Color.WhiteSmoke);
        public static readonly Color Black = Color.FromArgb(AlphaLevel, Color.Black);
        public static readonly Color LightYellow = Color.FromArgb(AlphaLevel, Color.LightYellow);
        public static readonly Color Yellow = Color.FromArgb(AlphaLevel, 150, 150, 0);

        public static readonly Color BlueFlagBlue = Color.FromArgb(0x02, 0x5F, 0xE9);
        public static readonly Color BlueFlagBlueDark = Color.FromArgb(0x00, 0x4B, 0xBD);

        public static readonly Color BlueFlagYellow = Color.FromArgb(0xE8, 0xD1, 0x03);
        public static readonly Color BlueFlagYellowDark = Color.FromArgb(0xB8, 0xA0, 0x00);

        public static readonly Pen BlackPen = new Pen(Black);
        public static readonly Pen ThickBlackPen = new Pen(Black, 2);

        public static readonly Brush BlackBrush = new SolidBrush(Color.Black);
        public static readonly Brush RedBrush = new SolidBrush(Color.Red);
        public static readonly Brush WhiteBrush = new SolidBrush(Color.White);
        public static readonly Brush YellowBrush = new SolidBrush(Color.Yellow);
        public static readonly Brush BlueFlagYellowBrush = new SolidBrush(Color.FromArgb(0xE8, 0xD1, 0x03));
        public static readonly Brush TransparentLightBlueBrush = new SolidBrush(Color.FromArgb(AlphaLevel, Color.LightBlue));
        public static readonly Brush TransparentLightGray = new SolidBrush(Color.FromArgb(180, Color.Gray));
        public static readonly Brush TransparentLighterGray = new SolidBrush(Color.FromArgb(40, Color.LightGray));
    }
}
