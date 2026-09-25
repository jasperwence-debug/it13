using System;
using System.Windows.Forms;
using App.WinForms.Views;

namespace App.WinForms
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            ApplicationConfiguration.Initialize();

            if (args.Length > 0 && args[0] == "--test-printing")
            {
                Console.WriteLine("PRINT_TEST: Verifying ReportDocumentEngine rendering...");
                bool ok = App.WinForms.Reporting.ReportDocumentEngine.VerifyDocumentRendering(out string err);
                if (ok)
                {
                    Console.WriteLine("PRINT_TEST_SUCCESS: All 3 document reports rendered with 0 errors!");
                    Environment.Exit(0);
                }
                else
                {
                    Console.WriteLine($"PRINT_TEST_FAILURE: {err}");
                    Environment.Exit(1);
                }
                return;
            }

            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) =>
            {
                var msg = $"THREAD EXCEPTION: {e.Exception}\nStackTrace:\n{e.Exception.StackTrace}";
                Console.WriteLine(msg);
                try { System.IO.File.WriteAllText("crash.log", msg); } catch { }
                MessageBox.Show(e.Exception.Message + "\n" + e.Exception.StackTrace, "Unhandled Exception", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                var msg = $"UNHANDLED EXCEPTION: {e.ExceptionObject}";
                Console.WriteLine(msg);
                try { System.IO.File.WriteAllText("crash.log", msg); } catch { }
            };

            EnsureApiRunning();

            Application.ApplicationExit += (s, e) =>
            {
                try
                {
                    if (_apiProcess != null && !_apiProcess.HasExited)
                    {
                        _apiProcess.Kill();
                    }
                }
                catch { }
            };

            while (true)
            {
                using var loginView = new LoginView();

                if (loginView.ShowDialog() != DialogResult.OK)
                {
                    break;
                }

                using var mainForm = new MainForm();
                Application.Run(mainForm);

                if (!mainForm.IsLoggedOut)
                {
                    break;
                }
            }
        }

        private static System.Diagnostics.Process? _apiProcess;

        private static void EnsureApiRunning()
        {
            try
            {
                using (var tcp = new System.Net.Sockets.TcpClient())
                {
                    var ar = tcp.BeginConnect("127.0.0.1", 5000, null, null);
                    if (ar.AsyncWaitHandle.WaitOne(400))
                    {
                        tcp.EndConnect(ar);
                        return;
                    }
                }
            }
            catch { }

            try
            {
                var existing = System.Diagnostics.Process.GetProcessesByName("App.API");
                if (existing.Length > 0) return;

                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string[] candidates = new[]
                {
                    System.IO.Path.Combine(baseDir, "..", "..", "..", "..", "App.API", "bin", "Debug", "net10.0", "App.API.exe"),
                    System.IO.Path.Combine(baseDir, "..", "App.API", "App.API.exe"),
                    System.IO.Path.Combine(baseDir, "App.API.exe")
                };

                foreach (var path in candidates)
                {
                    var full = System.IO.Path.GetFullPath(path);
                    if (System.IO.File.Exists(full))
                    {
                        var psi = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = full,
                            WorkingDirectory = System.IO.Path.GetDirectoryName(full)!,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };
                        _apiProcess = System.Diagnostics.Process.Start(psi);
                        System.Threading.Thread.Sleep(1200);
                        break;
                    }
                }
            }
            catch { }
        }
    }
}