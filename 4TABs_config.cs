using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;

namespace QuadExplorer
{
    public partial class Form1
    {
        private string settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.cfg");
        private string profilesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "profiles.cfg");
        
        // Default Path
        private static string defPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        
        // Tab Settings
        private string[] savedPaths = new string[] { defPath, defPath, defPath, defPath };
        private int savedSidebarWidth = 250;
        private int[] savedColWidths = new int[] { 250, 80, 120, 140 };
        private string[] savedSorts = new string[] { "0:Ascending", "0:Ascending", "0:Ascending", "0:Ascending" };

        // Global Settings
        public static bool EnableEverything = true;
        public static string EverythingPath = "";
        public static bool EnableToolTips = true;
        public static bool EnableDeleteConfirm = true; // NEW

        // Context Menu Settings (Default Toolbar)
        public static bool CtxEnableToolbar = true;
        public static bool CtxShowCut = true;
        public static bool CtxShowCopy = true;
        public static bool CtxShowPaste = true;
        public static bool CtxShowNew = true;
        public static bool CtxShowNewFolder = true; // NEW

        // Window Geometry Persistence (X,Y,W,H)
        public static Rectangle MainWinRect = new Rectangle(0, 0, 1400, 950);
        public static Rectangle SettingsWinRect = new Rectangle(0, 0, 850, 600);
        public static bool IsMainWinMaximized = false;

        private Dictionary<string, string[]> profiles = new Dictionary<string, string[]>();
        private string currentProfileName = "Default";

        // --- PROFILE MANAGEMENT ---
        private void LoadProfiles()
        {
            profiles.Clear();
            if (!profiles.ContainsKey("Default")) 
                profiles.Add("Default", new string[] { defPath, defPath, defPath, defPath });

            if (File.Exists(profilesPath))
            {
                try
                {
                    string[] lines = File.ReadAllLines(profilesPath);
                    foreach (string line in lines)
                    {
                        string[] parts = line.Split('|');
                        if (parts.Length == 5)
                        {
                            string name = parts[0];
                            string[] paths = new string[] { parts[1], parts[2], parts[3], parts[4] };
                            if (profiles.ContainsKey(name)) profiles[name] = paths;
                            else profiles.Add(name, paths);
                        }
                    }
                }
                catch { }
            }
            RefreshProfileCombo();
        }

