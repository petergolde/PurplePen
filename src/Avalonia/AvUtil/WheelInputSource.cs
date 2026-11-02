using Avalonia;
using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
#if MACOS
using AppKit;
#endif

namespace AvUtil
{
    // The device that a mouse wheel message most likely came from.
    public enum WheelSourceDevice
    {
        Unknown,        // No information is available (unsupported platform, or no raw input / current event was found).
        Mouse,          // A physical mouse wheel.
        Touchpad,       // A touchpad gesture: on Windows a two-finger pan, or pinch reported as Ctrl+wheel, or other synthesized
                        // wheel input; on macOS a two-finger scroll (trackpad or Magic Mouse), including its momentum.
    }

    // Information about the source of the wheel message that is currently being processed.
    //   Device: the source device, determined from the raw-input wheel event (Windows) or the current AppKit event (macOS).
    //   HasRawWheel: (Windows) true if a raw-input wheel event arrived just before this wheel message.
    //   RawDevice: (Windows) the hDevice of that raw-input wheel event (IntPtr.Zero for synthesized input).
    //   PreciseDelta: (macOS touchpad only) the scroll distance in logical pixels, with the system's scrolling direction
    //                 setting applied; positive values move the content toward the right/bottom, like a wheel rotated
    //                 up. Null if not available.
    public readonly record struct WheelSourceInfo(WheelSourceDevice Device, bool HasRawWheel, IntPtr RawDevice, Vector? PreciseDelta = null)
    {
        // A compact description for diagnostic logging.
        public override string ToString()
        {
            string precise = PreciseDelta.HasValue ? $" preciseDelta={PreciseDelta.Value}" : "";
            return $"{Device} (rawWheel={(HasRawWheel ? $"0x{RawDevice:X}" : "none")}{precise})";
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
    // IMDT_MOUSE / IMO_HARDWARE for both.) Normally raw input goes to the window with keyboard focus, so wheel
    // messages sent to an unfocused window (Windows scrolls the window under the cursor) would have no raw event.
    // To handle that, while the pointer is over a control that cares, SetBackgroundTarget redirects raw input to
    // that control's window with RIDEV_INPUTSINK, which delivers it even when the application is in the background.
    // This is limited to hover time so the application is not woken by every mouse move made in other applications.
    // Wheel messages that still have no raw event are reported as Unknown.
    //
    // On macOS (the net10.0-macos build), no hook is needed: Avalonia.Native raises PointerWheelChanged synchronously
    // from the native scrollWheel: handler, so NSApplication.CurrentEvent is the scroll event being handled, and Current
    // examines it directly. Avalonia does not pass on which device produced the scroll (or the phases), so this is the
    // only place to find out. A trackpad two-finger scroll is a gesture: it has precise (pixel) deltas and a phase or
    // momentum phase on every event. A mouse wheel has neither phase, even with a driver that sends smooth deltas.
    // A Magic Mouse cannot be told apart from a trackpad and is reported as Touchpad.
    //
    // All other methods are no-ops on non-Windows platforms. All state is only accessed on the UI thread.
    public static class WheelInputSource
    {
        private const uint WM_INPUT = 0x00FF;
        private const uint WM_MOUSEWHEEL = 0x020A;
        private const uint WM_MOUSEHWHEEL = 0x020E;

        private const uint RID_INPUT = 0x10000003;
        private const uint RIM_TYPEMOUSE = 0;
        private const ushort RI_MOUSE_WHEEL = 0x0400;
        private const ushort RI_MOUSE_HWHEEL = 0x0800;
        private const uint RIDEV_INPUTSINK = 0x00000100;

        // How recently (in ms) a raw wheel event must have arrived to be matched with a wheel message.
        private const long RawWheelMatchWindowMs = 50;

        // Number of controls on each top level that have requested the hook, so it is installed only once.
        private static readonly ConditionalWeakTable<TopLevel, StrongBox<int>> hookCounts = new ConditionalWeakTable<TopLevel, StrongBox<int>>();

        // Whether raw mouse input is registered, and the window it is sent to (IntPtr.Zero to follow keyboard focus).
        private static bool rawInputRegistered = false;
        private static IntPtr rawInputTarget = IntPtr.Zero;

        // The top level that raw input is currently redirected to by SetBackgroundTarget, or null if none.
        private static TopLevel? backgroundTarget;

        // The most recent raw-input wheel event that has not yet been matched with a wheel message.
        private static bool pendingRawWheel = false;
        private static IntPtr pendingRawWheelDevice = IntPtr.Zero;
        private static long pendingRawWheelTime = 0;

        // Information about the source of the wheel message currently (or, on Windows, most recently) processed.
        public static WheelSourceInfo Current {
            get {
#if MACOS
                return DetermineMacWheelSource();
#else
                return current;
#endif
            }
        }

        // The source of the wheel message most recently processed by the Windows hook.
        private static WheelSourceInfo current;

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
                if (!rawInputRegistered)
                    RegisterRawMouseInput(IntPtr.Zero);
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
                    ClearBackgroundTarget(topLevel);
                    Win32Properties.RemoveWndProcHookCallback(topLevel, WndProcHook);
                }
            }
        }

