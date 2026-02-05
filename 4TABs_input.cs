using System;
using System.IO;
using System.Drawing;
using System.Collections.Specialized;
using System.Windows.Forms;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace QuadExplorer
{
    public partial class ExplorerUnit
    {
        // Public method to force menu reload after Settings change
        public void ReloadContext()
        {
            InitializeMenu();
        }

        // --- INPUT HANDLING ---
        private void Lv_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete) DeleteSelected();
            
            // Clipboard Shortcuts
            if (e.Control && e.KeyCode == Keys.C) CutCopy(false);
            if (e.Control && e.KeyCode == Keys.X) CutCopy(true);
            if (e.Control && e.KeyCode == Keys.V) Paste();
        }

        private void TxtAddress_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) { Navigate(txtAddress.Text); e.SuppressKeyPress = true; }
        }

        private void Lv_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.XButton1) GoBack();
            if (e.Button == MouseButtons.XButton2) GoFwd();
        }

        private void Lv_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (e.Column == sorter.SortColumn)
            {
                sorter.Order = (sorter.Order == SortOrder.Ascending) ? SortOrder.Descending : SortOrder.Ascending;
            }
            else
            {
                sorter.SortColumn = e.Column;
                sorter.Order = SortOrder.Ascending;
            }
            listView.Sort();
        }

        // --- CONTEXT MENU ACTIONS ---
        private void InitializeMenu()
        {
            ctxMenu = new ContextMenuStrip();
            ctxMenu.Renderer = new DarkMenuRenderer();
            
            // 1. Custom Commands (Top)
            LoadCustomContext();

            // 2. Horizontal Toolbar (Win11 Style)
            if (Form1.CtxEnableToolbar)
            {
                if (ctxMenu.Items.Count > 0) ctxMenu.Items.Add(new ToolStripSeparator());
                ToolStripControlHost horizontalStrip = CreateHorizontalMenuStrip();
                if (horizontalStrip != null) ctxMenu.Items.Add(horizontalStrip);
            }

            // 3. Standard Commands (Bottom)
            if (ctxMenu.Items.Count > 0) ctxMenu.Items.Add(new ToolStripSeparator());
            ctxMenu.Items.Add(CreateMenuItem("Delete", (s,e) => DeleteSelected()));
            ctxMenu.Items.Add(CreateMenuItem("Properties", (s,e) => ShowProps()));
            
            listView.ContextMenuStrip = ctxMenu;
        }

        private ToolStripControlHost CreateHorizontalMenuStrip()
        {
            FlowLayoutPanel pnl = new FlowLayoutPanel();
            pnl.AutoSize = true;
            pnl.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            pnl.FlowDirection = FlowDirection.LeftToRight;
            pnl.BackColor = Color.FromArgb(43, 43, 43);
            pnl.Padding = new Padding(2);
            pnl.Margin = new Padding(0);

            Font iconFont = new Font("Segoe MDL2 Assets", 12);

            if (Form1.CtxShowCut) pnl.Controls.Add(CreateMenuIconButton("\uE8C6", "Cut", (s,e) => { ctxMenu.Close(); CutCopy(true); }, iconFont));
            if (Form1.CtxShowCopy) pnl.Controls.Add(CreateMenuIconButton("\uE8C8", "Copy", (s,e) => { ctxMenu.Close(); CutCopy(false); }, iconFont));
            if (Form1.CtxShowPaste) pnl.Controls.Add(CreateMenuIconButton("\uE77F", "Paste", (s,e) => { ctxMenu.Close(); Paste(); }, iconFont));
            if (Form1.CtxShowNew) pnl.Controls.Add(CreateMenuIconButton("\uE710", "New", (s,e) => { ctxMenu.Close(); CreateNewFile(); }, iconFont));

            if (pnl.Controls.Count == 0) return null;

            ToolStripControlHost host = new ToolStripControlHost(pnl);
            host.AutoSize = true;
            host.Margin = Padding.Empty;
            host.Padding = Padding.Empty;
            return host;
        }

        private Button CreateMenuIconButton(string icon, string tooltip, EventHandler onClick, Font f)
        {
            Button btn = new Button();
            btn.Text = icon;
            btn.Font = f;
            btn.Size = new Size(36, 30);
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(70, 70, 70);
            btn.BackColor = Color.Transparent;
            btn.ForeColor = Color.White;
            btn.Click += onClick;
            
            if (Form1.EnableToolTips)
            {
                ToolTip tt = new ToolTip();
                tt.SetToolTip(btn, tooltip);
            }
            return btn;
        }

        private void LoadCustomContext()
        {
            string contextConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "context.cfg");
            if (!File.Exists(contextConfigPath)) return;
            try
            {
                string[] lines = File.ReadAllLines(contextConfigPath);
                foreach (string line in lines)
                {
                    string l = line.Trim();
                    if (string.IsNullOrEmpty(l) || l.StartsWith(";")) continue;
                    
                    // Format: Label|IconPath=Command
                    int eqIndex = l.IndexOf('=');
                    if (eqIndex > 0)
                    {
                        string leftSide = l.Substring(0, eqIndex).Trim();
                        string cmdFull = l.Substring(eqIndex + 1).Trim();
                        
                        string label = leftSide;
                        string iconPath = "";

                        int pipeIndex = leftSide.IndexOf('|');
                        if (pipeIndex > 0)
                        {
                            label = leftSide.Substring(0, pipeIndex);
                            iconPath = leftSide.Substring(pipeIndex + 1);
                        }

                        ToolStripMenuItem item = new ToolStripMenuItem(label);
                        item.ForeColor = Color.White;
                        
                        // Icon Handling
                        if (!string.IsNullOrEmpty(iconPath))
                        {
                            if (iconPath.StartsWith(" MDL2:"))
                            {
                                string charCode = iconPath.Replace(" MDL2:", "").Trim();
                                item.Image = RenderMdl2Icon(charCode);
                            }
                            else if (File.Exists(iconPath))
                            {
                                try { item.Image = Image.FromFile(iconPath); } catch {}
                            }
                        }

                        item.Click += (s, e) => RunCustomCommand(cmdFull);
                        ctxMenu.Items.Add(item);
                    }
                }
            } 
            catch { }
        }

        private Image RenderMdl2Icon(string charHex)
        {
            int size = 16;
            Bitmap bmp = new Bitmap(size, size);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                try
                {
                    int code = int.Parse(charHex, System.Globalization.NumberStyles.HexNumber);
                    string s = char.ConvertFromUtf32(code);
                    using (Font f = new Font("Segoe MDL2 Assets", 10))
                    {
                        TextRenderer.DrawText(g, s, f, new Rectangle(0,0,size,size), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    }
                }
                catch { }
            }
            return bmp;
        }

        private void RunCustomCommand(string cmdLine)
        {
            string selPath = "";
            if (listView.SelectedItems.Count > 0) selPath = listView.SelectedItems[0].Tag.ToString();
            string currentDir = CurrentPath;
            if (currentDir.EndsWith("\\") && currentDir.Length > 3) currentDir = currentDir.Substring(0, currentDir.Length - 1);
            
            string finalCmd = cmdLine.Replace("{path}", selPath).Replace("{dir}", currentDir);
            
            string exe = ""; string args = "";
            if (finalCmd.StartsWith("\"")) {
                int endQuote = finalCmd.IndexOf("\"", 1);
                if (endQuote > 0) { exe = finalCmd.Substring(1, endQuote - 1); if (endQuote + 1 < finalCmd.Length) args = finalCmd.Substring(endQuote + 1).Trim(); }
            } else {
                int firstSpace = finalCmd.IndexOf(' ');
                if (firstSpace > 0) { exe = finalCmd.Substring(0, firstSpace); args = finalCmd.Substring(firstSpace + 1); } else { exe = finalCmd; }
            }
            try { Process.Start(new ProcessStartInfo { FileName = exe, Arguments = args, WorkingDirectory = currentDir }); }
            catch (Exception ex) { MessageBox.Show("Error:\n" + ex.Message); }
        }

        private void OpenSelected()
        {
            if (listView.SelectedItems.Count == 0) return;
            string path = listView.SelectedItems[0].Tag as string;
            if (Directory.Exists(path)) Navigate(path);
            else try { Process.Start(path); } catch { }
        }

        private void CutCopy(bool cut)
        {
            if (listView.SelectedItems.Count == 0) return;
            StringCollection paths = new StringCollection();
            foreach (ListViewItem item in listView.SelectedItems) paths.Add(item.Tag.ToString());
            Clipboard.SetFileDropList(paths);
        }

		private void Paste()
        {
            if (Clipboard.ContainsFileDropList())
            {
                var files = Clipboard.GetFileDropList();
                ProcessDrop(CastStringCollection(files));
            }
        }

        private void CreateNewFile()
        {
            using (NewFileDialog dlg = new NewFileDialog())
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    string fname = dlg.FileName;
                    string ftype = dlg.FileType;
                    string full = Path.Combine(CurrentPath, fname + (ftype.StartsWith(".") ? ftype : "." + ftype));
                    try 
                    { 
                        File.Create(full).Close(); 
                        LoadDir(CurrentPath);
                    }
                    catch (Exception ex) { MessageBox.Show("Error creating file: " + ex.Message); }
                }
            }
        }

        private string[] CastStringCollection(StringCollection col)
        {
            string[] arr = new string[col.Count];
            col.CopyTo(arr, 0);
            return arr;
        }

        private string GetUniquePath(string path)
        {
            if (!File.Exists(path) && !Directory.Exists(path)) return path;
            string dir = Path.GetDirectoryName(path);
            string name = Path.GetFileNameWithoutExtension(path);
            string ext = Path.GetExtension(path); 
            if (Directory.Exists(path)) { name = Path.GetFileName(path); ext = ""; }

            int counter = 1;
            string newPath;
            do {
                newPath = Path.Combine(dir, string.Format("{0} [{1}]{2}", name, counter, ext));
                counter++;
            } while (File.Exists(newPath) || Directory.Exists(newPath));
            return newPath;
        }

        private void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destFile);
            }
            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                string destSub = Path.Combine(destDir, Path.GetFileName(dir));
                CopyDirectory(dir, destSub);
            }
        }

        private void DeleteSelected()
        {
            if (listView.SelectedItems.Count == 0) return;
            if (MessageBox.Show("Delete selected files?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                foreach(ListViewItem item in listView.SelectedItems)
                {
                    string p = item.Tag.ToString();
                    try { if(File.Exists(p)) File.Delete(p); else if(Directory.Exists(p)) Directory.Delete(p, true); } catch {}
                }
                LoadDir(CurrentPath);
            }
        }

		private void ShowProps()
        {
            if (listView.SelectedItems.Count == 0) return;
            string path = listView.SelectedItems[0].Tag.ToString();

            try
            {
                NativeMethods.SHELLEXECUTEINFO info = new NativeMethods.SHELLEXECUTEINFO();
                info.cbSize = Marshal.SizeOf(info);
                info.lpVerb = "properties";
                info.lpFile = path;
                info.nShow = NativeMethods.SW_SHOW;
                info.fMask = NativeMethods.SEE_MASK_INVOKEIDLIST;
                NativeMethods.ShellExecuteEx(ref info);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Cannot open properties: " + ex.Message);
            }
        }

        private ToolStripMenuItem CreateMenuItem(string text, EventHandler action)
        {
            ToolStripMenuItem item = new ToolStripMenuItem(text);
            item.Click += action;
            item.ForeColor = System.Drawing.Color.White;
            return item;
        }

        // --- DRAG & DROP ---
        private void Lv_ItemDrag(object sender, ItemDragEventArgs e)
        {
            if (listView.SelectedItems.Count == 0) return;
            StringCollection files = new StringCollection();
            foreach (ListViewItem item in listView.SelectedItems)
            {
                files.Add(item.Tag.ToString());
            }
            DataObject data = new DataObject();
            data.SetFileDropList(files);
            data.SetData(DataFormats.Text, files[0]);
            listView.DoDragDrop(data, DragDropEffects.Copy);
        }

        private void Lv_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
            else e.Effect = DragDropEffects.None;
        }

        private void Lv_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                ProcessDrop(files);
            }
        }

        private void ProcessDrop(string[] files)
        {
            foreach (string src in files)
            {
                try
                {
                    string dest = Path.Combine(CurrentPath, Path.GetFileName(src));
                    dest = GetUniquePath(dest);

                    if (File.Exists(src)) File.Copy(src, dest);
                    else if (Directory.Exists(src)) CopyDirectory(src, dest);
                }
                catch { MessageBox.Show("Error during Drop: " + src); }
            }
            LoadDir(CurrentPath);
        }
    }
}