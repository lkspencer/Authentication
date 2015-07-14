﻿using System.Windows;
using Microsoft.Kinect;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.IO;

namespace Basic
{

    public partial class SimpleAuth : Window
    {
        KinectSensor _sensor;
        MultiSourceFrameReader _reader;
        PixelFormat format = PixelFormats.Bgr32;

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
                if (frame != null)
                {
                    Photo.Source = ToBitmap(frame);
                    Image<Bgr, Byte> imageForCV = ToImage(frame);
                    imageForCV.Save(@"data\face.png");

                    


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
    }
}

