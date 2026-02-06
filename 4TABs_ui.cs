using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Reflection;
using System.Diagnostics;

namespace QuadExplorer
{
    // --- CONTROLLI CUSTOM SCURI ---
    public class DarkListView : ListView
    {
        public DarkListView() { this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true); }
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            NativeMethods.EnableDarkScrollbars(this.Handle);
            IntPtr hHeader = NativeMethods.SendMessage(this.Handle, NativeMethods.LVM_GETHEADER, IntPtr.Zero, IntPtr.Zero);
            NativeMethods.SetWindowTheme(hHeader, "ItemsView", null);
        }
    }

    public class DarkTreeView : TreeView
    {
        protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); NativeMethods.EnableDarkScrollbars(this.Handle); }
    }

    // --- CONTENITORI ANTI-FLICKER ---
    public class DarkPanel : Panel
    {
        public DarkPanel()
        {
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            this.UpdateStyles();
            this.BackColor = Color.FromArgb(32, 32, 32); 
        }
    }

    public class DarkSplitter : SplitContainer
    {
        public DarkSplitter()
        {
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            this.UpdateStyles();
            this.BackColor = Color.FromArgb(32, 32, 32);
        }
    }

    public class BufferedTableLayoutPanel : TableLayoutPanel
    {
        public BufferedTableLayoutPanel()
        {
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            this.UpdateStyles();
            this.BackColor = Color.FromArgb(24, 24, 24);
        }
    }

    // --- RENDERER MENU ---
    public class DarkMenuRenderer : ToolStripProfessionalRenderer
    {
        public DarkMenuRenderer() : base(new DarkColors()) { this.RoundedEdges = false; }
        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e) { e.TextColor = Color.White; base.OnRenderItemText(e); }
        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e) { e.ArrowColor = Color.White; base.OnRenderArrow(e); }
        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e) { }
    }

    public class DarkColors : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground { get { return Color.FromArgb(43, 43, 43); } }
        public override Color MenuStripGradientBegin { get { return Color.FromArgb(32, 32, 32); } }
        public override Color MenuStripGradientEnd { get { return Color.FromArgb(32, 32, 32); } }
        public override Color MenuBorder { get { return Color.FromArgb(80, 80, 80); } }
        public override Color MenuItemBorder { get { return Color.FromArgb(70, 70, 70); } }
        public override Color MenuItemSelected { get { return Color.FromArgb(70, 70, 70); } }
        public override Color MenuItemSelectedGradientBegin { get { return Color.FromArgb(70, 70, 70); } }
        public override Color MenuItemSelectedGradientEnd { get { return Color.FromArgb(70, 70, 70); } }
        public override Color MenuItemPressedGradientBegin { get { return Color.FromArgb(60, 60, 60); } }
        public override Color MenuItemPressedGradientEnd { get { return Color.FromArgb(60, 60, 60); } }
        public override Color MenuItemPressedGradientMiddle { get { return Color.FromArgb(60, 60, 60); } }
        public override Color ImageMarginGradientBegin { get { return Color.FromArgb(43, 43, 43); } }
        public override Color ImageMarginGradientMiddle { get { return Color.FromArgb(43, 43, 43); } }
        public override Color ImageMarginGradientEnd { get { return Color.FromArgb(43, 43, 43); } }
    }

    // --- SORTING ---
    public class ListViewItemComparer : IComparer
    {
        public int SortColumn = 0;
        public SortOrder Order = SortOrder.Ascending;
        public int Compare(object x, object y)
        {
            int returnVal = -1;
            string s1 = ((ListViewItem)x).SubItems[SortColumn].Text;
            string s2 = ((ListViewItem)y).SubItems[SortColumn].Text;
            if (SortColumn == 1) { long l1 = ParseSize(s1); long l2 = ParseSize(s2); returnVal = l1.CompareTo(l2); }
            else if (SortColumn == 3) { DateTime d1, d2; if (DateTime.TryParse(s1, out d1) && DateTime.TryParse(s2, out d2)) returnVal = DateTime.Compare(d1, d2); else returnVal = String.Compare(s1, s2); }
            else returnVal = String.Compare(s1, s2);
            if (Order == SortOrder.Descending) returnVal *= -1;
            return returnVal;
        }
        private long ParseSize(string s)
        {
            if (string.IsNullOrEmpty(s)) return -1;
            string[] parts = s.Split(' ');
            if (parts.Length < 2) return 0;
            long val; long.TryParse(parts[0], out val);
            string unit = parts[1].ToUpper();
            if (unit.Contains("KB")) val *= 1024; else if (unit.Contains("MB")) val *= 1024 * 1024; else if (unit.Contains("GB")) val *= 1024 * 1024 * 1024; else if (unit.Contains("TB")) val *= 1024L * 1024L * 1024L * 1024L;
            return val;
        }
    }

    // --- FINESTRE DIALOGO SCURE ---
    public class DarkDialogBase : Form
    {
        public DarkDialogBase()
        {
            this.BackColor = Color.FromArgb(32, 32, 32);
            this.ForeColor = Color.White;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
        }
    }

    public class DarkInputBox : DarkDialogBase
    {
        public string InputValue { get { return txtInput.Text; } }
        private TextBox txtInput;
        public DarkInputBox(string title, string prompt)
        {
            this.Text = title; this.Size = new Size(350, 150);
            Label lbl = new Label() { Text = prompt, Left = 10, Top = 15, AutoSize = true, ForeColor = Color.LightGray };
            txtInput = new TextBox() { Left = 10, Top = 40, Width = 310, BackColor = Color.FromArgb(50, 50, 50), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            Button btnOk = new Button() { Text = "OK", Left = 80, Top = 75, Width = 80, DialogResult = DialogResult.OK, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(60,60,60) }; btnOk.FlatAppearance.BorderSize = 0;
            Button btnCancel = new Button() { Text = "Cancel", Left = 170, Top = 75, Width = 80, DialogResult = DialogResult.Cancel, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(60, 60, 60) }; btnCancel.FlatAppearance.BorderSize = 0;
            this.Controls.Add(lbl); this.Controls.Add(txtInput); this.Controls.Add(btnOk); this.Controls.Add(btnCancel);
            this.AcceptButton = btnOk; this.CancelButton = btnCancel;
        }
    }

    public class NewFileDialog : DarkDialogBase
    {
        public string FileName { get { return txtName.Text; } }
        public string FileType { get { return txtType.Text; } }
        private TextBox txtName, txtType;
        public NewFileDialog()
        {
            this.Text = "New File"; this.Size = new Size(350, 150);
            txtName = new TextBox() { Left = 20, Top = 30, Width = 200, BackColor = Color.FromArgb(50, 50, 50), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Text = "NewFile" };
            Label lblDot = new Label() { Text = ".", Left = 225, Top = 32, AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            txtType = new TextBox() { Left = 240, Top = 30, Width = 70, BackColor = Color.FromArgb(50, 50, 50), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Text = "txt" };
            Button btnOk = new Button() { Text = "OK", Left = 120, Top = 70, Width = 100, DialogResult = DialogResult.OK, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(60, 60, 60) }; btnOk.FlatAppearance.BorderSize = 0;
            this.Controls.Add(txtName); this.Controls.Add(lblDot); this.Controls.Add(txtType); this.Controls.Add(btnOk);
            this.AcceptButton = btnOk;
        }
    }

    public class IconSelectorDialog : DarkDialogBase
    {
        public string SelectedIcon { get; private set; }
        private FlowLayoutPanel grid;
        private TextBox txtPath;
        
        public IconSelectorDialog()
        {
            this.Text = "Select Icon";
            this.Size = new Size(500, 450);

            Label lblSys = new Label() { Text = "System Icons (MDL2)", Top = 10, Left = 10, AutoSize = true, ForeColor = Color.LightGray };
            grid = new FlowLayoutPanel() { Top = 35, Left = 10, Width = 460, Height = 280, BackColor = Color.FromArgb(40,40,40), AutoScroll = true, BorderStyle = BorderStyle.FixedSingle };
            
            // Populate grid
            string[] commonIcons = { "E710", "E74D", "E711", "E8C6", "E8C8", "E77F", "E70F", "E729", "E762", "E738", "E736", "E72D", "E74B", "E81E", "E840", "EC50", "ED25", "E8D2", "E90E", "E896", "EA86", "E787", "E80F", "E7C3", "E8B7", "E8F4", "E7C3" };
            foreach(var code in commonIcons) {
                Label l = new Label() { Text = char.ConvertFromUtf32(int.Parse(code, System.Globalization.NumberStyles.HexNumber)), 
                    Font = new Font("Segoe MDL2 Assets", 16), ForeColor = Color.White, Size = new Size(40,40), TextAlign = ContentAlignment.MiddleCenter, Cursor = Cursors.Hand };
                l.Click += (s,e) => { SelectedIcon = " MDL2:" + code; this.DialogResult = DialogResult.OK; };
                l.MouseEnter += (s,e) => l.BackColor = Color.FromArgb(60,60,60);
                l.MouseLeave += (s,e) => l.BackColor = Color.Transparent;
                grid.Controls.Add(l);
            }

            Label lblCust = new Label() { Text = "Use a custom icon from your PC (suggested 250px)", Top = 330, Left = 10, AutoSize = true, ForeColor = Color.LightGray };
            txtPath = new TextBox() { Top = 355, Left = 10, Width = 370, BackColor = Color.FromArgb(50,50,50), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            Button btnBrowse = new Button() { Text = "SEARCH", Top = 354, Left = 390, Width = 80, Height = 25, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(60,60,60) }; btnBrowse.FlatAppearance.BorderSize = 0;
            btnBrowse.Click += (s,e) => {
                using(OpenFileDialog ofd = new OpenFileDialog() { Filter = "Images|*.png;*.ico;*.jpg" }) {
                    if(ofd.ShowDialog() == DialogResult.OK) { txtPath.Text = ofd.FileName; SelectedIcon = ofd.FileName; this.DialogResult = DialogResult.OK; }
                }
            };

            this.Controls.Add(lblSys); this.Controls.Add(grid);
            this.Controls.Add(lblCust); this.Controls.Add(txtPath); this.Controls.Add(btnBrowse);
        }
    }

    // --- ABOUT BOX (VERSIONE CHE LEGGE ASSEMBLYINFO.CS DA GITHUB) ---
    public class DarkAboutBox : DarkDialogBase
    {
        private Button btnCheckUpdate;
        private Label lblStatus;
        
        // URL del file RAW su GitHub. 
        // IMPORTANTE: Se il tuo ramo principale si chiama 'master' invece di 'main', cambia 'main' in 'master' qui sotto.
        private string rawVersionUrl = "https://raw.githubusercontent.com/70V07/4TABs/main/AssemblyInfo.cs";
        
        // Link per il download (rimane la pagina delle release)
        private string downloadUrl = "https://github.com/70V07/4TABs/releases/latest";
        
        private string outdatedMsg = "You have outdated version, so download the Latest Relase from GitHub";

        public DarkAboutBox()
        {
            this.Text = "About"; 
            this.Size = new Size(400, 200);

            // 1. Title
            Label lblTitle = new Label() { 
                Text = "4TABS by TOVOT", 
                AutoSize = false, 
                Width = 400, 
                Height = 30, 
                Top = 30, 
                TextAlign = ContentAlignment.MiddleCenter, 
                Font = new Font("Segoe UI", 14, FontStyle.Bold) 
            };

            // 2. Version
            Version v = Assembly.GetExecutingAssembly().GetName().Version;
            string verText = string.Format("v{0}.{1}.{2}", v.Major, v.Minor, v.Build);
            
            Label lblVer = new Label() { 
                Text = verText, 
                AutoSize = false, 
                Width = 400, 
                Height = 20, 
                Top = 65, 
                TextAlign = ContentAlignment.MiddleCenter, 
                ForeColor = Color.Gray 
            };

            // 3. Update Button
            btnCheckUpdate = new Button() { 
                Text = "[Check for updates]", 
                Width = 200, 
                Height = 30, 
                Top = 110, 
                FlatStyle = FlatStyle.Flat, 
                BackColor = Color.FromArgb(60, 60, 60),
                Cursor = Cursors.Hand
            };
            btnCheckUpdate.FlatAppearance.BorderSize = 0;
            btnCheckUpdate.Left = (this.ClientSize.Width - btnCheckUpdate.Width) / 2; 
            btnCheckUpdate.Click += BtnCheckUpdate_Click;

            // 4. Status Label (Hidden initially)
            lblStatus = new Label() {
                AutoSize = false,
                Width = 380,
                Height = 40,
                Top = 110,
                TextAlign = ContentAlignment.MiddleCenter,
                Visible = false
            };
            lblStatus.Left = (this.ClientSize.Width - lblStatus.Width) / 2;

            this.Controls.Add(lblTitle);
            this.Controls.Add(lblVer);
            this.Controls.Add(btnCheckUpdate);
            this.Controls.Add(lblStatus);
        }

        private async void BtnCheckUpdate_Click(object sender, EventArgs e)
        {
            btnCheckUpdate.Enabled = false;
            btnCheckUpdate.Text = "Checking...";
            
            bool isNewer = false;
            bool checkFailed = false;
            
            try
            {
                await Task.Factory.StartNew(() => 
                {
                    try 
                    {
                        // ℹ️ Forza TLS 1.2 per connessioni GitHub
                        ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;

                        using (WebClient wc = new WebClient())
                        {
                            // ⚠️ Fondamentale: GitHub respinge richieste senza User-Agent
                            wc.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
                            
                            string content = wc.DownloadString(rawVersionUrl);
                            
                            // 🧐 Regex flessibile per catturare la versione
                            string pattern = @"\[assembly:\s*AssemblyVersion\s*\(\s*""(?<v>.*?)""\s*\)\]";
                            System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(content, pattern);

                            if (match.Success)
                            {
                                string remoteVerStr = match.Groups["v"].Value;
                                isNewer = IsVersionNewer(remoteVerStr);
                            }
                            else
                            {
                                checkFailed = true;
                            }
                        }
                    }
                    catch { checkFailed = true; }
                });

                btnCheckUpdate.Visible = false;
                lblStatus.Visible = true;

                if (checkFailed)
                {
                    lblStatus.Text = "Connection error or file not found";
                    lblStatus.ForeColor = Color.Red;
                    btnCheckUpdate.Visible = true;
                    btnCheckUpdate.Enabled = true;
                    btnCheckUpdate.Text = "Retry";
                }
                else if (isNewer)
                {
                    // OUTDATED: Versione non aggiornata
                    lblStatus.Text = outdatedMsg;
                    lblStatus.ForeColor = Color.Orange;
                    lblStatus.Cursor = Cursors.Hand;

                    // Azione al Click: Apre il browser
                    lblStatus.Click += (s, args) => { 
                        try { Process.Start(downloadUrl); } catch { } 
                    };

                    // Effetto Hover: Mostra URL al passaggio del mouse
                    lblStatus.MouseEnter += (s, args) => { 
                        lblStatus.Text = downloadUrl; 
                        lblStatus.ForeColor = Color.LightBlue; 
                    };

                    // Ripristino: Torna al messaggio originale quando il mouse esce
                    lblStatus.MouseLeave += (s, args) => { 
                        lblStatus.Text = outdatedMsg; 
                        lblStatus.ForeColor = Color.Orange; 
                    };
                }
                else
                {
                    lblStatus.Text = "You have already the Latest Release version";
                    lblStatus.ForeColor = Color.Lime;
                }
            }
            catch
            {
                btnCheckUpdate.Text = "Error";
                btnCheckUpdate.Enabled = true; 
            }
        }

        private bool IsVersionNewer(string remoteVerString)
        {
            // Parse Remote Version from AssemblyInfo string
            string[] parts = remoteVerString.Split('.');
            int rMajor = 0, rMinor = 0, rBuild = 0;
            
            if (parts.Length > 0) int.TryParse(parts[0], out rMajor);
            if (parts.Length > 1) int.TryParse(parts[1], out rMinor);
            if (parts.Length > 2) int.TryParse(parts[2], out rBuild);
            // Ignoriamo la Revision (part[3]) come richiesto

            // Local Version
            Version local = Assembly.GetExecutingAssembly().GetName().Version;

            // Compare (Major > Minor > Build)
            if (rMajor > local.Major) return true;
            if (rMajor == local.Major && rMinor > local.Minor) return true;
            if (rMajor == local.Major && rMinor == local.Minor && rBuild > local.Build) return true;

            return false;
        }
    }

    // --- SETTINGS FORM ---
    public class DarkSettingsForm : DarkDialogBase
    {
        // UI Layout
        private ListBox listCategories;
        private Panel contentPanel;
        
        // Tab 1: Misc Controls
        private Panel miscPanel;
        private CheckBox chkEnableEv;
        private CheckBox chkToolTips;
        private CheckBox chkDelConf;
        private TextBox txtEvPath;
        private Button btnBrowse;
        
        // Tab 2: Context Menu Controls
        private Panel ctxPanel;
        private CheckBox chkCtxToolbar;
        private Button btnTogCut, btnTogCopy, btnTogPaste, btnTogNew, btnTogNewFolder;
        private FlowLayoutPanel listCommands;
        private string contextCfgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "context.cfg");

        public DarkSettingsForm()
        {
            this.Text = "Settings";
            this.Size = new Size(850, 600); 
            this.FormBorderStyle = FormBorderStyle.Sizable; 
            if (Form1.SettingsWinRect.Width > 0) this.Bounds = Form1.SettingsWinRect;

            // Setup Categories List (Sidebar)
            listCategories = new ListBox();
            listCategories.Dock = DockStyle.Left;
            listCategories.Width = 150;
            listCategories.BackColor = Color.FromArgb(40, 40, 40);
            listCategories.ForeColor = Color.White;
            listCategories.BorderStyle = BorderStyle.None;
            listCategories.Items.Add("MISC");
            listCategories.Items.Add("Context Menu");
            listCategories.SelectedIndexChanged += (s, e) => SwitchTab(listCategories.SelectedIndex);
            
            // Setup Content Panel
            contentPanel = new Panel();
            contentPanel.Dock = DockStyle.Fill;
            contentPanel.BackColor = Color.FromArgb(32, 32, 32);
            contentPanel.Padding = new Padding(20);
            contentPanel.Resize += (s, e) => AdjustControlsLayout(); 

            // Initialize Tabs (Both are created but hidden)
            InitializeMiscTab();
            InitializeContextTab();

            this.Controls.Add(contentPanel);
            this.Controls.Add(listCategories);
            
            // Show Default
            SwitchTab(0);
        }

        private void SwitchTab(int index)
        {
            if (miscPanel != null) miscPanel.Visible = (index == 0);
            if (ctxPanel != null) ctxPanel.Visible = (index == 1);
        }

        private void InitializeMiscTab()
        {
            miscPanel = new Panel() { Dock = DockStyle.Fill, Visible = false };

            chkEnableEv = new CheckBox() { Text = "Enable Everything support", AutoSize = true, Top = 20, Left = 20, Checked = Form1.EnableEverything };
            txtEvPath = new TextBox() { Top = 60, Left = 20, Height = 23, BackColor = Color.FromArgb(50, 50, 50), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Text = Form1.EverythingPath };
            btnBrowse = new Button() { Text = "SEARCH", Top = 59, Width = 80, Height = 25, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(60, 60, 60) }; btnBrowse.FlatAppearance.BorderSize = 0;
            btnBrowse.Click += (s, e) => { using (OpenFileDialog ofd = new OpenFileDialog() { Filter = "Executables (*.exe)|*.exe" }) { if (ofd.ShowDialog() == DialogResult.OK) txtEvPath.Text = ofd.FileName; } };

            chkToolTips = new CheckBox() { Text = "Enable tool tips on mouseover", AutoSize = true, Top = 100, Left = 20, Checked = Form1.EnableToolTips };
            
            chkDelConf = new CheckBox() { Text = "Enable delete confirmation dialog", AutoSize = true, Top = 130, Left = 20, Checked = Form1.EnableDeleteConfirm };

            miscPanel.Controls.Add(chkEnableEv); miscPanel.Controls.Add(txtEvPath); miscPanel.Controls.Add(btnBrowse); miscPanel.Controls.Add(chkToolTips); miscPanel.Controls.Add(chkDelConf);
            contentPanel.Controls.Add(miscPanel); // Add to main container
        }

        private void InitializeContextTab()
        {
            ctxPanel = new Panel() { Dock = DockStyle.Fill, Visible = false };
            
            // 1. Tutorial
            string tuto = "CONTEXT MENU EDITOR\n\n" +
                          "Customize your context menu here.\n" +
                          "- Use {path} to represent the selected file path.\n" +
                          "- Use {dir} to represent the current directory path.\n\n" +
                          "Example: Command='notepad.exe \"{path}\"' Name='Open in Notepad'";
            Label lblTut = new Label() { Text = tuto, AutoSize = true, Top = 0, Left = 0, ForeColor = Color.Gray };
            
            // 2. Options
            chkCtxToolbar = new CheckBox() { Text = "Enable Default commands section (Horizontal Toolbar)", AutoSize = true, Top = 100, Left = 0, Checked = Form1.CtxEnableToolbar };
            
            // 3. Symbolic Buttons
            Panel pnlSym = new Panel() { Top = 130, Left = 0, Height = 40, Width = 550 };
            Font f = new Font("Segoe MDL2 Assets", 12);
            btnTogCut = CreateToggleBtn("\uE8C6", "Cut", Form1.CtxShowCut, f, 0);
            btnTogCopy = CreateToggleBtn("\uE8C8", "Copy", Form1.CtxShowCopy, f, 40);
            btnTogPaste = CreateToggleBtn("\uE77F", "Paste", Form1.CtxShowPaste, f, 80);
            btnTogNew = CreateToggleBtn("\uE7C3", "New File", Form1.CtxShowNew, f, 120);
            btnTogNewFolder = CreateToggleBtn("\uE8B7", "New Folder", Form1.CtxShowNewFolder, f, 160);
            
            pnlSym.Controls.Add(btnTogCut); pnlSym.Controls.Add(btnTogCopy); pnlSym.Controls.Add(btnTogPaste); pnlSym.Controls.Add(btnTogNew); pnlSym.Controls.Add(btnTogNewFolder);

            // 4. Custom Commands Section
            Label lblCust = new Label() { Text = "Custom Commands:", Top = 180, Left = 0, AutoSize = true, ForeColor = Color.LightGray };
            
            listCommands = new FlowLayoutPanel() { Top = 200, Left = 0, Width = 600, Height = 300, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
            listCommands.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            Button btnAddRow = new Button() { Text = "Add New Command Row (+)", Top = 175, Left = 150, Width = 200, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(40,40,40), ForeColor = Color.Lime };
            btnAddRow.FlatAppearance.BorderSize = 0;
            btnAddRow.Click += (s, e) => AddCommandRow("", "", "");

            ctxPanel.Controls.Add(lblTut); ctxPanel.Controls.Add(chkCtxToolbar); ctxPanel.Controls.Add(pnlSym);
            ctxPanel.Controls.Add(lblCust); ctxPanel.Controls.Add(btnAddRow); ctxPanel.Controls.Add(listCommands);
            
            contentPanel.Controls.Add(ctxPanel); // Add to main container
            LoadContextRows();
        }

        private Button CreateToggleBtn(string icon, string tooltip, bool active, Font f, int x)
        {
            Button b = new Button() { Text = icon, Font = f, Location = new Point(x, 0), Size = new Size(36, 36), FlatStyle = FlatStyle.Flat };
            b.FlatAppearance.BorderSize = 1;
            
            if (Form1.EnableToolTips)
            {
                ToolTip tt = new ToolTip();
                tt.SetToolTip(b, tooltip);
            }

            ApplyToggleStyle(b, active);
            b.Click += (s, e) => { 
                bool state = (b.Tag != null && (bool)b.Tag); 
                ApplyToggleStyle(b, !state); 
            };
            return b;
        }

        private void ApplyToggleStyle(Button b, bool active)
        {
            b.Tag = active;
            if (active) { b.BackColor = Color.FromArgb(60, 60, 60); b.ForeColor = Color.White; b.FlatAppearance.BorderColor = Color.Lime; }
            else { b.BackColor = Color.FromArgb(32, 32, 32); b.ForeColor = Color.Gray; b.FlatAppearance.BorderColor = Color.Gray; }
        }

        private void LoadContextRows()
        {
            listCommands.Controls.Clear();
            if (!File.Exists(contextCfgPath)) return;
            string[] lines = File.ReadAllLines(contextCfgPath);
            foreach (string line in lines)
            {
                if (string.IsNullOrEmpty(line) || line.StartsWith(";")) continue;
                // Label|Icon=Command
                string label = ""; string icon = ""; string cmd = "";
                int eq = line.IndexOf('=');
                if (eq > 0)
                {
                    string left = line.Substring(0, eq);
                    cmd = line.Substring(eq + 1);
                    int pipe = left.IndexOf('|');
                    if (pipe > 0) { label = left.Substring(0, pipe); icon = left.Substring(pipe + 1); }
                    else { label = left; }
                }
                AddCommandRow(label, cmd, icon);
            }
        }

        private void AddCommandRow(string label, string cmd, string iconPath)
        {
            Panel p = new Panel() { Size = new Size(650, 32), BackColor = Color.Transparent, Margin = new Padding(0, 0, 0, 2) };
            
            // @ Icon Preview
            Label lblIconPrev = new Label() { Text = "@", Size = new Size(24, 24), Location = new Point(0, 4), TextAlign = ContentAlignment.MiddleCenter, ForeColor = Color.Yellow, Font = new Font("Segoe MDL2 Assets", 10) };
            if (!string.IsNullOrEmpty(iconPath) && iconPath.StartsWith(" MDL2:")) lblIconPrev.Text = char.ConvertFromUtf32(int.Parse(iconPath.Replace(" MDL2:", ""), System.Globalization.NumberStyles.HexNumber));
            else if (!string.IsNullOrEmpty(iconPath)) lblIconPrev.Text = "I"; // Image file indicator

            Button btnUp = new Button() { Text = "\uE70E", Font = new Font("Segoe MDL2 Assets", 8), Size = new Size(24, 24), Location = new Point(30, 4), FlatStyle = FlatStyle.Flat, ForeColor = Color.White };
            Button btnDown = new Button() { Text = "\uE70D", Font = new Font("Segoe MDL2 Assets", 8), Size = new Size(24, 24), Location = new Point(56, 4), FlatStyle = FlatStyle.Flat, ForeColor = Color.White };
            
            // Placeholders
            string phCmd = "Command...";
            string phName = "Name...";

            TextBox txtCmd = new TextBox() { Location = new Point(85, 5), Width = 200, BackColor = Color.FromArgb(50,50,50), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            TextBox txtName = new TextBox() { Location = new Point(290, 5), Width = 150, BackColor = Color.FromArgb(50,50,50), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };

            // Setup Placeholder Logic
            SetPlaceholder(txtCmd, cmd, phCmd);
            SetPlaceholder(txtName, label, phName);

            Button btnIcon = new Button() { Text = "ICON", Location = new Point(445, 4), Size = new Size(50, 24), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(60,60,60) }; btnIcon.FlatAppearance.BorderSize = 0;
            Button btnDel = new Button() { Text = "-", Location = new Point(500, 4), Size = new Size(24, 24), FlatStyle = FlatStyle.Flat, ForeColor = Color.Red };

            // Logic
            string currentIcon = iconPath;
            btnIcon.Click += (s, e) => {
                using (IconSelectorDialog isd = new IconSelectorDialog()) {
                    if (isd.ShowDialog() == DialogResult.OK) {
                        currentIcon = isd.SelectedIcon;
                        if (currentIcon.StartsWith(" MDL2:")) lblIconPrev.Text = char.ConvertFromUtf32(int.Parse(currentIcon.Replace(" MDL2:", ""), System.Globalization.NumberStyles.HexNumber));
                        else lblIconPrev.Text = "Img";
                    }
                }
            };

            btnUp.Click += (s, e) => MoveRow(p, -1);
            btnDown.Click += (s, e) => MoveRow(p, 1);
            
            btnDel.Click += (s, e) => {
                if (MessageBox.Show("Delete this command?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    listCommands.Controls.Remove(p);
            };

            // Store data accessors in Tag
            p.Tag = new Func<string>(() => {
                // Return null if placeholders are still present or empty
                string c = txtCmd.Text; 
                string n = txtName.Text;
                if (c == phCmd || string.IsNullOrWhiteSpace(c)) return null;
                if (n == phName || string.IsNullOrWhiteSpace(n)) n = "Command"; // Fallback name
                
                return string.Format("{0}|{1}={2}", n, currentIcon, c);
            });

            p.Controls.AddRange(new Control[] { lblIconPrev, btnUp, btnDown, txtCmd, txtName, btnIcon, btnDel });
            listCommands.Controls.Add(p);
        }

        private void SetPlaceholder(TextBox txt, string value, string placeholder)
        {
            if (string.IsNullOrEmpty(value)) { txt.Text = placeholder; txt.ForeColor = Color.Gray; }
            else { txt.Text = value; txt.ForeColor = Color.White; }

            txt.GotFocus += (s,e) => { if(txt.Text == placeholder) { txt.Text = ""; txt.ForeColor = Color.White; } };
            txt.LostFocus += (s,e) => { if(string.IsNullOrWhiteSpace(txt.Text)) { txt.Text = placeholder; txt.ForeColor = Color.Gray; } };
        }

        private void MoveRow(Panel p, int direction)
        {
            int idx = listCommands.Controls.GetChildIndex(p);
            int newIdx = idx + direction;
            if (newIdx >= 0 && newIdx < listCommands.Controls.Count)
                listCommands.Controls.SetChildIndex(p, newIdx);
        }

        private void AdjustControlsLayout()
        {
            if (txtEvPath != null && btnBrowse != null)
            {
                int pW = contentPanel.Width;
                int btnW = btnBrowse.Width;
                txtEvPath.Width = pW - 40 - btnW - 1 - 20;
                btnBrowse.Left = txtEvPath.Right + 1;
            }
            if (listCommands != null) listCommands.Width = contentPanel.Width - 20;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Save Misc
            if (chkEnableEv != null) Form1.EnableEverything = chkEnableEv.Checked;
            if (chkToolTips != null) Form1.EnableToolTips = chkToolTips.Checked;
            if (chkDelConf != null) Form1.EnableDeleteConfirm = chkDelConf.Checked;
            if (txtEvPath != null) Form1.EverythingPath = txtEvPath.Text;
            Form1.SettingsWinRect = this.Bounds;

            // Save Context Config
            if (chkCtxToolbar != null)
            {
                Form1.CtxEnableToolbar = chkCtxToolbar.Checked;
                Form1.CtxShowCut = (bool)btnTogCut.Tag;
                Form1.CtxShowCopy = (bool)btnTogCopy.Tag;
                Form1.CtxShowPaste = (bool)btnTogPaste.Tag;
                Form1.CtxShowNew = (bool)btnTogNew.Tag;
                Form1.CtxShowNewFolder = (bool)btnTogNewFolder.Tag;

                try
                {
                    using (StreamWriter sw = new StreamWriter(contextCfgPath))
                    {
                        foreach (Control c in listCommands.Controls)
                        {
                            if (c is Panel && c.Tag is Func<string>)
                            {
                                string line = ((Func<string>)c.Tag)();
                                if (line != null) sw.WriteLine(line);
                            }
                        }
                    }
                }
                catch { }
            }
            
            base.OnFormClosing(e);
        }
    }
}