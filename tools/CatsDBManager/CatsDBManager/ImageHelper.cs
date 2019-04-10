using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CatsDBManager
{
    public static class ImageHelper
    {
        /// <summary>
        /// Resize the image to the specified width and height.
        /// </summary>
        /// <param name="image">The image to resize.</param>
        /// <param name="width">The width to resize to.</param>
        /// <param name="height">The height to resize to.</param>
        /// <returns>The resized image.</returns>
        public static Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.Tile);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }

        public static Bitmap CropImage(Image image, Rectangle cropArea)
        {
            Bitmap bmpImage = new Bitmap(image);
            return bmpImage.Clone(cropArea, bmpImage.PixelFormat);
        }

        public static Bitmap CropImageCenter(Image image, bool cropMinimum = false) 
        {
            if (cropMinimum)
            {
                var size = Math.Max(image.Width, image.Height);
                var cropArea = new Rectangle(0, 0, image.Width, image.Height);
                Bitmap bmpImage = new Bitmap(image, size, size);
                using (Graphics graph = Graphics.FromImage(bmpImage))
                {
                    Rectangle ImageSize = new Rectangle(0, 0, size, size);
                    graph.FillRectangle(Brushes.White, ImageSize);
                    graph.DrawImage(image, cropArea, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, new ImageAttributes());

                    if (image.Width > image.Height)
                    {
                        var mirrorImage = new Bitmap(image);
                        mirrorImage.RotateFlip(RotateFlipType.RotateNoneFlipY);
                        var mirrorArea = new Rectangle(0, image.Height, image.Width, size - image.Height);

                        graph.DrawImage(mirrorImage, mirrorArea, 0, 0, image.Width, size - image.Height, GraphicsUnit.Pixel, new ImageAttributes());
                    }
                    if (image.Width < image.Height)
                    {
                        var mirrorImage = new Bitmap(image);
                        mirrorImage.RotateFlip(RotateFlipType.RotateNoneFlipX);
                        var mirrorArea = new Rectangle(image.Width, 0, size - image.Width, image.Height);

                        graph.DrawImage(mirrorImage, mirrorArea, 0, 0, size - image.Width, image.Height, GraphicsUnit.Pixel, new ImageAttributes());
                    }
                }
                return bmpImage;
            }
            else
            {
                var size = Math.Min(image.Width, image.Height);
                var x = 0;
                var y = 0;
                var w = size;
                var h = size;

                if (image.Width > image.Height)
                {
                    x = (image.Width - size) / 2;
                }
                else
                {
                    y = (image.Height - size) / 2;
                }

                var cropArea = new Rectangle(x, y, w, h);
                Bitmap bmpImage = new Bitmap(image);
                return bmpImage.Clone(cropArea, bmpImage.PixelFormat);
            }
        }

        public static Bitmap CropImageCenterAndResize(Image image, int size, bool cropMinimum = false)
        {
            var cropped = CropImageCenter(image, cropMinimum);
            var resized = ResizeImage(cropped, size, size);
            return resized;
        }

        public static Bitmap RotateImage(Bitmap image, float angle)
        {
            Bitmap rotatedImage = new Bitmap(image.Width, image.Height);
            using (Graphics g = Graphics.FromImage(rotatedImage))
            {
                // Set the rotation point to the center in the matrix
                g.TranslateTransform(image.Width / 2, image.Height / 2);
                // Rotate
                g.RotateTransform(angle);
                // Restore rotation point in the matrix
                g.TranslateTransform(-image.Width / 2, -image.Height / 2);
                // Draw the image on the bitmap
                g.DrawImage(image, new Point(0, 0));
            }
            return rotatedImage;
        }

        public static Bitmap RotateAndFitImage(Bitmap image, int a)
        {
            var r = (int)(image.Width / 2 * Math.Sqrt(2));
            var rotated = RotateImage(image, a);
            var d = (int)(image.Width - r) / 2;
            var cropped = CropImage(rotated, new Rectangle(d, d, r, r));
            return ResizeImage(cropped, image.Width, image.Height);
        }

        public static Bitmap AddRegion(Bitmap image, RectangleF rect)
        {
            Bitmap newImage = new Bitmap(image.Width, image.Height);
            using (Graphics g = Graphics.FromImage(newImage))
            {
                g.DrawImage(image, new Point(0, 0));
                Pen pen = new Pen(Color.Yellow);
                g.DrawRectangle(pen, TranslatePercentsToSize(rect, image.Width));
            }
            return newImage;
        }

        public static Rectangle TranslatePercentsToSize(RectangleF rect, int size)
        {
            return new Rectangle((int) (rect.X * size), (int) (rect.Y * size), (int) (rect.Width * size), (int) (rect.Height * size));
        }

        public static Bitmap FitImage(Bitmap bitmap, Size size)
        {
            var newSize = new Size();
            if (bitmap.Width > bitmap.Height)
            {
                double k = (double)bitmap.Width / size.Width;
                newSize.Width = size.Width;
                newSize.Height = (int)Math.Round(bitmap.Height / k);
            }
            else
            {
                double k = (double)bitmap.Height / size.Height;
                newSize.Height = size.Height;
                newSize.Width = (int)Math.Round(bitmap.Width / k);
            }
            var resized = new Bitmap(bitmap, newSize);
            var blank = new Bitmap(size.Width, size.Height);
            CopyRegionIntoImage(resized, new Rectangle(new Point(0, 0), size), ref blank, new Rectangle(new Point(0, 0), size));
            return blank;
        }

        public static void CopyRegionIntoImage(Bitmap srcBitmap, Rectangle srcRegion, ref Bitmap destBitmap, Rectangle destRegion)
        {
            using (Graphics grD = Graphics.FromImage(destBitmap))
            {
                grD.DrawImage(srcBitmap, destRegion, srcRegion, GraphicsUnit.Pixel);
            }
        }
    }
}
