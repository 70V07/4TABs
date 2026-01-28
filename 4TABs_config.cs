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
        
        // --- MODIFICA DEFAULT: Imposta Desktop come base ---
        private static string defPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        
        private string[] savedPaths = new string[] { defPath, defPath, defPath, defPath };
        private int savedSidebarWidth = 250;
        private int[] savedColWidths = new int[] { 250, 80, 120, 140 };
        private string[] savedSorts = new string[] { "0:Ascending", "0:Ascending", "0:Ascending", "0:Ascending" };

        private Dictionary<string, string[]> profiles = new Dictionary<string, string[]>();
        private string currentProfileName = "Default";

        // --- GESTIONE PROFILI ---
        private void LoadProfiles()
        {
            profiles.Clear();
            
            // Se il profilo Default non esiste (primo avvio), crealo con 4 tab su Desktop
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
                        // Salva: Nome|Path1|Path2|Path3|Path4
                        sw.WriteLine(string.Format("{0}|{1}|{2}|{3}|{4}", 
                            kvp.Key, kvp.Value[0], kvp.Value[1], kvp.Value[2], kvp.Value[3]));
                    }
                }
            }
            catch { }
        }

        private void CreateNewProfile()
        {
            using (DarkInputBox input = new DarkInputBox("Nuovo Profilo", "Nome del profilo:"))
            {
                if (input.ShowDialog() == DialogResult.OK)
                {
                    string name = input.InputValue.Trim();
                    if (string.IsNullOrEmpty(name)) return;
                    if (profiles.ContainsKey(name))
                    {
                        MessageBox.Show("Profilo già esistente!");
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

            if (MessageBox.Show("Eliminare il profilo '" + currentProfileName + "'?", "Conferma", MessageBoxButtons.YesNo) == DialogResult.Yes)
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

        // --- GESTIONE SETTINGS (Generali) ---
        private void SaveSettings()
        {
            try
            {
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
            }
            catch { }
        }
    }

    public partial class ExplorerUnit
    {
        private string contextConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "context.cfg");

        private void LoadCustomContext()
        {
            if (!File.Exists(contextConfigPath)) return;
            try
            {
                string[] lines = File.ReadAllLines(contextConfigPath);
                ctxMenu.Items.Add(new ToolStripSeparator()); 
                ToolStripMenuItem currentParent = null; 
                foreach (string line in lines)
                {
                    string l = line.Trim();
                    if (string.IsNullOrEmpty(l) || l.StartsWith(";")) continue;
                    if (l.StartsWith("[") && l.EndsWith("]")) {
                        string groupName = l.Substring(1, l.Length - 2);
                        if (groupName.ToLower() == "default" || groupName.ToLower() == "menu") currentParent = null; 
                        else { currentParent = new ToolStripMenuItem(groupName); currentParent.ForeColor = Color.White; ctxMenu.Items.Add(currentParent); }
                        continue;
                    }
                    int eqIndex = l.IndexOf('=');
                    if (eqIndex > 0) {
                        string label = l.Substring(0, eqIndex).Trim();
                        string cmdFull = l.Substring(eqIndex + 1).Trim();
                        ToolStripMenuItem item = new ToolStripMenuItem(label);
                        item.ForeColor = Color.Yellow; 
                        item.Click += (s, e) => RunCustomCommand(cmdFull);
                        if (currentParent != null) currentParent.DropDownItems.Add(item); else ctxMenu.Items.Add(item);
                    }
                }
            } catch { }
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
            catch (Exception ex) { MessageBox.Show("Errore:\n" + ex.Message); }
        }
    }
}