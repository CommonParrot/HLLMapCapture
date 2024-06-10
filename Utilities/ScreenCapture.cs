namespace HLLMapCapture;
internal static class ScreenCapture
{
    /// <summary>
    /// Takes a screenshot of the current primary display.
    /// </summary>
    /// <returns>Bitmap containing the screenshot</returns>
    /// <exception cref="Exception">Throws when no screen was found</exception>
    public static Bitmap CreateScreenshot()
    {

        if (Screen.PrimaryScreen == null)
        {
            StaticLog.For(typeof(ScreenCapture)).Error("No primary screen found. " +
                "No screenshot can be taken.");
            throw new Exception("No primary screen connected.");
        }
        Bitmap bitmap = new Bitmap(Screen.PrimaryScreen.Bounds.Width, 
            Screen.PrimaryScreen.Bounds.Height);
        using (Graphics graphics = Graphics.FromImage(bitmap))
        {
            graphics.CopyFromScreen(0, 0, 0, 0, bitmap.Size, CopyPixelOperation.SourceCopy);
        }
        return bitmap;
    }

    /// <summary>
    /// Checks if there is a unzoomed HLL map
    /// in the screenshot and cuts it out.
    /// </summary>
    /// <param name="screenshot">A screenshot</param>
    /// <returns>The map cut out from the screenshot or null</returns>
    public static Bitmap? CutOutMapZoomless(Bitmap screenshot)
    {
        Bitmap? map = null;
        int width = screenshot.Width;
        int height = screenshot.Height;
        int middle = (width / 2);
        int dodgeUIOffset = 300;
        int maxSearchHeight = height / 8;

        int minBrightnessDelta = 85;
        int lastPixelBrightness = -1;

        int heightFromBottom = -1;

        // If the monitor is 1080p high, use the specific detection instead
        if (screenshot.Height == 1080) return FHDSpecificDetection(screenshot);

        // Check pixel brightness coming from the bottom center/left of the image.
        // (Dodging the bright ui elements, which get in the middle of the screen on ultrawide monitors)
        // When the brightness delta is higher than the detection delta,
        // the edge of the zoomed out map could be there.
        for (int i = height-1; i >= height-maxSearchHeight; --i)
        {
            int brightness = (int)CalculateColorBrightness(
                screenshot.GetPixel(middle - dodgeUIOffset, i));
            if (lastPixelBrightness > -1 && 
                brightness - lastPixelBrightness >= minBrightnessDelta)
            {
                heightFromBottom = i;
                break;
            }
            lastPixelBrightness = brightness;
        }

        if (heightFromBottom == -1)
        {
            StaticLog.For(typeof(ScreenCapture)).Warn("Lower edge of map not found. " +
                "Be sure to zoom to 1x for capture.");
            return map;
        }

        int xPosLeft = -1;
        int minConsistencyDelta = 40;
        lastPixelBrightness = -1;
        
        // When a possible lower map edge was detected check along the edge horizontally.
        // The edge of a map is consistent in it's pixel brightness/color and ends with a 
        // brightness delta, due to the black gradient overlay behind the map in game.
        for (int i = middle; i >= 0; --i)
        {
            int brightness = (int)CalculateColorBrightness(
                screenshot.GetPixel(i, heightFromBottom));

            if (lastPixelBrightness > -1 && 
                lastPixelBrightness - brightness >= minBrightnessDelta)
            {
                xPosLeft = i+1;
                break;
            }

            if(lastPixelBrightness > -1 && 
                Math.Abs(lastPixelBrightness - brightness) > minConsistencyDelta)
            {
                break;
            }
            lastPixelBrightness = brightness;
        }

        if (xPosLeft == -1)
        {
            StaticLog.For(typeof(ScreenCapture)).Warn("Left edge of map not found. " +
                "Be sure to zoom to 1x for capture.");
            return map;
        }

        int xPosRight = -1;

        // Do the same thing for the right edge of the map.
        // Just mirroring the left width could lead to errors.
        for (int i = middle; i <= width-1; ++i)
        {
            int brightness = (int) CalculateColorBrightness(
                screenshot.GetPixel(i, heightFromBottom));
            if (lastPixelBrightness > -1 && 
                lastPixelBrightness - brightness >= minBrightnessDelta)
            {
                xPosRight = i-1;
                break;
            }
            if(lastPixelBrightness > -1 && 
                Math.Abs(lastPixelBrightness - brightness) > minConsistencyDelta)
            {
                break;
            }
            lastPixelBrightness = brightness;
        }

        if (xPosRight == -1)
        {
            StaticLog.For(typeof(ScreenCapture)).Warn("Right edge of map not found. " +
                "Be sure to zoom to 1x for capture.");
            return map;
        }

        int gridLineCount = 0;
        int minGridLineDistance = 50;
        int currentGridLineDistance = 0;
        float deltaSum = -1;

        
        // When bottom edge of the map is signaled by a bright whiteish color
        // the row above it is decidedly darker in every resolution (1080p, 1440p, 2160p).
        // That row is interrupted by bright pixels, when a vertical grid line hits the bottom edge.
        // This works on the difference in brightness of the pixels in these two rows.
        // The deltas are summed up to calculate a mean difference, if a delta deviates from this mean
        // by 50% it is registered as a grid line.
        for (int i = 0; i <= xPosRight - xPosLeft; ++i)
        {
            int bright1 = (int) CalculateColorBrightness(
                screenshot.GetPixel(xPosLeft + i, heightFromBottom - 1));
            int bright2 = (int) CalculateColorBrightness(
                screenshot.GetPixel(xPosLeft + i, heightFromBottom));
            int delta = bright2 - bright1;
            if (deltaSum == -1)
            {
                deltaSum = delta;
                continue;
            }
            float meanDelta = deltaSum / i;
            if (currentGridLineDistance >= minGridLineDistance && Math.Abs(delta) < 0.5 * meanDelta)
            {
                ++gridLineCount;
                currentGridLineDistance = 0;
            }
            // This distance ensures that that no line is detected twice
            ++currentGridLineDistance;
            deltaSum += delta;
        }
        
        // The maps in HLL always have 10x10 map grids.
        // There are 11 vertical grid lines if you the edges of the map and 9 if not.
        if (gridLineCount < 9)
        {
            StaticLog.For(typeof(ScreenCapture)).Warn($"To few gridlines detected. " +
                "Be sure to zoom to 1x for capture.");
            return map;
        }

        int mapWidth = xPosRight - xPosLeft;

        // This adds the statistics portion above the map to the cut out.
        // At the time of writing this is 2% of screen above the map on 1080p, 1440p, 2160p.
        int mapHeight = mapWidth + (int)(0.02 * height);
        if (heightFromBottom - mapHeight <= 0)
        {
            StaticLog.For(typeof(ScreenCapture)).Error("Map goes out of bounds.");
        }

        Rectangle section = new Rectangle(xPosLeft, heightFromBottom - mapHeight, 
            mapWidth, mapHeight);
        map = screenshot.Clone(section, screenshot.PixelFormat);

        StaticLog.For(typeof(ScreenCapture)).Info("Map found in screenshot. " +
            "Continuing processing.");
        return map;
    }

