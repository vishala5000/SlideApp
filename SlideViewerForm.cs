using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Windows.Forms;

namespace SlideApp {
    public class SlideViewerForm : Form {
        private readonly Project project;
        private int index = 0;
        private Image bg;
        private Font customFont;
        private PrivateFontCollection pfc;
        private Button btnBack, btnNext, btnExit;
        
        private Bitmap bgBuffer;
        private Bitmap textBuffer;
        private int cachedIndex = -1;
        private int cachedWidth = -1;
        private int cachedHeight = -1;
        
        private StringFormat sf;
        private SolidBrush whiteBrush;

        public SlideViewerForm(Project p) {
            project = p;
            this.SuspendLayout();
            Text = "Slide Viewer";
            FormBorderStyle = FormBorderStyle.Sizable;
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            KeyPreview = true;
            BackColor = Color.Black;
            Size = new Size(1280, 720);
            MinimumSize = new Size(800, 600);
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = true;
            MinimizeBox = true;

            string appBg = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bg.png");
            if (File.Exists(appBg)) {
                try {
                    using (var fs = new FileStream(appBg, FileMode.Open, FileAccess.Read)) {
                        using (var tempImg = Image.FromStream(fs)) { bg = new Bitmap(tempImg); }
                    }
                } catch { }
            }
            
            string appFont = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "font.ttf");
            if (File.Exists(appFont)) {
                try {
                    pfc = new PrivateFontCollection();
                    pfc.AddFontFile(appFont);
                    if (pfc.Families.Length > 0) customFont = new Font(pfc.Families[0], 48, FontStyle.Bold, GraphicsUnit.Point);
                } catch { pfc?.Dispose(); pfc = null; }
            }
            if (customFont == null) customFont = new Font("Segoe UI", 48, FontStyle.Bold, GraphicsUnit.Point);

            sf = new StringFormat {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.Word,
                FormatFlags = StringFormatFlags.LineLimit
            };
            whiteBrush = new SolidBrush(Color.White);

            btnBack = CreateNavButton("◀ Back");
            btnNext = CreateNavButton("Next ▶");
            btnExit = CreateNavButton("✕ Exit");

            btnBack.Click += (s, e) => { if (index > 0) { index--; InvalidateTextOnly(); } };
            btnNext.Click += (s, e) => { if (index < project.Slides.Count - 1) { index++; InvalidateTextOnly(); } };
            btnExit.Click += (s, e) => Close();

            this.Controls.Add(btnBack);
            this.Controls.Add(btnNext);
            this.Controls.Add(btnExit);

            this.Load += (s, e) => { PositionButtons(); RebuildBuffers(); };
            this.Resize += (s, e) => { PositionButtons(); RebuildBuffers(); };

            KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Escape) Close();
                else if (e.KeyCode == Keys.Left && index > 0) { index--; InvalidateTextOnly(); }
                else if (e.KeyCode == Keys.Right && index < project.Slides.Count - 1) { index++; InvalidateTextOnly(); }
            };
            this.ResumeLayout(false);
        }

        private void InvalidateTextOnly() {
            cachedIndex = -1;
            Invalidate();
        }

        private Button CreateNavButton(string text) {
            var btn = new Button {
                Text = text, Size = new Size(140, 50), Font = new Font("Segoe UI", 13, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(30, 30, 30), ForeColor = Color.White,
                Cursor = Cursors.Hand, Visible = true, TabStop = false
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 80);
            btn.FlatAppearance.BorderSize = 2;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(60, 60, 60);
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(15, 15, 15);
            return btn;
        }

        private void PositionButtons() {
            int w = this.ClientSize.Width, h = this.ClientSize.Height;
            btnBack.Location = new Point(25, h - 70);
            btnNext.Location = new Point(w - 165, h - 70);
            btnExit.Location = new Point(w - 140, 25);
        }

        private void RebuildBuffers() {
            int w = this.ClientSize.Width, h = this.ClientSize.Height;
            if (w <= 0 || h <= 0) return;
            
            if (bgBuffer == null || bgBuffer.Width != w || bgBuffer.Height != h) {
                bgBuffer?.Dispose();
                bgBuffer = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
                using (var g = Graphics.FromImage(bgBuffer)) {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    if (bg != null) {
                        float scale = Math.Min((float)w / bg.Width, (float)h / bg.Height);
                        int sw = (int)(bg.Width * scale), sh = (int)(bg.Height * scale);
                        g.DrawImage(bg, (w - sw) / 2, (h - sh) / 2, sw, sh);
                    } else {
                        using (var brush = new SolidBrush(Color.FromArgb(30, 60, 114))) g.FillRectangle(brush, 0, 0, w, h);
                    }
                }
            }
            
            if (textBuffer == null || textBuffer.Width != w || textBuffer.Height != h || cachedIndex != index || cachedWidth != w || cachedHeight != h) {
                RebuildTextBuffer(w, h);
            }
        }
        
        private void RebuildTextBuffer(int w, int h) {
            textBuffer?.Dispose();
            textBuffer = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            if (project.Slides.Count == 0 || index < 0 || index >= project.Slides.Count) {
                cachedIndex = index; cachedWidth = w; cachedHeight = h; return;
            }
            
            using (var g = Graphics.FromImage(textBuffer)) {
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                
                var text = project.Slides[index];
                float scale = Math.Min((float)w / (bg?.Width ?? 1920), (float)h / (bg?.Height ?? 1080));
                int sw = (int)((bg?.Width ?? 1920) * scale), sh = (int)((bg?.Height ?? 1080) * scale);
                int ox = (w - sw) / 2, oy = (h - sh) / 2;
                
                var textArea = new Rectangle(ox + (int)(sw * 0.1), oy + (int)(sh * 0.2), (int)(sw * 0.8), (int)(sh * 0.6));
                if (textArea.Width <= 0 || textArea.Height <= 0) return;
                
                float fontSize = FindOptimalFontSize(g, text, textArea);
                using (var font = new Font(customFont.FontFamily, fontSize, customFont.Style, GraphicsUnit.Point)) {
                    DrawStrokedTextFast(g, text, font, textArea);
                }
            }
            cachedIndex = index; cachedWidth = w; cachedHeight = h;
        }

        private float FindOptimalFontSize(Graphics g, string text, Rectangle rect) {
            float low = 16f, high = 140f, best = 16f;
            while (high - low > 0.5f) {
                float mid = (low + high) / 2f;
                using (var testFont = new Font(customFont.FontFamily, mid, customFont.Style, GraphicsUnit.Point)) {
                    var measured = g.MeasureString(text, testFont, rect.Width, sf);
                    if (measured.Height <= rect.Height * 0.95f) {
                        best = mid;
                        low = mid;
                    } else {
                        high = mid;
                    }
                }
            }
            return best;
        }

        private void DrawStrokedTextFast(Graphics g, string text, Font font, Rectangle rect) {
            using (var path = new GraphicsPath()) {
                path.AddString(text, font.FontFamily, (int)font.Style, font.SizeInPoints, rect, sf);
                using (var pen = new Pen(Color.Black, 6f) { LineJoin = LineJoin.Round }) {
                    g.DrawPath(pen, path);
                }
                g.FillPath(whiteBrush, path);
            }
        }

        protected override void OnPaint(PaintEventArgs e) {
            RebuildBuffers();
            var g = e.Graphics;
            if (bgBuffer != null) g.DrawImage(bgBuffer, 0, 0);
            if (textBuffer != null) g.DrawImage(textBuffer, 0, 0);
        }

        protected override void OnPaintBackground(PaintEventArgs e) { }

        // GUARANTEE CLEAN DISPOSAL
        protected override void OnFormClosed(FormClosedEventArgs e) {
            customFont?.Dispose();
            pfc?.Dispose();
            bg?.Dispose();
            bgBuffer?.Dispose();
            textBuffer?.Dispose();
            sf?.Dispose();
            whiteBrush?.Dispose();
            base.OnFormClosed(e);
        }
    }
}