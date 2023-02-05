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
using System.Windows.Forms;

namespace iRacingDirector.Plugin
{
    public class CenterGraphicRect : GraphicRect
    {
        readonly int left;
        readonly int right;

        public CenterGraphicRect(Graphics g, Rectangle r, Brush b, Pen p, Font f, StringFormat sf, int lo, int to)
            : base(g, r, b, p, f, sf, lo, to)
        {
            this.left = int.MaxValue;
            this.right = int.MinValue;
        }

        public CenterGraphicRect(Graphics g, Rectangle r, Brush b, Pen p, Font f, StringFormat sf, int lo, int to, int left, int right)
            : base(g, r, b, p, f, sf, lo, to)
        {
            this.left = left;
            this.right = right;
        }

        protected override GraphicRect New(Graphics g, Rectangle r, Brush b, Pen p, Font f, StringFormat sf, int lo, int to)
        {
            return new CenterGraphicRect(g, r, b, p, f, sf, lo, to, left, right);
        }

        internal int Width { get { return right - left; } }

        public override GraphicRect DrawText(string text, int? leftOffset = 0, int? topOffset = 0)
        {
            var lo = this.leftOffset;
            var to = this.topOffset;

            if (leftOffset.HasValue)
                lo = leftOffset.Value;

            if (topOffset.HasValue)
                to = topOffset.Value;

            var size = TextRenderer.MeasureText(g, text, f, r.Size, TextFormatFlags.NoPadding);

            var newleft = Math.Min(r.Left + lo, left);
            var newRight = Math.Max(right, r.Left + lo + (int)size.Width);

            return new CenterGraphicRect(g, r, b, p, f, sf, lo, to, newleft, newRight);
        }

        public override GraphicRect DrawRectangleWithBorder()
        {
            return this;
        }
    }
}
