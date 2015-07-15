using System.Windows;
using Microsoft.Kinect;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Drawing;
using System.IO;

namespace Basic
{

    public partial class SimpleAuth : Window
    {
        KinectSensor _sensor;
        MultiSourceFrameReader _reader;
        PixelFormat format = PixelFormats.Bgr32;
        Image<Bgr, Byte> imageForCV;
        Rectangle[] rectArray;
        bool authenticating = false;

        public SimpleAuth()
        {
            InitializeComponent();
            _sensor = KinectSensor.GetDefault();

            if (_sensor != null)
            {
                _sensor.Open();
            }
            _reader = _sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color);
            _reader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;

        }

        void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {

            var reference = e.FrameReference.AcquireFrame();
            using (var frame = reference.ColorFrameReference.AcquireFrame())
            {
                if (frame == null)
                {
                    return;
                }
                Bitmap p = BitmapFromSource(ToBitmap(frame));
                Photo.Source = ToImageSource(frame);

                if (!authenticating)
                {
                    
                    imageForCV = ToImage(frame);

                    Rectangle[] rectArray;
                    rectArray = DetectFace.Detect(imageForCV.Mat, @"haarcascade\haarcascade_eye.xml", @"haarcascade\haarcascade_frontalface_default.xml");

                    if (rectArray.Length > 0)
                    {
                        authenticating = true;
                        Rectangle r = rectArray[0];

                        Bitmap bm = CropBitmap(p, r.X, r.Y, r.Width, r.Height);
                        Face.Source = loadBitmap(bm);
                        //authenticating = false;

                    }
                }
            }
        }

        public static BitmapSource loadBitmap(System.Drawing.Bitmap source)
        {
            return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(source.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty,
                System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
        }

        public Bitmap CropBitmap(Bitmap bitmap, int cropX, int cropY, int cropWidth, int cropHeight)
        {
            Rectangle rect = new Rectangle(cropX, cropY, cropWidth, cropHeight);
            using (Bitmap bmpImage = new Bitmap(bitmap))
            {
                Bitmap cropped = bmpImage.Clone(rect, bmpImage.PixelFormat);
                return cropped;
            }
        }

        private ImageSource ToImageSource(ColorFrame frame)
        {
            int width = frame.FrameDescription.Width;
            int height = frame.FrameDescription.Height;

            byte[] pixels = new byte[width * height * ((PixelFormats.Bgr32.BitsPerPixel + 7) / 8)];

            if (frame.RawColorImageFormat == ColorImageFormat.Bgra)
            {
                frame.CopyRawFrameDataToArray(pixels);
            }
            else
            {
                frame.CopyConvertedFrameDataToArray(pixels, ColorImageFormat.Bgra);
            }

            int stride = width * format.BitsPerPixel / 8;

            return BitmapSource.Create(width, height, 96, 96, format, null, pixels, stride);
        }

        private BitmapSource ToBitmap(ColorFrame frame)
        {
            int width = frame.FrameDescription.Width;
            int height = frame.FrameDescription.Height;

            byte[] pixels = new byte[width * height * ((PixelFormats.Bgr32.BitsPerPixel + 7) / 8)];

            if (frame.RawColorImageFormat == ColorImageFormat.Bgra)
            {
                frame.CopyRawFrameDataToArray(pixels);
            }
            else
            {
                frame.CopyConvertedFrameDataToArray(pixels, ColorImageFormat.Bgra);
            }

            int stride = width * format.BitsPerPixel / 8;

            return BitmapSource.Create(width, height, 96, 96, format, null, pixels, stride);
        }

        private Image<Bgr, Byte> ToImage(ColorFrame frame)
        {
            int width = frame.FrameDescription.Width;
            int height = frame.FrameDescription.Height;
            PixelFormat format = PixelFormats.Bgr32;

            byte[] pixels = new byte[width * height * ((format.BitsPerPixel + 7) / 8)];

            if (frame.RawColorImageFormat == ColorImageFormat.Bgra)
            {
                frame.CopyRawFrameDataToArray(pixels);
            }
            else
            {
                frame.CopyConvertedFrameDataToArray(pixels, ColorImageFormat.Bgra);
            }

            Image<Bgr, Byte> img = new Image<Bgr, Byte>(width, height);
            img.Bytes = pixels;

            return img;
        }

        public static Bitmap BitmapFromSource(BitmapSource bitmapsource)
        {
            Bitmap bitmap; using (var outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapsource));
                enc.Save(outStream); bitmap = new Bitmap(outStream);
            }
            return bitmap;
        }
    }
}

