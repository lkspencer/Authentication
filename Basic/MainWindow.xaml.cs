namespace Basic {
    using Basic;
    using DirectShowLib;
    using Emgu.CV;
    using Emgu.CV.Structure;
    using Microsoft.Kinect;
    using Microsoft.Kinect.Face;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;

  public partial class MainWindow : Window, INotifyPropertyChanged {
    // MainWindow Variables
    private KinectSensor kinectSensor = null;
    /// <summary>
    /// Reader for color frames
    /// </summary>
    private ColorFrameReader colorFrameReader = null;
    /// <summary>
    /// Bitmap to display
    /// </summary>
    private WriteableBitmap colorBitmap = null;
    /// <summary>
    /// The face frame source
    /// </summary>
    public ImageSource ImageSource {
      get {
        return this.colorBitmap;
      }
    }
    public event PropertyChangedEventHandler PropertyChanged;
    bool capturingFrame = false;

    // Constructors
    public MainWindow() {
      // get the kinectSensor object
      this.kinectSensor = KinectSensor.GetDefault();

      // create the colorFrameDescription from the ColorFrameSource using Bgra format
      FrameDescription colorFrameDescription = this.kinectSensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);

      // create the writeable bitmap to display our frames
      this.colorBitmap = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);

      // open the reader for the face frames
      this.colorFrameReader = this.kinectSensor.ColorFrameSource.OpenReader();
      // wire handlers for frame arrivals
      this.colorFrameReader.FrameArrived += this.Reader_ColorFrameArrived;

      // open the sensor
      this.kinectSensor.Open();

      // use the window object as the view model in this simple example
      this.KeyDown += new KeyEventHandler(MainWindow_KeyDown);
      this.DataContext = this;
      InitializeComponent();
    }

    private void Reader_ColorFrameArrived(object sender, ColorFrameArrivedEventArgs e)
    {
        // ColorFrame is IDisposable
        using (ColorFrame colorFrame = e.FrameReference.AcquireFrame())
        {
            if (colorFrame != null)
            {
                FrameDescription colorFrameDescription = colorFrame.FrameDescription;

                using (KinectBuffer colorBuffer = colorFrame.LockRawImageBuffer())
                {
                    this.colorBitmap.Lock();

                    // verify data and write the new color frame data to the display bitmap
                    if ((colorFrameDescription.Width == this.colorBitmap.PixelWidth) && (colorFrameDescription.Height == this.colorBitmap.PixelHeight))
                    {
                        colorFrame.CopyConvertedFrameDataToIntPtr(
                            this.colorBitmap.BackBuffer,
                            (uint)(colorFrameDescription.Width * colorFrameDescription.Height * 4),
                            ColorImageFormat.Bgra);

                        this.colorBitmap.AddDirtyRect(new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight));
                    }

                    this.colorBitmap.Unlock();
                }
            }
        }
    }

    void MainWindow_KeyDown(object sender, KeyEventArgs e)
    {
        //Clear any text and images previously set
        //TrainedPerson.Text = "";

        switch (e.Key)
        {
            case Key.K:
                //Capture photo for Kirk
                capturingFrame = true;

                break;
            case Key.D:
                //Captue photo for Delvin
                capturingFrame = true;

                break;
            case Key.C:
                //Clear screen of any pre auth names
                break;
        }
    }

    public void NotifyPropertyChanged(string propertyName) {
      if (PropertyChanged != null) {
        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
      }
    }

  }
}
