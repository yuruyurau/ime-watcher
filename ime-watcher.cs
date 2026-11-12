using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    [DllImport("user32.dll")]
    static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, uint processId);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern bool QueryFullProcessImageName(IntPtr hProcess, uint dwFlags, StringBuilder lpExeName, ref uint lpdwSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool CloseHandle(IntPtr hObject);

    [DllImport("imm32.dll")]
    static extern IntPtr ImmGetDefaultIMEWnd(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam,
        uint fuFlags, uint uTimeout, out IntPtr lpdwResult);

    const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
    const uint WM_IME_CONTROL = 0x0283;
    const int IMC_GETOPENSTATUS = 0x0005;
    const uint SMTO_ABORTIFHUNG = 0x0002;

    static int last = -1;
    static IntPtr lastHwnd = IntPtr.Zero;
    static bool lastHwndIsZebar = false;
    static StringBuilder pathBuffer = new StringBuilder(260);

    static void Main()
    {
        // すでに動いている古い同名プロセス（ime-watcher）があれば終了させる（リロード対策）
        var current = Process.GetCurrentProcess();
        foreach (var proc in Process.GetProcessesByName(current.ProcessName))
        {
            if (proc.Id != current.Id)
            {
                try
                {
                    proc.Kill();
                    proc.WaitForExit(500);
                }
                catch
                {
                    // すでに終了している場合などは無視
                }
            }
        }

        // 親プロセス（Zebar本体）終了時に一緒に終了する
        Task.Run(() =>
        {
            while (Console.ReadLine() != null) { }
            Environment.Exit(0);
        });

        Console.OutputEncoding = Encoding.UTF8;

        while (true)
        {
            try
            {
                IntPtr hwnd = GetForegroundWindow();
                if (hwnd == IntPtr.Zero)
                {
                    Thread.Sleep(200);
                    continue;
                }

                // ウィンドウが切り替わったときだけ Zebar かどうか判定する
                if (hwnd != lastHwnd)
                {
                    lastHwnd = hwnd;
                    lastHwndIsZebar = CheckIsZebar(hwnd);
                }

                // Zebar自身がアクティブなときは状態を更新しない（直前のIME状態を維持）
                if (lastHwndIsZebar)
                {
                    Thread.Sleep(200);
                    continue;
                }

                int ime = GetImeState(hwnd);
                if (ime != last)
                {
                    last = ime;
                    Console.WriteLine("{\"ime\":" + ime + "}");
                    Console.Out.Flush();
                }
            }
            catch
            {
                // 一時的なエラーは無視
            }

            Thread.Sleep(200);
        }
    }

    static bool CheckIsZebar(IntPtr hwnd)
    {
        uint pid;
        GetWindowThreadProcessId(hwnd, out pid);
        if (pid == 0) return false;

        IntPtr hProc = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
        if (hProc == IntPtr.Zero) return false;

        try
        {
            pathBuffer.Clear();
            uint size = (uint)pathBuffer.Capacity;
            if (QueryFullProcessImageName(hProc, 0, pathBuffer, ref size))
            {
                string path = pathBuffer.ToString();
                return path.IndexOf("zebar", StringComparison.OrdinalIgnoreCase) >= 0;
            }
        }
        finally
        {
            CloseHandle(hProc);
        }

        return false;
    }

    static int GetImeState(IntPtr hwnd)
    {
        IntPtr imeWnd = ImmGetDefaultIMEWnd(hwnd);
        if (imeWnd == IntPtr.Zero) return 0;

        IntPtr result;
        SendMessageTimeout(imeWnd, WM_IME_CONTROL, (IntPtr)IMC_GETOPENSTATUS, IntPtr.Zero,
            SMTO_ABORTIFHUNG, 100, out result);

        return result != IntPtr.Zero ? 1 : 0;
    }
}
