namespace Trainer {
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
  using System.Windows.Media;
  using System.Windows.Media.Imaging;

  // EMGU documentation link for our reference: http://www.emgu.com/wiki/files/3.0.0-alpha/document/html/b72c032d-59ae-c36f-5e00-12f8d621dfb8.htm
  public partial class MainWindow : Window, INotifyPropertyChanged {
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
    /// Reader for depth
    /// </summary>
    private DepthFrameReader depthFrameReader = null;
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
    private WriteableBitmap threeDBitmap = null;
    /// <summary>
    /// Gets the bitmap to display
    /// </summary>
    public ImageSource ThreeDSource {
      get {
        return this.threeDBitmap;
      }
    }
    private int faceFramePosition = 0;
    private HighDefinitionFaceFrameSource highDefinitionFaceSource = null;
    private HighDefinitionFaceFrameReader highDefinitionFaceReader = null;
    private FaceAlignment highdefinitionFaceAlignment = null;
    private FaceModel highDefinitionFaceModel = null;
    private List<System.Windows.Shapes.Ellipse> points = new List<System.Windows.Shapes.Ellipse>();

    private RectI ThreeDFaceBox;
    private IReadOnlyDictionary<FacePointType, PointF> facePoints;

    private ushort minDepth = 500; // frame.DepthMinReliableDistance;
    private ushort maxDepth = 585; // frame.DepthMaxReliableDistance;
    private double multiplier;
    public event PropertyChangedEventHandler PropertyChanged;

    private int depthFrame = 0;
    private List<int> nose_xes = new List<int>();
    private List<int> nose_yes = new List<int>();
    private List<int> nose_zes = new List<int>();
    private List<int> lefteye_xes = new List<int>();
    private List<int> lefteye_yes = new List<int>();
    private List<int> lefteye_zes = new List<int>();
    private List<int> righteye_xes = new List<int>();
    private List<int> righteye_yes = new List<int>();
    private List<int> righteye_zes = new List<int>();
    private List<int> leftmouth_xes = new List<int>();
    private List<int> leftmouth_yes = new List<int>();
    private List<int> leftmouth_zes = new List<int>();
    private List<int> rightmouth_xes = new List<int>();
    private List<int> rightmouth_yes = new List<int>();
    private List<int> rightmouth_zes = new List<int>();

    private string averageFaceDepth;


    public string AverageFaceDepth {
      get {
        return this.averageFaceDepth;
      }
      set {
        NotifyPropertyChanged("AverageFaceDepth");
        this.averageFaceDepth = value;
      }
    }



    // Constructors
    public MainWindow() {
      // get the kinectSensor object
      this.kinectSensor = KinectSensor.GetDefault();

      // create the colorFrameDescription from the ColorFrameSource using Bgra format
      FrameDescription colorFrameDescription = this.kinectSensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);
      FrameDescription depthFrameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;

      // create the writeable bitmap to display our frames
      this.colorBitmap = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);

      // create the writeable bitmap to display our frames
      this.threeDBitmap = new WriteableBitmap(depthFrameDescription.Width, depthFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);

      // specify which facial features we're interested in capturing
      this.faceFrameSource = new FaceFrameSource(this.kinectSensor, 0,
        FaceFrameFeatures.BoundingBoxInColorSpace |
        FaceFrameFeatures.BoundingBoxInInfraredSpace |
        FaceFrameFeatures.PointsInColorSpace |
        FaceFrameFeatures.PointsInInfraredSpace);

      //this.highDefinitionFaceModel = new FaceModel();
      //this.highdefinitionFaceAlignment = new FaceAlignment();
      //this.highDefinitionFaceSource = new HighDefinitionFaceFrameSource(this.kinectSensor);
      //this.highDefinitionFaceSource.TrackingQuality = FaceAlignmentQuality.High;
      //this.highDefinitionFaceSource.FaceModel = this.highDefinitionFaceModel;

      // open the reader for the face frames
      this.colorFrameReader = this.kinectSensor.ColorFrameSource.OpenReader();
      this.bodyFrameReader = this.kinectSensor.BodyFrameSource.OpenReader();
      this.faceFrameReader = this.faceFrameSource.OpenReader();
      this.depthFrameReader =  this.kinectSensor.DepthFrameSource.OpenReader();
      //this.highDefinitionFaceReader = this.highDefinitionFaceSource.OpenReader();

      // wire handlers for frame arrivals
      this.colorFrameReader.FrameArrived += this.Reader_ColorFrameArrived;
      this.bodyFrameReader.FrameArrived += this.BodyFrameReader_FrameArrived;
      this.faceFrameReader.FrameArrived += this.FaceFrameReader_FrameArrived;
      this.depthFrameReader.FrameArrived += this.DepthFrameReader_FrameArrived;
      //this.highDefinitionFaceReader.FrameArrived += this.HighDefinitionFaceFrameReader_FrameArrived;

