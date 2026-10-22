using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace SlideApp {
    public class MainForm : Form {
        private ListBox lst;
        private Button btnNew, btnOpen, btnDelete, btnView;

        public MainForm() {
            this.SuspendLayout();
            Text = "SlideApp — Projects";
            Size = new Size(700, 550);
            MinimumSize = new Size(500, 400);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 10);
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimizeBox = true;
            DoubleBuffered = true;

            lst = new ListBox { 
                Location = new Point(20, 20), 
                Size = new Size(645, 420), 
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                IntegralHeight = false
            };
            lst.DoubleClick += (s, e) => ViewSelected();
            Controls.Add(lst);

            btnNew = NewBtn("New Project", 20, 460, 150);
            btnNew.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnNew.Click += (s, e) => NewProject();
            
            btnOpen = NewBtn("Edit", 180, 460, 100);
            btnOpen.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnOpen.Click += (s, e) => EditSelected();
            
            btnView = NewBtn("▶ View", 290, 460, 100);
            btnView.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnView.Click += (s, e) => ViewSelected();
            
            btnDelete = NewBtn("Delete", 400, 460, 100);
            btnDelete.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnDelete.Click += (s, e) => DeleteSelected();

            Controls.Add(btnNew); Controls.Add(btnOpen);
            Controls.Add(btnView); Controls.Add(btnDelete);
            this.ResumeLayout(false);
        }

        private Button NewBtn(string t, int x, int y, int w) => new Button { Text = t, Location = new Point(x, y), Size = new Size(w, 40) };
        protected override void OnShown(EventArgs e) { base.OnShown(e); RefreshList(); }
        private void RefreshList() { lst.Items.Clear(); foreach (var p in ProjectStore.ListProjects()) lst.Items.Add(p); }
        private string Selected() => lst.SelectedItem as string;

        private void NewProject() {
            var name = AskString("New Project", "Project name:");
            if (string.IsNullOrWhiteSpace(name)) return;
            name = name.Trim();
            ProjectStore.Save(new Project { Name = name });
            RefreshList();
            EditProject(name);
        }

        private void EditSelected() { var s = Selected(); if (s != null) EditProject(s); }
        private void ViewSelected() { var s = Selected(); if (s != null) ViewProject(s); }
        private void EditProject(string name) { using (var f = new CreateProjectForm(name)) f.ShowDialog(this); RefreshList(); }
        private void ViewProject(string name) {
            var p = ProjectStore.Load(name);
            if (p.Slides.Count == 0) { MessageBox.Show("No slides yet.", name); return; }
            using (var f = new SlideViewerForm(p)) f.ShowDialog(this);
        }
        private void DeleteSelected() {
            var s = Selected();
            if (s == null) return;
            if (MessageBox.Show($"Delete project '{s}'?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes) {
                ProjectStore.Delete(s); RefreshList();
            }
        }

        private string AskString(string title, string prompt) {
            using (var f = new Form()) {
                f.Text = title; 
                f.Size = new Size(400, 200); 
                f.StartPosition = FormStartPosition.CenterParent;
                f.FormBorderStyle = FormBorderStyle.FixedDialog; 
                f.MaximizeBox = false; 
                f.MinimizeBox = false;
                var lbl = new Label { Text = prompt, Location = new Point(15, 15), Size = new Size(360, 25) };
                var txt = new TextBox { Location = new Point(15, 45), Size = new Size(360, 28) };
                var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(170, 110), Size = new Size(90, 32) };
                var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(270, 110), Size = new Size(90, 32) };
                f.Controls.AddRange(new Control[] { lbl, txt, ok, cancel });
                f.AcceptButton = ok; f.CancelButton = cancel;
                return f.ShowDialog(this) == DialogResult.OK ? txt.Text : null;
            }
        }
    }
}