    /// <summary>
    /// An update shrank the map screen by 20x20pixels at 1080p, making the image more blurry.
    /// It is not possible anymore to detect the maps bottom border with the pixel brightness difference.
    /// So this is a hardcoded specific detection for 1080p (in height) monitors.
    /// </summary>
    /// <param name="screenshot">A screenshot</param>
    /// <returns>The map cut out from the screenshot or null</returns>
    public static Bitmap? FHDSpecificDetection(Bitmap screenshot)
    {
        Bitmap? map = null;
        int width = screenshot.Width;
        // The map is 862x861 big on 1080p height resolution monitors
        int mapWidth = 862;

        // Stats are 21 pixels high
        int mapHeight = 861 + 21;

        // Specific height from bottom (screen height is counted from top to bottom)
        int heightFromBottom = 1080 - 129;

        // Find the left edge of the map
        // (important for when the map isn't centered because of commander screen)
        int xPosLeft = -1;
        int minBrightnessDelta = 85;
        int lastPixelBrightness = -1;
        
        for (int i = screenshot.Width/2; i >= 0; --i)
        {
            int brightness = (int)CalculateColorBrightness(
                screenshot.GetPixel(i, heightFromBottom));

            if (lastPixelBrightness > -1 && 
                lastPixelBrightness - brightness >= minBrightnessDelta)
            {
                xPosLeft = i+1;
                break;
            }
            lastPixelBrightness = brightness;
        }

        // Count the grid lines of the map to determine whether it is zoomed out to 1x
        int gridLineCount = 0;
        int minGridLineDistance = 80;
        int currentGridLineDistance = 0;
        float brightSum = -1;

        for (int i = 0; i <= mapWidth; ++i)
        {
            int bright = (int) CalculateColorBrightness(
                screenshot.GetPixel(xPosLeft + i, heightFromBottom));
            if (brightSum == -1)
            {
                brightSum = bright;
                continue;
            }
            float meanBright = brightSum / i;
            if (currentGridLineDistance >= minGridLineDistance && Math.Abs(bright) > 1.3 * meanBright)
            {
                ++gridLineCount;
                currentGridLineDistance = 0;
            }
            // This distance ensures that that no line is detected twice
            ++currentGridLineDistance;
            brightSum += bright;
        }

        // The maps in HLL always have 10x10 map grids.
        // There are 11 vertical grid lines if you the edges of the map and 9 if not.
        if (gridLineCount < 9)
        {
            StaticLog.For(typeof(ScreenCapture)).Warn($"To few gridlines detected. " +
                "Be sure to open the map and zoom to 1x for capture.");
            return map;
        }


        Rectangle section = new Rectangle(xPosLeft, heightFromBottom + 1 - mapHeight,
            mapWidth, mapHeight);
        map = screenshot.Clone(section, screenshot.PixelFormat);

        StaticLog.For(typeof(ScreenCapture)).Info("Map found in screenshot. " +
            "Continuing processing.");

        return map;
    }
    
    // https://www.baeldung.com/cs/compute-similarity-of-colours
    public static float CalculateColorDifference(Color color1, Color color2)
    {
        return 0.3f * MathF.Pow(color1.R - color2.R, 2) + 0.59f * 
            MathF.Pow(color1.G - color2.G,2) + 0.11f * 
            MathF.Pow(color1.B - color2.B,2);
    }

    // http://alienryderflex.com/hsp.html
    public static float CalculateColorBrightness(Color color)
    {
        return MathF.Sqrt(0.299f * MathF.Pow(color.R, 2) + 
            0.587f * MathF.Pow(color.G, 2) + 
            0.114f * MathF.Pow(color.B, 2));
    }


}
