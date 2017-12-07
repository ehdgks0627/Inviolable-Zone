using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace WALLnutClient
{
    public class Utilitys
    {
        /// <summary>
        /// Bitmap을 BitmapImage로 변환 한다.
        /// </summary>
        /// <param name="bitmap"></param>
        /// <param name="imgFormat"></param>
        /// <param name="imgSize"></param>
        /// <returns></returns>
        public static BitmapImage BitMapToBitmapImage(Bitmap bitmap, ImageFormat imgFormat, System.Drawing.Size imgSize = new System.Drawing.Size())
        {
            Bitmap objBitmap;
            BitmapImage bitmapImage = new BitmapImage();
            using (var ms = new System.IO.MemoryStream())
            {
                if (imgSize.Width > 0 && imgSize.Height > 0)
                    objBitmap = new Bitmap(bitmap, new System.Drawing.Size(imgSize.Width, imgSize.Height));
                else
                    objBitmap = new Bitmap(bitmap, new System.Drawing.Size(bitmap.Width, bitmap.Height));
                objBitmap.Save(ms, imgFormat);

                bitmapImage.BeginInit();
                bitmapImage.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.UriSource = null;
                bitmapImage.DecodePixelWidth = imgSize.Width;
                bitmapImage.DecodePixelHeight = imgSize.Height;
                bitmapImage.StreamSource = ms;
                bitmapImage.EndInit();
                bitmapImage.Freeze();
            }
            return bitmapImage;
        }
    }
}
