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
        private Panel buttonPanel;

        public SlideViewerForm(Project p) {
            project = p;
            Text = "Slide Viewer";
            FormBorderStyle = FormBorderStyle.Sizable;
            DoubleBuffered = true;
            KeyPreview = true;
            BackColor = Color.Black;
            Size = new Size(1280, 720);
            MinimumSize = new Size(800, 600);
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = true;
            MinimizeBox = true;

            // Load Background
            string appBg = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bg.png");
            if (File.Exists(appBg)) {
                try {
                    using (var fs = new FileStream(appBg, FileMode.Open, FileAccess.Read)) {
                        using (var tempImg = Image.FromStream(fs)) {
                            bg = new Bitmap(tempImg);
                        }
                    }
                } catch { }
            }
            
            // Load Custom Font
            string appFont = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "font.ttf");
            if (File.Exists(appFont)) {
                try {
                    pfc = new PrivateFontCollection();
                    pfc.AddFontFile(appFont);
                    if (pfc.Families.Length > 0) {
                        customFont = new Font(pfc.Families[0], 48, FontStyle.Bold);
                    }
                } catch {
                    pfc?.Dispose();
                    pfc = null;
                }
            }

            if (customFont == null) {
                customFont = new Font("Segoe UI", 48, FontStyle.Bold);
            }

            // Button overlay
            buttonPanel = new Panel { BackColor = Color.Transparent, Dock = DockStyle.Fill };

            btnBack = CreateNavButton("◀ Back");
            btnNext = CreateNavButton("Next ▶");
            btnExit = CreateNavButton("✕ Exit");

            btnBack.Click += (s, e) => GoToPreviousSlide();
            btnNext.Click += (s, e) => GoToNextSlide();
            btnExit.Click += (s, e) => Close();

            buttonPanel.Controls.Add(btnBack);
            buttonPanel.Controls.Add(btnNext);
            buttonPanel.Controls.Add(btnExit);
            this.Controls.Add(buttonPanel);

            btnBack.BringToFront();
            btnNext.BringToFront();
            btnExit.BringToFront();
            buttonPanel.BringToFront();

            this.Load += (s, e) => PositionButtons();
            this.Resize += (s, e) => PositionButtons();

            KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Escape) Close();
                else if (e.KeyCode == Keys.Left) GoToPreviousSlide();
                else if (e.KeyCode == Keys.Right) GoToNextSlide();
            };
        }

        private void GoToNextSlide() {
            if (index < project.Slides.Count - 1) {
                index++;
                Invalidate();
            }
        }

        private void GoToPreviousSlide() {
            if (index > 0) {
                index--;
                Invalidate();
            }
        }

        private Button CreateNavButton(string text) {
            var btn = new Button {
                Text = text,
                Size = new Size(140, 50),
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Visible = true,
                TabStop = false
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 80);
            btn.FlatAppearance.BorderSize = 2;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(60, 60, 60);
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(15, 15, 15);
            return btn;
        }

        private void PositionButtons() {
            if (btnBack == null) return;
            int w = this.ClientSize.Width;
            int h = this.ClientSize.Height;
            btnBack.Location = new Point(25, h - 70);
            btnNext.Location = new Point(w - 165, h - 70);
            btnExit.Location = new Point(w - 140, 25);
        }

        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            // Draw background (scaled to fit window while maintaining 16:9 aspect ratio)
            if (bg != null) {
                // Calculate scaling to fit background in window
                float scaleX = (float)ClientSize.Width / bg.Width;
                float scaleY = (float)ClientSize.Height / bg.Height;
                float scale = Math.Min(scaleX, scaleY);
                
                int scaledWidth = (int)(bg.Width * scale);
                int scaledHeight = (int)(bg.Height * scale);
                int offsetX = (ClientSize.Width - scaledWidth) / 2;
                int offsetY = (ClientSize.Height - scaledHeight) / 2;
                
                g.DrawImage(bg, offsetX, offsetY, scaledWidth, scaledHeight);
                
                // Text area is STRICTLY within the scaled background
                var textArea = new Rectangle(
                    offsetX + (int)(scaledWidth * 0.1),
                    offsetY + (int)(scaledHeight * 0.2),
                    (int)(scaledWidth * 0.8),
                    (int)(scaledHeight * 0.6)
                );
                
                if (textArea.Width <= 0 || textArea.Height <= 0) return;
                
                if (project.Slides.Count == 0 || index < 0 || index >= project.Slides.Count) return;
                
                var text = project.Slides[index];
                
                // Auto-fit font size
                float fontSize = FindOptimalFontSize(g, text, textArea);
                
                using (var font = new Font(customFont.FontFamily, fontSize, customFont.Style)) {
                    // INSTANT rendering - NO animation, draws everything at once
                    DrawStrokedTextInstant(g, text, font, textArea);
                }
            } else {
                g.Clear(Color.FromArgb(30, 60, 114));
            }
        }

        private float FindOptimalFontSize(Graphics g, string text, Rectangle rect) {
            using (var sf = new StringFormat {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.Word,
                FormatFlags = StringFormatFlags.LineLimit
            }) {
                float size = 120f;
                float minSize = 16f;
                
                while (size >= minSize) {
                    using (var testFont = new Font(customFont.FontFamily, size, customFont.Style)) {
                        var measured = g.MeasureString(text, testFont, rect.Width, sf);
                        if (measured.Height <= rect.Height && measured.Width <= rect.Width) {
                            return size;
                        }
                    }
                    size -= 2f;
                }
                return minSize;
            }
        }

        // INSTANT rendering - NO animation, draws everything in one pass
        private void DrawStrokedTextInstant(Graphics g, string text, Font font, Rectangle rect) {
            using (var sf = new StringFormat { 
                Alignment = StringAlignment.Center, 
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.Word,
                FormatFlags = StringFormatFlags.LineLimit
            }) {
                int stroke = 4;
                
                // Draw all stroke positions in ONE pass (no animation)
                using (var black = new SolidBrush(Color.Black)) {
                    for (int x = -stroke; x <= stroke; x++) {
                        for (int y = -stroke; y <= stroke; y++) {
                            if (x == 0 && y == 0) continue;
                            if (x * x + y * y > stroke * stroke) continue;
                            g.DrawString(text, font, black, 
                                new Rectangle(rect.X + x, rect.Y + y, rect.Width, rect.Height), sf);
                        }
                    }
                }
                
                // Draw white text on top in ONE pass
                using (var white = new SolidBrush(Color.White)) {
                    g.DrawString(text, font, white, rect, sf);
                }
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e) {
            customFont?.Dispose();
            pfc?.Dispose();
            bg?.Dispose();
            base.OnFormClosed(e);
        }
    }
}