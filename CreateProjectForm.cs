using System;
using System.Drawing;
using System.Windows.Forms;

namespace SlideApp {
    public class CreateProjectForm : Form {
        private readonly string projectName;
        private Project project;
        private Panel panel;
        private Button btnAdd;
        private int cursorY = 10;

        public CreateProjectForm(string name) {
            projectName = name;
            project = ProjectStore.Load(name);
            Text = "Edit Project: " + name;
            Size = new Size(720, 620);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 10);

            panel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            Controls.Add(panel);

            int slideNum = 1;
            foreach (var s in project.Slides) {
                AppendSlideLabel(s, slideNum++);
            }
            AppendAddButton();
        }

        private void AppendSlideLabel(string text, int num) {
            var lbl = new Label {
                Text = $"Slide {num}: {text}",
                AutoSize = false,
                Size = new Size(660, 0),
                Location = new Point(15, cursorY),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(8, 6, 8, 6),
                BackColor = Color.White
            };
            lbl.Height = TextRenderer.MeasureText(lbl.Text, lbl.Font, new Size(lbl.Width - 20, 0)).Height + 16;
            panel.Controls.Add(lbl);
            cursorY += lbl.Height + 8;
        }

        private void AppendAddButton() {
            btnAdd = new Button {
                Text = "+ Add Slide",
                Size = new Size(180, 40),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Location = new Point(15, cursorY),
                BackColor = Color.FromArgb(230, 240, 255)
            };
            btnAdd.Click += (s, e) => ShowEditor();
            panel.Controls.Add(btnAdd);
            cursorY += 50;
        }

        private void ShowEditor() {
            panel.Controls.Remove(btnAdd);

            var txt = new TextBox {
                Multiline = true,
                Size = new Size(660, 110),
                Location = new Point(15, cursorY),
                Font = new Font("Segoe UI", 11)
            };
            panel.Controls.Add(txt);

            var btnDone = new Button {
                Text = "Done",
                Size = new Size(100, 36),
                Location = new Point(15, cursorY + 120),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(200, 230, 200)
            };
            panel.Controls.Add(btnDone);

            var btnCancel = new Button {
                Text = "Cancel", Size = new Size(100, 36),
                Location = new Point(125, cursorY + 120)
            };
            panel.Controls.Add(btnCancel);

            btnDone.Click += (s, e) => {
                var t = txt.Text.Trim();
                if (!string.IsNullOrEmpty(t)) {
                    project.Slides.Add(t);
                    ProjectStore.Save(project);
                    AppendSlideLabel(t, project.Slides.Count);
                }
                panel.Controls.Remove(txt);
                panel.Controls.Remove(btnDone);
                panel.Controls.Remove(btnCancel);
                btnAdd.Location = new Point(15, cursorY);
                panel.Controls.Add(btnAdd);
                cursorY += 50;
                panel.ScrollControlIntoView(btnAdd);
            };

            btnCancel.Click += (s, e) => {
                panel.Controls.Remove(txt);
                panel.Controls.Remove(btnDone);
                panel.Controls.Remove(btnCancel);
                btnAdd.Location = new Point(15, cursorY);
                panel.Controls.Add(btnAdd);
            };

            txt.Focus();
            panel.ScrollControlIntoView(txt);
        }
    }
}
