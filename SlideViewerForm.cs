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
        private Button btnBack, btnNext, btnExit;

        public SlideViewerForm(Project p) {
            project = p;
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
            DoubleBuffered = true;
            KeyPreview = true;
            BackColor = Color.Black;

            var bgPath = ProjectStore.GetBgPath(p.Name);
            if (File.Exists(bgPath)) {
                try { bg = Image.FromFile(bgPath); } catch { }
            }

            btnBack = NavBtn("◀ Back");
            btnNext = NavBtn("Next ▶");
            btnExit = NavBtn("✕ Exit");

            btnBack.Click += (s, e) => { if (index > 0) { index--; Invalidate(); } };
            btnNext.Click += (s, e) => { if (index < project.Slides.Count - 1) { index++; Invalidate(); } };
            btnExit.Click += (s, e) => Close();

            Controls.Add(btnBack); Controls.Add(btnNext); Controls.Add(btnExit);

            Layout += (s, e) => {
                btnBack.Location = new Point(20, ClientSize.Height - 60);
                btnNext.Location = new Point(ClientSize.Width - 140, ClientSize.Height - 60);
                btnExit.Location = new Point(ClientSize.Width - 110, 20);
            };

            KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Escape) Close();
                else if (e.KeyCode == Keys.Left && index > 0) { index--; Invalidate(); }
                else if (e.KeyCode == Keys.Right && index < project.Slides.Count - 1) { index++; Invalidate(); }
            };
        }

        private Button NavBtn(string t) => new Button {
            Text = t, Size = new Size(120, 40),
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(180, 0, 0, 0),
            ForeColor = Color.White
        };

        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            if (bg != null) g.DrawImage(bg, ClientRectangle);
            else g.Clear(Color.FromArgb(30, 60, 114));

            if (project.Slides.Count == 0) return;
            if (index < 0 || index >= project.Slides.Count) return;

            var text = project.Slides[index];
            float fontSize = Math.Max(16f, Math.Min(ClientSize.Width, ClientSize.Height) / 14f);
            using (var font = new Font("Segoe UI", fontSize, FontStyle.Bold)) {
                var rect = new Rectangle(
                    ClientSize.Width / 10,
                    ClientSize.Height / 5,
                    ClientSize.Width * 8 / 10,
                    ClientSize.Height * 3 / 5);
                DrawStrokedText(g, text, font, rect);
            }
        }

        private void DrawStrokedText(Graphics g, string text, Font font, Rectangle rect) {
            var sf = new StringFormat {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
                FormatFlags = StringFormatFlags.NoClip
            };
            int stroke = 4;
            using (var black = new SolidBrush(Color.Black)) {
                for (int x = -stroke; x <= stroke; x++) {
                    for (int y = -stroke; y <= stroke; y++) {
                        if (x == 0 && y == 0) continue;
                        if (x * x + y * y > stroke * stroke) continue;
                        var r = new Rectangle(rect.X + x, rect.Y + y, rect.Width, rect.Height);
                        g.DrawString(text, font, black, r, sf);
                    }
                }
            }
            using (var white = new SolidBrush(Color.White)) {
                g.DrawString(text, font, white, rect, sf);
            }
        }
    }
}
