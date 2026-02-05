using System;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;

namespace QuadExplorer
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }

    public partial class Form1 : Form
    {
        private BufferedTableLayoutPanel gridLayout;
        private ExplorerUnit[] units = new ExplorerUnit[4];
        private MenuStrip mainMenu;
        private ToolStripComboBox cmbProfiles;
        private ToolStripTextBox txtGlobalSearch;
        private bool isSyncing = false;

        public Form1()
        {
            try { NativeMethods.SetPreferredAppMode(2); } catch { }
            this.Text = "Quad Explorer Ultimate (Dark)";
            this.StartPosition = FormStartPosition.Manual; // Important for custom bounds!
            
            this.BackColor = ColorTranslator.FromHtml("#202020");
            this.ForeColor = Color.White;

            int darkMode = 1;
            try { NativeMethods.DwmSetWindowAttribute(this.Handle, NativeMethods.DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int)); } catch {}
            try { this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            LoadSettings(); 
            
            // --- RESTORE WINDOW GEOMETRY ---
            // Ensure bounds are visible on screen (prevent lost window)
            if (MainWinRect.Width > 0 && IsOnScreen(MainWinRect))
            {
                this.Bounds = MainWinRect;
                if (IsMainWinMaximized) this.WindowState = FormWindowState.Maximized;
            }
            else
            {
                this.Size = new Size(1400, 950);
                this.CenterToScreen();
            }

            LoadProfiles(); 
            InitializeLayout();
            
            this.Load += new EventHandler(Form1_Load);
            this.FormClosing += new FormClosingEventHandler(Form1_FormClosing);
        }

        private bool IsOnScreen(Rectangle rect)
        {
            foreach (Screen screen in Screen.AllScreens)
            {
                if (screen.WorkingArea.IntersectsWith(rect)) return true;
            }
            return false;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000; 
                return cp;
            }
        }

        private void InitializeLayout()
        {
            mainMenu = new MenuStrip();
            mainMenu.Renderer = new DarkMenuRenderer();
            mainMenu.BackColor = Color.FromArgb(32, 32, 32);
            mainMenu.ForeColor = Color.White;
            mainMenu.Dock = DockStyle.Top;

            ToolStripMenuItem menuSystem = new ToolStripMenuItem("System");
            ToolStripMenuItem itemSettings = new ToolStripMenuItem("Settings");
            
            // --- SETTINGS CLICK LOGIC (UPDATED FOR HOT RELOAD) ---
            itemSettings.Click += (s, e) => { 
                new DarkSettingsForm().ShowDialog(); 
                ApplySettingsUI(); 
                // Force reload of context menus in all units
                foreach(var u in units) u.ReloadContext();
            };

            menuSystem.DropDownItems.Add(itemSettings); 
            menuSystem.DropDownItems.Add(new ToolStripSeparator());
            menuSystem.DropDownItems.Add(new ToolStripMenuItem("Help") { Enabled = false }); 
            menuSystem.DropDownItems.Add(new ToolStripSeparator());
            ToolStripMenuItem menuAbout = new ToolStripMenuItem("About");
            menuAbout.Click += (s, e) => { new DarkAboutBox().ShowDialog(); };
            menuSystem.DropDownItems.Add(menuAbout);

            Font iconFont = new Font("Segoe MDL2 Assets", 10);

            ToolStripMenuItem btnAddProfile = new ToolStripMenuItem("\uE710");
            btnAddProfile.Click += (s, e) => CreateNewProfile();
            btnAddProfile.ForeColor = Color.Lime;
            btnAddProfile.Font = iconFont;
            btnAddProfile.Alignment = ToolStripItemAlignment.Right;

            ToolStripMenuItem btnDelProfile = new ToolStripMenuItem("\uE711");
            btnDelProfile.Click += (s, e) => DeleteCurrentProfile();
            btnDelProfile.ForeColor = Color.Red;
            btnDelProfile.Font = iconFont;
            btnDelProfile.Alignment = ToolStripItemAlignment.Right;

            cmbProfiles = new ToolStripComboBox();
            cmbProfiles.BackColor = Color.FromArgb(50, 50, 50);
            cmbProfiles.ForeColor = Color.White;
            cmbProfiles.FlatStyle = FlatStyle.Flat; 
            cmbProfiles.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbProfiles.Alignment = ToolStripItemAlignment.Right;

            ComboBox innerCombo = cmbProfiles.ComboBox;
            innerCombo.SizeChanged += (s, e) => { innerCombo.Region = new Region(new Rectangle(2, 2, innerCombo.Width - 4, innerCombo.Height - 4)); };
            innerCombo.Paint += (s, e) => 
            {
                e.Graphics.Clear(Color.FromArgb(50, 50, 50)); 
                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                int w = innerCombo.Width; int h = innerCombo.Height;
                Rectangle btnRect = new Rectangle(w - 24, 0, 24, h); 
                using (SolidBrush bBtn = new SolidBrush(Color.FromArgb(160, 160, 160))) e.Graphics.FillRectangle(bBtn, btnRect);
                using (SolidBrush arrowBrush = new SolidBrush(Color.Black)) {
                    int cx = btnRect.X + (btnRect.Width / 2); int cy = btnRect.Y + (btnRect.Height / 2);
                    Point[] arrow = { new Point(cx - 3, cy - 1), new Point(cx + 3, cy - 1), new Point(cx, cy + 2) };
                    e.Graphics.FillPolygon(arrowBrush, arrow);
                }
                string textToShow = ""; if (innerCombo.SelectedItem != null) textToShow = innerCombo.SelectedItem.ToString(); else if (innerCombo.Items.Count > 0) textToShow = "Default";
                TextRenderer.DrawText(e.Graphics, textToShow, innerCombo.Font, new Rectangle(2, 0, w - 26, h), Color.White, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.SingleLine);
                using (Pen p = new Pen(Color.FromArgb(100, 100, 100), 1)) { e.Graphics.DrawRectangle(p, 0, 0, w - 1, h - 1); }
            };
            
            RefreshProfileCombo(); 
            cmbProfiles.SelectedIndexChanged += (s, e) => { if (cmbProfiles.SelectedItem != null) LoadProfileToTabs(cmbProfiles.SelectedItem.ToString()); innerCombo.Invalidate(); };

            ToolStripLabel lblProf = new ToolStripLabel("Profile:");
            lblProf.ForeColor = Color.LightGray;
            lblProf.Alignment = ToolStripItemAlignment.Right;

            txtGlobalSearch = new ToolStripTextBox();
            txtGlobalSearch.BackColor = Color.FromArgb(40, 40, 40);
            txtGlobalSearch.ForeColor = Color.Gray;
            txtGlobalSearch.BorderStyle = BorderStyle.FixedSingle;
            txtGlobalSearch.Text = "Everything...";
            txtGlobalSearch.Alignment = ToolStripItemAlignment.Right;
            txtGlobalSearch.Size = new Size(150, 23);
            
            txtGlobalSearch.Enter += (s, e) => { if (txtGlobalSearch.Text == "Everything...") { txtGlobalSearch.Text = ""; txtGlobalSearch.ForeColor = Color.White; } };
            txtGlobalSearch.Leave += (s, e) => { if (string.IsNullOrWhiteSpace(txtGlobalSearch.Text)) { txtGlobalSearch.Text = "Everything..."; txtGlobalSearch.ForeColor = Color.Gray; } };
            txtGlobalSearch.KeyDown += TxtGlobalSearch_KeyDown;

            mainMenu.Items.Add(menuSystem);
            mainMenu.Items.Add(btnAddProfile);
            mainMenu.Items.Add(btnDelProfile);
            mainMenu.Items.Add(cmbProfiles);
            mainMenu.Items.Add(lblProf);
            mainMenu.Items.Add(txtGlobalSearch); 

            this.MainMenuStrip = mainMenu;
            this.Controls.Add(mainMenu);

            gridLayout = new BufferedTableLayoutPanel();
            gridLayout.Dock = DockStyle.Fill; 
            gridLayout.ColumnCount = 2;
            gridLayout.RowCount = 2;
            gridLayout.Margin = new Padding(0);
            gridLayout.Padding = new Padding(2);
            gridLayout.BackColor = ColorTranslator.FromHtml("#181818");

            gridLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            gridLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            gridLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            gridLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

            for (int i = 0; i < 4; i++)
            {
                units[i] = new ExplorerUnit(i, savedSidebarWidth, savedColWidths);
                units[i].SidebarResized += OnUnitSidebarResized;
                units[i].ColumnResized += OnUnitColumnResized;
                gridLayout.Controls.Add(units[i].MainPanel);
            }
            
            this.Controls.Add(gridLayout);
            gridLayout.BringToFront();
            
            ApplySettingsUI();
        }

        private void ApplySettingsUI()
        {
            if (txtGlobalSearch != null)
                txtGlobalSearch.Visible = Form1.EnableEverything;
        }

        private void TxtGlobalSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                string query = txtGlobalSearch.Text.Trim();
                e.SuppressKeyPress = true;

                if (string.IsNullOrEmpty(query)) return;

                if (string.IsNullOrEmpty(Form1.EverythingPath) || !File.Exists(Form1.EverythingPath))
                {
                    // Auto-open settings if path missing
                    new DarkSettingsForm().ShowDialog();
                    ApplySettingsUI();
                    
                    // If still missing, stop
                    if (string.IsNullOrEmpty(Form1.EverythingPath) || !File.Exists(Form1.EverythingPath))
                        return;
                }

                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo();
                    psi.FileName = Form1.EverythingPath;
                    psi.Arguments = string.Format("-s \"{0}\"", query); 
                    Process.Start(psi);
                    txtGlobalSearch.Text = "";
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error launching Everything:\n" + ex.Message);
                }
            }
        }

        private void OnUnitSidebarResized(object sender, int newWidth)
        {
            if (isSyncing) return;
            isSyncing = true;
            foreach (var u in units) if (u != sender) u.SidebarWidth = newWidth;
            isSyncing = false;
        }

        private void OnUnitColumnResized(object sender, ColumnWidthChangedEventArgs e)
        {
            if (isSyncing) return;
            isSyncing = true;
            ExplorerUnit source = (ExplorerUnit)sender;
            int w = source.GetColumnWidth(e.ColumnIndex);
            foreach (var u in units) if (u != sender) u.SetColumnWidth(e.ColumnIndex, w);
            isSyncing = false;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            for (int i = 0; i < 4; i++) 
            {
                units[i].Navigate(savedPaths[i]);
                units[i].SidebarWidth = savedSidebarWidth;
                
                try {
                    string[] parts = savedSorts[i].Split(':');
                    int col = int.Parse(parts[0]);
                    SortOrder ord = (SortOrder)Enum.Parse(typeof(SortOrder), parts[1]);
                    units[i].ApplySort(col, ord);
                } catch {}
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveSettings();
        }
    }
}