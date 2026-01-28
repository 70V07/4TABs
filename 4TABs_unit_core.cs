using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace QuadExplorer
{
    public partial class ExplorerUnit
    {
        public DarkPanel MainPanel { get; private set; }
        public string CurrentPath { get; private set; }
        public event EventHandler<int> SidebarResized;
        public event ColumnWidthChangedEventHandler ColumnResized;

        public int SidebarWidth
        {
            get { return splitter.SplitterDistance; }
            set { try { splitter.SplitterDistance = value; } catch {} }
        }

        private DarkListView listView; 
        private DarkTreeView sideTree;
        private ImageList sharedIcons;
        private DarkSplitter splitter;
        private TextBox txtAddress;
        private Label lblStatus;
        private Button btnBack, btnFwd, btnUp;
        private ContextMenuStrip ctxMenu;

        private Stack<string> historyBack = new Stack<string>();
        private Stack<string> historyFwd = new Stack<string>();
        private bool isNavigating = false;

        private Color clrBg = ColorTranslator.FromHtml("#202020");
        private Color clrHeader = ColorTranslator.FromHtml("#2D2D2D");
        private Color clrText = Color.White;
        private Color clrSelect = ColorTranslator.FromHtml("#444444");

        private int initialSidebarW;
        private int[] initialColW;
        private ListViewItemComparer sorter; 

        public ExplorerUnit(int index, int sidebarW, int[] colWs)
        {
            this.CurrentPath = "C:\\";
            this.initialSidebarW = sidebarW;
            this.initialColW = colWs;
            
            sharedIcons = new ImageList();
            sharedIcons.ColorDepth = ColorDepth.Depth32Bit;
            sharedIcons.ImageSize = new Size(16, 16);

            InitializeUI();
            InitializeMenu();
        }

        public int GetColumnWidth(int index)
        {
            if (index >= 0 && index < listView.Columns.Count) return listView.Columns[index].Width;
            return 100;
        }

        public void SetColumnWidth(int index, int width)
        {
            if (index >= 0 && index < listView.Columns.Count) listView.Columns[index].Width = width;
        }

        public Tuple<int, SortOrder> GetSortState()
        {
            return new Tuple<int, SortOrder>(sorter.SortColumn, sorter.Order);
        }

        public void ApplySort(int col, SortOrder ord)
        {
            sorter.SortColumn = col;
            sorter.Order = ord;
            listView.Sort();
        }

        private void InitializeUI()
        {
            MainPanel = new DarkPanel();
            MainPanel.Dock = DockStyle.Fill;
            MainPanel.BackColor = clrBg;
            MainPanel.Padding = new Padding(1);

            // TOP BAR
            Panel topBar = new Panel();
            topBar.Dock = DockStyle.Top;
            topBar.Height = 36;
            topBar.BackColor = clrBg;

            // --- TASTI CON ICONE MDL2 (Windows 10/11 Style) ---
            // Usiamo il font "Segoe MDL2 Assets" per avere le icone vettoriali native bianche
            Font iconFont = new Font("Segoe MDL2 Assets", 10);

            btnBack = CreateButton("\uE72B"); // Back Arrow
            btnBack.Font = iconFont;
            
            btnFwd = CreateButton("\uE72A"); // Fwd Arrow
            btnFwd.Font = iconFont;
            
            btnUp = CreateButton("\uE74A"); // Up Arrow
            btnUp.Font = iconFont;

            btnBack.Click += (s, e) => GoBack();
            btnFwd.Click += (s, e) => GoFwd();
            btnUp.Click += (s, e) => GoUp();

            txtAddress = new TextBox();
            txtAddress.Dock = DockStyle.Fill;
            txtAddress.BackColor = clrHeader;
            txtAddress.ForeColor = clrText;
            txtAddress.BorderStyle = BorderStyle.FixedSingle;
            txtAddress.Font = new Font("Segoe UI", 10F);
            txtAddress.KeyDown += TxtAddress_KeyDown;

			FlowLayoutPanel navBtns = new FlowLayoutPanel();
            navBtns.Dock = DockStyle.Left;
            navBtns.Width = 100;
            navBtns.WrapContents = false;
            navBtns.Controls.Add(btnBack);
            navBtns.Controls.Add(btnFwd);
            navBtns.Controls.Add(btnUp);

            Panel addrPanel = new Panel();
            addrPanel.Dock = DockStyle.Fill;
            addrPanel.Padding = new Padding(5, 5, 5, 5);
            addrPanel.Controls.Add(txtAddress);

            topBar.Controls.Add(addrPanel);
            topBar.Controls.Add(navBtns);

            // SPLITTER
            splitter = new DarkSplitter();
            splitter.Dock = DockStyle.Fill;
            splitter.BackColor = clrBg;
            splitter.SplitterWidth = 2;
            splitter.FixedPanel = FixedPanel.Panel1;
            splitter.SplitterDistance = initialSidebarW;
            splitter.SplitterMoved += (s, e) => { if (SidebarResized != null) SidebarResized(this, splitter.SplitterDistance); };

            // SIDEBAR
            sideTree = new DarkTreeView();
            sideTree.Dock = DockStyle.Fill;
            sideTree.BackColor = clrBg;
            sideTree.ForeColor = clrText;
            sideTree.BorderStyle = BorderStyle.None;
            sideTree.LineColor = Color.Gray;
            sideTree.ImageList = sharedIcons;
            sideTree.ItemHeight = 22;
            sideTree.Font = new Font("Segoe UI", 9F);
            sideTree.BeforeExpand += SideTree_BeforeExpand;
            sideTree.NodeMouseClick += SideTree_Click;
            PopulateSidebarRoot();

            // LISTVIEW
            listView = new DarkListView();
            listView.Dock = DockStyle.Fill;
            listView.View = View.Details;
            listView.FullRowSelect = true;
            listView.BackColor = clrBg;
            listView.ForeColor = clrText;
            listView.BorderStyle = BorderStyle.None;
            listView.SmallImageList = sharedIcons;
            listView.OwnerDraw = true; 
            listView.Font = new Font("Segoe UI", 9F);
            
            listView.AllowDrop = true;
            listView.ItemDrag += Lv_ItemDrag;
            listView.DragEnter += Lv_DragEnter;
            listView.DragDrop += Lv_DragDrop;
            
            listView.Columns.Add("Nome", initialColW[0]);
            listView.Columns.Add("Dimensione", initialColW[1]);
            listView.Columns.Add("Tipo", initialColW[2]);
            listView.Columns.Add("Ultima modifica", initialColW[3]);
            
            listView.ColumnWidthChanged += (s, e) => { if (ColumnResized != null) ColumnResized(this, e); };
            listView.DrawColumnHeader += Lv_DrawColumnHeader;
            listView.DrawItem += Lv_DrawItem;
            listView.DrawSubItem += Lv_DrawSubItem;
            listView.MouseDoubleClick += (s, e) => OpenSelected();
			listView.KeyDown += Lv_KeyDown;
            listView.MouseDown += Lv_MouseDown;

            sorter = new ListViewItemComparer();
            listView.ListViewItemSorter = sorter;
            listView.ColumnClick += Lv_ColumnClick;

            splitter.Panel1.Controls.Add(sideTree);
            splitter.Panel2.Controls.Add(listView);

            // STATUS BAR
            lblStatus = new Label();
            lblStatus.Dock = DockStyle.Bottom;
            lblStatus.Height = 24;
            lblStatus.BackColor = clrHeader;
            lblStatus.ForeColor = Color.Silver;
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            lblStatus.Font = new Font("Segoe UI", 8F);

            MainPanel.Controls.Add(splitter);
            MainPanel.Controls.Add(topBar);
            MainPanel.Controls.Add(lblStatus);
        }

        // --- NAVIGAZIONE ---
        public void Navigate(string path)
        {
            if (path.StartsWith("::"))
            {
                try { Process.Start("explorer.exe", path); } catch {}
                return;
            }

            if (!Directory.Exists(path)) return;
            
            if (!isNavigating && CurrentPath != path)
            {
                historyBack.Push(CurrentPath);
                historyFwd.Clear();
            }
            LoadDir(path);
            UpdateButtons();
        }

        private void LoadDir(string path)
        {
            try
            {
                listView.BeginUpdate();
                listView.Items.Clear();
                DirectoryInfo di = new DirectoryInfo(path);
                List<ListViewItem> items = new List<ListViewItem>();

                foreach (var d in di.GetDirectories())
                {
                    if ((d.Attributes & FileAttributes.Hidden) != 0) continue;
                    ListViewItem item = new ListViewItem(d.Name);
                    item.SubItems.Add("");
                    item.SubItems.Add("Cartella");
                    item.SubItems.Add(d.LastWriteTime.ToString("yyyy/MM/dd HH:mm"));
                    item.Tag = d.FullName;
                    item.ImageIndex = GetIconIndex(d.FullName, true);
                    items.Add(item);
                }

                foreach (var f in di.GetFiles())
                {
                    if ((f.Attributes & FileAttributes.Hidden) != 0) continue;
                    ListViewItem item = new ListViewItem(f.Name);
                    item.SubItems.Add(FormatSize(f.Length));
                    item.SubItems.Add(f.Extension);
                    item.SubItems.Add(f.LastWriteTime.ToString("yyyy/MM/dd HH:mm"));
                    item.Tag = f.FullName;
                    item.ImageIndex = GetIconIndex(f.FullName, false);
                    items.Add(item);
                }

                listView.Items.AddRange(items.ToArray());
                CurrentPath = path;
                txtAddress.Text = path;
                lblStatus.Text = string.Format(" {0} elementi", items.Count);
                
                // Rialloca il sorter
                listView.Sort();
            }
            catch (Exception ex) { lblStatus.Text = "Errore: " + ex.Message; }
            finally { listView.EndUpdate(); }
        }

        private void GoBack()
        {
            if (historyBack.Count > 0)
            {
                isNavigating = true;
                historyFwd.Push(CurrentPath);
                Navigate(historyBack.Pop());
                isNavigating = false;
            }
        }

        private void GoFwd()
        {
            if (historyFwd.Count > 0)
            {
                isNavigating = true;
                historyBack.Push(CurrentPath);
                Navigate(historyFwd.Pop());
                isNavigating = false;
            }
        }

        private void GoUp()
        {
            try
            {
                DirectoryInfo di = new DirectoryInfo(CurrentPath);
                if (di.Parent != null) Navigate(di.Parent.FullName);
            }
            catch {}
        }

        private void UpdateButtons()
        {
            btnBack.Enabled = historyBack.Count > 0;
            btnFwd.Enabled = historyFwd.Count > 0;
            btnBack.ForeColor = btnBack.Enabled ? clrText : Color.Gray;
            btnFwd.ForeColor = btnFwd.Enabled ? clrText : Color.Gray;
        }

        // --- UTILS ---
        private int GetIconIndex(string path, bool isDir)
        {
            NativeMethods.SHFILEINFO shinfo = new NativeMethods.SHFILEINFO();
            uint flags = NativeMethods.SHGFI_ICON | NativeMethods.SHGFI_SMALLICON;
            bool isSpecial = !path.Contains("\\"); 
            if (isDir && !isSpecial) flags |= NativeMethods.SHGFI_USEFILEATTRIBUTES;
            if (path.StartsWith("::")) flags = NativeMethods.SHGFI_ICON | NativeMethods.SHGFI_SMALLICON;
            IntPtr hImg = NativeMethods.SHGetFileInfo(path, (uint)(isDir ? 0x10 : 0), ref shinfo, (uint)Marshal.SizeOf(shinfo), flags);
            if (shinfo.hIcon != IntPtr.Zero) { Icon icon = Icon.FromHandle(shinfo.hIcon); sharedIcons.Images.Add(icon); return sharedIcons.Images.Count - 1; }
            return -1;
        }

        private string FormatSize(long bytes)
        {
            if (bytes < 1024) return bytes + " B";
            if (bytes < 1024 * 1024) return (bytes / 1024) + " KB";
            return (bytes / (1024 * 1024)) + " MB";
        }

        // --- DRAWING ---
        private void Lv_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            using (SolidBrush b = new SolidBrush(ColorTranslator.FromHtml("#2D2D2D"))) e.Graphics.FillRectangle(b, e.Bounds);
            using (Pen p = new Pen(Color.FromArgb(80, 80, 80))) e.Graphics.DrawRectangle(p, e.Bounds);
            TextRenderer.DrawText(e.Graphics, e.Header.Text, e.Font, e.Bounds, Color.WhiteSmoke, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
        }

        private void Lv_DrawItem(object sender, DrawListViewItemEventArgs e) { e.DrawDefault = false; }

        private void Lv_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            using (SolidBrush b = new SolidBrush(e.Item.Selected ? clrSelect : clrBg)) e.Graphics.FillRectangle(b, e.Bounds);
            if (e.ColumnIndex == 0)
            {
                if (e.Item.ImageList != null && e.Item.ImageIndex >= 0) e.Item.ImageList.Draw(e.Graphics, e.Bounds.Left + 2, e.Bounds.Top + 2, 16, 16, e.Item.ImageIndex);
                TextRenderer.DrawText(e.Graphics, e.SubItem.Text, listView.Font, new Point(e.Bounds.Left + 20, e.Bounds.Top + 2), clrText);
            }
            else TextRenderer.DrawText(e.Graphics, e.SubItem.Text, listView.Font, new Point(e.Bounds.Left + 2, e.Bounds.Top + 2), clrText);
        }

        private Button CreateButton(string text)
        {
            Button b = new Button(); b.Text = text; b.Width = 30; b.Height = 28; b.FlatStyle = FlatStyle.Flat; b.FlatAppearance.BorderSize = 0; b.BackColor = clrHeader; b.ForeColor = clrText; b.Margin = new Padding(1); return b;
        }
    }
}