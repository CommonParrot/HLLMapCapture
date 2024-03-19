using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;
using Windows.Storage.Provider;

namespace HLLMapCapture;
internal class Settings
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(Settings));
    private const string SettingsFilePath = "settings.xml";

    public string LocalFolderPath { get; set; } = "";
    public string Username { get; set; } = "";
    public bool CompressImages { get; set; } = true;
    public bool IsObfuscated { get; set; } = false;
    public string? FtpServer { get; set; } = "";
    public string? FtpFolderPath { get; set; } = "";
    public string? FtpUsername { get; set; } = "";
    public string? FtpPassword { get; set; } = "";

    public void LoadSettings()
    {
        var doc = XDocument.Load(SettingsFilePath);

        XElement? root = doc.Root ?? 
            throw new Exception("Supposed settings file does not exist or doesn't contain a XML root element.");

        string? compressionString = root.Element("CompressImages")?.Value;
        bool parsed = bool.TryParse(compressionString, out bool compression);
        if (parsed)
            CompressImages = compression;

        string? obfuscatedString = root.Element("IsObfuscated")?.Value;
        parsed = bool.TryParse(obfuscatedString, out bool obfuscated);
        if (parsed)
            IsObfuscated = obfuscated;

        string? username= root.Element("Username")?.Value;
        if (username != null)
             Username = username;
        string? localFolder= root.Element("LocalFolderPath")?.Value;
        if (localFolder != null)
            LocalFolderPath = localFolder;

        if (!IsObfuscated)
        {
            FtpFolderPath = root.Element("FtpFolderPath")?.Value;
            FtpServer = root.Element("FtpServer")?.Value;
            FtpUsername = root.Element("FtpUsername")?.Value;
            FtpPassword = root.Element("FtpPassword")?.Value;
        }
        else
        {
            FtpFolderPath = Obfuscator.DeobfuscateString(root.Element("FtpFolderPath")?.Value);
            FtpServer = Obfuscator.DeobfuscateString(root.Element("FtpServer")?.Value);
            FtpUsername = Obfuscator.DeobfuscateString(root.Element("FtpUsername")?.Value);
            FtpPassword = Obfuscator.DeobfuscateString(root.Element("FtpPassword")?.Value);
        }

        if (FtpFolderPath == "")
        {
            FtpFolderPath = ".";
        }
    }

    public void SaveSettings()
    {
        try
        {
            XElement xFtpServer;
            XElement xFtpFolerPath;
            XElement xFtpUsername;
            XElement xFtpPassword;
            if (!IsObfuscated)
            {
                xFtpServer = new XElement("FtpServer", FtpServer);
                xFtpFolerPath = new XElement("FtpFolderPath", FtpFolderPath);
                xFtpUsername = new XElement("FtpUsername", FtpUsername);
                xFtpPassword = new XElement("FtpPassword", FtpPassword);
            }
            else
            { 
                xFtpServer = new XElement("FtpServer", 
                    Obfuscator.ObfuscateString(FtpServer));
                xFtpFolerPath = new XElement("FtpFolderPath", 
                    Obfuscator.ObfuscateString(FtpFolderPath));
                xFtpUsername = new XElement("FtpUsername", 
                    Obfuscator.ObfuscateString(FtpUsername));
                xFtpPassword = new XElement("FtpPassword", 
                    Obfuscator.ObfuscateString(FtpPassword));
            }
            var doc = new XDocument(
                new XElement("Settings",
                    new XElement("LocalFolderPath", LocalFolderPath),
                    new XElement("Username", Username),
                    new XElement("CompressImages", CompressImages),
                    new XElement("IsObfuscated", IsObfuscated),
                    xFtpServer, xFtpFolerPath, xFtpUsername, xFtpPassword)
            );

            doc.Save(SettingsFilePath);
            log.Info("Settings saved successfully.");
        }
        catch (Exception ex)
        {
            log.Error($"Error saving settings: {ex.Message}");
        }
    }
}
