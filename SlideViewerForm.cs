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
        
        // Animation properties
        private Timer animationTimer;
        private float animationProgress = 1.0f;
        private int animationDirection = 0; // 0 = none, 1 = next (from right), -1 = back (from left)
        private const int ANIMATION_DURATION_MS = 400;
        private DateTime animationStartTime;

        public SlideViewerForm(Project p) {
            project = p;
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
            DoubleBuffered = true;
            KeyPreview = true;
            BackColor = Color.Black;
            TopMost = true;

            // Load Background safely
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
                        float fontSize = Math.Max(16f, Math.Min(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height) / 14f);
                        customFont = new Font(pfc.Families[0], fontSize, FontStyle.Bold);
                    }
                } catch {
                    pfc?.Dispose();
                    pfc = null;
                }
            }

            if (customFont == null) {
                float fontSize = Math.Max(16f, Math.Min(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height) / 14f);
                customFont = new Font("Segoe UI", fontSize, FontStyle.Bold);
            }

            // Button overlay panel
            buttonPanel = new Panel {
                BackColor = Color.Transparent,
                Dock = DockStyle.Fill
            };

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

            // Setup animation timer
            animationTimer = new Timer();
            animationTimer.Interval = 16; // ~60 FPS
            animationTimer.Tick += AnimationTimer_Tick;
        }

        private void GoToNextSlide() {
            if (index < project.Slides.Count - 1 && animationDirection == 0) {
                index++;
                StartAnimation(1); // Animate from right
            }
        }

        private void GoToPreviousSlide() {
            if (index > 0 && animationDirection == 0) {
                index--;
                StartAnimation(-1); // Animate from left
            }
        }

        private void StartAnimation(int direction) {
            animationDirection = direction;
            animationProgress = 0.0f;
            animationStartTime = DateTime.Now;
            animationTimer.Start();
        }

        private void AnimationTimer_Tick(object sender, EventArgs e) {
            var elapsed = (DateTime.Now - animationStartTime).TotalMilliseconds;
            animationProgress = (float)Math.Min(1.0, elapsed / ANIMATION_DURATION_MS);
            
            Invalidate(); // Trigger repaint
            
            if (animationProgress >= 1.0f) {
                animationTimer.Stop();
                animationDirection = 0;
                animationProgress = 1.0f;
            }
        }

        // Easing function for smooth animation
        private float EaseOutCubic(float t) {
            return 1.0f - (float)Math.Pow(1.0 - t, 3);
        }

        private Button CreateNavButton(string text) {
            var btn = new Button {
                Text = text,
                Size = new Size(130, 45),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Visible = true,
                TabStop = false
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(100, 100, 100);
            btn.FlatAppearance.BorderSize = 2;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(70, 70, 70);
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(20, 20, 20);
            return btn;
        }

        private void PositionButtons() {
            if (btnBack == null) return;
            int w = this.ClientSize.Width;
            int h = this.ClientSize.Height;
            btnBack.Location = new Point(20, h - 65);
            btnNext.Location = new Point(w - 150, h - 65);
            btnExit.Location = new Point(w - 130, 20);
        }

        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            if (bg != null) g.DrawImage(bg, ClientRectangle);
            else g.Clear(Color.FromArgb(30, 60, 114));

            if (project.Slides.Count == 0 || index < 0 || index >= project.Slides.Count) return;

            var text = project.Slides[index];
            var baseRect = new Rectangle(ClientRectangle.Width / 10, ClientRectangle.Height / 5, 
                                        ClientRectangle.Width * 8 / 10, ClientRectangle.Height * 3 / 5);
            
            if (baseRect.Width <= 0 || baseRect.Height <= 0) return;

            // Apply animation
            if (animationDirection != 0 && animationProgress < 1.0f) {
                float easedProgress = EaseOutCubic(animationProgress);
                
                // Calculate offset based on direction
                float maxOffset = ClientRectangle.Width * 0.3f;
                float offset = maxOffset * (1.0f - easedProgress) * animationDirection;
                
                // Calculate opacity (fade in)
                float opacity = easedProgress;
                
                // Create animated rectangle
                var animRect = new Rectangle(
                    (int)(baseRect.X + offset),
                    baseRect.Y,
                    baseRect.Width,
                    baseRect.Height
                );
                
                DrawStrokedTextAnimated(g, text, customFont, animRect, opacity);
            } else {
                // No animation, draw normally
                DrawStrokedText(g, text, customFont, baseRect);
            }
        }

        private void DrawStrokedTextAnimated(Graphics g, string text, Font font, Rectangle rect, float opacity) {
            using (var sf = new StringFormat { 
                Alignment = StringAlignment.Center, 
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.Word,
                FormatFlags = StringFormatFlags.LineLimit
            }) {
                int stroke = 4;
                
                // Black stroke with opacity
                using (var black = new SolidBrush(Color.FromArgb((int)(255 * opacity), 0, 0, 0))) {
                    for (int x = -stroke; x <= stroke; x++) {
                        for (int y = -stroke; y <= stroke; y++) {
                            if (x == 0 && y == 0) continue;
                            if (x * x + y * y > stroke * stroke) continue;
                            g.DrawString(text, font, black, new Rectangle(rect.X + x, rect.Y + y, rect.Width, rect.Height), sf);
                        }
                    }
                }
                
                // White text with opacity
                using (var white = new SolidBrush(Color.FromArgb((int)(255 * opacity), 255, 255, 255))) {
                    g.DrawString(text, font, white, rect, sf);
                }
            }
        }

        private void DrawStrokedText(Graphics g, string text, Font font, Rectangle rect) {
            DrawStrokedTextAnimated(g, text, font, rect, 1.0f);
        }

        protected override void OnFormClosed(FormClosedEventArgs e) {
            animationTimer?.Stop();
            animationTimer?.Dispose();
            customFont?.Dispose();
            pfc?.Dispose();
            bg?.Dispose();
            base.OnFormClosed(e);
        }
    }
}