      // open the sensor
      this.kinectSensor.Open();

      // specify which opencv recognizer to use for facial recognition
      //this.recognizeFace = new RecognizeFace("EMGU.CV.EigenFaceRecognizer");
      //this.recognizeFace = new RecognizeFace("EMGU.CV.LBPHFaceRecognizer");
      this.recognizeFace = new RecognizeFace("EMGU.CV.FisherFaceRecognizer");

      // setup facial recognition callbacks
      this.recognizeFace.FaceRecognitionArrived += FaceRecognitionArrived;
      this.recognizeFace.FaceTrainingComplete += FaceTrainingComplete;

      // use the window object as the view model in this simple example
      this.DataContext = this;

      InitializeComponent();

      LoadTrainedFaces();
      multiplier = (255.0 / (this.maxDepth - this.minDepth));
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
      //*
      this.Dispatcher.Invoke((Action)(() => {
        this.PredictMessage.Content = result;
      }));
      //*/
    }

    private void HighDefinitionFaceFrameReader_FrameArrived(object sender, HighDefinitionFaceFrameArrivedEventArgs e) {
      using (var frame = e.FrameReference.AcquireFrame()) {
        if (frame != null && frame.IsFaceTracked) {
          frame.GetAndRefreshFaceAlignmentResult(highdefinitionFaceAlignment);
          UpdateFacePoints();
        }
      }
    }

    private void BodyFrameReader_FrameArrived(object sender, BodyFrameArrivedEventArgs e) {
      using (var frame = e.FrameReference.AcquireFrame()) {
        if (frame != null) {
          bodies = new Body[frame.BodyCount];
          frame.GetAndRefreshBodyData(bodies);

          // only use the first body
          Body body = bodies.Where(b => b.IsTracked).FirstOrDefault();

          if (!faceFrameSource.IsTrackingIdValid) {
            if (body != null) {
              // Assign a tracking ID to the face source
              faceFrameSource.TrackingId = body.TrackingId;
              //highDefinitionFaceSource.TrackingId = body.TrackingId;
            }
          }
        }
      }
    }

    private void FaceFrameReader_FrameArrived(object sender, FaceFrameArrivedEventArgs e) {
      var frame = e.FrameReference.AcquireFrame();
      if (frame != null && frame.FaceFrameResult != null && frame.FaceFrameResult.FaceBoundingBoxInColorSpace != null) {
        ThreeDFaceBox = frame.FaceFrameResult.FaceBoundingBoxInInfraredSpace;
        facePoints = frame.FaceFrameResult.FacePointsInInfraredSpace;
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
        if (predicting && faceFramePosition == 0 && face.Width > 0 && face.Height > 0) {
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
          //recognizeFace.Recognise(grayFace, 80);
          recognizeFace.Recognise(grayFace, 1800);
        }
        faceFramePosition = ++faceFramePosition % 10;
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
          }
        }
      }
    }

    private void DepthFrameReader_FrameArrived(object sender, DepthFrameArrivedEventArgs e) {
      //*
      using (var depthFrame = e.FrameReference.AcquireFrame()) {
        if (depthFrame != null) {
          DrawDepth(depthFrame);
        }
      }
      //*/
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

    private List<int> averageDepth = new List<int>();
    private void DrawDepth(DepthFrame frame) {
      this.averageDepth.Clear();
      this.threeDBitmap.Lock();
      int width = frame.FrameDescription.Width;
      int height = frame.FrameDescription.Height;
      ushort[] depthData = new ushort[width * height];
      byte[] pixelData = new byte[width * height * (PixelFormats.Bgr32.BitsPerPixel + 7) / 8];
      int stride = width * PixelFormats.Bgr32.BitsPerPixel / 8;

      frame.CopyFrameDataToArray(depthData);
      int colorIndex = 0;
      float xnose = 0;
      float xlefteye = 0;
      float xrighteye = 0;
      float xleftmouth = 0;
      float xrightmouth = 0;
      float ynose = 0;
      float ylefteye = 0;
      float yrighteye = 0;
      float yleftmouth = 0;
      float yrightmouth = 0;
      if (facePoints != null) {
        xnose = facePoints[FacePointType.Nose].X;
        xlefteye = facePoints[FacePointType.EyeLeft].X;
        xrighteye = facePoints[FacePointType.EyeRight].X;
        xleftmouth = facePoints[FacePointType.MouthCornerLeft].X;
        xrightmouth = facePoints[FacePointType.MouthCornerRight].X;
        ynose = facePoints[FacePointType.Nose].Y;
        ylefteye = facePoints[FacePointType.EyeLeft].Y;
        yrighteye = facePoints[FacePointType.EyeRight].Y;
        yleftmouth = facePoints[FacePointType.MouthCornerLeft].Y;
        yrightmouth = facePoints[FacePointType.MouthCornerRight].Y;
      }
      for (int depthIndex = 0; depthIndex < depthData.Length; ++depthIndex) {
        ushort z = depthData[depthIndex];
        // this math only works because x and y are integers
        int y = depthIndex / width;
        int x = depthIndex - (y * width);
        if (z > this.maxDepth || z < minDepth) {
          pixelData[colorIndex++] = 0; // Blue
          pixelData[colorIndex++] = 0; // Green
          pixelData[colorIndex++] = 0; // Red
          ++colorIndex;
          continue;
        } else  if (x < ThreeDFaceBox.Left || x > ThreeDFaceBox.Right || y < ThreeDFaceBox.Top || y > ThreeDFaceBox.Bottom) {
          pixelData[colorIndex++] = 0; // Blue
          pixelData[colorIndex++] = 0; // Green
          pixelData[colorIndex++] = 0; // Red
          ++colorIndex;
          continue;
        } else {
          if (xnose < x + 2 && xnose > x - 2 && ynose < y + 2 && ynose > y - 2) {
            averageDepth.Add(z);
            nose_xes.Add(x);
            nose_yes.Add(y);
            nose_zes.Add(z);
            pixelData[colorIndex++] = 0; // Blue
            pixelData[colorIndex++] = 0; // Green
            pixelData[colorIndex++] = 255; // Red
            ++colorIndex;
            continue;
          } else if (xlefteye < x + 2 && xlefteye > x - 2 && ylefteye < y + 2 && ylefteye > y - 2) {
            averageDepth.Add(z);
            lefteye_xes.Add(x);
            lefteye_yes.Add(y);
            lefteye_zes.Add(z);
            pixelData[colorIndex++] = 0; // Blue
            pixelData[colorIndex++] = 255; // Green
            pixelData[colorIndex++] = 0; // Red
            ++colorIndex;
            continue;
          } else if (xrighteye < x + 2 && xrighteye > x - 2 && yrighteye < y + 2 && yrighteye > y - 2) {
            averageDepth.Add(z);
            righteye_xes.Add(x);
            righteye_yes.Add(y);
            righteye_zes.Add(z);
            pixelData[colorIndex++] = 255; // Blue
            pixelData[colorIndex++] = 0; // Green
            pixelData[colorIndex++] = 0; // Red
            ++colorIndex;
            continue;
          } else if (xleftmouth < x + 2 && xleftmouth > x - 2 && yleftmouth < y + 2 && yleftmouth > y - 2) {
            averageDepth.Add(z);
            leftmouth_xes.Add(x);
            leftmouth_yes.Add(y);
            leftmouth_zes.Add(z);
            pixelData[colorIndex++] = 255; // Blue
            pixelData[colorIndex++] = 255; // Green
            pixelData[colorIndex++] = 0; // Red
            ++colorIndex;
            continue;
          } else if (xrightmouth < x + 2 && xrightmouth > x - 2 && yrightmouth < y + 2 && yrightmouth > y - 2) {
            averageDepth.Add(z);
            rightmouth_xes.Add(x);
            rightmouth_yes.Add(y);
            rightmouth_zes.Add(z);
            pixelData[colorIndex++] = 255; // Blue
            pixelData[colorIndex++] = 0; // Green
            pixelData[colorIndex++] = 255; // Red
            ++colorIndex;
            continue;
          }
        }
        //var distance = Math.Sqrt(Math.Pow((x1 - x2), 2) + Math.Pow((y1 - y2), 2) + Math.Pow((z1 - z2), 2));

        averageDepth.Add(z);
        byte intensity = (byte)((z - minDepth) * multiplier);

        pixelData[colorIndex++] = intensity; // Blue
        pixelData[colorIndex++] = intensity; // Green
        pixelData[colorIndex++] = intensity; // Red

        ++colorIndex;
      }

      if (depthFrame == 0) {
        if (nose_xes.Count > 0 && nose_yes.Count > 0 && nose_zes.Count > 0
          && lefteye_xes.Count > 0 && lefteye_yes.Count > 0 && lefteye_zes.Count > 0
          && righteye_xes.Count > 0 && righteye_yes.Count > 0 && righteye_zes.Count > 0
           && leftmouth_xes.Count > 0 && leftmouth_yes.Count > 0 && leftmouth_zes.Count > 0
           && rightmouth_xes.Count > 0 && rightmouth_yes.Count > 0 && rightmouth_zes.Count > 0) {
          /*
          // this doesn't exactly work because the z's are a measurement in meters
          // and the x's and y's are measurements in pixels
          var distance1 = Math.Sqrt(
            Math.Pow((nose_xes.Average() - lefteye_xes.Average()), 2)
            + Math.Pow((nose_yes.Average() - lefteye_yes.Average()), 2)
            + Math.Pow((nose_zes.Average() - lefteye_zes.Average()), 2));
          var distance2 = Math.Sqrt(
            Math.Pow((nose_xes.Average() - righteye_xes.Average()), 2)
            + Math.Pow((nose_yes.Average() - righteye_yes.Average()), 2)
            + Math.Pow((nose_zes.Average() - righteye_zes.Average()), 2));
          //*/
          var nosededepth = nose_zes.Average();
          var lefteyedepth = lefteye_zes.Average();
          var righteyedepth = righteye_zes.Average();
          var leftmouthdepth = leftmouth_zes.Average();
          var rightmouthdepth = rightmouth_zes.Average();
          if (nosededepth < lefteyedepth) {
            //Distance1Label.Content = String.Format("depth distance: {0}", lefteyedepth - nosededepth);
            Distance1Label.Content = String.Format("You have a lovely left eye");
          } else {
            Distance1Label.Content = String.Format("your left eye is sticking out");
          }
          if (nosededepth < righteyedepth) {
            //Distance2Label.Content = String.Format("depth distance: {0}", righteyedepth - nosededepth);
            Distance2Label.Content = String.Format("You have a lovely right eye");
          } else {
            Distance2Label.Content = String.Format("your right eye is sticking out");
          }
          if (nosededepth < leftmouthdepth && nosededepth < righteyedepth) {
            //Distance3Label.Content = String.Format("You have great lips: {0} - {1}", rightmouthdepth, righteyedepth);
            Distance3Label.Content = String.Format("You have great lips");
          } else {
            //Distance3Label.Content = String.Format("Your lips are on wrong: {0} - {1}", leftmouthdepth, lefteyedepth);
            Distance3Label.Content = String.Format("Your lips are on wrong!");
          }
          nose_xes.Clear();
          nose_yes.Clear();
          nose_zes.Clear();
          lefteye_xes.Clear();
          lefteye_yes.Clear();
          lefteye_zes.Clear();
          righteye_xes.Clear();
          righteye_yes.Clear();
          righteye_zes.Clear();
          leftmouth_xes.Clear();
          leftmouth_yes.Clear();
          leftmouth_zes.Clear();
          rightmouth_xes.Clear();
          rightmouth_yes.Clear();
          rightmouth_zes.Clear();
        }
      }
      if (averageDepth.Count > 0) {
        this.AverageFaceDepth = String.Format("Average face depth: {0}", averageDepth.Average());
      }

      if ((frame.FrameDescription.Width == this.threeDBitmap.PixelWidth) && (frame.FrameDescription.Height == this.threeDBitmap.PixelHeight)) {
        this.threeDBitmap.WritePixels(new Int32Rect(0, 0, this.threeDBitmap.PixelWidth, this.threeDBitmap.PixelHeight), pixelData, stride, 0);
        this.threeDBitmap.AddDirtyRect(new Int32Rect(0, 0, this.threeDBitmap.PixelWidth, this.threeDBitmap.PixelHeight));
      }

      this.threeDBitmap.Unlock();
      depthFrame = ++depthFrame % 5;
    }

    private void UpdateFacePoints() {
      if (highDefinitionFaceModel == null) return;

      var vertices = highDefinitionFaceModel.CalculateVerticesForAlignment(highdefinitionFaceAlignment);

      if (vertices.Count > 0) {
        if (points.Count == 0) {
          for (int index = 0; index < vertices.Count; index++) {
            System.Windows.Shapes.Ellipse ellipse = new System.Windows.Shapes.Ellipse {
              Width = 2.0,
              Height = 2.0,
              Fill = new SolidColorBrush(Colors.Blue)
            };

            points.Add(ellipse);
          }

          foreach (System.Windows.Shapes.Ellipse ellipse in points) {
            canvas.Children.Add(ellipse);
          }
        }

        for (int index = 0; index < vertices.Count; index++) {
          CameraSpacePoint vertice = vertices[index];
          DepthSpacePoint point = this.kinectSensor.CoordinateMapper.MapCameraPointToDepthSpace(vertice);

          if (float.IsInfinity(point.X) || float.IsInfinity(point.Y)) continue;

          System.Windows.Shapes.Ellipse ellipse = points[index];

          System.Windows.Controls.Canvas.SetLeft(ellipse, point.X);
          System.Windows.Controls.Canvas.SetTop(ellipse, point.Y);
        }
      }
    }

    public void NotifyPropertyChanged(string propertyName) {
      if (PropertyChanged != null) {
        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
      }
    }

  }
}
