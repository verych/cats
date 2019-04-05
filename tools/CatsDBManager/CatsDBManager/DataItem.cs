using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace CatsDBManager
{
    [Serializable]
    public class DataItem
    {
        [NonSerialized]
        private Dictionary<string, Bitmap> imageCache = new Dictionary<string, Bitmap>();


        public string path;
        public string name;
        public string breed;
        public string source;

        public List<RectangleF> regions = new List<RectangleF>();
        public List<RectangleF> regionsBack = new List<RectangleF>();

        public DataItem(string path, string breed, string source)
        {
            this.path = path;
            this.name = System.IO.Path.GetFileName(path);
            this.breed = breed;
            this.source = source;
        }

        public Bitmap GetImageBitmap(int size, bool cropMinimum = false)
        {
            var image = new Bitmap(path);
            return ImageHelper.CropImageCenterAndResize(image, size, cropMinimum);
        }

        public Bitmap GetImageBitmapFromCache(int size, bool cropMinimum = false)
        {
            var key = GetCacheKey(size, cropMinimum);
            if (imageCache.ContainsKey(key))
            {
                return new Bitmap(imageCache[key]);
            }

            var image = GetImageBitmap(size, cropMinimum);
            imageCache.Add(key, image);
            return image;
        }

        public Bitmap GetImageBitmap(bool cropMinimum = false)
        {
            var image = new Bitmap(path);
            var size = Math.Max(image.Width, image.Height);
            return ImageHelper.CropImageCenterAndResize(image, size, cropMinimum);
        }

        public Bitmap GetImageBitmapFromCache(bool cropMinimum = false)
        {
            //default size is 1000000
            Bitmap image;
            var key = GetCacheKey(0, cropMinimum);
            if (imageCache.ContainsKey(key))
            {
                image = new Bitmap(imageCache[key]);
                return image;
            }
            else
            {
                image = new Bitmap(path);
                var size = Math.Max(image.Width, image.Height);
                image = ImageHelper.CropImageCenterAndResize(image, size, cropMinimum);
                imageCache.Add(key, image);
            }
            return image;
        }

        public ImageList GetOutputImages(int size, bool cropMinimum = false, int rotationStep = 15)
        {
            var result = new ImageList();
            result.ImageSize = new Size(size, size);
            result.ColorDepth = ColorDepth.Depth32Bit;
            //main image
            var main = GetImageBitmapFromCache(size, cropMinimum);
            result.Images.Add(main);

            Image[] rotated = GetRotatedImages(main, rotationStep);
            result.Images.AddRange(rotated);

            var regions = GetRegionImages(size, cropMinimum);
            foreach (Image region in regions.Images)
            {
                var rotatedRegions = GetRotatedImages(region, rotationStep);
                result.Images.AddRange(rotatedRegions);
            }

            return result;
        }
        private Image[] GetRotatedImages(Image image, int rotationStep)
        {
            return GetRotatedImages((Bitmap)image, rotationStep);
        }
        private Image[] GetRotatedImages(Bitmap image, int rotationStep)
        {
            List<Image> result = new List<Image>();

            for (int a = 0; a < 360; a += rotationStep)
            {
                result.Add(ImageHelper.RotateAndFitImage(image, a));
            }
            return result.ToArray();
        }

        public ImageList GetOutputBackImages(int size, bool cropMinimum = false, int rotationStep = 15)
        {
            var result = new ImageList();
            result.ImageSize = new Size(size, size);
            result.ColorDepth = ColorDepth.Depth32Bit;

            var regions = GetRegionBackImages(size, cropMinimum);
            foreach (Image region in regions.Images)
            {

                var rotatedRegions = GetRotatedImages(region, rotationStep);
                result.Images.AddRange(rotatedRegions);
            }
            return result;
        }

        public RectangleF AddRegion(RectangleF rectangle, bool background = false)
        {
            if (background)
            {
                regionsBack.Add(rectangle);
            }
            else
            {
                regions.Add(rectangle);
            }

            return rectangle;
        }

        public ImageList GetRegionImages(int size, bool cropMinimum = false, int rotationStep = 0 /*not implemented*/)
        {
            var result = new ImageList();
            result.ImageSize = new Size(size, size);
            result.ColorDepth = ColorDepth.Depth32Bit;
            var image = GetImageBitmap(cropMinimum);

            //validation
            for (var i = regions.Count - 1; i >= 0; i--)
            {
                var region = regions[i];
                if (region.Width == 0 || region.Height == 0)
                {
                    //remove wrong region
                    regions.Remove(region);

                }
            }

            foreach (var region in regions)
            {
                var regionImage = GetRegion(size, cropMinimum, region);
                result.Images.Add(regionImage);
            }

            return result;
        }

        public ImageList GetRegionBackImages(int size, bool cropMinimum = false, int rotationStep = 0 /*not implemented*/)
        {
            var result = new ImageList();
            result.ImageSize = new Size(size, size);
            result.ColorDepth = ColorDepth.Depth32Bit;

            //validation
            for (var i = regionsBack.Count - 1; i >= 0; i--)
            {
                var region = regionsBack[i];
                if (region.Width == 0 || region.Height == 0)
                {
                    //remove wrong region
                    regionsBack.Remove(region);
                }
            }

            foreach (var region in regionsBack)
            {
                var regionImage = GetRegion(size, cropMinimum, region);
                result.Images.Add(regionImage);
            }

            return result;
        }

        private Bitmap GetRegionFromCache(int size, bool cropMinimum, RectangleF region)
        {
            var key = GetCacheKey(size, cropMinimum, region);
            if (imageCache.ContainsKey(key))
            {
                return imageCache[key];
            }
            var image = GetImageBitmapFromCache(cropMinimum);
            var regionImage = ImageHelper.CropImage(image, ImageHelper.TranslatePercentsToSize(region, image.Width));
            regionImage = ImageHelper.CropImageCenterAndResize(regionImage, size, cropMinimum);

            imageCache.Add(key, regionImage);
            return regionImage;
        }

        private Bitmap GetRegion(int size, bool cropMinimum, RectangleF region)
        {
            var image = GetImageBitmapFromCache(cropMinimum);
            var regionImage = ImageHelper.CropImage(image, ImageHelper.TranslatePercentsToSize(region, image.Width));
            regionImage = ImageHelper.CropImageCenterAndResize(regionImage, size, cropMinimum);
            return regionImage;
        }

        private string GetCacheKey(int size, bool cropMinimum, RectangleF? region = null)
        {
            var result = String.Format("size{0}crop{1}", size, cropMinimum);

            if (region.HasValue)
            {
                result = String.Format("{0}region{1}", result, region);
            }
            return result;
        }

        public bool DeleteRegion(string index, string source)
        {
            int result = 0;
            if (int.TryParse(index, out result))
            {
                if (source == "region")
                {
                    regions.RemoveAt(result);
                }
                if (source == "back")
                {

                    regionsBack.RemoveAt(result - regions.Count);
                }
                return true;
            }
            return false;
        }

        public void ClearCache()
        {
            imageCache.Clear();
        }
    }
}