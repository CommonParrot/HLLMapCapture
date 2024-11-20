using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Windows.System;

namespace HLLMapCapture
{
    internal partial class HotKeyHandler
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(HotKeyHandler));

        private Window? _Window;

        private HwndSource? _source;

        private const int HOTKEY_ID = 9000;

        private const int HOTKEY_ID2 = 9001;

        // Parameter for SetWindowsHookExW, which specifies that a LowLevelMouseProc will be hooked
        private const int WH_MOUSE_LL = 14;

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        private LowLevelMouseProc _mouseProc;
        // Reference for the mouse hook. Used for disconnecting the hook.
        private IntPtr _mouseHookID = IntPtr.Zero;
        // Value which is checked against in the _mouseProc, when a mouse message is received
        private int mouseHotKeyCode = 520;
        private int mouseExtraButtonIndex = 1;
        public Thread? _mouseActionThread = null;

        public bool isHotkeyRegisterd = false;

        /// Behavior which is attached to the assigned hot key
        private Action? action;

        // Is M by default
        private uint hotKeyCode = 77;

        private bool isMouseHotKey = false;

        private readonly int delayMS = 300;

        /// <summary>
        /// Constructor for the HotKeyHandler class
        /// </summary>
        /// <param name="delay">Delay in ms until the action is triggered, after the HotKey is pressed</param>
        /// <param name="act">Action which will be triggered by the HotKey</param>
        public HotKeyHandler(int delay, Action? act)
        {
            delayMS = delay;
            action = act;
            _mouseProc = LLMouseProc;
        }

        /// <summary>
        /// Registers the chosen keyboard hotkey with the application window
        /// </summary>
        /// <param name="window">Window of this application</param>
        /// <param name="keyCode">Chosen hotkey keycode</param>
        public void RegisterKeyboardKey(Window window, uint keyCode = 77)
        {
            isMouseHotKey = false;
            hotKeyCode = keyCode;
            _Window = window;
            IntPtr handle = new WindowInteropHelper(_Window).Handle;
            _source = HwndSource.FromHwnd(handle);
            _source.AddHook(HwndHook);
            RegisterHotKey((int)keyCode, mouseHotKey: false);
        }
        
        /// <summary>
        /// Registers the chosen mouse hotkey
        /// </summary>
        /// <param name="keyCode"></param>
        public void RegisterMouseKey(int keyCode)
        {
            isMouseHotKey = true;
            RegisterHotKey(keyCode, mouseHotKey: true);
        }

        /// <summary>
        /// Unhooks all the hotkeys which might have been set.
        /// </summary>
        public void Unregister()
        {
            if (isHotkeyRegisterd)
            {
                _source?.RemoveHook(HwndHook);
                _source = null;
                if (!isMouseHotKey) UnregisterHotKey();
                else UnhookWindowsHookEx(_mouseHookID);
                isHotkeyRegisterd = false;
            }
        }
        
        /// <summary>
        /// Resets the action associated with this HotKeyHandler to null
        /// </summary>
        public void RemoveAction()
        {
            action = null;
        }

        /// <summary>
        /// Uses SetWindowsHookEx to attach a low level mouse hook.
        /// </summary>
        /// <param name="proc">Hook which will be called on low level mouse events</param>
        /// <returns>Reference to the hook which was connected</returns>
        private static IntPtr SetMouseHook(LowLevelMouseProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule? curModule = curProcess.MainModule)
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
        /// Registers a HotKey to the application window, when a keyboard key was chosen (mouse hooks work differently).
        /// Registers the key alone and in combination with the shift modifier.
        /// The two hotkeys are registered under the hotkey ids which are statically defined,
        /// in the class for future reference.
        /// </summary>
        /// <param name="keyCode">Keycode of the hotkey</param>
        private void RegisterHotKey(int keyCode, bool mouseHotKey = false)
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
        private bool SetMouseHotKeyCode(int keyCode)
        {
            if ((VirtualKey)keyCode == VirtualKey.XButton1)
            {
                mouseHotKeyCode = (int)MouseMessages.ExtraMouseUp;
                mouseExtraButtonIndex = 1;
                return true;
            }
            if ((VirtualKey)keyCode == VirtualKey.XButton2)
            {
                mouseHotKeyCode = (int)MouseMessages.ExtraMouseUp;
                mouseExtraButtonIndex = 2;
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
        private void UnregisterHotKey()
        {
            WindowInteropHelper windowInteropHelper = new WindowInteropHelper(_Window);
            UnregisterHotKey(windowInteropHelper.Handle, HOTKEY_ID);
            UnregisterHotKey(windowInteropHelper.Handle, HOTKEY_ID2);
        }

        // Hook for keyboard events
        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
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
        private nint LLMouseProc(int nCode, nint wParam, nint lParam)
        {
            if (wParam != mouseHotKeyCode)
            {
                return CallNextHookEx(_mouseHookID, nCode, wParam, lParam);
            }
            if (mouseHotKeyCode == (int)MouseMessages.ExtraMouseUp)
            {
                MSLLHOOKSTRUCT hookStruct =
                    (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
                uint xButton = (hookStruct.mouseData >> 16);
                if (xButton == mouseExtraButtonIndex)
                    OnMouseHotKeyPressed();
            }
            else if (mouseHotKeyCode == (int)MouseMessages.MiddleMouseUp)
                OnMouseHotKeyPressed();

            return CallNextHookEx(_mouseHookID, nCode, wParam, lParam);
        }

        private void OnMouseHotKeyPressed()
        {
            if (_mouseActionThread != null && _mouseActionThread.IsAlive)
            {
                log.Warn("Can't trigger parallel map capture, still processing last screenshot!");
                return;
            }
            // Start a thread. Don't block 
            _mouseActionThread =
            new Thread(() =>
            {
                // Wait for the map to open in-game
                Thread.CurrentThread.IsBackground = false;
                Thread.Sleep(delayMS);
                action?.Invoke();
            });
            _mouseActionThread.Start();
            log.Info("Mouse Hotkey Pressed.");

        }

        private void OnHotKeyPressed()
        {
            try
            {
                log.Info("Mouse Hotkey Pressed.");
                // Stop HotKey hook temporarily
                UnregisterHotKey();
                // Fire the key event as if pressed normally
                RefireKeyEvent();
                RegisterHotKey((int)hotKeyCode);
                // Only start capture task, when prior task was finished
                if (_mouseActionThread != null &&
                    _mouseActionThread.IsAlive)
                {
                    log.Warn("Can't trigger parallel map capture, still processing last screenshot!");
                    return;
                }

                _mouseActionThread =
                new Thread(() =>
                {
                    // Wait for the map to open in-game
                    Thread.CurrentThread.IsBackground = false;
                    Thread.Sleep(delayMS);
                    action?.Invoke();
                });

                // Start capture task in thread
                _mouseActionThread.Start();
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
        private void RefireKeyEvent()
        {
            keybd_event((byte)hotKeyCode, 0, 1u, 0u);
        }

        /// <summary>
        /// The mouse messages by which can be used for the hook
        /// </summary>
        private enum MouseMessages
        {
            MiddleMouseUp = 520,
            ExtraMouseUp = 524
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }
    }
}
