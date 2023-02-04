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
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace iRacingDirector.Plugin
{
    public class GraphicRect
    {
        protected readonly Graphics g;
        protected readonly Rectangle r;
        protected readonly Brush b;
        protected readonly Pen p;
        protected readonly Font f;
        protected readonly StringFormat sf;
        protected readonly int leftOffset = 0;
        protected readonly int topOffset = 0;

        public GraphicRect(Graphics g, Rectangle r)
        {
            this.g = g;
            this.r = r;
        }

        internal GraphicRect(Graphics g, Rectangle r, Brush b, Pen p, Font f, StringFormat sf, int lo, int to)
        {
            this.g = g;
            this.r = r;
            this.b = b;
            this.p = p;
            this.f = f;
            this.sf = sf;
            this.leftOffset = lo;
            this.topOffset = to;
        }

        public Rectangle Rectangle { get { return r; } }

        protected virtual GraphicRect New(Graphics g, Rectangle r, Brush b, Pen p, Font f, StringFormat sf, int lo, int to)
        {
            return new GraphicRect(g, r, b, p, f, sf, lo, to);
        }

        public GraphicRect InRectangle(int x, int y, int w, int h)
        {
            return new GraphicRect(g, new Rectangle(x, y, w, h), b, p, f, sf, leftOffset, topOffset);
        }

        public GraphicRect WithLinearGradientBrush(Color color1, Color color2, LinearGradientMode linearGradientMode)
        {
            return New(g, r, new LinearGradientBrush(r, color1, color2, linearGradientMode), p, f, sf, leftOffset, topOffset);
        }

        public GraphicRect With(Func<GraphicRect, GraphicRect> modifiers)
        {
            return modifiers(this);
        }

        public GraphicRect Draw(Func<GraphicRect, GraphicRect> modifiers)
        {
            return modifiers(this);
        }

        public GraphicRect WithPen(Pen pen)
        {
            return New(g, r, b, pen, f, sf, leftOffset, topOffset);
        }

        public virtual GraphicRect DrawRectangleWithoutBorder()
        {
            g.FillRectangle(b, r);
            return this;
        }

        public virtual GraphicRect DrawRectangleWithBorder()
        {
            g.FillRectangle(b, r);
            g.DrawRectangle(p, r);
            return this;
        }

        const int TEXT_LEFT_OFFSET_MAGIC = -4;
        const int TEXT_RIGHT_PADDING_MAGIC = 10;


        public virtual GraphicRect DrawText(string text, int? leftOffset = null, int? topOffset = null)
        {
            var lo = this.leftOffset;
            var to = this.topOffset;

            if (leftOffset.HasValue)
                lo = leftOffset.Value;

            if (topOffset.HasValue)
                to = topOffset.Value;

            g.TextRenderingHint = TextRenderingHint.AntiAlias;
            var rect2 = new Rectangle(r.Left + lo + TEXT_LEFT_OFFSET_MAGIC, r.Top + to, r.Width + TEXT_RIGHT_PADDING_MAGIC, r.Height);
            g.DrawString(text, f, b, rect2, sf);
            return this;
        }

        public virtual GraphicRect WithTextOffset(int? leftOffset = null, int? topOffset = null)
        {
            var lo = this.leftOffset;
            var to = this.topOffset;

            if (leftOffset.HasValue)
                lo = leftOffset.Value;

            if (topOffset.HasValue)
                to = topOffset.Value;

            return new GraphicRect(g, r, b, p, f, sf, lo, to);
        }

        public virtual GraphicRect DrawText(int number, int leftOffset = 0, int topOffset = 0)
        {
            return DrawText(number.ToString(), leftOffset, topOffset);
        }

        public GraphicRect AfterText(string str, int i = 0)
        {
            var size = TextRenderer.MeasureText(g, str, f, new Size(0, 0), TextFormatFlags.NoPadding);

            var newRect = new Rectangle(r.Left + (int)size.Width + i, r.Top, r.Width - (int)size.Width - i, r.Height);
            return New(g, newRect, b, p, f, sf, leftOffset, topOffset);
        }

        public GraphicRect MoveRight(int right)
        {
            return New(g, new Rectangle(r.Left + right, r.Top, r.Width, r.Height), b, p, f, sf, leftOffset, topOffset);
        }

        public GraphicRect MoveLeft(int left)
        {
            return New(g, new Rectangle(r.Left - left, r.Top, r.Width, r.Height), b, p, f, sf, leftOffset, topOffset);
        }

        public GraphicRect MoveDown(int top)
        {
            return New(g, new Rectangle(r.Left, r.Top + top, r.Width, r.Height + top), b, p, f, sf, leftOffset, topOffset);
        }

        public GraphicRect MoveUp(int v)
        {
            return New(g, new Rectangle(r.Left, r.Top - v, r.Width, r.Height), b, p, f, sf, leftOffset, topOffset);
        }

        public GraphicRect WithHeight(int v)
        {
            return New(g, new Rectangle(r.Left, r.Top, r.Width, v), b, p, f, sf, leftOffset, topOffset);
        }

        public GraphicRect WithBrush(Brush brush)
        {
            return New(g, r, brush, p, f, sf, leftOffset, topOffset);
        }

        public GraphicRect WithFont(string prototype, float emSize, FontStyle style)
        {
            return New(g, r, b, p, new Font(prototype, emSize, style), sf, leftOffset, topOffset);
        }

        public GraphicRect WithFontSize(float emSize)
        {
            return New(g, r, b, p, new Font(this.f.Name, emSize, f.Style), sf, leftOffset, topOffset);
        }

        public GraphicRect ToBelow(int? width = null, int? height = null)
        {
            var w = width == null ? r.Width : width.Value;
            var h = height == null ? r.Height : height.Value;

            return New(g, new Rectangle(r.Left, r.Top + r.Height, w, h), b, p, f, sf, leftOffset, topOffset);
        }

        public GraphicRect ToRight(int? width = null, int? height = null, int left = 0)
        {
            var w = width == null ? r.Width : width.Value;
            var h = height == null ? r.Height : height.Value;

            return New(g, new Rectangle(r.Left + r.Width + left, r.Top, w, h), b, p, f, sf, leftOffset, topOffset);
        }

        public GraphicRect WithStringFormat(StringAlignment alignment, StringAlignment lineAlignment = StringAlignment.Near)
        {
            var sf = new StringFormat { Alignment = alignment, LineAlignment = lineAlignment };
            return New(g, r, b, p, f, sf, leftOffset, topOffset);
        }

        public GraphicRect DrawLine(float x1, float y1, float x2, float y2)
        {
            g.DrawLine(p, x1, y1, x2, y2);
            return this;
        }

        public GraphicRect Center(Func<GraphicRect, GraphicRect> operation)
        {
            var newG = new CenterGraphicRect(g, r, b, p, f, sf, leftOffset, topOffset);

            var calculateCenter = (CenterGraphicRect)operation(newG);

            var width = calculateCenter.Width;
            var currentCenterPoint = r.Left + r.Width / 2;

            var newRect = new Rectangle(currentCenterPoint - width / 2, r.Top, width, r.Height);

            var centeredGr = new GraphicRect(g, newRect, b, p, f, sf, leftOffset, topOffset);

            operation(centeredGr).ToBelow();

            return this;
        }

        public GraphicRect DrawRoundRectangle(float radius)
        {
            var rectangle = new RectangleF(r.Left, r.Top, r.Width, r.Height);
            var path = this.GetRoundedRect(rectangle, radius);
            g.FillPath(b, path);

            return this;
        }

        GraphicsPath GetRoundedRect(RectangleF baseRect, float radius)
        {
            if (radius <= 0.0F)
            {
                var mPath = new GraphicsPath();
                mPath.AddRectangle(baseRect);
                mPath.CloseFigure();
                return mPath;
            }

            if (radius >= (Math.Min(baseRect.Width, baseRect.Height)) / 2.0)
                return GetCapsule(baseRect);

            var diameter = radius * 2.0F;
            var sizeF = new SizeF(diameter, diameter);
            var arc = new RectangleF(baseRect.Location, sizeF);
            var path = new GraphicsPath();

            path.AddArc(arc, 180, 90);

            arc.X = baseRect.Right - diameter;
            path.AddArc(arc, 270, 90);

            arc.Y = baseRect.Bottom - diameter;
            path.AddArc(arc, 0, 90);

            arc.X = baseRect.Left;
            path.AddArc(arc, 90, 90);

            path.CloseFigure();
            return path;
        }

        GraphicsPath GetCapsule(RectangleF baseRect)
        {
            var path = new GraphicsPath();
            try
            {
                if (baseRect.Width > baseRect.Height)
                {
                    var diameter = baseRect.Height;
                    var sizeF = new SizeF(diameter, diameter);
                    var arc = new RectangleF(baseRect.Location, sizeF);
                    path.AddArc(arc, 90, 180);
                    arc.X = baseRect.Right - diameter;
                    path.AddArc(arc, 270, 180);
                }
                else if (baseRect.Width < baseRect.Height)
                {
                    var diameter = baseRect.Width;
                    var sizeF = new SizeF(diameter, diameter);
                    var arc = new RectangleF(baseRect.Location, sizeF);
                    path.AddArc(arc, 180, 180);
                    arc.Y = baseRect.Bottom - diameter;
                    path.AddArc(arc, 0, 180);
                }
                else
                    path.AddEllipse(baseRect);
            }
            catch (Exception)
            {
                path.AddEllipse(baseRect);
            }
            finally
            {
                path.CloseFigure();
            }
            return path;
        }
    }
}
