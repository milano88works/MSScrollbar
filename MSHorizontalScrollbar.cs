using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace milano88.UI.Controls
{
    [DefaultEvent("ValueChanged")]

    public class MSHorizontalScrollbar : Control
    {
        public MSHorizontalScrollbar()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            this.Width = 250;
            this.Height = 10;
            this.Value = 0;
            this.BackColor = Color.Transparent;
            UpdateGraphicsBuffer();
        }

        #region Default Event
        [Description("Occurs when the scroll percentage is changed")]
        public event EventHandler ValueChanged;
        #endregion

        #region Overrides
        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _isMouseOverSlider = false;
            this.Invalidate(_knobRect);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _isMouseOverSlider = false;
            this.Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            bool maxedLeft = e.X < 0 && Percent == 0F;
            bool maxedRight = e.X > this.Width && Percent == 1F;
            if (_isMouseOverSlider && !(maxedLeft || maxedRight))
            {
                KnobX += e.X - _lastX;
                _lastX = e.X;
                this.Invalidate();
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                _isMouseOverSlider = true;
                _lastX = e.X;
                KnobX = e.X - _thumbWidth / 2;
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            int delta = -(e.Delta / 120);
            if (delta == -1) KnobX += MouseWheelScroll;
            else if (delta == 1) KnobX -= MouseWheelScroll;
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            _knobRect.X = (int)((Size.Width - _knobRect.Width) * Percent + 0.5);
            UpdateGraphicsBuffer();
            _knobRect.Height = this.Height;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            DrawSlider(_bufGraphics.Graphics);
            _bufGraphics.Render(e.Graphics);
        }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            if (Parent != null && BackColor == Color.Transparent)
            {
                Rectangle rect = new Rectangle(Left, Top, Width, Height);
                _bufGraphics.Graphics.TranslateTransform(-rect.X, -rect.Y);
                try
                {
                    using (PaintEventArgs pea = new PaintEventArgs(_bufGraphics.Graphics, rect))
                    {
                        pea.Graphics.SetClip(rect);
                        InvokePaintBackground(Parent, pea);
                        InvokePaint(Parent, pea);
                    }
                }
                finally
                {
                    _bufGraphics.Graphics.TranslateTransform(rect.X, rect.Y);
                }
            }
            else
            {
                using (SolidBrush backColor = new SolidBrush(this.BackColor))
                    _bufGraphics.Graphics.FillRectangle(backColor, ClientRectangle);
            }
        }
        #endregion

        #region Properties
        private BufferedGraphics _bufGraphics;
        private Rectangle _knobRect;
        private int _lastX;
        private bool _isMouseOverSlider;

        private int KnobX
        {
            get { return _knobRect.X; }
            set
            {
                if (value < 0) value = 0;
                if (value > this.Width - _knobRect.Width) value = this.Width - _knobRect.Width;
                Percent = (float)_knobRect.Left / (this.Width - _knobRect.Width);
                _knobRect.X = value;
            }
        }

        private float Percent
        {
            get { return (_value - _minimum) / (float)(_maximum - _minimum); }
            set
            {
                if (value > 1F) value = 1F;
                if (value < 0F) value = 0F;
                float val = (_maximum - _minimum) * value;
                Value = (int)(val + _minimum + 0.5);
                this.Invalidate();
            }
        }

        private Color _scrollbarColor = Color.Gainsboro;
        private Color _thumbColor = Color.LightCoral;
        private bool _rounded = false;

        [Category("Custom Properties")]
        [DefaultValue(0)]
        public bool RoundedCorners
        {
            get => _rounded;
            set
            {
                _rounded = value;
                this.Invalidate();
            }
        }

        [Category("Custom Properties")]
        [Description("The scrollbar back color")]
        [DefaultValue(typeof(Color), "Transparent")]
        public override Color BackColor { get => base.BackColor; set => base.BackColor = value; }

        [Category("Custom Properties")]
        [Description("Thumb color")]
        [DefaultValue(typeof(Color), "LightCoral")]
        public Color ThumbColor
        {
            get { return _thumbColor; }
            set { _thumbColor = value; this.Invalidate(); }
        }

        private int _thumbWidth = 50;
        [Category("Custom Properties")]
        [Description("Thumb Width")]
        [DefaultValue(50)]
        public int ThumbWidth
        {
            get => _thumbWidth;
            set
            {
                _thumbWidth = value;
                _knobRect.Size = new Size(_thumbWidth, this.Height);
                OnSizeChanged(EventArgs.Empty);
                this.Invalidate();
            }
        }

        [Category("Custom Properties")]
        [Description("The scrollbar color")]
        [DefaultValue(typeof(Color), "Gainsboro")]
        public Color ScrollbarColor
        {
            get { return _scrollbarColor; }
            set { _scrollbarColor = value; this.Invalidate(); }
        }

        private int _maximum = 100;
        [Description("The highest possible value")]
        [Category("Custom Properties")]
        [DefaultValue(100)]
        [RefreshProperties(RefreshProperties.Repaint)]
        public int Maximum
        {
            get { return _maximum; }
            set
            {
                if (value <= _minimum) throw new ArgumentOutOfRangeException("Value must be greater than Minimum");
                _maximum = value;
                if (_value > _maximum) Value = _maximum;
                UpdateKnobX();
                this.Invalidate();
            }
        }

        private int _minimum;
        [Description("The lowest possible value")]
        [Category("Custom Properties")]
        [DefaultValue(0)]
        [RefreshProperties(RefreshProperties.Repaint)]
        public int Minimum
        {
            get { return _minimum; }
            set
            {
                if (value >= _maximum) throw new ArgumentOutOfRangeException("Value must be less than Maximum");
                _minimum = value;
                if (_value < _minimum) Value = _minimum;
                UpdateKnobX();
                this.Invalidate();
            }
        }

        private int _value;
        [Description("The position of the scrollbar")]
        [Category("Custom Properties")]
        [DefaultValue(0)]
        public int Value
        {
            get { return _value; }
            set
            {
                if (value < _minimum || value > _maximum) throw new ArgumentOutOfRangeException("value must be less than or equal to Maximum and greater than or equal to Minimum");
                bool changed = value != _value;
                if (changed)
                {
                    _value = value;
                    this.Invalidate();
					ValueChanged?.Invoke(this, EventArgs.Empty);
                }
                UpdateKnobX();
            }
        }

        [Category("Custom Properties")]
        [DefaultValue(10)]
        public int MouseWheelScroll { get; set; } = 10;

        [Browsable(false)]
        public override Image BackgroundImage { get => base.BackgroundImage; set { } }
        [Browsable(false)]
        public override ImageLayout BackgroundImageLayout { get => base.BackgroundImageLayout; set { } }
        [Browsable(false)]
        public override Font Font { get => base.Font; set { } }
        [Browsable(false)]
        public override string Text { get => base.Text; set { } }
        [Browsable(false)]
        public override Color ForeColor { get => base.ForeColor; set { } }
        #endregion

        private void UpdateGraphicsBuffer()
        {
            if (this.Width > 0 && this.Height > 0)
            {
                _knobRect.Size = new Size(_thumbWidth, this.Height);
                BufferedGraphicsContext context = BufferedGraphicsManager.Current;
                context.MaximumBuffer = new Size(this.Width + 1, this.Height + 1);
                _bufGraphics = context.Allocate(this.CreateGraphics(), this.ClientRectangle);
                IncreaseGraphicsQuality(_bufGraphics.Graphics);
            }
        }

        private void IncreaseGraphicsQuality(Graphics graphics)
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        }

        private void UpdateKnobX() => _knobRect.X = (int)((Size.Width - _knobRect.Width) * Percent + 0.5);

        protected virtual void DrawSlider(Graphics graphics)
        {
            if (_rounded)
            {
                Rectangle rectBorderSmooth = this.ClientRectangle;
                Rectangle rectBorder = Rectangle.Inflate(rectBorderSmooth, -1, -1);
                Rectangle rectThumbSmooth = new Rectangle(KnobX, 0, _thumbWidth, this.Height);
                Rectangle rectThumb = Rectangle.Inflate(rectThumbSmooth, -1, -1);
                using (GraphicsPath pathBorderSmooth = GetRoundPath(rectBorderSmooth, this.Height))
                using (GraphicsPath pathBorder = GetRoundPath(rectBorder, this.Height - 1))
                using (GraphicsPath pathThumb = GetRoundPath(rectThumb, this.Height))
                using (SolidBrush srollbarColorBrush = new SolidBrush(_scrollbarColor))
                using (SolidBrush thumbBrush = new SolidBrush(_thumbColor))
                {
                    this.Region = new Region(pathBorderSmooth);
                    graphics.FillPath(srollbarColorBrush, pathBorder);
                    graphics.FillPath(thumbBrush, pathThumb);
                }
            }
            else
            {
                this.Region = new Region(this.ClientRectangle);
                Rectangle rect = new Rectangle(0, 0, this.Width, this.Height);
                using (SolidBrush srollbarColorBrush = new SolidBrush(_scrollbarColor))
                using (SolidBrush thumbBrush = new SolidBrush(_thumbColor))
                {
                    graphics.FillRectangle(srollbarColorBrush, rect);
                    graphics.FillRectangle(thumbBrush, KnobX, 0, _thumbWidth, this.Height);
                }
            }
        }

        GraphicsPath GetRoundPath(Rectangle Rect, int radius, float width = 0)
        {
            radius = (int)Math.Max((Math.Min(radius, Math.Min(Rect.Width, Rect.Height))), 1);
            float r2 = radius / 2f;
            float w2 = width / 2f;
            GraphicsPath GraphPath = new GraphicsPath();
            GraphPath.AddArc(Rect.X + w2, Rect.Y + w2, radius, radius, 180, 90);
            GraphPath.AddArc(Rect.X + Rect.Width - radius - w2, Rect.Y + w2, radius, radius, 270, 90);
            GraphPath.AddArc(Rect.X + Rect.Width - w2 - radius, Rect.Y + Rect.Height - w2 - radius, radius, radius, 0, 90);
            GraphPath.AddArc(Rect.X + w2, Rect.Y - w2 + Rect.Height - radius, radius, radius, 90, 90);
            GraphPath.AddLine(Rect.X + w2, Rect.Y + Rect.Height - r2 - w2, Rect.X + w2, Rect.Y + r2 + w2);
            return GraphPath;
        }
    }
}
