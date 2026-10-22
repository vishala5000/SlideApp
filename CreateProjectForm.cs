using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SlideApp {
    public class CreateProjectForm : Form {
        private readonly string projectName;
        private Project project;
        private TextBox txtBulk;
        private ListBox lstSlides;

        public CreateProjectForm(string name) {
            this.SuspendLayout();
            projectName = name;
            project = ProjectStore.Load(name);
            Text = "Edit Project: " + name;
            Size = new Size(900, 750);
            MinimumSize = new Size(700, 600);
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Segoe UI", 10);
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimizeBox = true;
            DoubleBuffered = true;

            var lblInfo = new Label {
                Text = "Enter each slide on a separate line. Empty lines are ignored. Click 'Save All' to create slides.",
                Location = new Point(15, 10),
                Size = new Size(860, 25),
                ForeColor = Color.FromArgb(80, 80, 80),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            Controls.Add(lblInfo);

            txtBulk = new TextBox {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Location = new Point(15, 40),
                Size = new Size(860, 320),
                Font = new Font("Segoe UI", 11),
                AcceptsReturn = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            
            if (project.Slides.Count > 0) {
                txtBulk.Text = string.Join(Environment.NewLine, project.Slides);
            } else {
                txtBulk.Text = "How to Create a New Folder" + Environment.NewLine +
                              "How to Rename a Folder Quickly in Windows" + Environment.NewLine +
                              "How to Change a Folder Icon in Windows";
            }
            Controls.Add(txtBulk);

            var btnSave = new Button {
                Text = "💾 Save All as Slides",
                Size = new Size(220, 45),
                Location = new Point(15, 375),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                BackColor = Color.FromArgb(200, 230, 200),
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };
            btnSave.Click += (s, e) => SaveAllSlides();
            Controls.Add(btnSave);

            var lblPreview = new Label {
                Text = "Preview (slides created):",
                Location = new Point(15, 435),
                Size = new Size(300, 25),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };
            Controls.Add(lblPreview);

            lstSlides = new ListBox {
                Location = new Point(15, 465),
                Size = new Size(860, 230),
                Font = new Font("Segoe UI", 10),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                IntegralHeight = false
            };
            RefreshSlideList();
            Controls.Add(lstSlides);
            this.ResumeLayout(false);
        }

        private void SaveAllSlides() {
            var lines = txtBulk.Text
                .Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrEmpty(l))
                .ToList();

            if (lines.Count == 0) {
                MessageBox.Show("Please enter at least one slide.", "No slides", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            project.Slides = lines;
            ProjectStore.Save(project);
            RefreshSlideList();
            MessageBox.Show($"✅ Saved {lines.Count} slide(s) successfully!", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void RefreshSlideList() {
            lstSlides.Items.Clear();
            for (int i = 0; i < project.Slides.Count; i++) {
                lstSlides.Items.Add($"Slide {i + 1}: {project.Slides[i]}");
            }
        }
    }
}