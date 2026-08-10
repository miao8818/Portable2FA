using System;
using System.IO;
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
        private static void Main(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += ResolveEmbeddedDependency;
            bool startHidden = args != null && Array.Exists(args,
                delegate(string value)
                {
                    return value.Equals("--startup", StringComparison.OrdinalIgnoreCase);
                });

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
                using (MainForm form = new MainForm(startHidden))
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

        private static Assembly ResolveEmbeddedDependency(object sender, ResolveEventArgs args)
        {
            AssemblyName requested = new AssemblyName(args.Name);
            if (!requested.Name.Equals("zxing", StringComparison.OrdinalIgnoreCase))
                return null;

            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(
                "Portable2FA.Dependencies.zxing.dll"))
            {
                if (stream == null)
                    return null;

                byte[] data = new byte[stream.Length];
                int offset = 0;
                while (offset < data.Length)
                {
                    int read = stream.Read(data, offset, data.Length - offset);
                    if (read == 0)
                        break;
                    offset += read;
                }
                return Assembly.Load(data);
            }
        }
    }
}
