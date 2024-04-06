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

	// Is M by default
    private static uint hotKeyCode = 77;

	private static bool isMapOpen;

	// Minimum time between screenshots
	public static int delayMS = 150;

	public static void RegisterWindow(Window window, uint keyCode = 77)
	{
		_Window = window;
		hotKeyCode = keyCode;
		IntPtr handle = new WindowInteropHelper(_Window).Handle;
		_source = HwndSource.FromHwnd(handle);
		_source.AddHook(HwndHook);
		RegisterHotKey((int)keyCode);
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
	
	/// <summary>
	/// Registers a HotKey to the application window.
	/// Registers the key alone and in combination with the shift modifier.
	/// The two hotkeys are registered under the hotkey ids which are statically defined,
	/// in the class for future reference.
	/// </summary>
	/// <param name="keyCode">Keycode of the hotkey</param>
	private static void RegisterHotKey(int keyCode)
	{
		WindowInteropHelper windowInteropHelper = new WindowInteropHelper(_Window);
		if (!RegisterHotKey(windowInteropHelper.Handle, HOTKEY_ID, 0, keyCode) 
				|| !RegisterHotKey(windowInteropHelper.Handle, HOTKEY_ID2, 4, keyCode))
		{
			log.Error("Hotkey registration was unsuccessful.");
		}
		isHotkeyRegisterd = true;
	}

	/// <summary>
	/// Unregisters the two hotkeys (hotkey & hotkey+shift) referenced by
	/// the statically defined hotkey ids.
	/// </summary>
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
			RegisterHotKey((int)hotKeyCode);
		}
		catch (Exception ex)
		{
			log.Error(ex.Message);
		}
	}

	/// <summary>
	/// Fires the keyboard event which was intercepted by the hotkey triggering,
	/// to open/close the map in game.
	/// </summary>
	private static void ToggleMap()
	{
		keybd_event((byte)hotKeyCode, 0, 1u, 0u);
		isMapOpen = !isMapOpen;
	}

    }
}
