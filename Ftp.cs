using FluentFTP;
using FluentFTP.Helpers;
using HLLMapCapture;
using System.IO;
using System.Net;

namespace HLLMapCapture;

internal static class Ftp
{
    internal static bool UploadPNG(string path, Settings settings)
    {
        using (var client = new FtpClient(settings.FtpServer,
            new NetworkCredential(settings.FtpUsername, settings.FtpPassword)))
        {
            bool success = false;

            string filename = Path.GetFileName(path);
            path = path + ".png"; 

            // Upload the file without the .png ending
            FtpStatus status = client.UploadFile(path, $"{settings.FtpFolderPath}/{filename}");

            if (status.IsSuccess()) {
                // Rename the uploaded file to its final name with .png, to signal completed file upload
                success = client.MoveFile($"{settings.FtpFolderPath}/{filename}", $"{settings.FtpFolderPath}/{filename}.png");
            }
            else
            {
                StaticLog.For(typeof(Ftp)).Error("FTP Upload failed: " + status.ToString());
            }
            return success;
        }
	}

    internal static bool UploadJPEG(string path, Settings settings)
    {
        using (var client = new FtpClient(settings.FtpServer,
            new NetworkCredential(settings.FtpUsername, settings.FtpPassword)))
        {
            bool success = false;

            string filename = Path.GetFileName(path);
            path = path + ".jpg"; 

            // Upload the file without the .png ending
            FtpStatus status = client.UploadFile(path, $"{settings.FtpFolderPath}/{filename}");

            if (status.IsSuccess()) {
                // Rename the uploaded file to its final name with .png, to signal completed file upload
                success = client.MoveFile($"{settings.FtpFolderPath}/{filename}", $"{settings.FtpFolderPath}/{filename}.jpg");
            }
            else
            {
                StaticLog.For(typeof(Ftp)).Error("FTP Upload failed: " + status.ToString());
            }
            return success;
        }
	}

}
