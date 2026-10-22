using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace SlideApp {
    public class MainForm : Form {
        private ListBox lst;
        private Button btnNew, btnOpen, btnDelete, btnView;
        private Panel titleBar;
        private Label lblTitle;
        private Button btnMin, btnMax, btnClose;
        
        // Dragging
        private bool dragging = false;
        private Point dragCursorPoint;
        private Point dragFormPoint;

        public MainForm() {
            Text = "SlideApp — Projects";
            Size = new Size(640, 480);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 10);
            FormBorderStyle = FormBorderStyle.None;
            DoubleBuffered = true;

            // Custom title bar
            titleBar = new Panel {
                Height = 35,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(45, 45, 48),
                Cursor = Cursors.SizeAll
            };
            titleBar.MouseDown += TitleBar_MouseDown;
            titleBar.MouseMove += TitleBar_MouseMove;
            titleBar.MouseUp += TitleBar_MouseUp;

            lblTitle = new Label {
                Text = "SlideApp — Projects",
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
            btnMax.Click += (s, e) => WindowState = WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized;
            
            btnClose = CreateTitleBarButton("✕", 35);
            btnClose.BackColor = Color.FromArgb(232, 17, 35);
            btnClose.Click += (s, e) => Close();

            titleBar.Controls.Add(btnMin);
            titleBar.Controls.Add(btnMax);
            titleBar.Controls.Add(btnClose);
            Controls.Add(titleBar);

            // Main content
            lst = new ListBox { Location = new Point(20, 55), Size = new Size(585, 340) };
            lst.DoubleClick += (s, e) => ViewSelected();
            Controls.Add(lst);

            btnNew = NewBtn("New Project", 20, 410, 150);
            btnNew.Click += (s, e) => NewProject();
            btnOpen = NewBtn("Edit", 180, 410, 100);
            btnOpen.Click += (s, e) => EditSelected();
            btnView = NewBtn("▶ View", 290, 410, 100);
            btnView.Click += (s, e) => ViewSelected();
            btnDelete = NewBtn("Delete", 400, 410, 100);
            btnDelete.Click += (s, e) => DeleteSelected();

            Controls.Add(btnNew); Controls.Add(btnOpen);
            Controls.Add(btnView); Controls.Add(btnDelete);

            PositionTitleBarButtons();
            this.Resize += (s, e) => PositionTitleBarButtons();
        }

        private Button CreateTitleBarButton(string text, int size) {
            var btn = new Button {
                Text = text,
                Size = new Size(size, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(45, 45, 48),
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
                f.Text = title; f.Size = new Size(360, 180); f.StartPosition = FormStartPosition.CenterParent;
                f.FormBorderStyle = FormBorderStyle.FixedDialog; f.MaximizeBox = false; f.MinimizeBox = false;
                var lbl = new Label { Text = prompt, Location = new Point(15, 15), Size = new Size(320, 25) };
                var txt = new TextBox { Location = new Point(15, 45), Size = new Size(320, 28) };
                var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(150, 95), Size = new Size(90, 32) };
                var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(250, 95), Size = new Size(90, 32) };
                f.Controls.AddRange(new Control[] { lbl, txt, ok, cancel });
                f.AcceptButton = ok; f.CancelButton = cancel;
                return f.ShowDialog(this) == DialogResult.OK ? txt.Text : null;
            }
        }
    }
}