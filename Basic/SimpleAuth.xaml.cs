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
        private ColorFrameReader colorFrameReader = null;

        KinectSensor _sensor;
        ColorFrameReader _reader;
        PixelFormat format = PixelFormats.Bgr32;
        Image<Bgr, Byte> imageForCV;
        bool authenticating = false;
        Bitmap p;
        Bitmap bm;
        Rectangle[] rectArray;
        Rectangle r;

        public SimpleAuth()
        {
            InitializeComponent();
            _sensor = KinectSensor.GetDefault();

            if (_sensor != null)
            {
                _sensor.Open();
            }
            _reader = this._sensor.ColorFrameSource.OpenReader();
            _reader.FrameArrived += this.Reader_ColorFrameArrived;
            //Reader_ColorFrameArrived

            //_reader = _sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color);
            //_reader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;

        }



        private void Reader_ColorFrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            var reference = e.FrameReference.AcquireFrame();
            using (var frame = e.FrameReference.AcquireFrame())
            {
                if (frame == null)
                {
                    return;
                }
                p = BitmapFromSource(ToBitmap(frame));
                Photo.Source = ToImageSource(frame);

                if (!authenticating)
                {

                    imageForCV = ToImage(frame);


                    rectArray = DetectFace.Detect(imageForCV.Mat, @"haarcascade\haarcascade_eye.xml", @"haarcascade\haarcascade_frontalface_default.xml");

                    if (rectArray.Length > 0)
                    {
                        authenticating = true;
                        r = rectArray[0];

                        Mat roi = new Mat(imageForCV.Mat, r);
                        Face.Source = roi.ToImage<>();

                        //bm = CropBitmap(p, r.X, r.Y, r.Width, r.Height);
                        //Face.Source = loadBitmap(bm);
                        //authenticating = false;

                    }
                }
            }
        }


        //void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        //{

        //    var reference = e.FrameReference.AcquireFrame();
        //    using (var frame = reference.ColorFrameReference.AcquireFrame())
        //    {
        //        if (frame == null)
        //        {
        //            return;
        //        }
        //        p = BitmapFromSource(ToBitmap(frame));
        //        Photo.Source = ToImageSource(frame);

        //        if (!authenticating)
        //        {

        //            imageForCV = ToImage(frame);


        //            rectArray = DetectFace.Detect(imageForCV.Mat, @"haarcascade\haarcascade_eye.xml", @"haarcascade\haarcascade_frontalface_default.xml");

        //            if (rectArray.Length > 0)
        //            {
        //                authenticating = true;
        //                Rectangle r = rectArray[0];

        //                Bitmap bm = CropBitmap(p, r.X, r.Y, r.Width, r.Height);
        //                Face.Source = loadBitmap(bm);
        //                //authenticating = false;

        //            }
        //        }
        //    }
        //}

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


        //private void ProcessFrame(object sender, EventArgs arg)
        //{
        //    Image<Bgr, Byte> frame = _capture.QueryFrame();
        //    Image<Gray, Byte> gray = frame.Convert<Gray, Byte>(); //Convert it to Grayscale

        //    //normalizes brightness and increases contrast of the image
        //    gray._EqualizeHist();

        //    //Read the HaarCascade objects
        //    HaarCascade face = new HaarCascade("haarcascade_frontalface_alt_tree.xml");
        //    HaarCascade eye = new HaarCascade("haarcascade_eye.xml");

        //    //Detect the faces  from the gray scale image and store the locations as rectangle
        //    //The first dimensional is the channel
        //    //The second dimension is the index of the rectangle in the specific channel
        //    MCvAvgComp[][] facesDetected = gray.DetectHaarCascade(
        //       face,
        //       1.1,
        //       10,
        //       Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_ROUGH_SEARCH,
        //       new Size(20, 20));

        //    foreach (MCvAvgComp f in facesDetected[0])
        //    {
        //        //draw the face detected in the 0th (gray) channel with blue color
        //        frame.Draw(f.rect, new Bgr(Color.Blue), 2);
        //        /*
        //        //Set the region of interest on the faces
        //        gray.ROI = f.rect;
        //        MCvAvgComp[][] eyesDetected = gray.DetectHaarCascade(
        //           eye,
        //           1.1,
        //           10,
        //           Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_ROUGH_SEARCH,
        //           new Size(20, 20));
        //        gray.ROI = Rectangle.Empty;

        //        foreach (MCvAvgComp e in eyesDetected[0])
        //        {
        //            Rectangle eyeRect = e.rect;
        //            eyeRect.Offset(f.rect.X, f.rect.Y);
        //            frame.Draw(eyeRect, new Bgr(Color.Red), 2);

        //        }*/
        //    }

        //    pictureBox1.Image = frame.ToBitmap(); Application.DoEvents();
        //}








    }
}

