using AdonisUI.Controls;
using FluentFTP.Helpers;
using Microsoft.Win32;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Media.Imaging;
using MessageBox = AdonisUI.Controls.MessageBox;
using MessageBoxButton = AdonisUI.Controls.MessageBoxButton;
using MessageBoxImage = AdonisUI.Controls.MessageBoxImage;

namespace HLLMapCapture;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : AdonisWindow
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(MainWindow));
    private bool isListening = false;
    public string SelectedFolderPath { get; private set; } = "";
    private Settings settings;

    public MainWindow()
    {
        InitializeComponent();

        var iconDir = Directory.CreateDirectory(Directory.GetCurrentDirectory() + @"\Icon");
        var files = Directory.GetFiles(iconDir.FullName, "*.ico");
        if (files.Length > 0)
            Icon = new BitmapImage(new Uri(files[0], UriKind.Absolute));

        settings = new Settings();
        try
        {
            settings.LoadSettings();
            ShowStateMessage("Settings loaded.");
        }
        catch (Exception ex)
        {
            log.Warn($"Error loading settings: {ex.Message}");
            ShowStateMessage("No settings.xml found. Generated new settings file.");
        }

        // Set default path if nothing is configured
        if (settings.LocalFolderPath != "")
        {
            ui_folderPathTextBox.Text = settings.LocalFolderPath;
        }
        else
        {
            Directory.CreateDirectory(Directory.GetCurrentDirectory() + @"\Screenshots");
            settings.LocalFolderPath = Directory.GetCurrentDirectory() + @"\Screenshots";
            ui_folderPathTextBox.Text = settings.LocalFolderPath;
        }

        if (settings.Username != "")
        {
            ui_username.Text = settings.Username;
        }
        else
        {
            settings.Username = "user";
            ui_username.Text = settings.Username;
        }

        settings.SaveSettings();

        // Automatically obfuscate FTP settings if there are any
        if (!settings.FtpPassword.IsBlank() && !settings.IsObfuscated)
        {
            settings.IsObfuscated = true;
            settings.SaveSettings();
            log.Info("FTP settings are now obfuscated.");
        }
    }

    private void SelectFolderButton_Click(object sender, RoutedEventArgs e)
    {
        OpenFolderDialog dialog = new OpenFolderDialog()
        {
            InitialDirectory = Directory.GetCurrentDirectory()
        };
        if (dialog.ShowDialog() == true)
        {
            SelectedFolderPath = dialog.FolderName;
            ui_folderPathTextBox.Text = SelectedFolderPath;
            settings.LocalFolderPath = SelectedFolderPath;
        }
    }

    /// <summary>
    /// Creates a screenshot, starts detection and cut out of a 1x zoomed map.
    /// When a cut out was made it is saved to disk and a FTP upload is started.
    /// </summary>
    private void StartMapCapture()
    {
        Stopwatch sw = new Stopwatch();
        sw.Start();

        Bitmap? mapCutOut = null;
        using (Bitmap screenshot = ScreenCapture.CreateScreenshot())
        {
            mapCutOut = ScreenCapture.CutOutMapZoomless(screenshot);
        }

        if (mapCutOut == null)
        {
            if (ui_stateMessage.Text.Contains("Screenshot uploaded to FTP-Server"))
            {
                ShowStateMessage("Map screen closed. Screenshot upload successful.");
                log.Info("No map detected in the screenshot. Probably because map was just closed.");
                return;
            }
            ShowStateMessage("No map detected in the screenshot.");
            log.Info("No map detected in the screenshot");
            return;
        }

        // TODO: This all runs in the UI thread, so only the the last ShowStateMessage this method runs is shown.
        // ShowStateMessage("Screenshot captured and map detected.");
        string filename = "Screenshot_" + ui_username.Text + "_" + DateTime.Now.ToString("yyyyMMddHHmmss");

        string filePath = Path.Combine(settings.LocalFolderPath, filename);

        try
        {
            if (!settings.CompressImages)
                ImageSaving.SaveAsPNG(mapCutOut, filePath);
            else
                ImageSaving.SaveAsJPEG(mapCutOut, filePath);
        }
        catch (Exception ex)
        {
            ShowStateMessage("Screenshot couldn't be saved to disk.");
            log.Error($"Screenshot couldn't be saved to disk. Error: {ex.Message}");
            return;
        }

        bool success;
        try
        {
            if (!settings.CompressImages)
                success = Ftp.UploadPNG(filePath, settings);
            else
                success = Ftp.UploadJPEG(filePath, settings);
        }
        catch (Exception ex)
        {
            ShowStateMessage("Screenshot was saved, but FTP upload failed.");
            log.Error($"Screenshot was saved, but FTP upload failed: {ex.Message}");
            return;
        }
        if (success)
        {
            ShowStateMessage("Screenshot uploaded to FTP-Server.");
            log.Info("Screenshot uploaded to FTP-Server");
            sw.Stop();
            log.Debug($"Screenshot map detecting and uploading process took: {sw.ElapsedMilliseconds}ms");
        }
        else
        {
            ShowStateMessage("Screenshot was saved, but FTP upload failed.");
            log.Error("Screenshot was saved, but FTP upload failed.");
        }
    }

    /// <summary>
    /// Updates the state message in the UI and adds the time.
    /// </summary>
    /// <param name="message">Message to be displayed</param>
    private void ShowStateMessage(string message)
    {
        ui_stateMessage.Text = $"State: {message} {DateTime.Now.ToString("HH:mm:ss")}";
    }

    private void StartButton_Click(object sender, RoutedEventArgs e)
    {
        if (!isListening && !HotKey.isHotkeyRegisterd)
        {
            HotKey.RegisterWindow(this);
            HotKey.action = new Action(() => StartMapCapture());
            isListening = true;
            ui_startButton.Visibility = Visibility.Hidden;
            ui_stopButton.Visibility = Visibility.Visible;
            ShowStateMessage("Map capture active.");
            log.Debug("Listening for Hotkey now.");
        }
        else
        {
            HotKey.Unregister();
            HotKey.action = null;
            isListening = false;
            ui_startButton.Visibility = Visibility.Visible;
            ui_stopButton.Visibility = Visibility.Hidden;
            ShowStateMessage("Map capture stopped.");
            log.Debug("Stopped listening for Hotkey.");
        }
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        settings.Username = ui_username.Text;
        settings.SaveSettings();

        // Automatically obfuscate FTP settings if there are any
        if (!settings.FtpPassword.IsBlank() && !settings.IsObfuscated)
        {
            settings.IsObfuscated = true;
            settings.SaveSettings();
            log.Info("FTP settings are now obfuscated.");
        }
    }

    private void InfoButton_Click(object sender, RoutedEventArgs e)
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        MessageBox.Show($"HLLMapCapture Version: {version} \n" +
            "MIT License \nCopyright (c) 2024 Jakob Feldmann \n",
            "Info", MessageBoxButton.OK, MessageBoxImage.None);
    }

}