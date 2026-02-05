using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Drawing;
using System.Reflection;

namespace QuadExplorer
{
    public partial class ExplorerUnit
    {
        // --- SIDEBAR LOGIC ---
        private void PopulateSidebarRoot()
        {
            sideTree.Nodes.Clear();
            sideTree.BeginUpdate(); 

            // Define DLL paths
            string sysDir = Environment.SystemDirectory;
            string imageres = Path.Combine(sysDir, "imageres.dll");
            string shell32 = Path.Combine(sysDir, "shell32.dll");

            // 1. QUICK ACCESS
            TreeNode rootFav = new TreeNode("Quick Access");
            int iconStar = GetManualIcon(shell32, 43); // Star Icon
            if (iconStar == -1) iconStar = GetIconIndex("shell:::{679f85cb-0220-4080-b29b-5540cc05aab6}", false);
            rootFav.ImageIndex = rootFav.SelectedImageIndex = iconStar;
            
            LoadQuickAccessViaShell(rootFav);
            rootFav.Expand(); 
            sideTree.Nodes.Add(rootFav);

            // 2. THIS PC
            TreeNode rootPC = new TreeNode("This PC");
            int iconPC = GetManualIcon(shell32, 15); // Computer Icon
            if (iconPC == -1) iconPC = 0;
            rootPC.ImageIndex = rootPC.SelectedImageIndex = iconPC;

            foreach (var d in DriveInfo.GetDrives())
            {
                if (d.IsReady)
                {
                    TreeNode node = new TreeNode(d.Name + " " + d.VolumeLabel);
                    node.Tag = d.Name;
                    node.ImageIndex = node.SelectedImageIndex = GetIconIndex(d.Name, false);
                    node.Nodes.Add("Dummy");
                    rootPC.Nodes.Add(node);
                }
            }
            rootPC.Expand();
            sideTree.Nodes.Add(rootPC);

            // 3. RECYCLE BIN
            TreeNode trash = new TreeNode("Recycle Bin");
            trash.Tag = "::{645FF040-5081-101B-9F08-00AA002F954E}";
            int trashIconIdx = GetRecycleBinIcon();
            if (trashIconIdx == -1) trashIconIdx = GetIconIndex("::{645FF040-5081-101B-9F08-00AA002F954E}", false);
            trash.ImageIndex = trash.SelectedImageIndex = trashIconIdx;
            sideTree.Nodes.Add(trash);

            sideTree.EndUpdate();
        }

        private int GetManualIcon(string dll, int index)
        {
            Icon ico = NativeMethods.GetIconFromDll(dll, index);
            if (ico != null)
            {
                sharedIcons.Images.Add(ico);
                return sharedIcons.Images.Count - 1;
            }
            return -1;
        }

        private void LoadQuickAccessViaShell(TreeNode parentNode)
        {
            try
            {
                Type shellAppType = Type.GetTypeFromProgID("Shell.Application");
                if (shellAppType == null) return;
                object shell = Activator.CreateInstance(shellAppType);
                string quickAccessGUID = "shell:::{679f85cb-0220-4080-b29b-5540cc05aab6}";
                object folder = shellAppType.InvokeMember("NameSpace", BindingFlags.InvokeMethod, null, shell, new object[] { quickAccessGUID });
                if (folder == null) return;
                object items = folder.GetType().InvokeMember("Items", BindingFlags.InvokeMethod, null, folder, null);
                if (items == null) return;
                int count = (int)items.GetType().InvokeMember("Count", BindingFlags.GetProperty, null, items, null);

                for (int i = 0; i < count; i++)
                {
                    object item = items.GetType().InvokeMember("Item", BindingFlags.InvokeMethod, null, items, new object[] { i });
                    if (item != null)
                    {
                        string path = (string)item.GetType().InvokeMember("Path", BindingFlags.GetProperty, null, item, null);
                        string name = (string)item.GetType().InvokeMember("Name", BindingFlags.GetProperty, null, item, null);
                        if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
                        {
                            TreeNode node = new TreeNode(name);
                            node.Tag = path;
                            node.ImageIndex = node.SelectedImageIndex = GetIconIndex(path, false);
                            node.Nodes.Add("Dummy");
                            parentNode.Nodes.Add(node);
                        }
                    }
                }
            }
            catch { }
        }

        private int GetRecycleBinIcon()
        {
            try
            {
                string keyPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\CLSID\{645FF040-5081-101B-9F08-00AA002F954E}\DefaultIcon";
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(keyPath))
                {
                    if (key != null)
                    {
                        string val = key.GetValue("Full") as string;
                        if (val == null) val = key.GetValue("") as string;
                        if (!string.IsNullOrEmpty(val))
                        {
                            string[] parts = val.Split(',');
                            string file = parts[0];
                            int index = 0;
                            if (parts.Length > 1) int.TryParse(parts[1], out index);
                            return GetManualIcon(file, index);
                        }
                    }
                }
            }
            catch { }
            return -1;
        }

        private void SideTree_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            TreeNode current = e.Node;
            if (current.Nodes.Count == 1 && current.Nodes[0].Text == "Dummy")
            {
                current.Nodes.Clear();
                string path = current.Tag as string;
                if (string.IsNullOrEmpty(path) || path.StartsWith("::") || !Directory.Exists(path)) return; 
                try
                {
                    DirectoryInfo di = new DirectoryInfo(path);
                    foreach (var d in di.GetDirectories())
                    {
                        if ((d.Attributes & FileAttributes.Hidden) != 0) continue;
                        TreeNode sub = new TreeNode(d.Name);
                        sub.Tag = d.FullName;
                        sub.ImageIndex = sub.SelectedImageIndex = GetIconIndex(d.FullName, true); 
                        sub.Nodes.Add("Dummy");
                        current.Nodes.Add(sub);
                    }
                }
                catch {}
            }
        }

        private void SideTree_Click(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node.Tag != null) Navigate(e.Node.Tag.ToString());
        }
    }
}