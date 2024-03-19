using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace HLLMapCapture
{
    internal static class HotKey
    {
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(HotKey));

	private static Window? _Window;

	private static HwndSource? _source;

	private const int HOTKEY_ID = 9000;

	private const int HOTKEY_ID2 = 9001;

	public static bool isHotkeyRegisterd;

	public static Action? action;

    // Keycode for the M key
    public const uint VK_M = 77;

	private static bool isMapOpen;

	// Minimum time between screenshots
	public static int delayMS = 150;

	public static void RegisterWindow(Window window)
	{
		_Window = window;
		IntPtr handle = new WindowInteropHelper(_Window).Handle;
		_source = HwndSource.FromHwnd(handle);
		_source.AddHook(HwndHook);
		RegisterHotKey();
	}

	public static void Unregister()
	{
		if (isHotkeyRegisterd)
		{
            _source?.RemoveHook(HwndHook);
			_source = null;
			UnregisterHotKey();
		}
	}

	[DllImport("user32.dll")]
	public static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vlc);

	[DllImport("user32.dll")]
	public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

	private static void RegisterHotKey()
	{
		WindowInteropHelper windowInteropHelper = new WindowInteropHelper(_Window);
		if (!RegisterHotKey(windowInteropHelper.Handle, HOTKEY_ID, 0, 77) || !RegisterHotKey(windowInteropHelper.Handle, HOTKEY_ID2, 4, 77))
		{
			log.Error("Hotkey registration was unsuccessful.");
		}
		isHotkeyRegisterd = true;
	}

	private static void UnregisterHotKey()
	{
		WindowInteropHelper windowInteropHelper = new WindowInteropHelper(_Window);
		UnregisterHotKey(windowInteropHelper.Handle, HOTKEY_ID);
		UnregisterHotKey(windowInteropHelper.Handle, HOTKEY_ID2);
		isHotkeyRegisterd = false;
	}

	[DllImport("user32.dll")]
	public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, uint dwExtraInfo);

	private static IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
	{
		if (msg == 786)
		{
			int num = wParam.ToInt32();
			int num2 = num;
			if ((uint)(num2 - 9000) <= 1u)
			{
				OnHotKeyPressed();
				handled = true;
			}
		}
		return IntPtr.Zero;
	}

	private static void OnHotKeyPressed()
	{
		try
		{
			UnregisterHotKey();
			ToggleMap();
			Thread.Sleep(delayMS);
			log.Info("Hotkey Pressed.");
			action?.Invoke();
			RegisterHotKey();
		}
		catch (Exception ex)
		{
			log.Error(ex.Message);
		}
	}

	private static void ToggleMap()
	{
		keybd_event((byte)VK_M, 0, 1u, 0u);
		isMapOpen = !isMapOpen;
	}

    }
}
