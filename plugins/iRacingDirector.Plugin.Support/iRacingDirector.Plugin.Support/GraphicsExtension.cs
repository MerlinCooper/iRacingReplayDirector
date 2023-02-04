// This file is part of iRacingReplayDirector.
//
// Copyright 2014 Dean Netherton
// https://github.com/vipoo/iRacingReplayDirector.net
//
// iRacingReplayDirector is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// iRacingReplayDirector is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with iRacingReplayDirector.  If not, see <http://www.gnu.org/licenses/>.
//

using System;
using System.Drawing;

namespace iRacingDirector.Plugin
{
    public static class GraphicsExtension
    {
        public static GraphicRect InRectangle(this Graphics g, int x, int y, int w, int h)
        {
            return new GraphicRect(g, new Rectangle(x, y, w, h));
        }

        public static GraphicRect With(this Graphics g)
        {
            return new GraphicRect(g, new Rectangle(0, 0, 0, 0));
        }

        public static Color BrightenBy(this Color self, double amount)
        {
            Func<byte, int> adjust = x => Math.Min((int)(x * amount), 255);

            return Color.FromArgb(adjust(self.R), adjust(self.G), adjust(self.B));
        }
    }
}
