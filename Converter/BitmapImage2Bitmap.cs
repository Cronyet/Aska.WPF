using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;

namespace Aska.WPF.Converter
{
    public class BitmapImage2Bitmap
    {
        /// <summary>
        /// 转换 BitmapImage 到 Bitmap
        /// </summary>
        /// <param name="bitmapImage">BitmapImage</param>
        /// <returns>Bitmap</returns>
        public static Bitmap BitmapImageToBitmap(BitmapImage bitmapImage)
        {
            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapImage));
                enc.Save(outStream);
                Bitmap bitmap = new Bitmap(outStream);
                return new Bitmap(bitmap);
            }
        }
    }
}
