using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace CatsDBManager
{
    [Serializable]
    public class DataItem
    {
        public string path;
        public string name;
        public string breed;
        public string source;

        public List<Rectangle> regions = new List<Rectangle>();

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

        public ImageList GetOutputImages(int size, bool cropMinimum = false, int rotationStep = 15)
        {
            var result = new ImageList();
            result.ImageSize = new Size(size, size);
            result.ColorDepth = ColorDepth.Depth16Bit;
            //main image
            var main = GetImageBitmap(size, cropMinimum);
            result.Images.Add(main);
            //rotation
            for (int a = 0; a < 360; a += rotationStep)
            {
                result.Images.Add(ImageHelper.RotateAndFitImage(main, a));
            }
            return result;
        }

        public Rectangle AddRegion(Rectangle rectangle)
        {
            regions.Add(rectangle);
            return rectangle;
        }

        public ImageList GetRegionImages(int size, bool cropMinimum = false, int rotationStep = 0 /*not implemented*/)
        {
            var result = new ImageList();
            result.ImageSize = new Size(size, size);
            result.ColorDepth = ColorDepth.Depth16Bit;
            var image = GetImageBitmap(size, cropMinimum);

            foreach (var region in regions)
            {
                var regionImage = ImageHelper.CropImage(image, region);
                regionImage = ImageHelper.CropImageCenterAndResize(regionImage, size, cropMinimum);
                result.Images.Add(regionImage);
            }

            return result;
        }
    }
}