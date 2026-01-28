using System;
using System.IO;
using System.Collections.Specialized;
using System.Windows.Forms;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace QuadExplorer
{
    public partial class ExplorerUnit
    {
        // --- INPUT (Shortcuts) ---
        private void Lv_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete) DeleteSelected();
            
            // Gestione Clipboard
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

        // --- MENU ACTIONS ---
        private void InitializeMenu()
        {
            ctxMenu = new ContextMenuStrip();
            ctxMenu.Renderer = new DarkMenuRenderer();
            
            ctxMenu.Items.Add(CreateMenuItem("Apri", (s,e) => OpenSelected()));
            ctxMenu.Items.Add(new ToolStripSeparator());
            ctxMenu.Items.Add(CreateMenuItem("Taglia", (s,e) => CutCopy(true)));
            ctxMenu.Items.Add(CreateMenuItem("Copia", (s,e) => CutCopy(false)));
            ctxMenu.Items.Add(CreateMenuItem("Incolla", (s,e) => Paste()));
            ctxMenu.Items.Add(new ToolStripSeparator());
            ctxMenu.Items.Add(CreateMenuItem("Elimina", (s,e) => DeleteSelected()));
            ctxMenu.Items.Add(CreateMenuItem("Proprietà", (s,e) => ShowProps()));
            
            LoadCustomContext();
            listView.ContextMenuStrip = ctxMenu;
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
                newPath = Path.Combine(dir, string.Format("{0}[{1}]{2}", name, counter, ext));
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
            if (MessageBox.Show("Eliminare i file selezionati?", "Conferma", MessageBoxButtons.YesNo) == DialogResult.Yes)
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
                MessageBox.Show("Impossibile aprire proprietà: " + ex.Message);
            }
        }

        private ToolStripMenuItem CreateMenuItem(string text, EventHandler action)
        {
            ToolStripMenuItem item = new ToolStripMenuItem(text);
            item.Click += action;
            item.ForeColor = System.Drawing.Color.White;
            return item;
        }

        // --- DRAG & DROP IMPLEMENTATION ---
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
                catch { MessageBox.Show("Errore durante Drop: " + src); }
            }
            LoadDir(CurrentPath);
        }
    }
}