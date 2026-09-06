using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Platform;
using CIAToolsR.ViewModels;
using Octokit;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;

namespace CIAToolsR.Views
{
    public partial class MainWindow : Window
    {
        private const string CurrentVersion = "10.0.0";

        public MainWindow()
        {
            InitializeComponent();

            _ = CheckUpdateAsync();

            DragDrop.AddDropHandler(this, OnDrop);
            DragDrop.AddDragOverHandler(this, OnDragOver);

            if (DataContext == null)
            {
                DataContext = new MainWindowViewModel();
            }
        }

        public static string FindRootPath()
        {
            string? dir = AppDomain.CurrentDomain.BaseDirectory;

            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir, "root_path")) ||
                    Directory.Exists(Path.Combine(dir, "RSSCRIPT")) ||
                    Directory.Exists(Path.Combine(dir, "USER_FILES")))
                {
                    return dir;
                }

                dir = Directory.GetParent(dir)?.FullName;
            }

            return AppDomain.CurrentDomain.BaseDirectory;
        }

        private void OnDragOver(object? sender, DragEventArgs e)
        {
            e.DragEffects = e.DataTransfer.Contains(DataFormat.File)
                ? DragDropEffects.Copy
                : DragDropEffects.None;
        }

        private void OnDrop(object? sender, DragEventArgs e)
        {
            var files = e.DataTransfer.TryGetFiles();

            if (files == null)
                return;

            var paths = files.Select(f => f.Path.LocalPath).ToList();

            if (paths.Count > 0 && DataContext is MainWindowViewModel vm)
            {
                vm.ImportFiles(paths);
                
                // Switch Panels
                DropZonePanel.IsVisible = false;
                ConfirmationPanel.IsVisible = true;
            }
        }

        public void OnDropZoneClick(object? sender, PointerPressedEventArgs e)
        {
            ImportFiles(sender, e);
        }

        public void OnCancelImport(object? sender, RoutedEventArgs e)
        {
            DropZonePanel.IsVisible = true;
            ConfirmationPanel.IsVisible = false;
        }

        public async void ImportFiles(object? sender, RoutedEventArgs e)
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "CIATools Import - Select Files",
                AllowMultiple = true
            });

            if (files.Count > 0 && DataContext is MainWindowViewModel vm)
            {
                var paths = files.Select(f => f.Path.LocalPath).ToList();
                vm.ImportFiles(paths);

                // Switch Panels
                DropZonePanel.IsVisible = false;
                ConfirmationPanel.IsVisible = true;
            }
        }

        public void OnClearMenuClick(object? sender, RoutedEventArgs e)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.ClearUserFiles();
            }
        }

        public async void OnSetAuthorName(object? sender, RoutedEventArgs e)
        {
            var dialog = new Window
            {
                Width = 380,
                SizeToContent = SizeToContent.Height,
                Title = "Set Author",
                CanResize = false,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var textBox = new TextBox
            {
                PlaceholderText = "Author Name",
                MinWidth = 320,
                Margin = new Thickness(0, 0, 0, 16)
            };

            var saveButton = new Button
            {
                Content = "Save",
                IsEnabled = false,
                MinWidth = 100
            };

            var cancelButton = new Button
            {
                Content = "Cancel",
                MinWidth = 100
            };

            textBox.TextChanged += (_, _) =>
            {
                saveButton.IsEnabled = !string.IsNullOrWhiteSpace(textBox.Text);
            };

            cancelButton.Click += (_, _) =>
            {
                dialog.Close();
            };

            saveButton.Click += async (_, _) =>
            {
                string authorName = textBox.Text?.Trim() ?? "";

                if (string.IsNullOrWhiteSpace(authorName))
                    return;

                await SaveCreatorAsync(FindRootPath(), authorName);
                dialog.Close();
            };

            var buttons = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                Spacing = 8,
                Children =
                {
                    cancelButton,
                    saveButton
                }
            };

            dialog.Content = new Border
            {
                Padding = new Thickness(20),
                Child = new StackPanel
                {
                    Spacing = 4,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = "Define author",
                            FontSize = 18,
                            FontWeight = Avalonia.Media.FontWeight.SemiBold,
                            Margin = new Thickness(0, 0, 0, 8)
                        },

                        new TextBlock
                        {
                            Text = "Enter the name that will be used as the project author.",
                            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                            Opacity = 0.75,
                            Margin = new Thickness(0, 0, 0, 12)
                        },

                        textBox,
                        buttons
                    }
                }
            };

            textBox.Focus();

            await dialog.ShowDialog(this);
        }

        public static async Task SaveCreatorAsync(string rootPath, string creatorName)
        {
            string userFilesPath = Path.Combine(rootPath, "USER_FILES");
            Directory.CreateDirectory(userFilesPath);

            string creatorPath = Path.Combine(userFilesPath, "AUTHOR.txt");

            await File.WriteAllTextAsync(creatorPath, creatorName.Trim());
        }

        public void OnRestoreFILEPATH(object? sender, RoutedEventArgs e)
        {
            string rootPath = FindRootPath();
            string userFilesPath = Path.Combine(rootPath, "USER_FILES");
            string filePath = Path.Combine(userFilesPath, "FILE_PATH");

            try
            {
                Directory.CreateDirectory(userFilesPath);

                using var stream = File.Create(filePath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to restore FILE_PATH: {ex.Message}");
            }
        }

        public void OnRestoreAuthor(object? sender, RoutedEventArgs e)
        {
            string rootPath = FindRootPath();
            string userFilesPath = Path.Combine(rootPath, "USER_FILES");
            string creatorPath = Path.Combine(userFilesPath, "AUTHOR.txt");

            try
            {
                Directory.CreateDirectory(userFilesPath);

                using var stream = File.Create(creatorPath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to restore AUTHOR.txt: {ex.Message}");
            }
        }

        public async void OnRSFCREATOR(object? sender, RoutedEventArgs e)
        {
            var dialog = new Window
            {
                Width = 520,
                MaxHeight = 720,
                SizeToContent = SizeToContent.Height,
                Title = "RSF Creator",
                CanResize = false,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var titleBox = new TextBox
            {
                Text = "My Homebrew",
                PlaceholderText = "Title"
            };

            var companyCodeBox = new TextBox
            {
                Text = "00",
                PlaceholderText = "Company Code"
            };

            var productCodeBox = new TextBox
            {
                Text = "CTR-P-PWDR",
                PlaceholderText = "Product Code"
            };

            var titleIdBox = new TextBox
            {
                Text = "000400000FF00000",
                PlaceholderText = "Title ID"
            };

            var romFsBox = new TextBox
            {
                Text = "romfs",
                PlaceholderText = "RomFS path"
            };

            var saveDataSizeBox = new TextBox
            {
                Text = "128KB",
                PlaceholderText = "Save Data Size"
            };

            var new3dsCpuBox = new CheckBox
            {
                Content = "Enable New 3DS CPU",
                IsChecked = true,
                Margin = new Thickness(0, 6, 0, 8)
            };

            var statusText = new TextBlock
            {
                Text = "",
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                Opacity = 0.85,
                Margin = new Thickness(0, 8, 0, 0)
            };

            var generateButton = new Button
            {
                Content = "Generate RSF",
                MinWidth = 120
            };

            var closeButton = new Button
            {
                Content = "Close",
                MinWidth = 100
            };

            closeButton.Click += (_, _) =>
            {
                dialog.Close();
            };

            generateButton.Click += async (_, _) =>
            {
                string title = titleBox.Text?.Trim() ?? "";
                string companyCode = companyCodeBox.Text?.Trim() ?? "";
                string productCode = productCodeBox.Text?.Trim() ?? "";
                string titleId = titleIdBox.Text?.Trim() ?? "";
                string romFsPath = romFsBox.Text?.Trim() ?? "";
                string saveDataSize = saveDataSizeBox.Text?.Trim() ?? "";
                bool enableNew3dsCpu = new3dsCpuBox.IsChecked == true;

                string? validationError = ValidateRsfConfig(
                    title,
                    companyCode,
                    productCode,
                    titleId,
                    romFsPath,
                    saveDataSize
                );

                if (validationError != null)
                {
                    statusText.Text = validationError;
                    return;
                }

                try
                {
                    string rootPath = FindRootPath();
                    string userFilesPath = Path.Combine(rootPath, "USER_FILES");
                    Directory.CreateDirectory(userFilesPath);

                    string safeTitle = MakeSafeFileName(title);
                    string rsfPath = Path.Combine(userFilesPath, $"{safeTitle}.rsf");

                    if (File.Exists(rsfPath))
                    {
                        bool overwrite = await ShowConfirmDialogAsync(
                            "File already exists",
                            $"The file already exists:\n\n{safeTitle}.rsf\n\nDo you want to overwrite it?"
                        );

                        if (!overwrite)
                            return;
                    }

                    string rsfContent = BuildRsfContent(
                        title,
                        companyCode,
                        productCode,
                        titleId,
                        romFsPath,
                        saveDataSize,
                        enableNew3dsCpu
                    );

                    await File.WriteAllTextAsync(rsfPath, rsfContent);

                    statusText.Text = $"RSF generated successfully:\n{rsfPath}";

                    if (DataContext is MainWindowViewModel vm)
                    {
                        vm.Debug_output = $"RSF generated: {Path.GetFileName(rsfPath)}";
                    }
                }
                catch (Exception ex)
                {
                    statusText.Text = $"RSF generation failed:\n{ex.Message}";

                    if (DataContext is MainWindowViewModel vm)
                    {
                        vm.Debug_output = $"RSF generation failed: {ex.Message}";
                    }
                }
            };

            var buttons = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                Spacing = 8,
                Children =
                {
                    closeButton,
                    generateButton
                }
            };

            var form = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    new TextBlock
                    {
                        Text = "Create RSF file",
                        FontSize = 20,
                        FontWeight = Avalonia.Media.FontWeight.SemiBold,
                        Margin = new Thickness(0, 0, 0, 4)
                    },

                    new TextBlock
                    {
                        Text = "The generated .rsf file will be saved into USER_FILES.",
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                        Opacity = 0.75,
                        Margin = new Thickness(0, 0, 0, 8)
                    },

                    CreateField("Title", titleBox),
                    CreateField("Company Code", companyCodeBox),
                    CreateField("Product Code", productCodeBox),
                    CreateField("Title ID", titleIdBox),
                    CreateField("RomFS path", romFsBox),
                    CreateField("Save Data Size", saveDataSizeBox),

                    new3dsCpuBox,
                    buttons,
                    statusText
                }
            };

            dialog.Content = new Border
            {
                Padding = new Thickness(20),
                Child = new ScrollViewer
                {
                    Content = form
                }
            };

            titleBox.Focus();

            await dialog.ShowDialog(this);
        }

        private static Control CreateField(string label, Control input)
        {
            return new StackPanel
            {
                Spacing = 3,
                Children =
                {
                    new TextBlock
                    {
                        Text = label,
                        FontWeight = Avalonia.Media.FontWeight.SemiBold,
                        Opacity = 0.85
                    },
                    input
                }
            };
        }

        private static string? ValidateRsfConfig(
            string title,
            string companyCode,
            string productCode,
            string titleId,
            string romFsPath,
            string saveDataSize)
        {
            if (string.IsNullOrWhiteSpace(title))
                return "Title is required.";

            if (string.IsNullOrWhiteSpace(companyCode))
                return "Company Code is required.";

            if (companyCode.Length != 2)
                return "Company Code must contain exactly 2 characters.";

            if (string.IsNullOrWhiteSpace(productCode))
                return "Product Code is required.";

            if (string.IsNullOrWhiteSpace(titleId))
                return "Title ID is required.";

            if (titleId.Length < 5)
                return "Title ID is too short.";

            if (!titleId.All(Uri.IsHexDigit))
                return "Title ID must contain only hexadecimal characters.";

            if (string.IsNullOrWhiteSpace(romFsPath))
                return "RomFS path is required.";

            if (string.IsNullOrWhiteSpace(saveDataSize))
                return "Save Data Size is required.";

            return null;
        }

        private static string MakeSafeFileName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "output";

            char[] invalidChars = Path.GetInvalidFileNameChars();

            string safeName = string.Join("_", value.Split(invalidChars)).Trim();

            return string.IsNullOrWhiteSpace(safeName) ? "output" : safeName;
        }

        private static string EscapeRsfString(string value)
        {
            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"");
        }

        private static string BuildRsfContent(
            string title,
            string companyCode,
            string productCode,
            string titleId,
            string romFsPath,
            string saveDataSize,
            bool enableNew3dsCpu)
        {
            title = EscapeRsfString(title.Trim());
            companyCode = EscapeRsfString(companyCode.Trim());
            productCode = EscapeRsfString(productCode.Trim());
            romFsPath = EscapeRsfString(romFsPath.Trim());
            saveDataSize = saveDataSize.Trim();

            string normalizedTitleId = titleId.Trim();
            string uniqueId = "0x" + normalizedTitleId.Substring(Math.Max(0, normalizedTitleId.Length - 5));
            string cpuSpeed = enableNew3dsCpu ? "804MHz" : "268MHz";

            return $"""
                BasicInfo:
                  Title                    : "{title}"
                  CompanyCode              : "{companyCode}"
                  ProductCode              : "{productCode}"
                  ContentType              : Application
                  Logo                     : Homebrew

                RomFs:
                  RootPath                 : "{romFsPath}"

                TitleInfo:
                  UniqueId                 : {uniqueId}
                  Category                 : Application

                CardInfo:
                  MediaSize                : 128MB
                  MediaType                : Card1
                  CardDevice               : None

                Option:
                  UseOnSD                  : true
                  FreeProductCode          : true
                  MediaFootPadding         : false
                  EnableCrypt              : false
                  EnableCompress           : true

                SystemControlInfo:
                  SaveDataSize: {saveDataSize}
                  RemasterVersion: 0
                  StackSize: 0x40000

                # DO NOT EDIT BELOW HERE OR PROGRAMS WILL NOT LAUNCH (most likely)

                AccessControlInfo:
                  FileSystemAccess:
                   - Debug
                   - DirectSdmc
                   - DirectSdmcWrite

                  IdealProcessor                : 0
                  AffinityMask                  : 1
                  Priority                      : 16

                  MaxCpu                        : 0x9E
                  DisableDebug                  : false
                  EnableForceDebug              : false
                  CanWriteSharedPage            : false
                  CanUsePrivilegedPriority      : false
                  CanUseNonAlphabetAndNumber    : false
                  PermitMainFunctionArgument    : false
                  CanShareDeviceMemory          : false
                  RunnableOnSleep               : false
                  SpecialMemoryArrange          : false
                  CoreVersion                   : 2
                  DescVersion                   : 2

                  ReleaseKernelMajor            : "02"
                  ReleaseKernelMinor            : "33"
                  MemoryType                    : Application
                  HandleTableSize: 512

                  SystemModeExt                 : Legacy
                  CpuSpeed                      : {cpuSpeed}
                  EnableL2Cache                 : true
                  CanAccessCore2                : true

                  IORegisterMapping:
                   - 1ff50000-1ff57fff
                   - 1ff70000-1ff77fff
                  MemoryMapping:
                   - 1f000000-1f5fffff:r
                  SystemCallAccess:
                     ArbitrateAddress: 34
                     Break: 60
                     CancelTimer: 28
                     ClearEvent: 25
                     ClearTimer: 29
                     CloseHandle: 35
                     ConnectToPort: 45
                     ControlMemory: 1
                     CreateAddressArbiter: 33
                     CreateEvent: 23
                     CreateMemoryBlock: 30
                     CreateMutex: 19
                     CreateSemaphore: 21
                     CreateThread: 8
                     CreateTimer: 26
                     DuplicateHandle: 39
                     ExitProcess: 3
                     ExitThread: 9
                     GetCurrentProcessorNumber: 17
                     GetHandleInfo: 41
                     GetProcessId: 53
                     GetProcessIdOfThread: 54
                     GetProcessIdealProcessor: 6
                     GetProcessInfo: 43
                     GetResourceLimit: 56
                     GetResourceLimitCurrentValues: 58
                     GetResourceLimitLimitValues: 57
                     GetSystemInfo: 42
                     GetSystemTick: 40
                     GetThreadContext: 59
                     GetThreadId: 55
                     GetThreadIdealProcessor: 15
                     GetThreadInfo: 44
                     GetThreadPriority: 11
                     MapMemoryBlock: 31
                     OutputDebugString: 61
                     QueryMemory: 2
                     ReleaseMutex: 20
                     ReleaseSemaphore: 22
                     SendSyncRequest1: 46
                     SendSyncRequest2: 47
                     SendSyncRequest3: 48
                     SendSyncRequest4: 49
                     SendSyncRequest: 50
                     SetThreadPriority: 12
                     SetTimer: 27
                     SignalEvent: 24
                     SleepThread: 10
                     UnmapMemoryBlock: 32
                     WaitSynchronization1: 36
                     WaitSynchronizationN: 37
                  InterruptNumbers:
                  ServiceAccessControl:
                   - APT:U
                   - $hioFIO
                   - $hostio0
                   - $hostio1
                   - ac:u
                   - boss:U
                   - cam:u
                   - ir:rst
                   - cfg:u
                   - dlp:FKCL
                   - dlp:SRVR
                   - dsp::DSP
                   - frd:u
                   - fs:USER
                   - gsp::Gpu
                   - hid:USER
                   - http:C
                   - mic:u
                   - ndm:u
                   - news:s
                   - nwm::UDS
                   - ptm:u
                   - pxi:dev
                   - soc:U
                   - gsp::Lcd
                   - y2r:u
                   - ldr:ro
                   - ir:USER
                   - ir:u
                   - csnd:SND
                   - am:u
                   - ns:s

                SystemControlInfo:
                  Dependency:
                    ac: 0x0004013000002402L
                    am: 0x0004013000001502L
                    boss: 0x0004013000003402L
                    camera: 0x0004013000001602L
                    cecd: 0x0004013000002602L
                    cfg: 0x0004013000001702L
                    codec: 0x0004013000001802L
                    csnd: 0x0004013000002702L
                    dlp: 0x0004013000002802L
                    dsp: 0x0004013000001a02L
                    friends: 0x0004013000003202L
                    gpio: 0x0004013000001b02L
                    gsp: 0x0004013000001c02L
                    hid: 0x0004013000001d02L
                    http: 0x0004013000002902L
                    i2c: 0x0004013000001e02L
                    ir: 0x0004013000003302L
                    mcu: 0x0004013000001f02L
                    mic: 0x0004013000002002L
                    ndm: 0x0004013000002b02L
                    news: 0x0004013000003502L
                    nim: 0x0004013000002c02L
                    nwm: 0x0004013000002d02L
                    pdn: 0x0004013000002102L
                    ps: 0x0004013000003102L
                    ptm: 0x0004013000002202L
                    ro: 0x0004013000003702L
                    socket: 0x0004013000002e02L
                    spi: 0x0004013000002302L
                    ssl: 0x0004013000002f02L
                """;
        }

        public async void OnSMDHCREATOR(object? sender, RoutedEventArgs e)
        {
            var currentSmdh = new SmdhFile();
            int selectedLanguageIndex = 1;

            var dialog = new Window
            {
                Width = 560,
                MaxHeight = 760,
                SizeToContent = SizeToContent.Height,
                Title = "SMDH Creator",
                CanResize = false,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var languageBox = new ComboBox
            {
                ItemsSource = SmdhLanguages,
                SelectedIndex = selectedLanguageIndex,
                MinWidth = 240
            };

            var titleBox = new TextBox
            {
                PlaceholderText = "Title / Short description"
            };

            var descriptionBox = new TextBox
            {
                PlaceholderText = "Description",
                AcceptsReturn = true,
                Height = 90,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap
            };

            var publisherBox = new TextBox
            {
                PlaceholderText = "Publisher"
            };

            var smallPreview = new Image
            {
                Width = 48,
                Height = 48,
                Stretch = Avalonia.Media.Stretch.Uniform
            };

            var bigPreview = new Image
            {
                Width = 96,
                Height = 96,
                Stretch = Avalonia.Media.Stretch.Uniform
            };

            var statusText = new TextBlock
            {
                Text = "Ready.",
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                Opacity = 0.85,
                Margin = new Thickness(0, 8, 0, 0)
            };

            void ApplyTextsToLanguage(int languageIndex)
            {
                currentSmdh.SetShortDescription(languageIndex, titleBox.Text ?? "");
                currentSmdh.SetLongDescription(languageIndex, descriptionBox.Text ?? "");
                currentSmdh.SetPublisher(languageIndex, publisherBox.Text ?? "");
            }

            void LoadTextsFromLanguage(int languageIndex)
            {
                titleBox.Text = currentSmdh.GetShortDescription(languageIndex);
                descriptionBox.Text = currentSmdh.GetLongDescription(languageIndex);
                publisherBox.Text = currentSmdh.GetPublisher(languageIndex);
            }

            void RefreshIconPreviews()
            {
                smallPreview.Source = currentSmdh.CreateSmallIconBitmap();
                bigPreview.Source = currentSmdh.CreateBigIconBitmap();
            }

            RefreshIconPreviews();
            LoadTextsFromLanguage(selectedLanguageIndex);

            languageBox.SelectionChanged += (_, _) =>
            {
                int newIndex = languageBox.SelectedIndex;

                if (newIndex < 0 || newIndex > 15 || newIndex == selectedLanguageIndex)
                    return;

                ApplyTextsToLanguage(selectedLanguageIndex);

                selectedLanguageIndex = newIndex;

                LoadTextsFromLanguage(selectedLanguageIndex);
            };

            var newButton = new Button
            {
                Content = "New",
                MinWidth = 90
            };

            var openButton = new Button
            {
                Content = "Open SMDH",
                MinWidth = 110
            };

            var loadIconButton = new Button
            {
                Content = "Load Icon",
                MinWidth = 110
            };

            var saveToUserFilesButton = new Button
            {
                Content = "Save to USER_FILES",
                MinWidth = 150
            };

            var saveAsButton = new Button
            {
                Content = "Save As",
                MinWidth = 100
            };

            var closeButton = new Button
            {
                Content = "Close",
                MinWidth = 90
            };

            newButton.Click += (_, _) =>
            {
                currentSmdh = new SmdhFile();

                selectedLanguageIndex = 1;
                languageBox.SelectedIndex = selectedLanguageIndex;

                LoadTextsFromLanguage(selectedLanguageIndex);
                RefreshIconPreviews();

                statusText.Text = "New SMDH.";
            };

            openButton.Click += async (_, _) =>
            {
                if (!StorageProvider.CanOpen)
                {
                    statusText.Text = "Error: file picker not available.";
                    return;
                }

                var files = await StorageProvider.OpenFilePickerAsync(
                    new FilePickerOpenOptions
                    {
                        Title = "Open SMDH file",
                        AllowMultiple = false,
                        FileTypeFilter = new[]
                        {
                            new FilePickerFileType("SMDH files")
                            {
                                Patterns = new[] { "*.smdh" }
                            },
                            FilePickerFileTypes.All
                        }
                    });

                var file = files.FirstOrDefault();

                if (file is null)
                {
                    statusText.Text = "Open cancelled.";
                    return;
                }

                try
                {
                    await using Stream stream = await file.OpenReadAsync();

                    var smdh = new SmdhFile();
                    smdh.Load(stream);

                    if (!smdh.Valid)
                    {
                        statusText.Text = "Invalid file: SMDH signature missing.";
                        return;
                    }

                    currentSmdh = smdh;

                    LoadTextsFromLanguage(selectedLanguageIndex);
                    RefreshIconPreviews();

                    statusText.Text = "SMDH loaded.";
                }
                catch (Exception ex)
                {
                    statusText.Text = $"Error opening SMDH:\n{ex.Message}";
                }
            };

            loadIconButton.Click += async (_, _) =>
            {
                if (!StorageProvider.CanOpen)
                {
                    statusText.Text = "Error: file picker not available.";
                    return;
                }

                var files = await StorageProvider.OpenFilePickerAsync(
                    new FilePickerOpenOptions
                    {
                        Title = "Load icon image",
                        AllowMultiple = false,
                        FileTypeFilter = new[]
                        {
                            new FilePickerFileType("Images")
                            {
                                Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.bmp" },
                                MimeTypes = new[] { "image/png", "image/jpeg", "image/bmp" }
                            },
                            FilePickerFileTypes.All
                        }
                    });

                var file = files.FirstOrDefault();

                if (file is null)
                {
                    statusText.Text = "Icon loading cancelled.";
                    return;
                }

                try
                {
                    await using Stream stream = await file.OpenReadAsync();
                    using var bitmap = new Bitmap(stream);

                    currentSmdh.SetIconsFromBitmap(bitmap);
                    RefreshIconPreviews();

                    statusText.Text = "Icon loaded.";
                }
                catch (Exception ex)
                {
                    statusText.Text = $"Error loading icon:\n{ex.Message}";
                }
            };

            saveToUserFilesButton.Click += async (_, _) =>
            {
                try
                {
                    ApplyTextsToLanguage(selectedLanguageIndex);

                    string rootPath = FindRootPath();
                    string userFilesPath = Path.Combine(rootPath, "USER_FILES");

                    Directory.CreateDirectory(userFilesPath);

                    string safeName = MakeSmdhSafeFileName(titleBox.Text);
                    string smdhPath = Path.Combine(userFilesPath, $"{safeName}.smdh");

                    if (File.Exists(smdhPath))
                    {
                        bool overwrite = await ShowSmdhConfirmDialogAsync(
                            "File already exists",
                            $"The file already exists:\n\n{safeName}.smdh\n\nDo you want to overwrite it?"
                        );

                        if (!overwrite)
                        {
                            statusText.Text = "Save cancelled.";
                            return;
                        }
                    }

                    await using FileStream stream = File.Create(smdhPath);
                    currentSmdh.Save(stream);

                    statusText.Text = $"SMDH saved:\n{smdhPath}";

                    if (DataContext is MainWindowViewModel vm)
                    {
                        vm.Debug_output = $"SMDH saved: {Path.GetFileName(smdhPath)}";
                    }
                }
                catch (Exception ex)
                {
                    statusText.Text = $"Error saving SMDH:\n{ex.Message}";

                    if (DataContext is MainWindowViewModel vm)
                    {
                        vm.Debug_output = $"SMDH save failed: {ex.Message}";
                    }
                }
            };

            saveAsButton.Click += async (_, _) =>
            {
                if (!StorageProvider.CanSave)
                {
                    statusText.Text = "Error: save picker not available.";
                    return;
                }

                ApplyTextsToLanguage(selectedLanguageIndex);

                var file = await StorageProvider.SaveFilePickerAsync(
                    new FilePickerSaveOptions
                    {
                        Title = "Save SMDH file",
                        SuggestedFileName = $"{MakeSmdhSafeFileName(titleBox.Text)}.smdh",
                        DefaultExtension = "smdh",
                        ShowOverwritePrompt = true,
                        FileTypeChoices = new[]
                        {
                            new FilePickerFileType("SMDH files")
                            {
                                Patterns = new[] { "*.smdh" }
                            }
                        }
                    });

                if (file is null)
                {
                    statusText.Text = "Save cancelled.";
                    return;
                }

                try
                {
                    await using Stream stream = await file.OpenWriteAsync();
                    currentSmdh.Save(stream);

                    statusText.Text = "SMDH saved.";
                }
                catch (Exception ex)
                {
                    statusText.Text = $"Error saving SMDH:\n{ex.Message}";
                }
            };

            closeButton.Click += (_, _) =>
            {
                dialog.Close();
            };

            var topButtons = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                Spacing = 8,
                Children =
                {
                    newButton,
                    openButton,
                    loadIconButton
                }
            };

            var bottomButtons = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                Spacing = 8,
                Children =
                {
                    closeButton,
                    saveAsButton,
                    saveToUserFilesButton
                }
            };

            var previewPanel = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                Spacing = 18,
                Margin = new Thickness(0, 4, 0, 8),
                Children =
                {
                    new StackPanel
                    {
                        Spacing = 4,
                        Children =
                        {
                            new TextBlock
                            {
                                Text = "Small icon 24x24",
                                Opacity = 0.75
                            },
                            new Border
                            {
                                Width = 56,
                                Height = 56,
                                Padding = new Thickness(4),
                                Child = smallPreview
                            }
                        }
                    },

                    new StackPanel
                    {
                        Spacing = 4,
                        Children =
                        {
                            new TextBlock
                            {
                                Text = "Big icon 48x48",
                                Opacity = 0.75
                            },
                            new Border
                            {
                                Width = 104,
                                Height = 104,
                                Padding = new Thickness(4),
                                Child = bigPreview
                            }
                        }
                    }
                }
            };

            var content = new StackPanel
            {
                Spacing = 10,
                Children =
                {
                    new TextBlock
                    {
                        Text = "Create or edit SMDH file",
                        FontSize = 20,
                        FontWeight = Avalonia.Media.FontWeight.SemiBold
                    },

                    new TextBlock
                    {
                        Text = "Edit title metadata, load an icon image, then save the .smdh file into USER_FILES or another location.",
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                        Opacity = 0.75,
                        Margin = new Thickness(0, 0, 0, 6)
                    },

                    topButtons,

                    CreateSmdhField("Language", languageBox),
                    CreateSmdhField("Title", titleBox),
                    CreateSmdhField("Description", descriptionBox),
                    CreateSmdhField("Publisher", publisherBox),

                    previewPanel,
                    bottomButtons,
                    statusText
                }
            };

            dialog.Content = new Border
            {
                Padding = new Thickness(20),
                Child = new ScrollViewer
                {
                    Content = content
                }
            };

            await dialog.ShowDialog(this);
        }

        private static readonly string[] SmdhLanguages =
        {
            "Japanese",
            "English",
            "French",
            "German",
            "Italian",
            "Spanish",
            "Simplified Chinese",
            "Korean",
            "Dutch",
            "Portuguese",
            "Russian",
            "Traditional Chinese",
            "Language 12",
            "Language 13",
            "Language 14",
            "Language 15"
        };

        private static Control CreateSmdhField(string label, Control input)
        {
            return new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    new TextBlock
                    {
                        Text = label,
                        FontWeight = Avalonia.Media.FontWeight.SemiBold,
                        Opacity = 0.85
                    },
                    input
                }
            };
        }

        private static string MakeSmdhSafeFileName(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "icon";

            char[] invalidChars = Path.GetInvalidFileNameChars();
            string safeName = string.Join("_", value.Split(invalidChars)).Trim();

            return string.IsNullOrWhiteSpace(safeName) ? "icon" : safeName;
        }

        private async Task<bool> ShowSmdhConfirmDialogAsync(string title, string message)
        {
            var dialog = new Window
            {
                Width = 380,
                SizeToContent = SizeToContent.Height,
                Title = title,
                CanResize = false,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var yesButton = new Button
            {
                Content = "Yes",
                MinWidth = 90
            };

            var noButton = new Button
            {
                Content = "No",
                MinWidth = 90
            };

            yesButton.Click += (_, _) =>
            {
                dialog.Close(true);
            };

            noButton.Click += (_, _) =>
            {
                dialog.Close(false);
            };

            dialog.Content = new Border
            {
                Padding = new Thickness(20),
                Child = new StackPanel
                {
                    Spacing = 14,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = message,
                            TextWrapping = Avalonia.Media.TextWrapping.Wrap
                        },

                        new StackPanel
                        {
                            Orientation = Avalonia.Layout.Orientation.Horizontal,
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                            Spacing = 8,
                            Children =
                            {
                                noButton,
                                yesButton
                            }
                        }
                    }
                }
            };

            bool? result = await dialog.ShowDialog<bool?>(this);

            return result == true;
        }

        private sealed class SmdhFile
        {
            private const uint MagicSmdh = 0x48444D53;

            private readonly SmdhHeader header = new();
            private readonly SmdhTitle[] titles = new SmdhTitle[16];
            private readonly SmdhSettings settings = new();
            private readonly byte[] reserved = new byte[0x08];

            private readonly ushort[] smallIconData = new ushort[24 * 24];
            private readonly ushort[] bigIconData = new ushort[48 * 48];

            private Rgb24[] smallIconPixels = CreateSolidPixels(24, 24, new Rgb24(40, 40, 40));
            private Rgb24[] bigIconPixels = CreateSolidPixels(48, 48, new Rgb24(40, 40, 40));

            private readonly byte[] tileOrder =
            {
                0, 1, 8, 9, 2, 3, 10, 11,
                16, 17, 24, 25, 18, 19, 26, 27,
                4, 5, 12, 13, 6, 7, 14, 15,
                20, 21, 28, 29, 22, 23, 30, 31,
                32, 33, 40, 41, 34, 35, 42, 43,
                48, 49, 56, 57, 50, 51, 58, 59,
                36, 37, 44, 45, 38, 39, 46, 47,
                52, 53, 60, 61, 54, 55, 62, 63
            };

            public SmdhFile()
            {
                for (int i = 0; i < titles.Length; i++)
                {
                    titles[i] = new SmdhTitle();
                }

                header.Magic = MagicSmdh;
                header.Version = 0;
                header.Reserved = 0;

                EncodeIcon(smallIconPixels, smallIconData, 24);
                EncodeIcon(bigIconPixels, bigIconData, 48);
            }

            public bool Valid => header.Magic == MagicSmdh;

            public string GetShortDescription(int index)
            {
                return DecodeText(titles[index].ShortDescription);
            }

            public void SetShortDescription(int index, string value)
            {
                EncodeText(value, titles[index].ShortDescription);
            }

            public string GetLongDescription(int index)
            {
                return DecodeText(titles[index].LongDescription);
            }

            public void SetLongDescription(int index, string value)
            {
                EncodeText(value, titles[index].LongDescription);
            }

            public string GetPublisher(int index)
            {
                return DecodeText(titles[index].Publisher);
            }

            public void SetPublisher(int index, string value)
            {
                EncodeText(value, titles[index].Publisher);
            }

            public void SetIconsFromBitmap(Bitmap source)
            {
                bigIconPixels = AvaloniaBitmapTools.ExtractRgb24(source, 48, 48);
                smallIconPixels = AvaloniaBitmapTools.ExtractRgb24(source, 24, 24);

                EncodeIcon(smallIconPixels, smallIconData, 24);
                EncodeIcon(bigIconPixels, bigIconData, 48);
            }

            public Bitmap CreateSmallIconBitmap()
            {
                return AvaloniaBitmapTools.CreateBitmapFromRgb24(smallIconPixels, 24, 24);
            }

            public Bitmap CreateBigIconBitmap()
            {
                return AvaloniaBitmapTools.CreateBitmapFromRgb24(bigIconPixels, 48, 48);
            }

            public void Load(Stream stream)
            {
                using var reader = new BinaryReader(stream);

                header.Magic = reader.ReadUInt32();
                header.Version = reader.ReadUInt16();
                header.Reserved = reader.ReadUInt16();

                if (!Valid)
                {
                    return;
                }

                for (int i = 0; i < titles.Length; i++)
                {
                    ReadU16Array(reader, titles[i].ShortDescription);
                    ReadU16Array(reader, titles[i].LongDescription);
                    ReadU16Array(reader, titles[i].Publisher);
                }

                ReadBytes(reader, settings.GameRatings);
                settings.RegionLock = reader.ReadUInt32();
                ReadBytes(reader, settings.MatchMakerId);
                settings.Flags = reader.ReadUInt32();
                settings.EulaVersion = reader.ReadUInt16();
                settings.Reserved = reader.ReadUInt16();
                settings.DefaultFrame = reader.ReadUInt32();
                settings.CecId = reader.ReadUInt32();

                ReadBytes(reader, reserved);

                ReadU16Array(reader, smallIconData);
                ReadU16Array(reader, bigIconData);

                smallIconPixels = DecodeIcon(smallIconData, 24);
                bigIconPixels = DecodeIcon(bigIconData, 48);
            }

            public void Save(Stream stream)
            {
                EncodeIcon(smallIconPixels, smallIconData, 24);
                EncodeIcon(bigIconPixels, bigIconData, 48);

                using var writer = new BinaryWriter(stream);

                header.Magic = MagicSmdh;

                writer.Write(header.Magic);
                writer.Write(header.Version);
                writer.Write(header.Reserved);

                for (int i = 0; i < titles.Length; i++)
                {
                    WriteU16Array(writer, titles[i].ShortDescription);
                    WriteU16Array(writer, titles[i].LongDescription);
                    WriteU16Array(writer, titles[i].Publisher);
                }

                writer.Write(settings.GameRatings);
                writer.Write(settings.RegionLock);
                writer.Write(settings.MatchMakerId);
                writer.Write(settings.Flags);
                writer.Write(settings.EulaVersion);
                writer.Write(settings.Reserved);
                writer.Write(settings.DefaultFrame);
                writer.Write(settings.CecId);

                writer.Write(reserved);

                WriteU16Array(writer, smallIconData);
                WriteU16Array(writer, bigIconData);
            }

            private Rgb24[] DecodeIcon(ushort[] source, int size)
            {
                var destination = new Rgb24[size * size];

                int i = 0;

                for (int tileY = 0; tileY < size; tileY += 8)
                {
                    for (int tileX = 0; tileX < size; tileX += 8)
                    {
                        for (int k = 0; k < 64; k++)
                        {
                            int x = tileOrder[k] & 0x07;
                            int y = tileOrder[k] >> 3;

                            destination[(tileY + y) * size + tileX + x] = DecodeRgb565(source[i]);
                            i++;
                        }
                    }
                }

                return destination;
            }

            private void EncodeIcon(Rgb24[] source, ushort[] destination, int size)
            {
                int i = 0;

                for (int tileY = 0; tileY < size; tileY += 8)
                {
                    for (int tileX = 0; tileX < size; tileX += 8)
                    {
                        for (int k = 0; k < 64; k++)
                        {
                            int x = tileOrder[k] & 0x07;
                            int y = tileOrder[k] >> 3;

                            destination[i] = EncodeRgb565(source[(tileY + y) * size + tileX + x]);
                            i++;
                        }
                    }
                }
            }

            private static Rgb24 DecodeRgb565(ushort color)
            {
                int r5 = (color >> 11) & 0x1F;
                int g6 = (color >> 5) & 0x3F;
                int b5 = color & 0x1F;

                byte r = (byte)((r5 << 3) | (r5 >> 2));
                byte g = (byte)((g6 << 2) | (g6 >> 4));
                byte b = (byte)((b5 << 3) | (b5 >> 2));

                return new Rgb24(r, g, b);
            }

            private static ushort EncodeRgb565(Rgb24 pixel)
            {
                int r = pixel.R >> 3;
                int g = pixel.G >> 2;
                int b = pixel.B >> 3;

                return (ushort)((r << 11) | (g << 5) | b);
            }

            private static string DecodeText(ushort[] source)
            {
                int length = 0;

                while (length < source.Length && source[length] != 0)
                {
                    length++;
                }

                char[] chars = new char[length];

                for (int i = 0; i < length; i++)
                {
                    chars[i] = (char)source[i];
                }

                return new string(chars);
            }

            private static void EncodeText(string text, ushort[] destination)
            {
                Array.Clear(destination, 0, destination.Length);

                int length = Math.Min(text.Length, destination.Length);

                for (int i = 0; i < length; i++)
                {
                    destination[i] = text[i];
                }
            }

            private static void ReadBytes(BinaryReader reader, byte[] destination)
            {
                int read = reader.Read(destination, 0, destination.Length);

                if (read != destination.Length)
                {
                    throw new EndOfStreamException("Incomplete SMDH file");
                }
            }

            private static void ReadU16Array(BinaryReader reader, ushort[] destination)
            {
                for (int i = 0; i < destination.Length; i++)
                {
                    destination[i] = reader.ReadUInt16();
                }
            }

            private static void WriteU16Array(BinaryWriter writer, ushort[] source)
            {
                for (int i = 0; i < source.Length; i++)
                {
                    writer.Write(source[i]);
                }
            }

            private static Rgb24[] CreateSolidPixels(int width, int height, Rgb24 color)
            {
                var pixels = new Rgb24[width * height];

                for (int i = 0; i < pixels.Length; i++)
                {
                    pixels[i] = color;
                }

                return pixels;
            }

            private sealed class SmdhHeader
            {
                public uint Magic;
                public ushort Version;
                public ushort Reserved;
            }

            private sealed class SmdhTitle
            {
                public ushort[] ShortDescription = new ushort[0x40];
                public ushort[] LongDescription = new ushort[0x80];
                public ushort[] Publisher = new ushort[0x40];
            }

            private sealed class SmdhSettings
            {
                public byte[] GameRatings = new byte[0x10];
                public uint RegionLock;
                public byte[] MatchMakerId = new byte[0x0C];
                public uint Flags;
                public ushort EulaVersion;
                public ushort Reserved;
                public uint DefaultFrame;
                public uint CecId;
            }
        }

        private readonly struct Rgb24
        {
            public readonly byte R;
            public readonly byte G;
            public readonly byte B;

            public Rgb24(byte r, byte g, byte b)
            {
                R = r;
                G = g;
                B = b;
            }
        }

        private static class AvaloniaBitmapTools
        {
            public static Rgb24[] ExtractRgb24(Bitmap source, int width, int height)
            {
                using Bitmap scaled = source.CreateScaledBitmap(
                    new PixelSize(width, height),
                    BitmapInterpolationMode.HighQuality);

                int stride = width * 4;
                byte[] bgra = new byte[stride * height];

                GCHandle handle = GCHandle.Alloc(bgra, GCHandleType.Pinned);

                try
                {
                    scaled.CopyPixels(
                        new PixelRect(0, 0, width, height),
                        handle.AddrOfPinnedObject(),
                        bgra.Length,
                        stride);
                }
                finally
                {
                    handle.Free();
                }

                return BgraToRgb24(bgra, stride, width, height);
            }

            public static Bitmap CreateBitmapFromRgb24(Rgb24[] pixels, int width, int height)
            {
                int stride = width * 4;
                byte[] bgra = new byte[stride * height];

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        Rgb24 pixel = pixels[y * width + x];
                        int offset = y * stride + x * 4;

                        bgra[offset + 0] = pixel.B;
                        bgra[offset + 1] = pixel.G;
                        bgra[offset + 2] = pixel.R;
                        bgra[offset + 3] = 255;
                    }
                }

                var bitmap = new WriteableBitmap(
                    new PixelSize(width, height),
                    new Vector(96, 96),
                    PixelFormat.Bgra8888,
                    AlphaFormat.Premul);

                using (ILockedFramebuffer framebuffer = bitmap.Lock())
                {
                    Marshal.Copy(bgra, 0, framebuffer.Address, bgra.Length);
                }

                return bitmap;
            }

            private static Rgb24[] BgraToRgb24(byte[] bgra, int stride, int width, int height)
            {
                var pixels = new Rgb24[width * height];

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int offset = y * stride + x * 4;

                        byte b = bgra[offset + 0];
                        byte g = bgra[offset + 1];
                        byte r = bgra[offset + 2];
                        byte a = bgra[offset + 3];

                        if (a > 0 && a < 255)
                        {
                            r = Unpremultiply(r, a);
                            g = Unpremultiply(g, a);
                            b = Unpremultiply(b, a);
                        }

                        pixels[y * width + x] = new Rgb24(r, g, b);
                    }
                }

                return pixels;
            }

            private static byte Unpremultiply(byte value, byte alpha)
            {
                int result = value * 255 / alpha;
                return (byte)Math.Clamp(result, 0, 255);
            }
        }

        private void StartBundledTool(string folderName, string executableName)
        {
            string rootPath = FindRootPath();
            string toolFolder = Path.Combine(rootPath, folderName);

            string executablePath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? Path.Combine(toolFolder, $"{executableName}.exe")
                : Path.Combine(toolFolder, executableName);

            try
            {
                if (!Directory.Exists(toolFolder))
                {
                    Debug.WriteLine($"Tool folder not found: {toolFolder}");
                    return;
                }

                if (!File.Exists(executablePath))
                {
                    Debug.WriteLine($"Tool executable not found: {executablePath}");
                    return;
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    RemoveWindowsZoneIdentifier(executablePath);

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = executablePath,
                        WorkingDirectory = toolFolder,
                        UseShellExecute = true
                    });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    MakeExecutable(executablePath);

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = executablePath,
                        WorkingDirectory = toolFolder,
                        UseShellExecute = false
                    });
                }
                else
                {
                    Debug.WriteLine("Unsupported OS.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to start {folderName}: {ex.Message}");
            }
        }

        private static void RemoveWindowsZoneIdentifier(string filePath)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return;

            if (!File.Exists(filePath))
                return;

            try
            {
                File.Delete($"{filePath}:Zone.Identifier");
            }
            catch (FileNotFoundException)
            {
            }
            catch (DirectoryNotFoundException)
            {
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Zone.Identifier remove failed for {filePath}: {ex.Message}");
            }
        }

        private static void MakeExecutable(string path)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && !RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return;

            if (!File.Exists(path))
                return;

            try
            {
                var chmodInfo = new ProcessStartInfo
                {
                    FileName = "chmod",
                    UseShellExecute = false
                };

                chmodInfo.ArgumentList.Add("+x");
                chmodInfo.ArgumentList.Add(path);

                using Process? chmodProcess = Process.Start(chmodInfo);
                chmodProcess?.WaitForExit();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"chmod failed for {path}: {ex.Message}");
            }
        }

        private async Task CheckUpdateAsync()
        {
            await RunUpdateCheckLogicAsync(this);
        }

        private async Task RunUpdateCheckLogicAsync(Window ownerWindow)
        {
            var client = new GitHubClient(new ProductHeaderValue("CIAToolsR"));

            try
            {
                var latestRelease = await client.Repository.Release.GetLatest("saiitanaa", "CIATools");

                if (latestRelease == null)
                    return;

                string latestVersionText = latestRelease.TagName?.Replace("v", "").Trim() ?? "0.0.0";

                if (!Version.TryParse(CurrentVersion, out var currentVersion))
                    return;

                if (!Version.TryParse(latestVersionText, out var latestVersion))
                    return;

                if (latestVersion <= currentVersion)
                    return;

                var dialog = new Window
                {
                    Width = 340,
                    SizeToContent = SizeToContent.Height,
                    Title = "CIATools Updater",
                    CanResize = false,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                var updateButton = new Button
                {
                    Content = "Update",
                    MinWidth = 100
                };

                var cancelButton = new Button
                {
                    Content = "Later",
                    MinWidth = 100
                };

                cancelButton.Click += (_, _) =>
                {
                    dialog.Close();
                };

                updateButton.Click += (_, _) =>
                {
                    OpenBrowser(latestRelease.HtmlUrl);
                    dialog.Close();
                };

                dialog.Content = new Border
                {
                    Padding = new Thickness(20),
                    Child = new StackPanel
                    {
                        Spacing = 12,
                        Children =
                        {
                            new TextBlock
                            {
                                Text = "Update available",
                                FontSize = 18,
                                FontWeight = Avalonia.Media.FontWeight.SemiBold
                            },

                            new TextBlock
                            {
                                Text = $"New version available: {latestRelease.TagName}",
                                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                                Opacity = 0.8
                            },

                            new StackPanel
                            {
                                Orientation = Avalonia.Layout.Orientation.Horizontal,
                                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                                Spacing = 8,
                                Children =
                                {
                                    cancelButton,
                                    updateButton
                                }
                            }
                        }
                    }
                };

                await dialog.ShowDialog(ownerWindow);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Check update failed: " + ex.Message);
            }
        }

        private async Task<bool> ShowConfirmDialogAsync(string title, string message)
        {
            var dialog = new Window
            {
                Width = 380,
                SizeToContent = SizeToContent.Height,
                Title = title,
                CanResize = false,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var yesButton = new Button
            {
                Content = "Yes",
                MinWidth = 90
            };

            var noButton = new Button
            {
                Content = "No",
                MinWidth = 90
            };

            yesButton.Click += (_, _) =>
            {
                dialog.Close(true);
            };

            noButton.Click += (_, _) =>
            {
                dialog.Close(false);
            };

            dialog.Content = new Border
            {
                Padding = new Thickness(20),
                Child = new StackPanel
                {
                    Spacing = 14,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = message,
                            TextWrapping = Avalonia.Media.TextWrapping.Wrap
                        },

                        new StackPanel
                        {
                            Orientation = Avalonia.Layout.Orientation.Horizontal,
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                            Spacing = 8,
                            Children =
                            {
                                noButton,
                                yesButton
                            }
                        }
                    }
                }
            };

            bool? result = await dialog.ShowDialog<bool?>(this);

            return result == true;
        }

        private void OpenBrowser(string url)
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Process.Start(new ProcessStartInfo(url)
                    {
                        UseShellExecute = true
                    });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Open browser failed: {ex.Message}");
            }
        }

        public void OnOpenGitHubClick(object? sender, RoutedEventArgs e)
        {
            OpenBrowser("https://github.com/saiitanaa/CIATools");
        }

        private void OnOpenGitHubPointerPressed(object? sender, PointerPressedEventArgs e)
{
            OpenBrowser("https://github.com/saiitanaa/CIATools");
        }
    }
}