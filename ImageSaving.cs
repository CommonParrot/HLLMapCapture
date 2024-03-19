using System;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;

namespace HLLMapCapture
{
    internal static class ImageSaving
    {
        /// <summary>
        /// Saves a bitmap to a file with png encoding.
        /// </summary>
        /// <param name="source">Source Bitmap</param>
        /// <param name="fullPath">Full path of the file without .png ending</param>
        internal static void SaveAsPNG(Bitmap source, string fullPath)
        {
            using (source)
            {
                fullPath = fullPath + ".png";

                if (!File.Exists(fullPath))
                {
                    FileStream fileStream = File.Open(fullPath, FileMode.OpenOrCreate);
                    source.Save(fileStream, ImageFormat.Png);
                    fileStream.Close();
                }
                else
                {
                    StaticLog.For(typeof(ImageSaving)).Error($"File: {fullPath} already exists.");
                }
            }
        }

        internal static void SaveAsJPEG(Bitmap source, string fullPath, int quality = 97)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            using (source)
            {
                ImageCodecInfo? jpegEncoder = GetEncoder(ImageFormat.Jpeg);
                Encoder parameter = Encoder.Quality;
                EncoderParameters parameters = new EncoderParameters(1);

                parameters.Param[0] = new EncoderParameter(parameter, quality);

                if (jpegEncoder != null)
                {
                    source.Save(fullPath + ".jpg", jpegEncoder, parameters);
                }
            }
            sw.Stop();
            StaticLog.For(typeof(ImageSaving)).Info($"Compressing to .jpg took: {sw.ElapsedMilliseconds}ms");
        }

        private static ImageCodecInfo? GetEncoder(ImageFormat format)  
        {  
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();  
            foreach (ImageCodecInfo codec in codecs)  
            {  
                if (codec.FormatID == format.Guid)  
                {  
                    return codec;  
                }  
            }  
            return null;  
        }  
    }
}
