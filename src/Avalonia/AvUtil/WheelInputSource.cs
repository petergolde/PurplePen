using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace AvUtil
{
    // The device that a mouse wheel message most likely came from.
    public enum WheelSourceDevice
    {
        Unknown,        // No information is available (non-Windows platforms, or no raw input was received).
        Mouse,          // A physical mouse wheel.
        Touchpad,       // A touchpad gesture (two-finger pan, or pinch reported as Ctrl+wheel), or other synthesized wheel input.
    }

    // Information about the source of the wheel message that is currently being processed.
    //   Device: the source device, determined from the raw-input wheel event.
    //   HasRawWheel: true if a raw-input wheel event arrived just before this wheel message.
    //   RawDevice: the hDevice of that raw-input wheel event (IntPtr.Zero for synthesized input).
    public readonly record struct WheelSourceInfo(WheelSourceDevice Device, bool HasRawWheel, IntPtr RawDevice)
    {
        // A compact description for diagnostic logging.
        public override string ToString()
        {
            return $"{Device} (rawWheel={(HasRawWheel ? $"0x{RawDevice:X}" : "none")})";
        }
    }

    // Determines which device produced Windows mouse wheel messages, which Avalonia does not report (on Windows
    // both mouse wheels and touchpad gestures arrive as WM_MOUSEWHEEL/WM_MOUSEHWHEEL from the same mouse pointer).
    // A WndProc hook is installed on each top level that needs it; it captures the source of each wheel message
    // just before Avalonia processes it. Avalonia raises PointerWheelChanged synchronously while processing that
    // same message, so a wheel event handler can read Current to find the source of the event it is handling.
    //
    // The source is determined from raw mouse input: every wheel message is preceded by a WM_INPUT raw wheel event.
    // For a physical mouse wheel, that event carries the mouse's device handle; precision touchpad gestures are
    // synthesized by Windows and carry a null device handle. (GetCurrentInputMessageSource is no help: it reports
    // IMDT_MOUSE / IMO_HARDWARE for both.) Raw input goes to the window with keyboard focus, so wheel messages
    // sent to an unfocused window have no raw event and are reported as Unknown.
    //
    // All methods are no-ops on non-Windows platforms. All state is only accessed on the UI thread.
    public static class WheelInputSource
    {
        private const uint WM_INPUT = 0x00FF;
        private const uint WM_MOUSEWHEEL = 0x020A;
        private const uint WM_MOUSEHWHEEL = 0x020E;

        private const uint RID_INPUT = 0x10000003;
        private const uint RIM_TYPEMOUSE = 0;
        private const ushort RI_MOUSE_WHEEL = 0x0400;
        private const ushort RI_MOUSE_HWHEEL = 0x0800;

        // How recently (in ms) a raw wheel event must have arrived to be matched with a wheel message.
        private const long RawWheelMatchWindowMs = 50;

        // Number of controls on each top level that have requested the hook, so it is installed only once.
        private static readonly ConditionalWeakTable<TopLevel, StrongBox<int>> hookCounts = new ConditionalWeakTable<TopLevel, StrongBox<int>>();

        private static bool rawInputRegistered = false;

        // The most recent raw-input wheel event that has not yet been matched with a wheel message.
        private static bool pendingRawWheel = false;
        private static IntPtr pendingRawWheelDevice = IntPtr.Zero;
        private static long pendingRawWheelTime = 0;

        // Information about the source of the wheel message currently (or most recently) processed.
        public static WheelSourceInfo Current { get; private set; }

        // Start tracking wheel message sources for the given top level. Calls are reference counted, so each
        // call must be balanced by a call to Uninstall.
        //   topLevel: the window (or other top level) that receives the wheel messages.
        public static void Install(TopLevel topLevel)
        {
            if (!OperatingSystem.IsWindows())
                return;

            StrongBox<int> count = hookCounts.GetOrCreateValue(topLevel);
            if (count.Value++ == 0) {
                Win32Properties.AddWndProcHookCallback(topLevel, WndProcHook);
                RegisterRawMouseInput();
            }
        }

        // Stop tracking wheel message sources for the given top level (balances a call to Install).
        //   topLevel: the top level passed to Install.
        public static void Uninstall(TopLevel topLevel)
        {
            if (!OperatingSystem.IsWindows())
                return;

            if (hookCounts.TryGetValue(topLevel, out StrongBox<int>? count) && count.Value > 0) {
                if (--count.Value == 0) {
                    Win32Properties.RemoveWndProcHookCallback(topLevel, WndProcHook);
                }
            }
        }

        // Register for raw mouse input, delivered to whichever window has keyboard focus. Registration is
        // process-wide, so it only needs to happen once. Legacy mouse messages are still delivered.
        private static void RegisterRawMouseInput()
        {
            if (rawInputRegistered)
                return;

            RAWINPUTDEVICE[] devices = new RAWINPUTDEVICE[] {
                new RAWINPUTDEVICE { usUsagePage = 0x01, usUsage = 0x02, dwFlags = 0, hwndTarget = IntPtr.Zero }
            };
            rawInputRegistered = RegisterRawInputDevices(devices, (uint)devices.Length, (uint)Marshal.SizeOf<RAWINPUTDEVICE>());
        }

        // The WndProc hook. Captures the source of wheel messages before Avalonia processes them; never handles
        // a message itself.
        private static IntPtr WndProcHook(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_INPUT) {
                RecordRawInput(lParam);
            }
            else if (msg == WM_MOUSEWHEEL || msg == WM_MOUSEHWHEEL) {
                Current = DetermineWheelSource();
            }

            return IntPtr.Zero;
        }

        // Record a raw-input mouse wheel event, if the given WM_INPUT message is one.
        //   hRawInput: the lParam of the WM_INPUT message.
        private static void RecordRawInput(IntPtr hRawInput)
        {
            int headerSize = Marshal.SizeOf<RAWINPUTHEADER>();
            uint size = 0;
            GetRawInputData(hRawInput, RID_INPUT, IntPtr.Zero, ref size, (uint)headerSize);
            if (size == 0)
                return;

            IntPtr buffer = Marshal.AllocHGlobal((int)size);
            try {
                if (GetRawInputData(hRawInput, RID_INPUT, buffer, ref size, (uint)headerSize) != size)
                    return;

                RAWINPUTHEADER header = Marshal.PtrToStructure<RAWINPUTHEADER>(buffer);
                if (header.dwType != RIM_TYPEMOUSE)
                    return;

                RAWMOUSE mouse = Marshal.PtrToStructure<RAWMOUSE>(buffer + headerSize);
                if ((mouse.usButtonFlags & (RI_MOUSE_WHEEL | RI_MOUSE_HWHEEL)) != 0) {
                    pendingRawWheel = true;
                    pendingRawWheelDevice = header.hDevice;
                    pendingRawWheelTime = Environment.TickCount64;
                }
            }
            finally {
                Marshal.FreeHGlobal(buffer);
            }
        }

        // Determine the source of the wheel message being processed, consuming any pending raw wheel event.
        private static WheelSourceInfo DetermineWheelSource()
        {
            bool hasRawWheel = pendingRawWheel && (Environment.TickCount64 - pendingRawWheelTime) <= RawWheelMatchWindowMs;
            IntPtr rawDevice = hasRawWheel ? pendingRawWheelDevice : IntPtr.Zero;
            pendingRawWheel = false;

            WheelSourceDevice device;
            if (!hasRawWheel)
                device = WheelSourceDevice.Unknown;
            else if (rawDevice != IntPtr.Zero)
                device = WheelSourceDevice.Mouse;
            else
                device = WheelSourceDevice.Touchpad;    // Null device handle: synthesized, normally by the touchpad.

            return new WheelSourceInfo(device, hasRawWheel, rawDevice);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUTDEVICE
        {
            public ushort usUsagePage;
            public ushort usUsage;
            public uint dwFlags;
            public IntPtr hwndTarget;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUTHEADER
        {
            public uint dwType;
            public uint dwSize;
            public IntPtr hDevice;
            public IntPtr wParam;
        }

        // RAWMOUSE, with the ulButtons/usButtonFlags union flattened to its usButtonFlags/usButtonData view.
        [StructLayout(LayoutKind.Sequential)]
        private struct RAWMOUSE
        {
            public ushort usFlags;
            public ushort padding;
            public ushort usButtonFlags;
            public ushort usButtonData;
            public uint ulRawButtons;
            public int lLastX;
            public int lLastY;
            public uint ulExtraInformation;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterRawInputDevices(RAWINPUTDEVICE[] pRawInputDevices, uint uiNumDevices, uint cbSize);

        [DllImport("user32.dll")]
        private static extern uint GetRawInputData(IntPtr hRawInput, uint uiCommand, IntPtr pData, ref uint pcbSize, uint cbSizeHeader);
    }
}
