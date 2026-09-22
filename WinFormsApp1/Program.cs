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
    }
}