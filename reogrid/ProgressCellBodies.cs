using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using unvell.ReoGrid.CellTypes;
using unvell.ReoGrid.Graphics;
using unvell.ReoGrid.Rendering;

namespace mp.essentials.reogrid
{
    [Serializable]
    public class VerticalProgressCell : CellBody
    {
        public IColor Color { get; set; }

        /// <summary>
        /// Create progress cell body.
        /// </summary>
        public VerticalProgressCell()
        {
            Color = SolidColor.Gray;
        }

        private Rectangle _rect = new Rectangle(0, 0, 1, 1);

        /// <summary>
        /// Render the progress cell body.
        /// </summary>
        /// <param name="dc"></param>
        public override void OnPaint(CellDrawingContext dc)
        {
            double value = this.Cell.GetData<double>();

            if (value > 0)
            {
                var height = Bounds.Height - 1;
                var top = (float)(Bounds.Bottom + 1 - height * value);

                _rect.X = Bounds.Left;
                _rect.Y = top;
                _rect.Width = Bounds.Width - 1;
                _rect.Height = (float)(height * value);

                if (_rect.Width > 0 && _rect.Height > 0)
                {
                    dc.Graphics.FillRectangle(_rect, Color);
                }
            }
        }

        /// <summary>
        /// Clone a progress bar from this object.
        /// </summary>
        /// <returns>New instance of progress bar.</returns>
        public override ICellBody Clone()
        {
            return new VerticalProgressCell();
        }
    }

    [Serializable]
    public class HorizontalProgressCell : CellBody
    {
        public IColor Color { get; set; }

        /// <summary>
        /// Create progress cell body.
        /// </summary>
        public HorizontalProgressCell()
        {
            Color = SolidColor.Gray;
        }

        private Rectangle _rect = new Rectangle(0, 0, 1, 1);

        /// <summary>
        /// Render the progress cell body.
        /// </summary>
        /// <param name="dc"></param>
        public override void OnPaint(CellDrawingContext dc)
        {
            double value = this.Cell.GetData<double>();

            if (value > 0)
            {
                _rect.X = Bounds.Left;
                _rect.Y = Bounds.Top + 1;
                _rect.Width = (float)(Bounds.Width * value);
                _rect.Height = Bounds.Height - 1;

                if (_rect.Width > 0 && _rect.Height > 0)
                {
                    dc.Graphics.FillRectangle(_rect, Color);
                }
            }
        }

        /// <summary>
        /// Clone a progress bar from this object.
        /// </summary>
        /// <returns>New instance of progress bar.</returns>
        public override ICellBody Clone()
        {
            return new VerticalProgressCell();
        }
    }
}
