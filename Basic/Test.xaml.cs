using System.Windows;
using Microsoft.Kinect;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Basic
{

    public partial class Test : Window
    {
        KinectSensor _sensor;
        MultiSourceFrameReader _reader;
        PixelFormat format = PixelFormats.Bgr32;

        public Test()
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
                if (frame != null)
                {
                    Photo.Source = ToBitmap(frame);
                }
            }
        }

        private ImageSource ToBitmap(ColorFrame frame)
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

    }
}

