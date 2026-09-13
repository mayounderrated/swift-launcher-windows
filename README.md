# Swift for Windows

**Swift for Windows** is a lightweight, circular app and file launcher inspired by Linux's Vinyl Launcher. Built with native C# (.NET / GDI+), it uses zero browser runtimes.

<img src="https://github.com/user-attachments/assets/c1d6678e-9f24-4e01-b2ad-31c0ff6f0a57" width="100%" alt="Swift Preview" />

Version 2 adds a native settings window, themes, configurable global shortcuts, Google search, and a local maths calculator.

---

### **Quick Setup & Usage**

- **Shortcut:** Press `Ctrl+Space` to toggle, type to search, and press `Enter` to launch. The shortcut is configurable.
- **Requirements:** 64-bit Windows 10/11 with .NET Framework 4.7.2+.
- **Installation:** Download the setup `.exe` from the **v2.0.0 GitHub Release**. Close any running copy of Swift before installing the update.

---

### **Controls**

| Action | Control |
| :--- | :--- |
| **Show / Hide** | `Ctrl+Space` by default |
| **Search apps and files** | Type a name; supports case-insensitive subsequence matching |
| **Search Google** | Type `g your search`, then press `Enter` |
| **Calculate** | Type `(12+4)*3`; `Enter` copies the result |
| **Open settings** | Tray menu → Settings, or search for `settings` |
| **Switch filter** | `Tab`, `Shift+Tab`, or the All / Apps / Files button |
| **Select item** | `Up` / `Down`, mouse wheel, or drag the dial |
| **Open item** | `Enter` or click the centre |
| **Reveal file location** | `Ctrl+Enter` |
| **Refresh index** | `F5` or the tray menu |
| **Dismiss / Quit** | `Escape` / Tray menu → Exit |

All is the default filter and each opening clears the previous query. Both behaviours can be changed in Settings. Matching ranks exact names, prefixes, substrings, then scattered letters; it is not spelling correction.

### **Settings & Quick Actions**

Settings include dark, light, and system themes; five accent colours; fast, normal, or disabled animations; a configurable global shortcut; default result mode; Google and calculator toggles; clear-on-open behaviour; and start-at-sign-in.

Google only opens when you select its result and press Enter. The calculator runs locally and supports parentheses, `+`, `-`, `*`, `/`, `%`, powers (`^`), `sqrt`, `sin`, `cos`, `tan`, `abs`, `log`, `ln`, `floor`, `ceil`, `round`, `pi`, and `e`.

Preferences are stored in `%LOCALAPPDATA%\SwiftLauncher\settings.ini`.

### **Indexing Behaviour**

- **What's indexed:** Start Menu apps, Microsoft Store apps, and default user folders (`Desktop`, `Documents`, `Downloads`).
- **Custom paths:** Add directories in `%LOCALAPPDATA%\SwiftLauncher\roots.txt` from the tray menu.
- **Performance limits:** Runs in local memory, capped at 60,000 files; keeps the top 60 matches and displays up to 10 dial icons.
- **Exclusions:** Skips directory links and heavy/system folders (`node_modules`, `.git`, `.venv`, `AppData`).
- **Refresh:** There is no continuous filesystem watcher; press `F5` after filesystem changes.

---

### **Developer Notes**

- **Inspiration:** Independent, unofficial Windows reimplementation of [Vinyl Launcher by u/Jaskaran_jassal](https://www.reddit.com/r/unixporn/comments/1tpp7kq/oc_fed_up_with_other_app_launchers_so_i_built_my/). No original source code was translated or incorporated.
- **AI disclosure:** Substantially generated with OpenAI Codex under human direction.
- **Privacy:** No telemetry, account login, or background network service. Google search opens the selected query in the default browser.
- **Build from source:** Run `build.cmd` with Windows' built-in .NET Framework compiler. It writes `dist\Swift.exe`.
- **Installer:** Build with Inno Setup 6.7.3+ using `ISCC.exe installer\Swift.iss`. Output is `dist\Swift-Setup-2.0.0.exe`.

`dist\Swift.exe --self-test` checks matching, filtering, launch targets, quick-action metadata, and calculator behaviour. `--smoke-test` briefly exercises the UI without indexing files.

Swift is experimental and the installer is unsigned. Animation smoothness, mixed-DPI behaviour, application discovery, and file associations can vary across Windows systems.

### **Licensing**

No redistribution licence has been selected. Source availability alone does not grant a licence to third-party names, logos, or the original Linux project. Application icons are read from locally installed Windows applications at runtime and are not bundled here.
