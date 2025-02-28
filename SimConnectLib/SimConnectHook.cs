using CFIT.AppLogger;
using CFIT.AppTools;
using Microsoft.FlightSimulator.SimConnect;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace CFIT.SimConnectLib
{
    public class SimConnectHook(SimConnectManager manager, int msgSimConnect, int msgConnectRequest)
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr DefWindowProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);


        protected virtual SimConnectManager Manager { get; } = manager;
        protected virtual int MsgSimConnect { get; } = msgSimConnect;
        protected virtual int MsgConnectRequest { get; } = msgConnectRequest;
        public virtual bool IsHooked { get; protected set; }
        public virtual Window HelperWindow { get; set; } = null;
        public virtual IntPtr WindowHandle { get; set; } = IntPtr.Zero;

        public virtual Func<int, IntPtr, IntPtr, bool> HookCallback { get; set; } = null;
        public virtual Action<SimConnectHook> WindowShow { get; set; } = (hook) => { hook.HelperWindow.Show(); };
        public virtual Action<SimConnectHook> WindowHide { get; set; } = (hook) => { hook.HelperWindow.Hide(); };

        public virtual void SendConnectMessage()
        {
            Logger.Debug($"Send MsgConnectRequest ({MsgConnectRequest}) to WindowHandle");
            Sys.SendWindowMessage(WindowHandle, (uint)MsgConnectRequest, IntPtr.Zero, IntPtr.Zero);
        }

        public void CreateMainWindow()
        {
            if (WindowHandle == IntPtr.Zero)
            {
                HelperWindow = new Window()
                {
                    Width = 0,
                    Height = 0,
                    WindowStyle = WindowStyle.None,
                    ShowInTaskbar = false,
                    ShowActivated = false,
                    Visibility = Visibility.Collapsed,
                    ResizeMode = ResizeMode.NoResize,
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    Top = -10000,
                    Left = -10000,

                };
                Application.Current.MainWindow = HelperWindow;
                WindowShow.Invoke(this);
                WindowHide.Invoke(this);
                WindowHandle = new WindowInteropHelper(HelperWindow).Handle;
            }
            Logger.Debug($"Window Handle is: {WindowHandle}");
            HwndSource mainWindowSrc = HwndSource.FromHwnd(WindowHandle);
            mainWindowSrc.AddHook(WndProcHook);
            IsHooked = true;
        }

        public virtual void HookMainWindow()
        {
            if (WindowHandle == IntPtr.Zero)
            {
                HelperWindow = Application.Current.MainWindow;
                WindowHandle = new WindowInteropHelper(HelperWindow).Handle;
            }
            Logger.Debug($"Window Handle is: {WindowHandle}");
            HwndSource mainWindowSrc = HwndSource.FromHwnd(WindowHandle);
            mainWindowSrc.AddHook(WndProcHook);
            IsHooked = true;
        }

        public virtual void ClearHook()
        {
            if (WindowHandle != IntPtr.Zero)
            {
                HwndSource mainWindowSrc = HwndSource.FromHwnd(WindowHandle);
                mainWindowSrc.RemoveHook(WndProcHook);
            }

            HelperWindow = null;
            WindowHandle = IntPtr.Zero;
            IsHooked = false;
        }

        protected virtual IntPtr WndProcHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            var result = DefWindowProc(hwnd, msg, wParam, lParam).ToInt32();

            if (msg == MsgSimConnect)
            {
                try
                {
                    Logger.Verbose($"Received MsgSimConnect ({MsgSimConnect})");
                    Manager.ReceiveMessage();
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);
                }
                handled = true;
            }
            else if (msg == MsgConnectRequest)
            {
                try
                {
                    if (IsHooked)
                    {
                        Logger.Information($"Open SimConnect ...");
                        Manager.SetSimConnect(new SimConnect(Manager.Config.ClientName, WindowHandle, (uint)MsgSimConnect, null, 0));
                        Logger.Verbose($"SimConnect Object created");
                    }
                }
                catch (Exception ex)
                {
                    if (ex is not COMException)
                        Logger.LogException(ex);
                    else
                        Logger.Information($"COMException while opening SimConnect");
                }
                handled = true;
            }

            bool callbackResult = HookCallback?.Invoke(msg, wParam, lParam) ?? false;
            if (!handled && callbackResult)
                handled = true;

            return new IntPtr(result);
        }
    }
}
