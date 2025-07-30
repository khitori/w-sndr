module WindowInterop

open System
open System.Text
open System.Runtime.InteropServices





[<StructLayout(LayoutKind.Sequential)>]
type RECT =
    struct
        val mutable Left: int
        val mutable Top: int
        val mutable Right: int
        val mutable Bottom: int
    end

type ShowWindowCmd =
    | Hide = 0
    | ShowNormal = 1
    | Minimize = 6
    | Restore = 9
    | Maximize = 3

type EnumWindowsProc = delegate of IntPtr * IntPtr -> bool

[<DllImport("user32.dll")>]
extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam)

[<DllImport("user32.dll")>]
extern bool IsWindowVisible(IntPtr hWnd)

[<DllImport("user32.dll")>]
extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount)

[<DllImport("user32.dll")>]
extern int GetWindowTextLength(IntPtr hWnd)

[<DllImport("user32.dll")>]
extern bool GetWindowRect(IntPtr hWnd, RECT& lpRect)

[<DllImport("user32.dll")>]
extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint)

[<DllImport("user32.dll")>]
extern bool ShowWindow(IntPtr hWnd, ShowWindowCmd nCmdShow)

[<DllImport("user32.dll")>]
extern IntPtr GetForegroundWindow()

[<DllImport("user32.dll")>]
extern bool IsZoomed(IntPtr hWnd)

let isWindowMaximized hWnd = IsZoomed(hWnd)

let getAllWindows () =
    let results = ResizeArray()

    let callback =
        EnumWindowsProc(fun hWnd _ ->
            if IsWindowVisible hWnd && GetWindowTextLength hWnd > 0 then
                let sb = StringBuilder 256

                if GetWindowText(hWnd, sb, sb.Capacity) > 0 then
                    results.Add(hWnd, sb.ToString())

            true)

    EnumWindows(callback, IntPtr.Zero) |> ignore
    results.ToArray()

let moveWindow hWnd (x, y, w, h) =
    MoveWindow(hWnd, x, y, w, h, true) |> ignore

let minimizeWindow hWnd =
    ShowWindow(hWnd, ShowWindowCmd.Minimize) |> ignore

let maximizeWindow hWnd =
    ShowWindow(hWnd, ShowWindowCmd.ShowNormal) |> ignore

let restoreWindow hWnd =
    ShowWindow(hWnd, ShowWindowCmd.Restore) |> ignore


// MONITOR -----------------------------------------------------------------------------------------------

[<StructLayout(LayoutKind.Sequential)>]
type POINT =
    struct
        val mutable X: int
        val mutable Y: int
    end

[<StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)>]
type MONITORINFO =
    struct
        val mutable cbSize: uint32
        val mutable rcMonitor: RECT
        val mutable rcWork: RECT
        val mutable dwFlags: uint32
    end

[<DllImport("user32.dll")>]
extern nativeint MonitorFromWindow(nativeint hwnd, uint32 dwFlags)

[<DllImport("user32.dll")>]
extern nativeint MonitorFromPoint(POINT pt, uint32 dwFlags)

[<DllImport("user32.dll")>]
extern bool GetMonitorInfo(nativeint hMonitor, MONITORINFO& lpmi)

[<DllImport("user32.dll")>]
extern bool GetCursorPos(POINT& lpPoint)

let getCursorMonitor () =
    let mutable pt = POINT()

    if GetCursorPos(&pt) then
        MonitorFromPoint(pt, 2u) // MONITOR_DEFAULTTONEAREST
    else
        IntPtr.Zero

let getWindowMonitor (hWnd: nativeint) = MonitorFromWindow(hWnd, 2u)

let isPrimaryMonitor (hMonitor: nativeint) =
    let mutable mi = MONITORINFO(cbSize = uint32 (Marshal.SizeOf<MONITORINFO>()))

    if GetMonitorInfo(hMonitor, &mi) then
        mi.dwFlags &&& 1u = 1u // MONITORINFOF_PRIMARY = 0x00000001
    else
        false

let isWindowOnPrimaryMonitor (hWnd: nativeint) =
    let MONITOR_DEFAULTTONEAREST = 2u
    let hMonitor = MonitorFromWindow(hWnd, MONITOR_DEFAULTTONEAREST)
    isPrimaryMonitor hMonitor

let getMonitorResolutionAndWorkArea (hMonitor: nativeint) =
    let mutable mi = MONITORINFO(cbSize = uint32 (Marshal.SizeOf<MONITORINFO>()))

    if GetMonitorInfo(hMonitor, &mi) then
        let fullWidth = mi.rcMonitor.Right - mi.rcMonitor.Left
        let fullHeight = mi.rcMonitor.Bottom - mi.rcMonitor.Top

        let workWidth = mi.rcWork.Right - mi.rcWork.Left
        let workHeight = mi.rcWork.Bottom - mi.rcWork.Top

        (fullWidth, fullHeight), (workWidth, workHeight)
    else
        (0, 0), (0, 0)

let getMonitorPosition (hMonitor: nativeint) =
    let mutable mi = MONITORINFO(cbSize = uint32 (Marshal.SizeOf<MONITORINFO>()))

    if GetMonitorInfo(hMonitor, &mi) then
        mi.rcMonitor.Left, mi.rcMonitor.Top
    else
        0, 0


let getWindowMonitorResolutionAndWorkArea (hWnd: nativeint) =
    let hMonitor = getWindowMonitor hWnd
    getMonitorResolutionAndWorkArea hMonitor

let isWindowFocused (hWnd: IntPtr) = GetForegroundWindow() = hWnd

let getWindowSizePos (hWnd: nativeint) =
    let mutable rect = RECT()

    if GetWindowRect(hWnd, &rect) then
        let width = rect.Right - rect.Left
        let height = rect.Bottom - rect.Top
        Some(rect.Left, rect.Top, width, height)
    else
        None
