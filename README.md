<img width="1000" height="200" alt="BannerCIATools" src="https://github.com/user-attachments/assets/1c882709-0a5d-464e-b4dc-7455125c3a10" />

# The all-in-one tool for compiling your projects into `.cia` format with ease.

### About
`CIAToolsR` is a complete rewrite of the original tool, designed to be more stable, faster, and above all, **cross-platform**. We have migrated to an **Avalonia UI** architecture for a fluid experience on both Windows and Linux, while automating the entire compilation chain to save you from dealing with manual script handling.

### RSF-Creator for CIATools | Included in CIATools ( > v7.0.0)

https://github.com/saysaa/CIATools/tree/rsf-creator
---
### SMDH-Creator for CIATools | Included in CIATools ( > v8.1.1 )

https://github.com/saysaa/CIATools/tree/smdh-creator
---

### OLD vs Renewed: What’s changed?
* **Cross-platform**: Runs natively on Windows and Linux and macOS
* **Clean Architecture**: Migrated to .NET MVVM, code completely rewritten.
* **Modern Interface**: Material Design look, clean and readable.
* **Full Control**: Integrated debug console, real-time log management, and automation options for scripts.

---

### Compatibility

CIATools **V5 or earlier** is compatible only with Windows Vista and later (64-bits)

CIATools **V6 or later** is compatible with Windows Vista and later (64-bits) and Linux (64-bits)

CIATools **V10 or later** is compatible with Windows, Linux, macOS 64-Bits

CIAToolsXP is a **derivative version**, compatible with Windows XP, Vista, 7, 8, 8.1, 10, and 11 (32-bit). [Release](https://github.com/saysaa/CIATools/releases/tag/CIAToolsXP)

---
### Usage Guide

1. **Prerequisites**
   - Python must be installed and added to your `PATH`.
   - Use the "Install Python dependencies" button in the app to configure the environment automatically.

2. **Using**
   - Place your assets (icons, banners, binaries) in the `/USER_FILES` folder.
   - Launch the application.
   - Configure your preferences via the interface (Console, Auto-close, etc.).
   - Click **Build CIA** and let the tool handle the rest.
  
3. **Windows**
   - Locate `CIAToolsR.exe` and open it.
  
4. **Linux**
   - Open a terminal and run these commands: `cd ~/CIAToolsR-linux64` - `chmod +x CIAToolsR` - `./CIAToolsR`.

---

### Compile

**Windows x64** : `dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true`

**Windows ARM64** : `dotnet publish -c Release -r win-arm64 --self-contained true -p:PublishSingleFile=true`

**Linux x64** : `dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true`

**Linux ARM64** : `dotnet publish -c Release -r linux-arm64 --self-contained true -p:PublishSingleFile=true`

**macOS x64** : `dotnet publish -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true`

**macOS Apple Silicon** : `dotnet publish -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true`

---

### Credits
* **Manurocker95** for the original base of `CIABUILDER.bat`.

---

### Assets

<img width="64" height="64" alt="CIATools" src="https://github.com/user-attachments/assets/ab250833-6c03-4ff4-b2f9-2d1c6001ce2f" />
<img width="64" height="64" alt="icon_app" src="https://github.com/user-attachments/assets/ac2329bf-ce90-46f3-9cb0-3663bb042638" />
<img width="64" height="64" alt="Plan de travail 1" src="https://github.com/user-attachments/assets/441fb0d3-be10-4a52-a651-42e3570a9492" />

