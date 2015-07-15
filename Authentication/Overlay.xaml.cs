namespace Trainer {
  using Microsoft.Kinect;
  using Microsoft.Kinect.Face;
  using System;
  using System.Collections.Generic;
  using System.ComponentModel;
  using System.IO;
  using System.Linq;
  using System.Threading.Tasks;
  using System.Web.Script.Serialization;
  using System.Windows;
  using System.Windows.Controls;
  using System.Windows.Input;
  using System.Windows.Media;
  using System.Windows.Media.Imaging;

  public partial class Overlay : Window, INotifyPropertyChanged {
    public delegate void VerticesUpdated(CameraSpacePoint[] vertices, int[] colors);
    public delegate void HdFaceUpdated(CameraSpacePoint[] vertices, int[] colors, int matched, double w, double x, double y, double z);
    public delegate void TwoDMatchFound(string name);
    public event VerticesUpdated OnVerticesUpdated;
    public event HdFaceUpdated OnHdFaceUpdated;
    public event TwoDMatchFound OnTwoDMatchFound;
    // MainWindow Variables
    private KinectSensor kinectSensor = null;
    BodyFrameReader bodyFrameReader = null;
    // Body Variables
    IList<Body> bodies = null;
    private Body currentTrackedBody = null;
    private ulong currentTrackingId = 0;
    // HD variables
    private HighDefinitionFaceFrameSource highDefinitionFaceSource = null;
    private HighDefinitionFaceFrameReader highDefinitionFaceReader = null;
    private FaceAlignment highdefinitionFaceAlignment = null;
    private FaceModel highDefinitionFaceModel = null;
    private int checkPointMatches = 0;
    private IReadOnlyList<CameraSpacePoint> defaultVertices;
    private CameraSpacePoint tempVertice = new CameraSpacePoint();
    private float tollerance = 0.009f;
    private CameraSpacePoint[] hdFaceVertices;
    private int[] hdFaceColors;
    // Color Variables
    private WriteableBitmap colorImage;
    private bool isColor = false;
    int[] imageIntColors;
    // Saved HD Face Variables
    private List<System.Windows.Shapes.Ellipse> savedDots = new List<System.Windows.Shapes.Ellipse>();
    private CameraSpacePoint[] savedVertices = null;
    // Face Frame Variables
    private FaceFrameSource faceFrameSource = null;
    private FaceFrameReader faceFrameReader = null;
    private bool faceCaptured = false;
    // Depth Variables
    private DepthFrameReader depthFrameReader = null;
    private ushort minDepth = 500;
    private ushort maxDepth = 750;
    private double multiplier;
    private ushort[] depthData;
    private Image depthCanvasImage = new Image();
    private WriteableBitmap depthBitmap = null;
    private CameraSpacePoint[] depthVertices = null;
    private int depthWidth;
    private int depthHeight;
    private byte[] pixelData;
    private int stride;

    // Mouse Variables
    private Point mouseOrigin;
    private Point mouseStart;
    // Property Variables
    public event PropertyChangedEventHandler PropertyChanged;
    // Oxford Variables
    private string key = "";



    // Constructors
    public Overlay() {
      Emgu.CV.Capture capture = new Emgu.CV.Capture();
      // get the kinectSensor object
      this.kinectSensor = KinectSensor.GetDefault();

      // create the colorFrameDescription from the ColorFrameSource using Bgra format
      FrameDescription depthFrameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;
      this.depthBitmap = new WriteableBitmap(depthFrameDescription.Width, depthFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);
      depthVertices = new CameraSpacePoint[depthFrameDescription.Width * depthFrameDescription.Height];
      depthData = new ushort[depthFrameDescription.Width * depthFrameDescription.Height];
      pixelData = new byte[depthFrameDescription.Width * depthFrameDescription.Height * (PixelFormats.Bgr32.BitsPerPixel + 7) / 8];
      stride = depthFrameDescription.Width * PixelFormats.Bgr32.BitsPerPixel / 8;


      FrameDescription colorFrameDescription = this.kinectSensor.ColorFrameSource.FrameDescription;
      colorImage = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);
      imageIntColors = new int[colorFrameDescription.Width * colorFrameDescription.Height];


      // start with default alignment
      this.highdefinitionFaceAlignment = new FaceAlignment();

      // setup sources prior to opening readers
      this.faceFrameSource = new FaceFrameSource(this.kinectSensor, 0, FaceFrameFeatures.BoundingBoxInColorSpace);
      this.highDefinitionFaceModel = new FaceModel();
      this.highDefinitionFaceSource = new HighDefinitionFaceFrameSource(this.kinectSensor);
      this.highDefinitionFaceSource.TrackingQuality = FaceAlignmentQuality.High;
      this.highDefinitionFaceSource.FaceModel = this.highDefinitionFaceModel;


      // open readers for the face frames
      this.bodyFrameReader = this.kinectSensor.BodyFrameSource.OpenReader();
      this.highDefinitionFaceReader = this.highDefinitionFaceSource.OpenReader();
      this.faceFrameReader = this.faceFrameSource.OpenReader();

      // wire handlers for frame arrivals
      this.bodyFrameReader.FrameArrived += this.BodyFrameReader_FrameArrived;
      this.highDefinitionFaceReader.FrameArrived += this.HighDefinitionFaceFrameReader_FrameArrived;
      this.faceFrameReader.FrameArrived += this.FaceFrameReader_FrameArrived;

      // open the sensor
      this.kinectSensor.Open();

      // use the window object as the view model in this simple example
      this.DataContext = this;

      multiplier = (255.0 / (this.maxDepth - this.minDepth));

      InitializeComponent();

      //// Pan + Zoom event handlers
      //WPFWindow.MouseWheel += Overlay_MouseWheel;
      //canvas.MouseLeftButtonDown += canvas_MouseLeftButtonDown;
      //canvas.MouseLeftButtonUp += canvas_MouseLeftButtonUp;
      //canvas.MouseMove += canvas_MouseMove;

      // add depth image to our canvas
      //depthCanvasImage.Source = this.depthBitmap;
      //canvas.Children.Add(depthCanvasImage);

      if (File.Exists("key.txt")) {
        this.key = File.ReadAllText("key.txt");
      }
      if (string.IsNullOrWhiteSpace(this.key)) {
        MessageBox.Show(String.Format("{0}{1}{2}{3}",
          "Create a \"key.txt\" file in the root of the Client project. The key file should have your Azure Project Oxford key in it. It should be on a single line, no carriage return after the key.",
          "\r\n\r\n*********************************************************************************\r\n",
          "     DO NOT CHECK THE KEY.TXT FILE IN TO GITHUB!!!!!!!",
          "\r\n*********************************************************************************"));
        this.Close();
        return;
      }
      App.Initialize(this.key);
    }



    // Event Handlers
    private void BodyFrameReader_FrameArrived(object sender, BodyFrameArrivedEventArgs e) {
      using (var frame = e.FrameReference.AcquireFrame()) {
        if (frame == null) return;

        if (this.currentTrackedBody != null) {
          this.currentTrackedBody = FindBodyWithTrackingId(frame, this.currentTrackingId);
          if (this.currentTrackedBody != null) return;
        }
        Body selectedBody = FindClosestBody(frame);

        if (selectedBody == null) return;
        this.currentTrackedBody = selectedBody;
        this.currentTrackingId = selectedBody.TrackingId;
        highDefinitionFaceSource.TrackingId = this.currentTrackingId;
        faceFrameSource.TrackingId = this.currentTrackingId;
      }
    }

    private void HighDefinitionFaceFrameReader_FrameArrived(object sender, HighDefinitionFaceFrameArrivedEventArgs e) {
      using (var frame = e.FrameReference.AcquireFrame()) {
        if (frame != null) {
          using (var depthFrame = frame.DepthFrameReference.AcquireFrame()) {
            if (depthFrame != null) {
              SimpleDrawDepth(depthFrame, null);
            }
          }
          if (frame.IsFaceTracked) {
            frame.GetAndRefreshFaceAlignmentResult(highdefinitionFaceAlignment);
            UpdateFacePoints();
          }
        }
      }
    }

    private void FaceFrameReader_FrameArrived(object sender, FaceFrameArrivedEventArgs e) {
      var frame = e.FrameReference.AcquireFrame();
      if (frame != null && frame.FaceFrameResult != null && frame.FaceFrameResult.FaceBoundingBoxInColorSpace != null) {
        if (faceCaptured) return;
        using (var colorFrame = frame.ColorFrameReference.AcquireFrame()) {
          if (colorFrame == null) return;
          faceCaptured = true;
          var left = frame.FaceFrameResult.FaceBoundingBoxInColorSpace.Left - 150;
          left = left < 0 ? 0 : left;
          var top = frame.FaceFrameResult.FaceBoundingBoxInColorSpace.Top - 150;
          top = top < 0 ? 0 : top;
          var right = frame.FaceFrameResult.FaceBoundingBoxInColorSpace.Right + 150;
          right = right > colorFrame.FrameDescription.Width ? colorFrame.FrameDescription.Width : right;
          var bottom = frame.FaceFrameResult.FaceBoundingBoxInColorSpace.Bottom + 150;
          bottom = bottom > colorFrame.FrameDescription.Height ? colorFrame.FrameDescription.Height : bottom;
          var width = right - left;
          var height = bottom - top;
          using (KinectBuffer colorBuffer = colorFrame.LockRawImageBuffer()) {
            this.colorImage.Lock();
            if ((colorFrame.FrameDescription.Width == this.colorImage.PixelWidth) && (colorFrame.FrameDescription.Height == this.colorImage.PixelHeight)) {
              colorFrame.CopyConvertedFrameDataToIntPtr(
                  this.colorImage.BackBuffer,
                  (uint)(colorFrame.FrameDescription.Width * colorFrame.FrameDescription.Height * 4),
                  ColorImageFormat.Bgra);

              this.colorImage.AddDirtyRect(new Int32Rect(0, 0, this.colorImage.PixelWidth, this.colorImage.PixelHeight));
            }
            this.colorImage.Unlock();
          }
          SaveImage(left, top, width, height);
          Match2D(left, top, width, height).ConfigureAwait(continueOnCapturedContext: true);
        }
      }

    }

    private void uiScaleSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
      //TransformGroup transformGroup = (TransformGroup)canvas.LayoutTransform;
      //ScaleTransform transform = (ScaleTransform)transformGroup.Children[0];

      //double zoom = e.NewValue;
      //transform.ScaleX = zoom;
      //transform.ScaleY = zoom;
    }

    private void canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
      canvas.ReleaseMouseCapture();
    }

    private void canvas_MouseMove(object sender, MouseEventArgs e) {
      if (!canvas.IsMouseCaptured) return;
      Point p = e.MouseDevice.GetPosition(border);

      Matrix m = canvas.RenderTransform.Value;
      m.OffsetX = mouseOrigin.X + (p.X - mouseStart.X);
      m.OffsetY = mouseOrigin.Y + (p.Y - mouseStart.Y);

      canvas.RenderTransform = new MatrixTransform(m);
    }

    private void canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
      if (canvas.IsMouseCaptured) return;

      mouseStart = e.GetPosition(border);
      mouseOrigin.X = canvas.RenderTransform.Value.OffsetX;
      mouseOrigin.Y = canvas.RenderTransform.Value.OffsetY;
      canvas.CaptureMouse();
    }

    private void Overlay_MouseWheel(object sender, MouseWheelEventArgs e) {
      Point p = e.MouseDevice.GetPosition(canvas);

      Matrix m = canvas.RenderTransform.Value;
      if (e.Delta > 0)
        m.ScaleAtPrepend(1.1, 1.1, p.X, p.Y);
      else
        m.ScaleAtPrepend(1 / 1.1, 1 / 1.1, p.X, p.Y);

      canvas.RenderTransform = new MatrixTransform(m);
    }

    private void WPFWindow_Loaded(object sender, RoutedEventArgs e) {
      PointCloudWindow.Start(this);
    }



    // Methods
    private void SimpleDrawDepth(DepthFrame frame, ColorFrame colorFrame) {
      depthWidth = frame.FrameDescription.Width;
      depthHeight = frame.FrameDescription.Height;
      if (depthWidth == this.depthBitmap.PixelWidth && depthHeight == this.depthBitmap.PixelHeight) {
        frame.CopyFrameDataToArray(depthData);

        this.kinectSensor.CoordinateMapper.MapDepthFrameToCameraSpace(depthData, depthVertices);

        //int colorIndex = 0;
        var length = depthData.Length;
        for (int i = 0; i < length; ++i) {
          //depthVertices[i].X += 1;
          //depthVertices[i].Y += 1;
          //depthVertices[i].Z += 1;
          // This code will set the colors of the point cloud between the
          // minumum and maximum range
          ushort z = depthData[i];
          if (z > this.maxDepth || z < minDepth) {
            // Set all pixes out of bounds from our min/max range to white
            imageIntColors[i] = 0xffffff;
            continue;
          }
          int intensity = (int)((z - minDepth) * multiplier);
          //                       BLUE VALUE                 GREEN VALUE      RED VALUE
          //imageIntColors[i] = (intensity * 256 * 256) + (intensity * 256) + (intensity);
          //                     ONLY SETS THE BLUE VALUE
          //imageIntColors[i] = (intensity * 256 * 256);
          //                  ONLY SET THE GREEN VALUE This sets a fading green
          imageIntColors[i] = ((255 - intensity) * 256);
          // This sets a solid green
          //imageIntColors[i] = (0xff * 256);

        }
        if (this.OnVerticesUpdated != null) this.OnVerticesUpdated(depthVertices, imageIntColors);
      }
    }

    private Vector4 orientation;
    Queue<double> d1 = new Queue<double>(100);
    Queue<double> d2 = new Queue<double>(100);
    Queue<double> d3 = new Queue<double>(100);
    private void UpdateFacePoints() {
      if (highDefinitionFaceModel == null) return;

      var vertices = highDefinitionFaceModel.CalculateVerticesForAlignment(highdefinitionFaceAlignment);
      orientation = highdefinitionFaceAlignment.FaceOrientation;
      if (hdFaceVertices == null) {
        hdFaceVertices = new CameraSpacePoint[vertices.Count];
        hdFaceColors = new int[vertices.Count];
        for (int i = 0; i < hdFaceVertices.Length; i++) {
          hdFaceVertices[i] = new CameraSpacePoint();
        }
      }

      var matched = 0;
      if (vertices.Count > 0 && defaultVertices != null) {
        var count = defaultVertices.Count;
        for (int i = 0; i < count; i++) {
          // align saved vertice with live face
          tempVertice.X = savedVertices[i].X - (defaultVertices[i].X - vertices[i].X);
          tempVertice.Y = savedVertices[i].Y - (defaultVertices[i].Y - vertices[i].Y);
          tempVertice.Z = savedVertices[i].Z - (defaultVertices[i].Z - vertices[i].Z);

          hdFaceVertices[i].X = tempVertice.X;
          hdFaceVertices[i].Y = tempVertice.Y;
          hdFaceVertices[i].Z = tempVertice.Z;

          //var ellipse = savedDots[i];
          var point = this.kinectSensor.CoordinateMapper.MapCameraPointToDepthSpace(tempVertice);
          // do nothing if we cannot properly map the vertice to 2D space
          if (float.IsInfinity(point.X) || float.IsInfinity(point.Y)) continue;

          if (checkPointMatches == 0) {
            // reset dot to blue
            /*
            if (i == 210 || i == 469 || i == 843 || i == 1117 || i == 140 || i == 758 || i == 18 || i == 14 || i == 156 || i == 783 || i == 24 || i == 151 || i == 772) {
              hdFaceColors[i] = 0x00ffff;
              //ellipse.Fill = Brushes.Blue;
            } else {
              hdFaceColors[i] = 0xff0000;
              //ellipse.Fill = Brushes.Blue;
            }
            //*/

            hdFaceColors[i] = 0xff0000;
            int depthPosition = (int)((Math.Round(point.Y) * depthWidth) + Math.Round(point.X));
            if (depthPosition >= 0 && depthPosition < depthVertices.Length) {
              // NOTE: this depthVertice is a depth value from the point cloud converted
              //       to a CameraSpacePoint (aka vertice).
              var depthVertice = depthVertices[depthPosition];
              //* sweet spot tollerance is 0.008???
              if (VectorDistance(depthVertice, tempVertice) <= 0.008) {
                // change dot to red if it's vertice was within the set tollerance for the point on the live face
                hdFaceColors[i] = 0x0000ff;
                //ellipse.Fill = Brushes.Red;
                matched++;
              }
              //*/
              /* old dot comparison logic
              if (depthVertice.X >= tempVertice.X - tollerance
                  && depthVertice.Y >= tempVertice.Y - tollerance
                  && depthVertice.Z >= tempVertice.Z - tollerance
                  && depthVertice.X <= tempVertice.X + tollerance
                  && depthVertice.Y <= tempVertice.Y + tollerance
                  && depthVertice.Z <= tempVertice.Z + tollerance) {
                // change dot to red if it's vertice was within the set tollerance for the point on the live face
                ellipse.Fill = Brushes.Red;
                matched++;
              }
              //*/
            }
          }
          //Canvas.SetLeft(ellipse, point.X);
          //Canvas.SetTop(ellipse, point.Y);
        }
        if (matched != 0) matchCount.Content = String.Format("Red Dots: {0}", matched);
        checkPointMatches = ++checkPointMatches % 15;
      }








      //HighDetailFacePoints_LefteyeInnercorner = 210,
      //HighDetailFacePoints_LefteyeOutercorner = 469,
      //HighDetailFacePoints_RighteyeInnercorner = 843,
      //HighDetailFacePoints_RighteyeOutercorner = 1117,
      //HighDetailFacePoints_LefteyebrowOuter = 140,
      //HighDetailFacePoints_RighteyebrowOuter = 758,
      //HighDetailFacePoints_NoseTip = 18,
      //HighDetailFacePoints_NoseBottom = 14,
      //HighDetailFacePoints_NoseBottomleft = 156,
      //HighDetailFacePoints_NoseBottomright = 783,
      //HighDetailFacePoints_NoseTop = 24,
      //HighDetailFacePoints_NoseTopleft = 151,
      //HighDetailFacePoints_NoseTopright = 772,

      d1.Enqueue(GetMaskPointDistance(18, 24));
      d2.Enqueue(GetMaskPointDistance(18, 469));
      d3.Enqueue(GetMaskPointDistance(24, 469));
      if (d1.Count > 100) {
        d1.Dequeue();
        d2.Dequeue();
        d3.Dequeue();
      }
      if (this.OnHdFaceUpdated != null) {
        this.OnHdFaceUpdated(
          hdFaceVertices,
          hdFaceColors,
          matched,
          d1.Average(),
          d2.Average(),
          d3.Average(),
          highdefinitionFaceAlignment.FaceOrientation.Z);
      }
    }

    public double GetMaskPointDistance(int i1, int i2) {
      bool goodFrame = true;
      var p1 = this.kinectSensor.CoordinateMapper.MapCameraPointToDepthSpace(hdFaceVertices[i1]);
      if (float.IsInfinity(p1.X) || float.IsInfinity(p1.Y)) {
        p1.X = 0;
        p1.Y = 0;
        goodFrame = false;
      }
      int depthIndex1 = (int)((Math.Round(p1.Y) * depthWidth) + Math.Round(p1.X));
      var p2 = this.kinectSensor.CoordinateMapper.MapCameraPointToDepthSpace(hdFaceVertices[i2]);
      if (float.IsInfinity(p2.X) || float.IsInfinity(p2.Y)) {
        p2.X = 0;
        p2.Y = 0;
        goodFrame = false;
      }
      int depthIndex2 = (int)((Math.Round(p2.Y) * depthWidth) + Math.Round(p2.X));

      if (goodFrame) {
        return Math.Floor(VectorDistance(depthVertices[depthIndex1], depthVertices[depthIndex2]) * 10000) / 10000;
      }

      return 0;
    }

    public void NotifyPropertyChanged(string propertyName) {
      if (PropertyChanged != null) {
        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
      }
    }

    private static double VectorDistance(CameraSpacePoint point1, CameraSpacePoint point2) {
      var result = Math.Pow(point1.X - point2.X, 2) + Math.Pow(point1.Y - point2.Y, 2) + Math.Pow(point1.Z - point2.Z, 2);

      result = Math.Sqrt(result);

      return result;
    }

    /// <summary>
    /// Returns the length of a vector from origin
    /// </summary>
    /// <param name="point">Point in space to find it's distance from origin</param>
    /// <returns>Distance from origin</returns>
    private static double VectorLength(CameraSpacePoint point) {
      var result = Math.Pow(point.X, 2) + Math.Pow(point.Y, 2) + Math.Pow(point.Z, 2);

      result = Math.Sqrt(result);

      return result;
    }

    /// <summary>
    /// Finds the closest body from the sensor if any
    /// </summary>
    /// <param name="bodyFrame">A body frame</param>
    /// <returns>Closest body, null of none</returns>
    private static Body FindClosestBody(BodyFrame bodyFrame) {
      Body result = null;
      double closestBodyDistance = double.MaxValue;

      Body[] bodies = new Body[bodyFrame.BodyCount];
      bodyFrame.GetAndRefreshBodyData(bodies);

      foreach (var body in bodies) {
        if (body.IsTracked) {
          var currentLocation = body.Joints[JointType.SpineBase].Position;

          var currentDistance = VectorLength(currentLocation);

          if (result == null || currentDistance < closestBodyDistance) {
            result = body;
            closestBodyDistance = currentDistance;
          }
        }
      }

      return result;
    }

    /// <summary>
    /// Find if there is a body tracked with the given trackingId
    /// </summary>
    /// <param name="bodyFrame">A body frame</param>
    /// <param name="trackingId">The tracking Id</param>
    /// <returns>The body object, null of none</returns>
    private static Body FindBodyWithTrackingId(BodyFrame bodyFrame, ulong trackingId) {
      Body result = null;

      Body[] bodies = new Body[bodyFrame.BodyCount];
      bodyFrame.GetAndRefreshBodyData(bodies);

      foreach (var body in bodies) {
        if (body.IsTracked) {
          if (body.TrackingId == trackingId) {
            result = body;
            break;
          }
        }
      }

      return result;
    }

    // Loads the mesh that we think will best fit your face and then we run 3d
    // comparisons against it and your live face as a 3D facial recognition.
    private void LoadSavedFaceMesh(string path) {
      // load in saved face mesh
      var jss = new JavaScriptSerializer();
      using (var file = new System.IO.StreamReader(path)) {
        var data = file.ReadLine();
        savedVertices = jss.Deserialize<CameraSpacePoint[]>(data);
      }

      var averageModel = new FaceModel();
      var defaultAlignment = new FaceAlignment();
      defaultVertices = averageModel.CalculateVerticesForAlignment(defaultAlignment);

      var length = savedVertices.Length;
      for (int i = 0; i < length; i++) {
        System.Windows.Shapes.Ellipse ellipse = null;
        ellipse = new System.Windows.Shapes.Ellipse {
          Width = 2.0,
          Height = 2.0,
          Fill = new SolidColorBrush(Colors.Blue)
        };
        savedDots.Add(ellipse);
      }
      foreach (System.Windows.Shapes.Ellipse ellipse in savedDots) {
        canvas.Children.Add(ellipse);
      }
    }

    public static System.Drawing.Bitmap ScaleImage(System.Drawing.Bitmap image, int maxWidth, int maxHeight) {
      var ratioX = (double)maxWidth / image.Width;
      var ratioY = (double)maxHeight / image.Height;
      var ratio = Math.Min(ratioX, ratioY);

      var newWidth = (int)(image.Width * ratio);
      var newHeight = (int)(image.Height * ratio);

      var newImage = new System.Drawing.Bitmap(newWidth, newHeight);

      using (var graphics = System.Drawing.Graphics.FromImage(newImage))
        graphics.DrawImage(image, 0, 0, newWidth, newHeight);

      return newImage;
    }

    private void SaveImage(int left, int top, int width, int height) {
      var encoder = new JpegBitmapEncoder();
      System.Drawing.Bitmap bmp = null;
      encoder.Frames.Add(BitmapFrame.Create(colorImage));
      using (var jpgStream = new MemoryStream()) {
        encoder.Save(jpgStream);
        bmp = new System.Drawing.Bitmap(jpgStream);
        if (bmp == null) {
          faceCaptured = false;
          return;
        }
        bmp = bmp.Clone(new System.Drawing.Rectangle(left, top, width, height), System.Drawing.Imaging.PixelFormat.DontCare);
        // scale more to save on bandwidth at the cost of quality/precision
        bmp = ScaleImage(bmp, 256, 256);
        bmp.Save(@"data\face.png");
      }
    }

    private async Task Match2D(int left, int top, int width, int height) {
      if (!File.Exists(@"data\face.png")) return;
      using (var fStream = File.OpenRead(@"data\face.png")) {
        var groups = await App.Instance.GetPersonGroupsAsync();
        var group = groups.Where(g => g.Name == "First Test").FirstOrDefault();
        if (group == null) {
          faceCaptured = false;
          return;
        }
        var faces = await App.Instance.DetectAsync(fStream);
        if (faces == null || faces.Length == 0) {
          matchName.Content = "No face found";
          faceCaptured = false;
          return;
        } else {
          var identifyResults = await App.Instance.IdentifyAsync(group.PersonGroupId, faces.Select(f => f.FaceId).ToArray());
          var found = 0;
          var names = "";
          foreach (var result in identifyResults) {
            foreach (var candidate in result.Candidates) {
              if (candidate.Confidence > 0.5) {
                var person = await App.Instance.GetPersonAsync(group.PersonGroupId, candidate.PersonId);
                var attributes = faces.Where(f => f.FaceId == result.FaceId).Select(f => f.Attributes).FirstOrDefault();
                names += String.Format("{0}, ", person.Name);
                found++;
              }
            }
          }
          if (found > 0) {
            matchName.Content = String.Format("Name{0}: {1}", (found > 1 ? "s" : ""), names.Substring(0, names.Length - 2));
            var name = names.Split(new string[] { ", " }, StringSplitOptions.None).FirstOrDefault();
            OnTwoDMatchFound(name);
            var fileName = String.Format(@"data\{0}.fml", name);
            if (File.Exists(fileName)) {
              LoadSavedFaceMesh(fileName);
            } else {
              matchName.Content = String.Format("No saved mesh for {0}.", name);
              faceCaptured = false;
            }
          } else {
            matchName.Content = "No match found for this person";
            faceCaptured = false;
          }
        }
      }
    }
  }
}
