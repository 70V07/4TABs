![4TABs Preview](https://raw.githubusercontent.com/70V07/4TABs/refs/heads/main/screenshoot.jpg)

4TABs is a 4 tabs file manager for Windows 10/11+ designed to enhance file manipulation efficiency through a centralized four-unit interface

**download the .exe or compile from source (or any other C# compiler):**
```
& "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /target:winexe /win32icon:"<path_of_icon>\4TABs.ico" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /out:"<path_of_exe>\4TABs.exe" "<path_of_source>\*.cs"
```

## ⚠️ Warnings

* some text are still in my language: Italian

* **Note**:
This software is currently an **incomplete prototype**!

* **VirusTotal report ([99d91bed2ac11da21fa708d09a58bac81a8527ca23fc5121f06ae02e81fa8b62](https://www.virustotal.com/gui/file/99d91bed2ac11da21fa708d09a58bac81a8527ca23fc5121f06ae02e81fa8b62)):**  
detection results are **false positives** due to the nature of low-level Windows API hooks for the dark theme and shell integration

⚖️ **Disclaimer:**  
this software is provided "as is", without warranty of any kind. the Author takes no responsibility for data loss or system instability

## 💻 Features

* **Quad-Pane Layout:**  
four independent explorer units within a single window (high performance and almost total control)
* **Navigation & File Management shortcuts:**  
buttons next to the path bar and few Keyboard shortcuts, also a simple *context menus (WIP)*
* **Profile Management:**  
save and load custom path configurations for all tabs (profiles are saved via `profiles.cfg`)
* **Immersive Dark Mode:**  
full integration with Windows **dark theme (only)**, including custom-drawn scrollbars and headers
* **Sync-Resizing:**  
dynamic synchronization of sidebar and column widths across all panes
* **Shell Integration:**  
high-quality system icons and native *context menus (WIP)*
* **Persistent Settings:**  
remembers window size, positions, any column and sidebar dimensions and last used paths, also last profile used (all stored in `settings.cfg`)
* **Smart Sorting:**  
column sorting with logic for file sizes (KB/MB/GB) and alphabetical order, and clickable column sorting (Name, Smart Size, Date, Type)
* **Drag and Drop:**  
full drag and drop functionality inside and outside (example: to and from other file managers)
* **MISC QOL:**  
Recycle Bin under Drives in sidebar, Pinned in File Explorer are mirrored in 4TABs, simple status bar
* **Smart CopyPasta:**  
automatic conflict handling with Windows-style renaming (example: File[1].txt) and recursive folder copy support
* **Properties Dialog:**  
integration with the native *Windows Properties panel (light theme right now due the confusion of Microsoft development -_-)*

## ⌨️ Keyboard Shortcuts

| Combo | Action |
| :--- | :--- |
| `[CTRL]` + `[C]` | Copy selected items |
| `[CTRL]` + `[X]` | Cut selected items |
| `[CTRL]` + `[V]` | Paste items to current directory |
| `[DEL]` | Delete selected items |
| `[ENTER]` | Navigate to path in Address Bar |
| `[Mouse XButton 1]` | History Back |
| `[Mouse XButton 2]` | History Forward |

## TODO / MAYBE (probability %)

* **Full Localizations (99):**  
in Italian and English, and maybe some others :|
* **Custom Commands (86):**  
execute external tools (example: Terminal, VS Code) directly from the context menu via `settings.cfg` and `context.cfg`
* **Self-made and Community ADDONS (10):**  
like Tablacus Explorer
* **Support for Everything (55):**  
as replacer of the very inefficent and slow Windows Search (indexing)
* **Custom themes (74):**  
self-explanatory
