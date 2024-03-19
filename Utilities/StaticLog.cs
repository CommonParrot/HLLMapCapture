using log4net;

namespace HLLMapCapture
{
    /// <summary>
    /// Logger class callable from anywhere.
    /// I had issues with creating loggers for static utility classes like
    /// ScreenCapture, so this can be used for static methods in static classes.
    /// </summary>
    public class StaticLog
    {
        public static ILog For( Type ObjectType )

        {

            if ( ObjectType != null )

                return LogManager.GetLogger( ObjectType.Name );

            else

                return LogManager.GetLogger( string.Empty );

        }
    }
}
