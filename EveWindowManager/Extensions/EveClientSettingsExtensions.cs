using System.Diagnostics;
using EveWindowManager.Store;
using static EveWindowManager.Windows.WindowHelper;

namespace EveWindowManager.Extensions
{
    public static class EveClientSettingsExtensions
    {
        public static EveClientSetting ToEveClientSetting(this Process process)
        {
            var rect = new Rect();
            GetWindowRect(process.MainWindowHandle, ref rect);

            return new EveClientSetting
            {
                ProcessTitle = process.MainWindowTitle,
                Height = rect.Bottom - rect.Top,
                Width = rect.Right - rect.Left, 
                PositionX = rect.Left,
                PositionY = rect.Top
            };
        }
    }
}