        private void SaveProfilesToFile()
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(profilesPath))
                {
                    foreach (var kvp in profiles)
                    {
                        sw.WriteLine(string.Format("{0}|{1}|{2}|{3}|{4}", 
                            kvp.Key, kvp.Value[0], kvp.Value[1], kvp.Value[2], kvp.Value[3]));
                    }
                }
            }
            catch { }
        }

        private void CreateNewProfile()
        {
            using (DarkInputBox input = new DarkInputBox("New Profile", "Profile Name:"))
            {
                if (input.ShowDialog() == DialogResult.OK)
                {
                    string name = input.InputValue.Trim();
                    if (string.IsNullOrEmpty(name)) return;
                    if (profiles.ContainsKey(name))
                    {
                        MessageBox.Show("Profile already exists!");
                        return;
                    }

                    string[] currentPathsSnapshot = new string[4];
                    for (int i = 0; i < 4; i++) currentPathsSnapshot[i] = units[i].CurrentPath;

                    profiles.Add(name, currentPathsSnapshot);
                    SaveProfilesToFile();
                    currentProfileName = name;
                    RefreshProfileCombo();
                }
            }
        }

        private void DeleteCurrentProfile()
        {
            if (currentProfileName == "Default" || string.IsNullOrEmpty(currentProfileName)) return;

            if (MessageBox.Show("Delete profile '" + currentProfileName + "'?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                profiles.Remove(currentProfileName);
                SaveProfilesToFile();
                currentProfileName = "Default";
                RefreshProfileCombo();
                LoadProfileToTabs("Default");
            }
        }

        private void LoadProfileToTabs(string profileName)
        {
            if (profiles.ContainsKey(profileName))
            {
                string[] paths = profiles[profileName];
                for (int i = 0; i < 4; i++)
                {
                    units[i].Navigate(paths[i]);
                }
                currentProfileName = profileName;
            }
        }

        private void RefreshProfileCombo()
        {
            if (cmbProfiles == null) return;
            cmbProfiles.Items.Clear();
            foreach (string name in profiles.Keys)
            {
                cmbProfiles.Items.Add(name);
            }
            cmbProfiles.SelectedItem = currentProfileName;
        }

        // --- SETTINGS MANAGEMENT ---
        private void SaveSettings()
        {
            try
            {
                if (this.WindowState == FormWindowState.Normal)
                {
                    MainWinRect = this.Bounds;
                    IsMainWinMaximized = false;
                }
                else if (this.WindowState == FormWindowState.Maximized)
                {
                    MainWinRect = this.RestoreBounds;
                    IsMainWinMaximized = true;
                }

                using (StreamWriter sw = new StreamWriter(settingsPath))
                {
                    for (int i = 0; i < 4; i++) sw.WriteLine(units[i].CurrentPath);
                    sw.WriteLine(units[0].SidebarWidth);
                    string cols = string.Format("{0},{1},{2},{3}", 
                        units[0].GetColumnWidth(0), units[0].GetColumnWidth(1), units[0].GetColumnWidth(2), units[0].GetColumnWidth(3));
                    sw.WriteLine(cols);
                    for (int i = 0; i < 4; i++)
                    {
                        var s = units[i].GetSortState();
                        sw.WriteLine(string.Format("{0}:{1}", s.Item1, s.Item2));
                    }
                    sw.WriteLine(currentProfileName);
                    
                    // Global Settings
                    sw.WriteLine(EnableEverything.ToString());
                    sw.WriteLine(EverythingPath);
                    sw.WriteLine(EnableToolTips.ToString());
                    sw.WriteLine(EnableDeleteConfirm.ToString()); // NEW

                    // Window Geometries
                    sw.WriteLine(string.Format("{0},{1},{2},{3}", MainWinRect.X, MainWinRect.Y, MainWinRect.Width, MainWinRect.Height));
                    sw.WriteLine(IsMainWinMaximized.ToString());
                    sw.WriteLine(string.Format("{0},{1},{2},{3}", SettingsWinRect.X, SettingsWinRect.Y, SettingsWinRect.Width, SettingsWinRect.Height));

                    // Context Menu (Added 5th element)
                    sw.WriteLine(string.Format("{0}|{1}|{2}|{3}|{4}|{5}", CtxEnableToolbar, CtxShowCut, CtxShowCopy, CtxShowPaste, CtxShowNew, CtxShowNewFolder));
                }
            }
            catch { }
        }

        private void LoadSettings()
        {
            if (!File.Exists(settingsPath)) return;
            try
            {
                string[] lines = File.ReadAllLines(settingsPath);
                int idx = 0;
                
                for (int i = 0; i < 4; i++) { if (idx < lines.Length) savedPaths[i] = lines[idx++]; }
                if (idx < lines.Length) int.TryParse(lines[idx++], out savedSidebarWidth);
                
                if (idx < lines.Length)
                {
                    string[] parts = lines[idx++].Split(',');
                    if (parts.Length == 4) for (int k = 0; k < 4; k++) int.TryParse(parts[k], out savedColWidths[k]);
                }

                for (int i = 0; i < 4; i++) {
                    if (idx < lines.Length) savedSorts[i] = lines[idx++];
                }
                
                if (idx < lines.Length) currentProfileName = lines[idx++];
                if (string.IsNullOrEmpty(currentProfileName)) currentProfileName = "Default";

                if (idx < lines.Length) bool.TryParse(lines[idx++], out EnableEverything);
                if (idx < lines.Length) EverythingPath = lines[idx++];
                if (idx < lines.Length) bool.TryParse(lines[idx++], out EnableToolTips);
                if (idx < lines.Length) bool.TryParse(lines[idx++], out EnableDeleteConfirm); // NEW

                // Load Geometries
                if (idx < lines.Length) MainWinRect = ParseRect(lines[idx++]);
                if (idx < lines.Length) bool.TryParse(lines[idx++], out IsMainWinMaximized);
                if (idx < lines.Length) SettingsWinRect = ParseRect(lines[idx++]);

                // Context Menu
                if (idx < lines.Length)
                {
                    string[] ctxParts = lines[idx++].Split('|');
                    if (ctxParts.Length >= 5)
                    {
                        bool.TryParse(ctxParts[0], out CtxEnableToolbar);
                        bool.TryParse(ctxParts[1], out CtxShowCut);
                        bool.TryParse(ctxParts[2], out CtxShowCopy);
                        bool.TryParse(ctxParts[3], out CtxShowPaste);
                        bool.TryParse(ctxParts[4], out CtxShowNew);
                        if (ctxParts.Length > 5) bool.TryParse(ctxParts[5], out CtxShowNewFolder); // NEW
                    }
                }
            }
            catch { }
        }

        private Rectangle ParseRect(string line)
        {
            string[] p = line.Split(',');
            if (p.Length == 4)
            {
                int x, y, w, h;
                if (int.TryParse(p[0], out x) && int.TryParse(p[1], out y) && 
                    int.TryParse(p[2], out w) && int.TryParse(p[3], out h))
                {
                    return new Rectangle(x, y, w, h);
                }
            }
            return new Rectangle(0, 0, 0, 0); // Invalid
        }
    }
}