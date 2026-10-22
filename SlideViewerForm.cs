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
        private Bitmap bgBuffer;
        private Font customFont;
        private PrivateFontCollection pfc;
        private Button btnBack, btnNext, btnExit;
        private Panel buttonPanel;
        private Panel titleBar;
        private Label lblTitle;
        private Button btnMin, btnMax, btnClose;
        
        // Animation
        private Timer animationTimer;
        private float animationProgress = 1.0f;
        private int animationDirection = 0;
        private const int ANIMATION_DURATION_MS = 450;
        private DateTime animationStartTime;
        
        // Dragging
        private bool dragging = false;
        private Point dragCursorPoint;
        private Point dragFormPoint;

        public SlideViewerForm(Project p) {
            project = p;
            FormBorderStyle = FormBorderStyle.None;
            DoubleBuffered = true;
            KeyPreview = true;
            BackColor = Color.Black;
            Size = new Size(1280, 720);
            StartPosition = FormStartPosition.CenterScreen;

            // Custom title bar
            titleBar = new Panel {
                Height = 35,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(30, 30, 30),
                Cursor = Cursors.SizeAll
            };
            titleBar.MouseDown += TitleBar_MouseDown;
            titleBar.MouseMove += TitleBar_MouseMove;
            titleBar.MouseUp += TitleBar_MouseUp;

            lblTitle = new Label {
                Text = "Slide Viewer",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Location = new Point(10, 8),
                AutoSize = true
            };
            titleBar.Controls.Add(lblTitle);

            // Window control buttons
            btnMin = CreateTitleBarButton("—", 35);
            btnMin.Click += (s, e) => WindowState = FormWindowState.Minimized;
            
            btnMax = CreateTitleBarButton("□", 35);
            btnMax.Click += (s, e) => {
                WindowState = WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized;
                BuildBackgroundBuffer();
            };
            
            btnClose = CreateTitleBarButton("✕", 35);
            btnClose.BackColor = Color.FromArgb(232, 17, 35);
            btnClose.Click += (s, e) => Close();

            titleBar.Controls.Add(btnMin);
            titleBar.Controls.Add(btnMax);
            titleBar.Controls.Add(btnClose);
            Controls.Add(titleBar);

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

            this.Load += (s, e) => {
                PositionButtons();
                PositionTitleBarButtons();
                BuildBackgroundBuffer();
            };
            this.Resize += (s, e) => {
                PositionButtons();
                PositionTitleBarButtons();
                BuildBackgroundBuffer();
            };

            KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Escape) Close();
                else if (e.KeyCode == Keys.Left) GoToPreviousSlide();
                else if (e.KeyCode == Keys.Right) GoToNextSlide();
            };

            animationTimer = new Timer();
            animationTimer.Interval = 16;
            animationTimer.Tick += AnimationTimer_Tick;
        }

        private Button CreateTitleBarButton(string text, int size) {
            var btn = new Button {
                Text = text,
                Size = new Size(size, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(62, 62, 66);
            return btn;
        }

        private void PositionTitleBarButtons() {
            if (btnClose == null) return;
            int w = this.ClientSize.Width;
            btnClose.Location = new Point(w - 35, 0);
            btnMax.Location = new Point(w - 70, 0);
            btnMin.Location = new Point(w - 105, 0);
        }

        private void TitleBar_MouseDown(object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) {
                dragging = true;
                dragCursorPoint = Cursor.Position;
                dragFormPoint = this.Location;
            }
        }

        private void TitleBar_MouseMove(object sender, MouseEventArgs e) {
            if (dragging) {
                Point dif = Point.Subtract(Cursor.Position, new Size(dragCursorPoint));
                this.Location = Point.Add(dragFormPoint, new Size(dif));
            }
        }

        private void TitleBar_MouseUp(object sender, MouseEventArgs e) {
            dragging = false;
        }

        private void BuildBackgroundBuffer() {
            if (ClientSize.Width <= 0 || ClientSize.Height <= 0) return;
            bgBuffer?.Dispose();
            bgBuffer = new Bitmap(ClientSize.Width, ClientSize.Height);
            using (var g = Graphics.FromImage(bgBuffer)) {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                if (bg != null) {
                    g.DrawImage(bg, 0, 0, ClientSize.Width, ClientSize.Height);
                } else {
                    using (var brush = new SolidBrush(Color.FromArgb(30, 60, 114))) {
                        g.FillRectangle(brush, 0, 0, ClientSize.Width, ClientSize.Height);
                    }
                }
            }
        }

        private void GoToNextSlide() {
            if (index < project.Slides.Count - 1 && animationDirection == 0) {
                index++;
                StartAnimation(1);
            }
        }

        private void GoToPreviousSlide() {
            if (index > 0 && animationDirection == 0) {
                index--;
                StartAnimation(-1);
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
            Invalidate();
            
            if (animationProgress >= 1.0f) {
                animationTimer.Stop();
                animationDirection = 0;
                animationProgress = 1.0f;
            }
        }

        private float EaseOutCubic(float t) {
            return 1.0f - (float)Math.Pow(1.0 - t, 3.0);
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
            btnExit.Location = new Point(w - 140, 60);
        }

        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAlias;

            if (bgBuffer != null) {
                g.DrawImage(bgBuffer, 0, 0);
            } else {
                g.Clear(Color.FromArgb(30, 60, 114));
            }

            if (project.Slides.Count == 0 || index < 0 || index >= project.Slides.Count) return;

            var text = project.Slides[index];
            
            var textArea = new Rectangle(
                ClientSize.Width / 10,
                ClientSize.Height / 5 + 35,
                ClientSize.Width * 8 / 10,
                ClientSize.Height * 3 / 5
            );
            
            if (textArea.Width <= 0 || textArea.Height <= 0) return;

            float fontSize = FindOptimalFontSize(g, text, textArea);
            
            using (var font = new Font(customFont.FontFamily, fontSize, customFont.Style)) {
                if (animationDirection != 0 && animationProgress < 1.0f) {
                    float eased = EaseOutCubic(animationProgress);
                    float opacity = eased;
                    float yOffset = 25f * (1.0f - eased) * animationDirection;
                    
                    var animRect = new Rectangle(
                        textArea.X,
                        (int)(textArea.Y + yOffset),
                        textArea.Width,
                        textArea.Height
                    );
                    
                    DrawStrokedText(g, text, font, animRect, opacity);
                } else {
                    DrawStrokedText(g, text, font, textArea, 1.0f);
                }
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

        private void DrawStrokedText(Graphics g, string text, Font font, Rectangle rect, float opacity) {
            using (var sf = new StringFormat { 
                Alignment = StringAlignment.Center, 
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.Word,
                FormatFlags = StringFormatFlags.LineLimit
            }) {
                int stroke = 4;
                int alpha = (int)(255 * Math.Max(0, Math.Min(1, opacity)));
                
                using (var black = new SolidBrush(Color.FromArgb(alpha, 0, 0, 0))) {
                    for (int x = -stroke; x <= stroke; x++) {
                        for (int y = -stroke; y <= stroke; y++) {
                            if (x == 0 && y == 0) continue;
                            if (x * x + y * y > stroke * stroke) continue;
                            g.DrawString(text, font, black, 
                                new Rectangle(rect.X + x, rect.Y + y, rect.Width, rect.Height), sf);
                        }
                    }
                }
                
                using (var white = new SolidBrush(Color.FromArgb(alpha, 255, 255, 255))) {
                    g.DrawString(text, font, white, rect, sf);
                }
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e) {
            animationTimer?.Stop();
            animationTimer?.Dispose();
            customFont?.Dispose();
            pfc?.Dispose();
            bg?.Dispose();
            bgBuffer?.Dispose();
            base.OnFormClosed(e);
        }
    }
}