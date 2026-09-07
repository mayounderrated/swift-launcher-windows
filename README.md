# Swift for Windows

A minimal circular app and file launcher for Windows, inspired by the Linux **Vinyl Launcher**.

Press **Ctrl+Space**, type a name, and press **Enter**. Swift uses native Windows Forms and GDI+ rather than a browser runtime.

## Download

The Windows installer is attached to the **v1.0.0 GitHub Release**. Close an existing portable copy from its tray menu before installing.

Setup installs for the current user, adds a Start menu shortcut and Windows uninstall entry, and offers optional desktop and sign-in shortcuts. Search-folder preferences survive uninstallation.

Requires 64-bit Windows 10/11 and .NET Framework 4.7.2 or newer. The installer is unsigned.

## Preview
Here's a quick preview of the software:
<img width="3838" height="2158" alt="image" src="https://github.com/user-attachments/assets/c1d6678e-9f24-4e01-b2ad-31c0ff6f0a57" />



## Inspiration and credit

The circular disc layout and interaction idea come from **Vinyl Launcher**, demonstrated on Linux by **u/Jaskaran_jassal**:

[Original Reddit post — “Fed up with other app launchers, so I built my own! – Vinyl Launcher”](https://www.reddit.com/r/unixporn/comments/1tpp7kq/oc_fed_up_with_other_app_launchers_so_i_built_my/)

Swift brings that Linux launcher idea to Windows as an **independent reimplementation**. It was built from the visual reference and described behaviour, not by translating or incorporating the original project's source code. It is unofficial and is not affiliated with the original creator. Credit for the original design inspiration belongs to that creator.

The keyboard-first workflow was also motivated by rofi and wofi.

## AI-development disclosure

**This application was substantially generated with OpenAI Codex.** Its code, installer, documentation, and many fixes were produced through iterative AI-assisted development under human direction.

It is an experimental personal project, not a professionally audited product. Automated checks cover selected behaviours and installer lifecycle, but do not guarantee correctness, security, accessibility, or compatibility with every Windows configuration. Review the source and report problems before relying on it for important workflows.

## Controls

| Action | Control |
| --- | --- |
| Show or hide | Ctrl+Space |
| Search apps and files | Type a name; supports case-insensitive subsequence matching |
| Change All / Apps / Files filter | Tab, Shift+Tab, or filter button |
| Select | Up/Down, mouse wheel, or drag the dial |
| Open selection | Enter or click the centre |
| Reveal file location | Ctrl+Enter |
| Dismiss | Escape or switch to another app |
| Refresh index | F5 or tray menu |
| Quit | Tray menu → Exit |

All is the default filter. Each opening clears the previous search. Matching ranks exact names, prefixes, substrings, then scattered letters; it is not spelling correction.

## What gets indexed

- Desktop and Store apps from Windows' Applications catalogue, supplemented by Start menu shortcuts.
- Files under Desktop, Documents, and Downloads by default.
- Custom file roots listed in `%LOCALAPPDATA%\SwiftLauncher\roots.txt`, editable through the tray menu.

Indexing is local and in memory. The file index is capped at 60,000 files; search keeps the top 60 matches and displays up to 10 icons on the dial. Directory junctions/symlinks and folders named `node_modules`, `.git`, `.venv`, and `AppData` are skipped. Press F5 after filesystem changes; there is no continuous filesystem watcher. Opening a cloud placeholder can trigger the provider's normal download.

## Build from source

Run `build.cmd` from a Windows command prompt. It uses the .NET Framework compiler that ships with Windows and writes `dist\Swift.exe`. No package downloads are needed for the application build.

To build the installer, install **Inno Setup 6.7.3 or compatible**, then run:

```text
ISCC.exe installer\Swift.iss
```

The installer is written to `dist\Swift-Setup-1.0.0.exe`. Inno Setup is a build tool, not an application runtime dependency.

```text
src/          C# application and assembly metadata
assets/       Application icon
installer/    Inno Setup installer definition
 docs/        Installed getting-started guide
```

## Validation and limitations

`dist\Swift.exe --self-test` returns exit code 0 if its matching/filter/launch-target checks pass. `--smoke-test` opens the UI briefly without indexing files. The installer was tested by installing into a temporary directory, checking the shortcut and uninstall registration, then uninstalling and verifying that user preferences remained intact.

During development, separate local checks also exercised search-result motion, focus loss, icon rendering, and animation cleanup. Those checks are not a complete regression suite. Animation smoothness, mixed-DPI behaviour, application discovery, file associations, and hotkey conflicts can vary by machine.

The program has no built-in telemetry, account login, or network service. Launching an app or file invokes Windows and may start software that uses the network.

## Licensing

No redistribution license has been selected for this repository yet. Source availability alone does not grant a license to third-party names, logos, or the original Linux project. Application icons are obtained from locally installed Windows applications at runtime; they are not bundled here.