        // Redirect raw input to the given top level's window, so wheel sources are known even when the
        // application is in the background. Call when the pointer enters a control that uses Current, and
        // balance with ClearBackgroundTarget when it leaves.
        //   topLevel: a top level passed to Install.
        public static void SetBackgroundTarget(TopLevel topLevel)
        {
            if (!OperatingSystem.IsWindows())
                return;

            // Raw input must only go to a window that has the hook, or it would be lost.
            if (!hookCounts.TryGetValue(topLevel, out StrongBox<int>? count) || count.Value == 0)
                return;

            IntPtr hwnd = topLevel.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
            if (hwnd == IntPtr.Zero)
                return;

            if (RegisterRawMouseInput(hwnd))
                backgroundTarget = topLevel;
        }

        // Stop redirecting raw input to the given top level (balances SetBackgroundTarget), so it follows keyboard
        // focus again. Does nothing if raw input is not currently redirected to this top level.
        //   topLevel: the top level passed to SetBackgroundTarget.
        public static void ClearBackgroundTarget(TopLevel topLevel)
        {
            if (!OperatingSystem.IsWindows())
                return;

            if (backgroundTarget == topLevel) {
                backgroundTarget = null;
                RegisterRawMouseInput(IntPtr.Zero);
            }
        }

        // Register for raw mouse input. Registration is process-wide, and registering again replaces the previous
        // registration. Legacy mouse messages are still delivered.
        //   hwndTarget: the window to send raw input to, even when the application is in the background; or
        //               IntPtr.Zero to send it to whichever window has keyboard focus.
        // Returns true if the registration succeeded (or was already in place).
        private static bool RegisterRawMouseInput(IntPtr hwndTarget)
        {
            if (rawInputRegistered && rawInputTarget == hwndTarget)
                return true;

            RAWINPUTDEVICE[] devices = new RAWINPUTDEVICE[] {
                new RAWINPUTDEVICE { usUsagePage = 0x01, usUsage = 0x02,
                                     dwFlags = (hwndTarget != IntPtr.Zero) ? RIDEV_INPUTSINK : 0, hwndTarget = hwndTarget }
            };
            if (!RegisterRawInputDevices(devices, (uint)devices.Length, (uint)Marshal.SizeOf<RAWINPUTDEVICE>()))
                return false;

            rawInputRegistered = true;
            rawInputTarget = hwndTarget;
            return true;
        }

        // The WndProc hook. Captures the source of wheel messages before Avalonia processes them; never handles
        // a message itself.
        private static IntPtr WndProcHook(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_INPUT) {
                RecordRawInput(lParam);
            }
            else if (msg == WM_MOUSEWHEEL || msg == WM_MOUSEHWHEEL) {
                current = DetermineWheelSource();
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

#if MACOS
        // Determine the source of the scroll event being processed on macOS, from the current AppKit event.
        private static WheelSourceInfo DetermineMacWheelSource()
        {
            NSEvent? currentEvent = NSApplication.SharedApplication.CurrentEvent;
            if (currentEvent == null || currentEvent.Type != NSEventType.ScrollWheel)
                return new WheelSourceInfo(WheelSourceDevice.Unknown, false, IntPtr.Zero);

            bool isGesture = currentEvent.HasPreciseScrollingDeltas &&
                             (currentEvent.Phase != NSEventPhase.None || currentEvent.MomentumPhase != NSEventPhase.None);
            if (!isGesture)
                return new WheelSourceInfo(WheelSourceDevice.Mouse, false, IntPtr.Zero);

            // Precise scrolling deltas are in points, which are the same as Avalonia logical pixels.
            Vector preciseDelta = new Vector((double)currentEvent.ScrollingDeltaX, (double)currentEvent.ScrollingDeltaY);
            return new WheelSourceInfo(WheelSourceDevice.Touchpad, false, IntPtr.Zero, preciseDelta);
        }
#endif

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
