using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;

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
        public DarkMenuRenderer() : base(new DarkColors()) 
        { 
            this.RoundedEdges = false; 
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e) 
        { 
            e.TextColor = Color.White; 
            base.OnRenderItemText(e); 
        }

        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e) 
        { 
            e.ArrowColor = Color.White; 
            base.OnRenderArrow(e); 
        }

        // Questo rimuove la riga bianca sotto la MenuStrip
        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            // Non fare nulla = nessun bordo disegnato
        }
    }

    public class DarkColors : ProfessionalColorTable
    {
        // Sfondo generale del Menu
        public override Color ToolStripDropDownBackground { get { return Color.FromArgb(43, 43, 43); } }
        
        // Colore della barra del menu principale
        public override Color MenuStripGradientBegin { get { return Color.FromArgb(32, 32, 32); } }
        public override Color MenuStripGradientEnd { get { return Color.FromArgb(32, 32, 32); } }

        // Bordi
        public override Color MenuBorder { get { return Color.FromArgb(80, 80, 80); } }
        public override Color MenuItemBorder { get { return Color.FromArgb(70, 70, 70); } }

        // --- STATI DI SELEZIONE (HOVER/CLICK) ---
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
            if (string.IsNullOrEmpty(s)) return -1; // Mette le cartelle (stringa vuota) in cima o in fondo
            
            // Pulizia: Rimuove spazi extra e porta a maiuscolo
            s = s.Trim().ToUpper();
            
            // Trova la parte numerica
            string numberPart = "";
            string unitPart = "";
            
            int spaceIndex = s.IndexOf(' ');
            if (spaceIndex > 0)
            {
                numberPart = s.Substring(0, spaceIndex);
                unitPart = s.Substring(spaceIndex + 1);
            }
            else return 0; // Formato non riconosciuto

            long val;
            if (!long.TryParse(numberPart, out val)) return 0;

            if (unitPart.StartsWith("KB")) val *= 1024;
            else if (unitPart.StartsWith("MB")) val *= 1024 * 1024;
            else if (unitPart.StartsWith("GB")) val *= 1024 * 1024 * 1024;
            else if (unitPart.StartsWith("TB")) val *= 1024L * 1024L * 1024L * 1024L;

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
            this.Text = title;
            this.Size = new Size(350, 150);

            Label lbl = new Label() { Text = prompt, Left = 10, Top = 15, AutoSize = true, ForeColor = Color.LightGray };
            txtInput = new TextBox() { Left = 10, Top = 40, Width = 310, BackColor = Color.FromArgb(50, 50, 50), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            
            Button btnOk = new Button() { Text = "OK", Left = 80, Top = 75, Width = 80, DialogResult = DialogResult.OK, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(60,60,60) };
            btnOk.FlatAppearance.BorderSize = 0;
            Button btnCancel = new Button() { Text = "Annulla", Left = 170, Top = 75, Width = 80, DialogResult = DialogResult.Cancel, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(60, 60, 60) };
            btnCancel.FlatAppearance.BorderSize = 0;

            this.Controls.Add(lbl);
            this.Controls.Add(txtInput);
            this.Controls.Add(btnOk);
            this.Controls.Add(btnCancel);
            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
        }
    }

    public class DarkAboutBox : DarkDialogBase
    {
        public DarkAboutBox()
        {
            this.Text = "About";
            this.Size = new Size(300, 150);
            Label lbl = new Label() { Text = "4TABs by TOVOT", AutoSize = false, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 12, FontStyle.Bold) };
            lbl.Click += (s, e) => this.Close(); 
            this.Controls.Add(lbl);
        }
    }
}