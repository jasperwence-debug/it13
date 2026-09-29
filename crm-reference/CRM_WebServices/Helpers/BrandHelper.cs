using System.Drawing;

namespace CRM.winforms.Helpers
{
    public static class BrandHelper
    {
        public const string ProductName = "KonSpot";
        public const string ProductTagline = "CRM SYSTEM";
        public const string ProductFooter = "© 2026 KonSpot · Role-based access control";

        private static Image? _cachedLogo;

        /// <summary>Loads the app logo from Resources/Images/logo.png (cached).</summary>
        public static Image? LoadLogo()
        {
            if (_cachedLogo != null) return _cachedLogo;

            var path = Path.Combine(
                AppContext.BaseDirectory,
                "Resources", "Images", "logo.png");

            if (!File.Exists(path)) return null;

            try
            {
                // Load without locking the file on disk
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
                _cachedLogo = Image.FromStream(stream);
                return _cachedLogo;
            }
            catch
            {
                return null;
            }
        }
    }
}