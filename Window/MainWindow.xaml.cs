using AdonisUI.Controls;
using FluentFTP.Helpers;
using Microsoft.Win32;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Windows.System;
using Input = System.Windows.Input;
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
    private string currentStateMessage = "";
    private HotKeyHandler hotKeyHandler;

    /// <summary>
    /// Window setup, loads the icon, settings and sets the settings values in the UI.
    /// </summary>
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
        if (settings.LocalFolderPath == "")
        {
            Directory.CreateDirectory(Directory.GetCurrentDirectory() + @"\Screenshots");
            settings.LocalFolderPath = Directory.GetCurrentDirectory() + @"\Screenshots";
        }
        ui_folderPathTextBox.Text = settings.LocalFolderPath;

        // Set default HotKey if nothing is configured/badly configured
        if (!Enum.IsDefined(typeof(VirtualKey), settings.HotKey))
        {
            log.Info("HotKey in settings is not a defined virtual key.");
            settings.HotKey = (int)VirtualKey.M;
        }

        // Show which hotkey is selected
        switch ((VirtualKey)settings.HotKey)
        {
            case VirtualKey.XButton1:
                ui_startButtonHint.Text = "Toggle listening for HotKey: Extra mouse button 1";
                break;
            case VirtualKey.XButton2:
                ui_startButtonHint.Text = "Toggle listening for HotKey: Extra mouse button 2";
                break;
            case VirtualKey.MiddleButton:
                ui_startButtonHint.Text = "Toggle listening for HotKey: Middle mouse button";
                break;
            default:
                ui_startButtonHint.Text = "Toggle listening for HotKey: "
                    + ((VirtualKey)settings.HotKey).ToString();
                break;
        }

        if (settings.Username == "")
        {
            log.Info("Username in the settings file was empty.");
            settings.Username = "user";
        }
        ui_username.Text = settings.Username;

        settings.SaveSettings();

        // Automatically obfuscate FTP settings if there are any
        if (!settings.FtpPassword.IsBlank() && !settings.IsObfuscated)
        {
            settings.IsObfuscated = true;
            settings.SaveSettings();
            log.Info("FTP settings are now obfuscated.");
        }

        hotKeyHandler = new HotKeyHandler(settings.ScreenCaptureDelayMS, new Action(StartMapCapture));
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

    private void SelectHotKey_Click(object sender, RoutedEventArgs e)
    {
        if (isListening)
        {
            log.Error("Cannot change HotKey while capture process is active!");
            ShowStateMessage("Cannot change HotKey while capture process is active!");
            return;
        }
        PreviewKeyUp += SetNewHotKey;
        PreviewMouseUp += SetNewMouseHotKey;
        log.Debug("HotKey selection started.");
        ShowStateMessage($"Select a new HotKey. \n Allowed: 0-9, a-z, middle or extra mouse button.");
    }

    /// <summary>
    /// When a key was pressed while this handler was active and
    /// the key was a digit or letter, it is set and saved as the new HotKey.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    void SetNewMouseHotKey(object sender, MouseButtonEventArgs e)
    {
        VirtualKey key = VirtualKey.None;
        switch (e.ChangedButton)
        {
            case MouseButton.Left:
                log.Warn($"Left mouse button can't be assigned.");
                ShowStateMessage($"Left mouse button can't be assigned.");
                break;
            case MouseButton.Middle:
                key = VirtualKey.MiddleButton;
                ShowStateMessage($"Middle Mouse Button selected.");
                ui_startButtonHint.Text = "Toggle listening for HotKey: Middle Mouse Button";
                break;
            case MouseButton.Right:
                log.Warn($"Right mouse button can't be assigned.");
                ShowStateMessage($"Right mouse button can't be assigned.");
                break;
            case MouseButton.XButton1:
                ShowStateMessage($"Extra Mouse Button 1 is the new HotKey.");
                ui_startButtonHint.Text = "Toggle listening for HotKey: Extra Mouse Button 1";
                key = VirtualKey.XButton1;
                break;
            case MouseButton.XButton2:
                ShowStateMessage($"Extra Mouse Button 2 is the new HotKey.");
                ui_startButtonHint.Text = "Toggle listening for HotKey: Extra Mouse Button 2";
                key = VirtualKey.XButton2;
                break;
        }
        if (key == VirtualKey.None)
        {
            log.Warn($"Mouse key was not recognized.");
            ShowStateMessage($"Only Middle and Extra Mouse Buttons can be used!");
            return;
        }
        settings.HotKey = (int)key;
        settings.SaveSettings();

        PreviewKeyUp -= SetNewHotKey;
        PreviewMouseUp -= SetNewMouseHotKey;
    }

    /// <summary>
    /// When a key was pressed while this handler was active and
    /// the key was a digit or letter, it is set and saved as the new HotKey.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    void SetNewHotKey(object sender, Input.KeyEventArgs e)
    {
        if (!(e.Key >= Key.D0 && e.Key <= Key.D9)
            && !(e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9)
            && !(e.Key >= Key.A && e.Key <= Key.Z))
        {
            log.Warn($"HotKey has to be a digit or letter, but: {e.Key} was pressed.");
            ShowStateMessage($"HotKey has to be a digit or letter, but: {e.Key} was pressed.");
            return;
        }
        VirtualKey virtualKey = (VirtualKey)KeyInterop.VirtualKeyFromKey(e.Key);
        settings.HotKey = (int)virtualKey;
        settings.SaveSettings();
        ShowStateMessage($"New HotKey: {virtualKey.ToString()} selected.");
        ui_startButtonHint.Text = "Toggle listening for HotKey: " + ((VirtualKey)settings.HotKey).ToString();

        PreviewKeyUp -= SetNewHotKey;
        PreviewMouseUp -= SetNewMouseHotKey;
    }

    /// <summary>
    /// Creates a screenshot, starts detection and cut out of a 1x zoomed map.
    /// When a cut out was made it is saved to disk and a FTP upload is started.
    /// </summary>
    private void StartMapCapture()
    {
        Stopwatch sw = new Stopwatch();
        sw.Start();

        // TODO: This all runs in the UI thread, so only the the last ShowStateMessage this method runs is shown.
        string stateMessage = "";

        Bitmap? mapCutOut = null;
        using (Bitmap screenshot = ScreenCapture.CreateScreenshot())
        {
            mapCutOut = ScreenCapture.CutOutMapZoomless(screenshot);
        }

        if (mapCutOut == null)
        {
            if (currentStateMessage.Contains("Screenshot uploaded to FTP-Server"))
            {
                stateMessage = "Map screen closed. Screenshot upload successful.";
                log.Info("No map detected in the screenshot. Probably because map was just closed.");
                // Update the UI message even when the detection is running in a thread
                Dispatcher.Invoke(delegate () { ShowStateMessage(stateMessage); });
                return;
            }
            stateMessage = "No map detected in the screenshot.";
            log.Info("No map detected in the screenshot");
            Dispatcher.Invoke(delegate () { ShowStateMessage(stateMessage); });
            return;
        }

        string filename = "Screenshot_" + settings.Username + "_" + DateTime.Now.ToString("yyyyMMddHHmmss");

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
            stateMessage = "Screenshot couldn't be saved to disk.";
            log.Error($"Screenshot couldn't be saved to disk. Error: {ex.Message}");
            Dispatcher.Invoke(delegate () { ShowStateMessage(stateMessage); });
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
            stateMessage = "Screenshot was saved, but FTP upload failed.";
            log.Error($"Screenshot was saved, but FTP upload failed: {ex.Message}");
            Dispatcher.Invoke(delegate () { ShowStateMessage(stateMessage); });
            return;
        }
        if (success)
        {
            stateMessage = "Screenshot uploaded to FTP-Server.";
            log.Info("Screenshot uploaded to FTP-Server");
            sw.Stop();
            log.Debug($"Screenshot map detecting and uploading process took: {sw.ElapsedMilliseconds}ms");
        }
        else
        {
            stateMessage = "Screenshot was saved, but FTP upload failed.";
            log.Error("Screenshot was saved, but FTP upload failed.");
        }
        Dispatcher.Invoke(delegate () { ShowStateMessage(stateMessage); });
    }

    /// <summary>
    /// Updates the state message in the UI and adds the time.
    /// </summary>
    /// <param name="message">Message to be displayed</param>
    private void ShowStateMessage(string message)
    {
        ui_stateMessage.Text = $"State: {message} {DateTime.Now.ToString("HH:mm:ss")}";
        currentStateMessage = ui_stateMessage.Text;
    }

    /// <summary>
    /// Toggles listening for the HotKey to capture a screenshot.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void CaptureToggle_Click(object sender, RoutedEventArgs e)
    {
        if (!isListening && !hotKeyHandler.isHotkeyRegisterd)
        {
            // The 6 indicates that a mouse key was specified
            // The enum values come from the Windows VirtualKey Enum
            if (settings.HotKey <= 6) hotKeyHandler.RegisterMouseKey(settings.HotKey);
            else hotKeyHandler.RegisterKeyboardKey(this, (uint)settings.HotKey);
            isListening = true;
            ui_startButton.Visibility = Visibility.Hidden;
            ui_stopButton.Visibility = Visibility.Visible;
            ShowStateMessage("Map capture active.");
            log.Debug("Listening for Hotkey now.");
        }
        else
        {
            hotKeyHandler.Unregister();
            hotKeyHandler.RemoveAction();
            isListening = false;
            ui_startButton.Visibility = Visibility.Visible;
            ui_stopButton.Visibility = Visibility.Hidden;
            ShowStateMessage("Map capture stopped.");
            log.Debug("Stopped listening for Hotkey.");
        }
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        hotKeyHandler.Unregister();

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