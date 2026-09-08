# Swift for Windows

**Swift for Windows** is a lightweight, circular app and file launcher inspired by Linux's Vinyl Launcher. Built using native C# (.NET / GDI+), it uses zero browser runtimes.

<img src="https://github.com/user-attachments/assets/c1d6678e-9f24-4e01-b2ad-31c0ff6f0a57" width="100%" alt="Swift Preview" />

---

### **Quick Setup & Usage**
* **Shortcut:** Press `Ctrl+Space` to toggle, type to search, and hit `Enter` to launch.
* **Requirements:** 64-bit Windows 10/11 with .NET Framework 4.7.2+.
* **Installation:** Download the setup `.exe` from the **v1.0.0 GitHub Release**. *(Close any existing portable copy from its tray menu before installing)*.

---

### **Controls**

| Action | Control |
| :--- | :--- |
| **Show / Hide** | `Ctrl+Space` |
| **Search** | Type name (supports subsequence matching) |
| **Switch Filter (All / Apps / Files)** | `Tab` or `Shift+Tab` |
| **Select Item** | `Up` / `Down` arrows, mouse wheel, or drag dial |
| **Open Item** | `Enter` or click centre |
| **Reveal File Location** | `Ctrl+Enter` |
| **Refresh Index** | `F5` or via Tray menu |
| **Dismiss / Quit** | `Escape` / Tray menu → Exit |

---

### **Indexing Behavior**
* **What's Indexed:** Start Menu apps, Microsoft Store apps, and default user folders (`Desktop`, `Documents`, `Downloads`).
* **Custom Paths:** Add custom directories in `%LOCALAPPDATA%\SwiftLauncher\roots.txt` (accessible via tray menu).
* **Performance Limits:** Runs purely in local memory (capped at 60,000 files; keeps top 60 matches, displays up to 10 icons on the dial).
* **Exclusions:** Automatically skips heavy/system folders (`node_modules`, `.git`, `.venv`, `AppData`). 
* **Note:** No continuous background filesystem watcher; press `F5` to manually refresh after changes.

---

### **Developer Notes**
* **Inspiration:** Unofficial C# reimplementation of **Vinyl Launcher** (Reddit `u/Jaskaran_jassal`).
* **AI Disclosure:** Substantially generated using OpenAI Codex under human direction.
* **Privacy:** Zero telemetry, network calls, or account dependencies.
* **Build from Source:** Run `build.cmd` using Windows' built-in .NET compiler (`dist\Swift.exe`). Installer built via **Inno Setup 6.7.3+** (`ISCC.exe installer\Swift.iss`).
