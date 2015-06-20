namespace Trainer {
  using DirectShowLib;
  using Emgu.CV;
  using Emgu.CV.Structure;
  using Microsoft.Kinect;
  using Microsoft.Kinect.Face;
  using System;
  using System.Collections.Generic;
  using System.Drawing.Imaging;
  using System.IO;
  using System.Linq;
  using System.Windows;
  using System.Windows.Media;
  using System.Windows.Media.Imaging;

  // EMGU documentation link for our reference: http://www.emgu.com/wiki/files/3.0.0-alpha/document/html/b72c032d-59ae-c36f-5e00-12f8d621dfb8.htm
  public partial class MainWindow : Window {
    // MainWindow Variables
    private RecognizeFace recognizeFace;
    private bool training = false;
    private int trainingCount = 0;
    private System.Drawing.Rectangle face = new System.Drawing.Rectangle();
    private List<Image<Gray, Byte>> trainingFaces = new List<Image<Gray, byte>>();
    private bool predicting = false;
    /// <summary>
    /// Active Kinect sensor
    /// </summary>
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
    FaceFrameSource faceFrameSource = null;
    /// <summary>
    /// Reader for faces
    /// </summary>
    private FaceFrameReader faceFrameReader = null;
    /// <summary>
    /// The body frame reader is used to identify the bodies
    /// </summary>
    BodyFrameReader bodyFrameReader = null;
    /// <summary>
    /// The list of bodies identified by the sensor
    /// </summary>
    IList<Body> bodies = null;
    /// <summary>
    /// Gets the bitmap to display
    /// </summary>
    public ImageSource ImageSource {
      get {
        return this.colorBitmap;
      }
    }



    // Constructors
    public MainWindow() {
      // get the kinectSensor object
      this.kinectSensor = KinectSensor.GetDefault();

      // create the colorFrameDescription from the ColorFrameSource using Bgra format
      FrameDescription colorFrameDescription = this.kinectSensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);

      // create the writeable bitmap to display our frames
      this.colorBitmap = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);

      // create our bodies array to track human bodies in the field of view
      this.bodies = new Body[this.kinectSensor.BodyFrameSource.BodyCount];

      // specify which facial features we're interested in capturing
      this.faceFrameSource = new FaceFrameSource(this.kinectSensor, 0,
        FaceFrameFeatures.BoundingBoxInColorSpace |
        FaceFrameFeatures.FaceEngagement |
        FaceFrameFeatures.Glasses |
        FaceFrameFeatures.Happy |
        FaceFrameFeatures.LeftEyeClosed |
        FaceFrameFeatures.MouthOpen |
        FaceFrameFeatures.PointsInColorSpace |
        FaceFrameFeatures.RightEyeClosed);

      // open the reader for the face frames
      this.colorFrameReader = this.kinectSensor.ColorFrameSource.OpenReader();
      this.bodyFrameReader = this.kinectSensor.BodyFrameSource.OpenReader();
      this.faceFrameReader = this.faceFrameSource.OpenReader();

      // wire handlers for frame arrivals
      this.colorFrameReader.FrameArrived += this.Reader_ColorFrameArrived;
      this.bodyFrameReader.FrameArrived += this.BodyFrameReader_FrameArrived;
      this.faceFrameReader.FrameArrived += this.FaceFrameReader_FrameArrived;

      // open the sensor
      this.kinectSensor.Open();

      // specify which opencv recognizer to use for facial recognition
      //recognizeFace = new RecognizeFace("EMGU.CV.EigenFaceRecognizer");
      this.recognizeFace = new RecognizeFace("EMGU.CV.LBPHFaceRecognizer");
      //recognizeFace = new RecognizeFace("EMGU.CV.FisherFaceRecognizer");

      // setup facial recognition callbacks
      this.recognizeFace.FaceRecognitionArrived += FaceRecognitionArrived;
      this.recognizeFace.FaceTrainingComplete += FaceTrainingComplete;

      // use the window object as the view model in this simple example
      this.DataContext = this;


      InitializeComponent();

      LoadTrainedFaces();
    }



    // Event Handlers
    private void FaceTrainingComplete(bool trained) {
      if (trained) {
        TrainMessage.Content = "Done Training";
      } else {
        TrainMessage.Content = "Training Failed";
      }
      Train.IsEnabled = true;
    }

    private void FaceRecognitionArrived(string result) {
      this.PredictMessage.Content = result;
    }

    private void BodyFrameReader_FrameArrived(object sender, BodyFrameArrivedEventArgs e) {
      using (var frame = e.FrameReference.AcquireFrame()) {
        if (frame != null) {
          frame.GetAndRefreshBodyData(bodies);

          Body body = bodies.Where(b => b.IsTracked).FirstOrDefault();

          if (!faceFrameSource.IsTrackingIdValid) {
            if (body != null) {
              // Assign a tracking ID to the face source
              faceFrameSource.TrackingId = body.TrackingId;
            }
          }
        }
      }
    }

    private void FaceFrameReader_FrameArrived(object sender, FaceFrameArrivedEventArgs e) {
      var frame = e.FrameReference.AcquireFrame();
      if (frame != null && frame.FaceFrameResult != null && frame.FaceFrameResult.FaceBoundingBoxInColorSpace != null) {
        var box = frame.FaceFrameResult.FaceBoundingBoxInColorSpace;
        face.Width = box.Right - box.Left;
        face.Height = box.Bottom - box.Top;
        face.X = box.Left;
        face.Y = box.Top;

        try {
          if (training || predicting) {
            DrawSquare(face.Width, face.Height, box.Left, box.Top);
          }
        } catch (Exception ex) { }
      }
    }

    private void Reader_ColorFrameArrived(object sender, ColorFrameArrivedEventArgs e) {
      // ColorFrame is IDisposable
      using (ColorFrame colorFrame = e.FrameReference.AcquireFrame()) {
        if (colorFrame != null) {
          FrameDescription colorFrameDescription = colorFrame.FrameDescription;

          using (KinectBuffer colorBuffer = colorFrame.LockRawImageBuffer()) {
            this.colorBitmap.Lock();

            // verify data and write the new color frame data to the display bitmap
            if ((colorFrameDescription.Width == this.colorBitmap.PixelWidth) && (colorFrameDescription.Height == this.colorBitmap.PixelHeight)) {
              colorFrame.CopyConvertedFrameDataToIntPtr(
                  this.colorBitmap.BackBuffer,
                  (uint)(colorFrameDescription.Width * colorFrameDescription.Height * 4),
                  ColorImageFormat.Bgra);

              this.colorBitmap.AddDirtyRect(new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight));
            }

            this.colorBitmap.Unlock();
          }
          //*
          if (training && face.Width > 0 && face.Height > 0) {
            if (trainingCount % 10 == 0) {
              TrainMessage.Content = "Capturing face";
              // grab the bytes for the image inside of the frame
              byte[] pixels = new byte[face.Width * face.Height * 4];
              this.colorBitmap.CopyPixels(
                new Int32Rect(face.X, face.Y, face.Width, face.Height),
                pixels, face.Width * 4, 0);

              // load the image into a format emgu opencv understands
              var faceImage = new Image<Bgra, Byte>(face.Width, face.Height);
              faceImage.Bytes = pixels;
              // resize so we always have the same size of images to work with
              faceImage = faceImage.Resize(100, 100, Emgu.CV.CvEnum.Inter.Cubic);
              // convert to grayscale for emgu opencv facial recognition
              var grayFace = new Image<Gray, Byte>(100, 100);
              grayFace.ConvertFrom<Bgra, Byte>(faceImage);

              trainingFaces.Add(grayFace);
              if (trainingCount == 90) {
                StopTraining();
              }
            }
            trainingCount++;
          } else if (predicting && face.Width > 0 && face.Height > 0) {
            //predicting = false;
            // grab the bytes for the image inside of the frame
            byte[] pixels = new byte[face.Width * face.Height * 4];
            this.colorBitmap.CopyPixels(
              new Int32Rect(face.X, face.Y, face.Width, face.Height),
              pixels, face.Width * 4, 0);

            // load the image into a format emgu opencv understands
            var faceImage = new Image<Bgra, Byte>(face.Width, face.Height);
            faceImage.Bytes = pixels;
            // resize so we always have the same size of images to work with
            faceImage = faceImage.Resize(100, 100, Emgu.CV.CvEnum.Inter.Cubic);
            // convert to grayscale for emgu opencv facial recognition
            var grayFace = new Image<Gray, Byte>(100, 100);
            grayFace.ConvertFrom<Bgra, Byte>(faceImage);

            recognizeFace.RecogniseAsync(grayFace, 80);
          }
        }
      }
    }

    private void Train_Click(object sender, RoutedEventArgs e) {
      Train.IsEnabled = false;
      TrainMessage.Content = "Searching for face";
      trainingFaces.Clear();
      training = true;
      trainingCount = 0;
    }

    private void Predict_Click(object sender, RoutedEventArgs e) {
      predicting = Predict.IsChecked.GetValueOrDefault();
      if (!predicting) PredictMessage.Content = "";
    }



    // Methods
    private void StopTraining() {
      TrainMessage.Content = "Training software";
      training = false;
      DirectoryInfo di = new DirectoryInfo(@"data");
      var files = di.EnumerateFiles("face_*.bmp");
      int[] labels = new int[files.Count() + 10];
      int i = 0;
      Image<Gray, Byte>[] images = new Image<Gray, Byte>[files.Count() + 10];
      foreach (var file in files) {
        labels[i] = i;
        images[i] = new Image<Gray, Byte>(file.FullName);
        i++;
      }

      files = di.EnumerateFiles("face_" + NameTextBox.Text + "_*.bmp");
      // getting the current count of files should give us the next available index number that can be used
      int fileIndex = files.Count();
      foreach (var face in trainingFaces) {
        face.Save(@"data\face_" + NameTextBox.Text + "_" + fileIndex + ".bmp");
        images[i] = face;
        labels[i] = i;
        fileIndex++;
        i++;
      }
      recognizeFace.TrainAsync(images, labels);
    }

    private void DrawSquare(int width, int height, int x, int y) {
      int size = width * height;
      int borderWidth = 9;
      byte[] pixels = new byte[width * height * 4];
      this.colorBitmap.CopyPixels(
        new Int32Rect(x, y, width, height),
        pixels, width * 4, 0);
      for (int i = 0; i < size; i++) {
        if (i < (width * borderWidth)) {
          var start = i * 4;
          pixels[start] = 0;
          pixels[start + 1] = 0;
          pixels[start + 2] = 0;
          pixels[start + 3] = 0;
        } else if (i % width == 0) {
          for (int j = i - borderWidth; j < i + borderWidth; j++) {
            var start = (j) * 4;
            pixels[start] = 0;
            pixels[start + 1] = 0;
            pixels[start + 2] = 0;
            pixels[start + 3] = 0;
          }
        } else if (i > size - (width * borderWidth)) {
          var start = i * 4;
          pixels[start] = 0;
          pixels[start + 1] = 0;
          pixels[start + 2] = 0;
          pixels[start + 3] = 0;
        }
      }
      this.colorBitmap.WritePixels(
        new Int32Rect(0, 0, width, height),
        pixels, width * 4, x, y);
    }

    private void LoadTrainedFaces() {
      Train.IsEnabled = false;
      TrainMessage.Content = "Loading Training Faces";
      DirectoryInfo di = new DirectoryInfo(@"data");
      var files = di.EnumerateFiles("face_*.bmp");
      int[] labels = new int[files.Count()];
      int i = 0;
      Image<Gray, Byte>[] images = new Image<Gray, Byte>[files.Count()];
      foreach (var file in files) {
        labels[i] = i;
        images[i] = new Image<Gray, Byte>(file.FullName);
        i++;
      }
      recognizeFace.TrainAsync(images, labels, true);
    }

  }
}
