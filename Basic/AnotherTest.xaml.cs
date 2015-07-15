using Emgu.CV;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Emgu.CV.Structure;
using System.ComponentModel;

namespace Basic
{
    /// <summary>
    /// Interaction logic for AnotherTest.xaml
    /// </summary>
    public partial class AnotherTest : Window, INotifyPropertyChanged
    {
        KinectSensor _sensor;
        ColorFrameReader _reader;
        PixelFormat format = PixelFormats.Bgr32;
        WriteableBitmap capturedImage = null;
        WriteableBitmap capturedImageWithFace = null;
        public event PropertyChangedEventHandler PropertyChanged;
        FrameDescription colorFrameDescription;

        public ImageSource CapturedImage
        {
            get
            {
                return this.capturedImage;
            }
        }

        public ImageSource CapturedImageWithFace
        {
            get
            {
                return this.capturedImageWithFace;
            }
        }


        Rectangle[] rectArray;

        public AnotherTest()
        {
            InitializeComponent();
            _sensor = KinectSensor.GetDefault();

            if (_sensor != null)
            {
                _sensor.Open();
            }

            colorFrameDescription = _sensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);

            this.capturedImage = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);
            this.capturedImageWithFace = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);

            this.DataContext = this;
            _reader = this._sensor.ColorFrameSource.OpenReader();
            _reader.FrameArrived += this.Reader_ColorFrameArrived;
        }

        private void Reader_ColorFrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            using (var frame = e.FrameReference.AcquireFrame())
            {
                if (frame == null)
                {
                    return;
                }

                using (KinectBuffer colorBuffer = frame.LockRawImageBuffer())
                {
                    #region capturedImage
                    this.capturedImage.Lock();

                    // verify data and write the new color frame data to the display bitmap
                    if ((colorFrameDescription.Width == this.capturedImage.PixelWidth) && (colorFrameDescription.Height == this.capturedImage.PixelHeight))
                    {
                        frame.CopyConvertedFrameDataToIntPtr(
                            this.capturedImage.BackBuffer,
                            (uint)(colorFrameDescription.Width * colorFrameDescription.Height * 4),
                            ColorImageFormat.Bgra);

                        this.capturedImage.AddDirtyRect(new Int32Rect(0, 0, this.capturedImage.PixelWidth, this.capturedImage.PixelHeight));
                    }

                    this.capturedImage.Unlock();
                    #endregion

                    #region capturedWithFace
                    this.capturedImageWithFace.Lock();

                    // verify data and write the new color frame data to the display bitmap
                    if ((colorFrameDescription.Width == this.capturedImageWithFace.PixelWidth) && (colorFrameDescription.Height == this.capturedImageWithFace.PixelHeight))
                    {
                        frame.CopyConvertedFrameDataToIntPtr(
                            this.capturedImageWithFace.BackBuffer,
                            (uint)(colorFrameDescription.Width * colorFrameDescription.Height * 4),
                            ColorImageFormat.Bgra);

                        //DrawSquare(200, 200, 100, 100);

                        this.capturedImageWithFace.AddDirtyRect(new Int32Rect(0, 0, this.capturedImageWithFace.PixelWidth, this.capturedImageWithFace.PixelHeight));
                    }

                    this.capturedImageWithFace.Unlock();
                    #endregion

                    //if (!authenticating)
                    //{
                    //    authenticating = true;
                    //    Image<Bgr, Byte> imageForCV = ToImage(frame);
                    //    rectArray = DetectFace.Detect(imageForCV.Mat, @"haarcascade\haarcascade_eye.xml", @"haarcascade\haarcascade_frontalface_default.xml");

                    //    if (rectArray.Length > 0)
                    //    {
                    //        imageForCV.Draw(rectArray[0], new Bgr(0, double.MaxValue, 0), 3);
                    //        capturedImageWithFace = ToBitmapSource(imageForCV);
                    //    }
                    //    authenticating = false;
                    //}
                }
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

        public static BitmapSource ToBitmapSource(IImage image)
        {
            using (System.Drawing.Bitmap source = image.Bitmap)
            {
                IntPtr ptr = source.GetHbitmap(); //obtain the Hbitmap

                BitmapSource bs = System.Windows.Interop
                  .Imaging.CreateBitmapSourceFromHBitmap(
                  ptr,
                  IntPtr.Zero,
                  Int32Rect.Empty,
                  System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());

                return bs;
            }
        }

        public static BitmapSource loadBitmap(System.Drawing.Bitmap source)
        {
            return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(source.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty,
                System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
        }

        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void DrawSquare(int width, int height, int x, int y)
        {
            int size = width * height;
            int borderWidth = 9;
            byte[] pixels = new byte[width * height * 4];
            this.capturedImageWithFace.CopyPixels(
              new Int32Rect(x, y, width, height),
              pixels, width * 4, 0);
            for (int i = 0; i < size; i++)
            {
                if (i < (width * borderWidth))
                {
                    var start = i * 4;
                    pixels[start] = 0;
                    pixels[start + 1] = 0;
                    pixels[start + 2] = 0;
                    pixels[start + 3] = 0;
                }
                else if (i % width == 0)
                {
                    for (int j = i - borderWidth; j < i + borderWidth; j++)
                    {
                        var start = (j) * 4;
                        pixels[start] = 0;
                        pixels[start + 1] = 0;
                        pixels[start + 2] = 0;
                        pixels[start + 3] = 0;
                    }
                }
                else if (i > size - (width * borderWidth))
                {
                    var start = i * 4;
                    pixels[start] = 0;
                    pixels[start + 1] = 0;
                    pixels[start + 2] = 0;
                    pixels[start + 3] = 0;
                }
            }
            this.capturedImageWithFace.WritePixels(
              new Int32Rect(0, 0, width, height),
              pixels, width * 4, x, y);
        }

    }
}
