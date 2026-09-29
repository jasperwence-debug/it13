using CRM.winforms.Forms;

namespace CRM.winforms
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            // ─── Global exception handlers ─────────────────────
            // 1. UI-thread exceptions
            Application.ThreadException += (_, e) => HandleException(e.Exception);

            // 2. Non-UI-thread exceptions (background tasks)
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                    HandleException(ex);
            };

            // 3. Task exceptions that are unobserved
            TaskScheduler.UnobservedTaskException += (_, e) =>
            {
                HandleException(e.Exception);
                e.SetObserved();  // prevent process termination
            };

            // Route UI-thread exceptions to our handler above
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            Application.Run(new LoginForm());
        }

        private static void HandleException(Exception ex)
        {
            try
            {
                // Log to disk (simple)
                var logPath = Path.Combine(
                    AppContext.BaseDirectory,
                    "error.log");
                File.AppendAllText(logPath,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex.GetType().Name}: {ex.Message}{Environment.NewLine}{ex.StackTrace}{Environment.NewLine}{Environment.NewLine}");

                // Show friendly dialog
                MessageBox.Show(
                    $"Something went wrong.\n\n" +
                    $"{ex.Message}\n\n" +
                    $"Details have been written to error.log next to the app.",
                    "Unexpected Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch
            {
                // If even logging fails, do nothing — don't crash further
            }
        }
    }
}