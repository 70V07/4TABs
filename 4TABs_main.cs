using System;
using System.Drawing;
using System.Windows.Forms;

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
        private bool isSyncing = false;

        public Form1()
        {
            try { NativeMethods.SetPreferredAppMode(2); } catch { }
            this.Text = "Quad Explorer Ultimate (Dark)";
            this.Size = new Size(1400, 950);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = ColorTranslator.FromHtml("#202020");
            this.ForeColor = Color.White;

            int darkMode = 1;
            try { NativeMethods.DwmSetWindowAttribute(this.Handle, NativeMethods.DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int)); } catch {}
            try { this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            LoadSettings(); 
            LoadProfiles(); 

            InitializeLayout();
            
            this.Load += new EventHandler(Form1_Load);
            this.FormClosing += new FormClosingEventHandler(Form1_FormClosing);
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
            menuSystem.DropDownItems.Add(new ToolStripMenuItem("Settings") { Enabled = false }); 
            menuSystem.DropDownItems.Add(new ToolStripSeparator());
            menuSystem.DropDownItems.Add(new ToolStripMenuItem("Help") { Enabled = false }); 
            menuSystem.DropDownItems.Add(new ToolStripSeparator());
            ToolStripMenuItem menuAbout = new ToolStripMenuItem("About");
            menuAbout.Click += (s, e) => { new DarkAboutBox().ShowDialog(); };
            menuSystem.DropDownItems.Add(menuAbout);

            // Font per icone MDL2
            Font iconFont = new Font("Segoe MDL2 Assets", 10);

            // Bottone [+] (Add) - Glyph E710
            ToolStripMenuItem btnAddProfile = new ToolStripMenuItem("\uE710");
            btnAddProfile.Click += (s, e) => CreateNewProfile();
            btnAddProfile.ForeColor = Color.Lime;
            btnAddProfile.Font = iconFont;
            btnAddProfile.Alignment = ToolStripItemAlignment.Right;

            // Bottone [-] (Remove) - Glyph E711
            ToolStripMenuItem btnDelProfile = new ToolStripMenuItem("\uE711");
            btnDelProfile.Click += (s, e) => DeleteCurrentProfile();
            btnDelProfile.ForeColor = Color.Red;
            btnDelProfile.Font = iconFont;
            btnDelProfile.Alignment = ToolStripItemAlignment.Right;

            // 3. ComboBox Profili
            cmbProfiles = new ToolStripComboBox();
            cmbProfiles.BackColor = Color.FromArgb(50, 50, 50);
            cmbProfiles.ForeColor = Color.White;
            // IMPORTANTE: Flat rimuove il bordo bianco 3D di sistema
            cmbProfiles.FlatStyle = FlatStyle.Flat; 
            cmbProfiles.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbProfiles.Alignment = ToolStripItemAlignment.Right;

            // --- FIX BORDO COMBOBOX ---
            ComboBox innerCombo = cmbProfiles.ComboBox;
            
            // Intercettiamo il disegno per forzare il bordo del colore che vogliamo noi
            innerCombo.Paint += (s, e) => 
            {
                // Un bordo grigio scuro (#606060) che si abbina al tema
                // Lo disegniamo sopra il bordo nativo
                using (Pen p = new Pen(Color.FromArgb(96, 96, 96), 1))
                {
                    // Rettangolo preciso attorno al controllo
                    e.Graphics.DrawRectangle(p, 0, 0, innerCombo.Width - 1, innerCombo.Height - 1);
                }
            };
            
            RefreshProfileCombo(); 
            
            cmbProfiles.SelectedIndexChanged += (s, e) => 
            {
                if (cmbProfiles.SelectedItem != null)
                    LoadProfileToTabs(cmbProfiles.SelectedItem.ToString());
            };

            ToolStripLabel lblProf = new ToolStripLabel("Profile:");
            lblProf.ForeColor = Color.LightGray;
            lblProf.Alignment = ToolStripItemAlignment.Right;

            mainMenu.Items.Add(menuSystem);
            mainMenu.Items.Add(btnAddProfile);
            mainMenu.Items.Add(btnDelProfile);
            mainMenu.Items.Add(cmbProfiles);
            mainMenu.Items.Add(lblProf);

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