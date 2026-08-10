using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

[assembly: AssemblyTitle("Portable 2FA")]
[assembly: AssemblyDescription("Fast, portable TOTP code generator")]
[assembly: AssemblyCompany("Portable 2FA")]
[assembly: AssemblyProduct("Portable 2FA")]
[assembly: AssemblyCopyright("Copyright 2026")]
[assembly: ComVisible(false)]

namespace Portable2FA
{
    internal static class Program
    {
        private const string MutexName = "Local\\Portable2FA_3BEAB9D0_6DE2_4D89_92A5_33C38B9E7D31";
        private const string EventName = "Local\\Portable2FA_Activate_3BEAB9D0_6DE2_4D89_92A5_33C38B9E7D31";

        [STAThread]
        private static void Main()
        {
            bool ownsMutex;
            using (Mutex mutex = new Mutex(true, MutexName, out ownsMutex))
            {
                if (!ownsMutex)
                {
                    SignalRunningInstance();
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

                using (EventWaitHandle activation = new EventWaitHandle(
                    false, EventResetMode.AutoReset, EventName))
                using (MainForm form = new MainForm())
                {
                    RegisteredWaitHandle waiter = ThreadPool.RegisterWaitForSingleObject(
                        activation,
                        delegate
                        {
                            if (!form.IsDisposed && form.IsHandleCreated)
                            {
                                try { form.BeginInvoke(new Action(form.RestoreFromTray)); }
                                catch (InvalidOperationException) { }
                            }
                        },
                        null,
                        Timeout.Infinite,
                        false);

                    Application.Run(form);
                    waiter.Unregister(null);
                }
            }
        }

        private static void SignalRunningInstance()
        {
            try
            {
                using (EventWaitHandle activation = EventWaitHandle.OpenExisting(EventName))
                    activation.Set();
            }
            catch (WaitHandleCannotBeOpenedException) { }
        }
    }
}
