using System.Diagnostics;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Xml.Serialization;
using Windows.Devices.Printers.Extensions;
using Windows.System;

namespace HLLMapCapture
{
    internal static class HotKey
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(HotKey));

        private static Window? _Window;

        private static HwndSource? _source;

        private const int HOTKEY_ID = 9000;

        private const int HOTKEY_ID2 = 9001;

        // Parameter for SetWindowsHookExW, which specifies that a LowLevelMouseProc will be hooked
        private const int WH_MOUSE_LL = 14;

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static LowLevelMouseProc _mouseProc = OnMouseHotKeyPressed;
        // Reference for the mouse hook. Used for disconnecting the hook.
        private static IntPtr _mouseHookID = IntPtr.Zero;
        // Value which is checked against in the _mouseProc, when a mouse message is received
        private static int mouseHotKeyCode = 520;
        public static Thread? _mouseActionThread = null;

        public static bool isHotkeyRegisterd;

        /// <summary>
        /// Behavior which is attached to the assigned hot key
        /// </summary>
        public static Action? action;


        // Is M by default
        private static uint hotKeyCode = 77;

        private static bool isMouseHotKey = false;

        private static bool isMapOpen;

        // Minimum time between screenshots
        public static int delayMS = 300;

        public static void RegisterWindow(Window window, uint keyCode = 77)
        {
            isMouseHotKey = false;
            hotKeyCode = keyCode;
            _Window = window;
            IntPtr handle = new WindowInteropHelper(_Window).Handle;
            _source = HwndSource.FromHwnd(handle);
            _source.AddHook(HwndHook);
            RegisterHotKey((int)keyCode, mouseHotKey: false);
        }

        public static void RegisterMouseKey(int keyCode)
        {
            isMouseHotKey = true;
            RegisterHotKey(keyCode, mouseHotKey: true);
        }

        /// <summary>
        /// Unhooks all the hotkeys which might have been set.
        /// </summary>
        public static void Unregister()
        {
            if (isHotkeyRegisterd)
            {
                _source?.RemoveHook(HwndHook);
                _source = null;
                if (!isMouseHotKey) UnregisterHotKey();
                else UnhookWindowsHookEx(_mouseHookID);
            }
        }

        /// <summary>
        /// Uses SetWindowsHookEx to attach a low level mouse hook.
        /// </summary>
        /// <param name="proc">Hook which will be called on low level mouse events</param>
        /// <returns>Reference to the hook which was connected</returns>
        private static IntPtr SetMouseHook(LowLevelMouseProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vlc);

        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);


        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        // This allows for processing of the mouse event by hooks which other applications
        // like the game, might have set.
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        /// <summary>
        /// Registers a HotKey to the application window.
        /// Registers the key alone and in combination with the shift modifier.
        /// The two hotkeys are registered under the hotkey ids which are statically defined,
        /// in the class for future reference.
        /// </summary>
        /// <param name="keyCode">Keycode of the hotkey</param>
        private static void RegisterHotKey(int keyCode, bool mouseHotKey = false)
        {
            if (mouseHotKey)
            {
                if (!SetMouseHotKeyCode(keyCode))
                {
                    log.Warn("Mouse HotKeys can only be the middle or extra mouse buttons!");
                    return;
                }
                _mouseHookID = SetMouseHook(_mouseProc);
                isHotkeyRegisterd = true;
                return;
            }
            WindowInteropHelper windowInteropHelper = new WindowInteropHelper(_Window);
            if (!RegisterHotKey(windowInteropHelper.Handle, HOTKEY_ID, 0, keyCode)
                    || !RegisterHotKey(windowInteropHelper.Handle, HOTKEY_ID2, 4, keyCode))
            {
                log.Error("Hotkey registration was unsuccessful.");
            }
            isHotkeyRegisterd = true;
        }

        /// <summary>
        /// Maps the VirtualKey enum to the allowed keycodes which can be 
        /// checked against in the LowLevelMouseProc.
        /// Only Middle and Extra Mouse buttons are allowed.
        /// </summary>
        /// <param name="keyCode">Keycode from the VirtualKey enum range</param>
        /// <returns>True when the keycode is from the Middle or Extra Mouse buttons.</returns>
        private static bool SetMouseHotKeyCode(int keyCode)
        {
            if ((VirtualKey)keyCode == VirtualKey.XButton1
                || (VirtualKey)keyCode == VirtualKey.XButton2)
            {
                mouseHotKeyCode = (int)MouseMessages.ExtraMouseUp;
                return true;
            }
            if ((VirtualKey)keyCode == VirtualKey.MiddleButton)
            {
                mouseHotKeyCode = (int)MouseMessages.MiddleMouseUp;
                return true;
            }
            return false;
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

        // Hook for keyboard events
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

        // Hook for mouse events
        private static nint OnMouseHotKeyPressed(int nCode, nint wParam, nint lParam)
        {
            if(wParam == mouseHotKeyCode &&
               _mouseActionThread != null && _mouseActionThread.IsAlive)
            {
                log.Warn("Can't trigger parallel map capture, still processing last screenshot!");
            }
            if (wParam == mouseHotKeyCode && 
                (_mouseActionThread == null || !_mouseActionThread.IsAlive))
            {
                // Start a thread. Don't block 
                _mouseActionThread = 
                new Thread(() =>
                {
                    Thread.CurrentThread.IsBackground = false;
                    action?.Invoke();
                });
                _mouseActionThread.Start();
                log.Info("Mouse Hotkey Pressed.");
            }
            return CallNextHookEx(_mouseHookID, nCode, wParam, lParam);
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


        [DllImport("user32.dll")]
        public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, uint dwExtraInfo);

        /// <summary>
        /// Fires the keyboard event which was intercepted by the hotkey triggering,
        /// to open/close the map in game.
        /// </summary>
        private static void ToggleMap()
        {
            keybd_event((byte)hotKeyCode, 0, 1u, 0u);
            isMapOpen = !isMapOpen;
        }

        /// <summary>
        /// The mouse messages by which can be used for the hook
        /// </summary>
        private enum MouseMessages
        {
            MiddleMouseUp = 520,
            ExtraMouseUp = 524
        }

    }
}
