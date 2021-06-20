﻿using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Helper
{
    public class MouseHelper
    {
        [DllImport("user32.dll", EntryPoint = "SetCursorPos")]
        private static extern bool SetCursorPos(int x, int y);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string name);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hook, int nCode, IntPtr wp, IntPtr lp);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hook);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
        private static LowLevelMouseProc process = HookCallback;
        private static IntPtr hookId = IntPtr.Zero;

        private static bool IsVisible;
        public static event EventHandler<MouseEventArgs> MouseActivity;

        private const int WH_MOUSE_LL = 14;

        private enum MouseMessages
        {
            WM_LBUTTONDOWN = 0x0201,
            WM_LBUTTONUP = 0x0202,
            WM_MOUSEMOVE = 0x0200,
            WM_MOUSEWHEEL = 0x020A,
            WM_RBUTTONDOWN = 0x0204,
            WM_LBUTTONDBLCLK = 0x0203,
            WM_RBUTTONUP = 0x0205
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        private static IntPtr Hook(LowLevelMouseProc process)
        {
            using (Process currentProcess = Process.GetCurrentProcess())
            using (ProcessModule currentModule = currentProcess.MainModule)
            {
                return SetWindowsHookEx(WH_MOUSE_LL, process, GetModuleHandle(currentModule.ModuleName), 0);
            }
        }
        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if(nCode >= 0)
            {
                if (IsVisible)
                {
                    MouseButtons button = MouseButtons.None;
                    MSLLHOOKSTRUCT hookStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
                    short mouseDelta = 0;
                    switch ((MouseMessages)wParam)
                    {
                        case MouseMessages.WM_LBUTTONDOWN:
                            button = MouseButtons.Left;
                            break;
                        case MouseMessages.WM_RBUTTONDOWN:
                            button = MouseButtons.Right;
                            break;
                        case MouseMessages.WM_MOUSEWHEEL:
                            mouseDelta = (short)((hookStruct.mouseData >> 16) & 0xffff);
                            break;
                    }
                    int clickCount = 0;
                    if (button != MouseButtons.None)
                        if ((MouseMessages)wParam == MouseMessages.WM_LBUTTONDBLCLK) clickCount = 2;
                        else clickCount = 1;
                    MouseActivity(null, new MouseEventArgs(button,clickCount,hookStruct.pt.x, hookStruct.pt.y, mouseDelta));
                }
                else return (IntPtr)1;
            }
            return CallNextHookEx(hookId, nCode, wParam, lParam);
        }
        public IntPtr MouseHook(int left, int top)
        {
            SetCursorPos(left, top);
            hookId = Hook(process);
        }
        public static void Start(bool isVisible = true, int left=0, int top=0)
        {
            IsVisible = isVisible;
            if (!IsVisible) SetCursorPos(left, top);
            Hook(process);
        }
        public void UnHook()
        {
            UnhookWindowsHookEx(hookId);
        }
    }
}
