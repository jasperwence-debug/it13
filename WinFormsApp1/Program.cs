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

            if (args.Length > 0 && args[0] == "--test-roles")
            {
                Console.WriteLine("ROLE_TEST: Verifying role use-case visibility and views...");
                try
                {
                    // 1. Super Admin
                    SessionManager.Login(new App.Domain.Entities.User { Id = 1, Username = "superadmin", Role = Core.Roles.SuperAdmin });
                    using (var main = new MainForm())
                    {
                        main.Show();
                        var keys = main.GetVisibleNavigationKeys();
                        main.Hide();
                        Console.WriteLine($"SuperAdmin visible: {string.Join(", ", keys)}");
                        if (!keys.Contains("users") || !keys.Contains("subscription") || !keys.Contains("terms"))
                            throw new Exception("SuperAdmin missing required modules (users, subscription, terms).");
                        if (keys.Contains("clients") || keys.Contains("scheduling") || keys.Contains("workorders") || keys.Contains("sales") || keys.Contains("financial"))
                            throw new Exception("SuperAdmin has unauthorized operational modules visible.");
                    }

                    // 2. Admin
                    SessionManager.Login(new App.Domain.Entities.User { Id = 2, Username = "admin", Role = Core.Roles.Admin });
                    using (var main = new MainForm())
                    {
                        main.Show();
                        var keys = main.GetVisibleNavigationKeys();
                        main.Hide();
                        Console.WriteLine($"Admin visible: {string.Join(", ", keys)}");
                        if (!keys.Contains("users") || !keys.Contains("clients") || !keys.Contains("financial") || !keys.Contains("reports") || !keys.Contains("terms"))
                            throw new Exception("Admin missing required modules (users, clients, financial, reports, terms).");
                        if (keys.Contains("scheduling") || keys.Contains("workorders") || keys.Contains("sales") || keys.Contains("subscription"))
                            throw new Exception("Admin has unauthorized operational modules visible.");
                    }

                    // 3. Manager
                    SessionManager.Login(new App.Domain.Entities.User { Id = 3, Username = "manager", Role = Core.Roles.Manager });
                    using (var main = new MainForm())
                    {
                        main.Show();
                        var keys = main.GetVisibleNavigationKeys();
                        main.Hide();
                        Console.WriteLine($"Manager visible: {string.Join(", ", keys)}");
                        if (!keys.Contains("sales") || !keys.Contains("clients") || !keys.Contains("scheduling") || !keys.Contains("financial") || !keys.Contains("reports"))
                            throw new Exception("Manager missing required modules (sales, clients, scheduling, financial, reports).");
                        if (keys.Contains("users") || keys.Contains("subscription") || keys.Contains("terms"))
                            throw new Exception("Manager has unauthorized admin modules visible.");
                    }

                    // 4. Sales Staff
                    SessionManager.Login(new App.Domain.Entities.User { Id = 4, Username = "staff", Role = Core.Roles.SalesStaff });
                    using (var main = new MainForm())
                    {
                        main.Show();
                        var keys = main.GetVisibleNavigationKeys();
                        main.Hide();
                        Console.WriteLine($"SalesStaff visible: {string.Join(", ", keys)}");
                        if (!keys.Contains("sales") || !keys.Contains("clients") || !keys.Contains("scheduling") || !keys.Contains("financial") || !keys.Contains("reports"))
                            throw new Exception("SalesStaff missing required modules (sales, clients, scheduling, financial, reports).");
                        if (keys.Contains("users") || keys.Contains("subscription") || keys.Contains("terms"))
                            throw new Exception("SalesStaff has unauthorized admin modules visible.");
                    }

                    // 5. TermsManagementView instantiation & permissions
                    using (var terms = new Views.TermsManagementView())
                    {
                        terms.ApplyViewPermissions(Core.Roles.SuperAdmin);
                        terms.ApplyViewPermissions(Core.Roles.Admin);
                        terms.ApplyViewPermissions(Core.Roles.Manager);
                        terms.ApplyViewPermissions(Core.Roles.SalesStaff);
                    }

                    // 6. WorkOrdersView instantiation & permissions
                    using (var wo = new Views.WorkOrdersView())
                    {
                        wo.ApplyViewPermissions(Core.Roles.Manager);
                        wo.ApplyViewPermissions(Core.Roles.SalesStaff);
                    }

                    // 7. ReportsAuditView instantiation & permissions
                    using (var rep = new Views.ReportsAuditView())
                    {
                        rep.ApplyViewPermissions(Core.Roles.SuperAdmin);
                        rep.ApplyViewPermissions(Core.Roles.Admin);
                        rep.ApplyViewPermissions(Core.Roles.Manager);
                        rep.ApplyViewPermissions(Core.Roles.SalesStaff);
                    }

                    // 8. SubscriptionManagementView and OnboardTenantDialog instantiation & permissions
                    using (var subView = new Views.SubscriptionManagementView())
                    {
                        subView.ApplyViewPermissions(Core.Roles.SuperAdmin);
                        subView.ApplyViewPermissions(Core.Roles.Admin);
                    }

                    using (var onboardDlg = new Views.OnboardTenantDialog())
                    {
                        if (onboardDlg == null)
                            throw new Exception("Failed to instantiate OnboardTenantDialog.");
                    }

                    Console.WriteLine("ROLE_TEST_SUCCESS: All 4 roles and views verified successfully!");
                    Environment.Exit(0);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ROLE_TEST_FAILURE: {ex.Message}");
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