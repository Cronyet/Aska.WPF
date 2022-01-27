using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aska.WPF.Helper
{
    public class GetDesktopBackground
    {
        #region 获取windows桌面背景API
        [System.Runtime.InteropServices.DllImport("user32.dll",
CharSet = System.Runtime.InteropServices.CharSet.Unicode,
            SetLastError = true)]
        private static extern int SystemParametersInfo(int uAction, int uParam,
            StringBuilder lpvParam, int fuWinIni);
        private const int SPI_GETDESKWALLPAPER = 0x0073;
        #endregion

        /// <summary>
        /// 获取桌面背景图片路径
        /// </summary>
        /// <returns>路径</returns>
        public static string GetDesktopBackgroundPath()
        {
            StringBuilder s = new(300);
            _ = SystemParametersInfo(SPI_GETDESKWALLPAPER, 300, s, 0);
            return s.ToString();
        }
    }
}
