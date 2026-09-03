using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Fiddler.Plugin.GumpCreator.UserControls
{
    public abstract class CanvasElement
    {
        [Category("Layout")]
        public int X { get; set; }
        [Category("Layout")]
        public int Y { get; set; }
        [Category("Layout")]
        public virtual int Width { get; set; }
        [Category("Layout")]
        public virtual int Height { get; set; }
        [Category("Layout")]
        public int Page { get; set; }
        [Category("Layout")]
        [Description("Z-order value for the element. Lower values are further back. Note: Canvas visual layering is primarily by list order.")]
        public int Z { get; set; } = 0;
        [Browsable(false)]
        public bool Selected { get; set; }
        [Browsable(false)]
        public int OriginalWidth { get; protected set; }
        [Browsable(false)]
        public int OriginalHeight { get; protected set; }
        [Browsable(false)]
        public bool IsResized => Width != OriginalWidth || Height != OriginalHeight;
        [Browsable(false)]
        public abstract CanvasElementType ElementType { get; }

        protected const int HandleSize = 8;

        protected CanvasElement(int x, int y, int page)
        {
            X = x;
            Y = y;
            Page = page;
        }

        public Rectangle Bounds => new Rectangle(X, Y, Width, Height);
        public virtual bool Contains(Point p) => Bounds.Contains(p);
        public abstract void Draw(Graphics g);
        public virtual void DisposeResources() { }

        public virtual void DrawSelection(Graphics g)
        {
            if (Selected)
            {
                using (Pen selectionPen = new Pen(Color.Blue, 1) { DashStyle = DashStyle.Dash })
                {
                    g.DrawRectangle(selectionPen, Bounds);
                }
                DrawResizeHandles(g);
            }
        }

        protected virtual void DrawResizeHandles(Graphics g)
        {
            foreach (Rectangle handleRect in GetResizeHandleRectangles().Values)
            {
                g.FillRectangle(Brushes.White, handleRect);
                g.DrawRectangle(Pens.Black, handleRect);
            }
        }

        public virtual Dictionary<ResizeHandle, Rectangle> GetResizeHandleRectangles()
        {
            var handles = new Dictionary<ResizeHandle, Rectangle>();
            int halfHandle = HandleSize / 2;

            handles[ResizeHandle.TopLeft] = new Rectangle(X - halfHandle, Y - halfHandle, HandleSize, HandleSize);
            handles[ResizeHandle.TopMiddle] = new Rectangle(X + Width / 2 - halfHandle, Y - halfHandle, HandleSize, HandleSize);
            handles[ResizeHandle.TopRight] = new Rectangle(X + Width - halfHandle, Y - halfHandle, HandleSize, HandleSize);
            handles[ResizeHandle.MiddleLeft] = new Rectangle(X - halfHandle, Y + Height / 2 - halfHandle, HandleSize, HandleSize);
            handles[ResizeHandle.MiddleRight] = new Rectangle(X + Width - halfHandle, Y + Height / 2 - halfHandle, HandleSize, HandleSize);
            handles[ResizeHandle.BottomLeft] = new Rectangle(X - halfHandle, Y + Height - halfHandle, HandleSize, HandleSize);
            handles[ResizeHandle.BottomMiddle] = new Rectangle(X + Width / 2 - halfHandle, Y + Height - halfHandle, HandleSize, HandleSize);
            handles[ResizeHandle.BottomRight] = new Rectangle(X + Width - halfHandle, Y + Height - halfHandle, HandleSize, HandleSize);

            return handles;
        }
        
        public virtual ResizeHandle GetResizeHandleAtPoint(Point p)
        {
            if (!Selected) return ResizeHandle.None;
            foreach(var kvp in GetResizeHandleRectangles())
            {
                if (kvp.Value.Contains(p)) return kvp.Key;
            }
            return ResizeHandle.None;
        }
    }
} 