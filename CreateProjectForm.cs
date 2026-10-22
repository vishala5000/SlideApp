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
        private Label lblInfo;

        public CreateProjectForm(string name) {
            projectName = name;
            project = ProjectStore.Load(name);
            Text = "Edit Project: " + name;
            Size = new Size(820, 680);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 10);

            lblInfo = new Label {
                Text = "Enter each slide on a separate line. Empty lines are ignored. Click 'Save All' to create slides.",
                Location = new Point(15, 10),
                Size = new Size(780, 25),
                ForeColor = Color.FromArgb(80, 80, 80)
            };
            Controls.Add(lblInfo);

            txtBulk = new TextBox {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Location = new Point(15, 40),
                Size = new Size(780, 280),
                Font = new Font("Segoe UI", 11),
                AcceptsReturn = true
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
                Size = new Size(200, 45),
                Location = new Point(15, 330),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                BackColor = Color.FromArgb(200, 230, 200)
            };
            btnSave.Click += (s, e) => SaveAllSlides();
            Controls.Add(btnSave);

            var lblPreview = new Label {
                Text = "Preview (slides created):",
                Location = new Point(15, 390),
                Size = new Size(300, 25),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            Controls.Add(lblPreview);

            lstSlides = new ListBox {
                Location = new Point(15, 420),
                Size = new Size(780, 180),
                Font = new Font("Segoe UI", 10)
            };
            RefreshSlideList();
            Controls.Add(lstSlides);
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