using System;
using System.Windows.Forms;
using App.WinForms.Views;

namespace App.WinForms
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            while (true)
            {
                using var loginForm = new LoginForm();
                if (loginForm.ShowDialog() != DialogResult.OK || SessionManager.CurrentUser == null)
                {
                    break;
                }

                var mainForm = new MainForm();
                Application.Run(mainForm);

                // If user logged out (SessionManager.CurrentUser == null), loop and show login form again.
                // Otherwise (user closed the window), exit the application.
                if (SessionManager.CurrentUser != null)
                {
                    break;
                }
            }
        }
    }
}