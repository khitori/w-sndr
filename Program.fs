open WindowInterop
open System
open System.IO
open System.Runtime.InteropServices
open System.Diagnostics
open System.Reflection



let hotkeyUseEvent () =
    let cursorMon = getCursorMonitor ()
    let isPrimary = isPrimaryMonitor cursorMon
    printfn "Cursor on %s monitor" (if isPrimary then "main" else "other")

    let windows = getAllWindows ()

    windows
    |> Array.iter (fun (hWnd, _) ->
        if isWindowFocused hWnd then
            let winW, winH =
                match getWindowSizePos hWnd with
                | Some(_, _, w, h) -> w, h
                | None -> 0, 0

            let hMonitor = getWindowMonitor hWnd
            let x, y = getMonitorPosition hMonitor
            let (rwW, rwH), (resW, resH) = getWindowMonitorResolutionAndWorkArea hWnd

            // if (winW, winH) = (resW, resH) || isWindowMaximized hWnd then
            //     moveWindow hWnd (x, y, winW / 2, winH / 2)
            //     printfn "-size"
            // else
            //     moveWindow hWnd (x, y, resW, resH)
            //     printfn "+size")

            if isWindowMaximized hWnd then
                restoreWindow hWnd
            else
                maximizeWindow hWnd)


// match primaryWins.Count with
// | 1 ->
// | 2 ->
// | 3 ->
// | 4 ->
// | 5 ->
// | 6 ->

// let (resolution, _) = getMonitorResolutionAndWorkArea hMonitor
// let (_, _, windowResW, windowResH) = getWindowSizePos hWnd

// setWindowResOnly hWnd (w, h) = match getWindowSizePos hWnd with
//     | Some(x, y, _, _) -> moveWindow hWnd (x, y, w, h)
//     | None -> ()



// primaryWins |> Array.iter (fun (hWnd) ->
//     if isWindowFocused hWnd && not resolution = (windowResW, windowResH)
//         setWindowResOnly hWnd (resolution))



module NativeMethods =
    [<DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)>]
    extern IntPtr SetWindowsHookEx(int idHook, IntPtr lpfn, IntPtr hMod, uint dwThreadId)

    [<DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)>]
    extern bool UnhookWindowsHookEx(IntPtr hhk)

    [<DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)>]
    extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam)

    [<DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)>]
    extern int GetMessage(IntPtr& lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax)

    [<DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)>]
    extern bool TranslateMessage(IntPtr& lpMsg)

    [<DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)>]
    extern bool DispatchMessage(IntPtr& lpMsg)

    [<DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)>]
    extern IntPtr GetModuleHandle(string lpModuleName)

let WH_KEYBOARD_LL = 13
let WM_KEYDOWN = 0x0100
let WM_KEYUP = 0x0101
let WM_SYSKEYDOWN = 0x0104 // SysKeys like Alt
let WM_SYSKEYUP = 0x0105
let VK_LMENU = 0xA4 // Left Alt
let VK_RMENU = 0xA5 // Right Alt
let VK_X = 0x58 // X Key

type LowLevelKeyboardProc = delegate of int * IntPtr * IntPtr -> IntPtr

let mutable isAltPressed = false

let hookCallback (nCode: int) (wParam: IntPtr) (lParam: IntPtr) : IntPtr =
    if nCode >= 0 then
        let vkCode = Marshal.ReadInt32 lParam
        //printfn "nCode: %d, wParam: %A, vkCode: %X" nCode wParam vkCode

        if wParam = IntPtr WM_KEYDOWN || wParam = IntPtr WM_SYSKEYDOWN then
            if vkCode = VK_LMENU || vkCode = VK_RMENU then
                isAltPressed <- true
            //printfn "Alt pressed, isAltPressed: %b" isAltPressed
            elif vkCode = VK_X && isAltPressed then
                hotkeyUseEvent ()
        //printfn "Hotkey Alt+X pressed"
        elif wParam = IntPtr WM_KEYUP || wParam = IntPtr WM_SYSKEYUP then
            if vkCode = VK_LMENU || vkCode = VK_RMENU then
                isAltPressed <- false
    //printfn "Alt released, isAltPressed: %b" isAltPressed

    NativeMethods.CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam)

let setHook (proc: LowLevelKeyboardProc) =
    let hMod = NativeMethods.GetModuleHandle null
    NativeMethods.SetWindowsHookEx(WH_KEYBOARD_LL, Marshal.GetFunctionPointerForDelegate proc, hMod, 0u)

let runMessageLoop () =
    let mutable msg = IntPtr.Zero

    while NativeMethods.GetMessage(&msg, IntPtr.Zero, 0u, 0u) <> 0 do
        NativeMethods.TranslateMessage(&msg) |> ignore
        NativeMethods.DispatchMessage(&msg) |> ignore





[<EntryPoint>]
let main _ =

    let hookDelegate = LowLevelKeyboardProc hookCallback
    let hookId = setHook hookDelegate



    let windows = getAllWindows ()
    printfn "Найдено %d окон" windows.Length

    windows |> Array.iteri (fun i (_, title) -> printfn "[%d] %s" i title)

    if windows.Length > 0 then
        let hWnd, title = windows[0]
        printfn "\nРаботаем с: %s\n" title
    //moveWindow hWnd (100, 100, 800, 600)
    //minimizeWindow hWnd
    //System.Threading.Thread.Sleep 1000
    //restoreWindow hWnd

    // let cursorMon = getCursorMonitor ()
    // let isPrimary = isPrimaryMonitor cursorMon
    // printfn "Cursor on %s monitor" (if isPrimary then "main" else "other")

    // let hwnd, _ = getAllWindows()[0]
    // let winMon = getWindowMonitor hwnd
    // printfn "Window on %s monitor" (if isPrimaryMonitor winMon then "main" else "other")



    if hookId = IntPtr.Zero then
        printfn "Failed to set hook"
        1
    else
        runMessageLoop ()
        NativeMethods.UnhookWindowsHookEx hookId |> ignore
        0
