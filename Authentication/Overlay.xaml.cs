namespace Trainer {
  using Microsoft.Kinect;
  using Microsoft.Kinect.Face;
  using System;
  using System.Collections.Generic;
  using System.ComponentModel;
  using System.Web.Script.Serialization;
  using System.Windows;
  using System.Windows.Controls;
  using System.Windows.Input;
  using System.Windows.Media;
  using System.Windows.Media.Imaging;

  public partial class Overlay : Window, INotifyPropertyChanged {
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
    private float tollerance = 0.0015f;
    // Saved HD Face Variables
    private List<System.Windows.Shapes.Ellipse> savedDots = new List<System.Windows.Shapes.Ellipse>();
    private CameraSpacePoint[] savedVertices = null;
    // Face Frame Variables
    private FaceFrameSource faceFrameSource = null;
    private FaceFrameReader faceFrameReader = null;
    // Depth Variables
    private DepthFrameReader depthFrameReader = null;
    private ushort minDepth = 500;
    private ushort maxDepth = 1000;
    private double multiplier;
    private ushort[] depthData;
    private Image depthCanvasImage = new Image();
    private WriteableBitmap depthBitmap = null;
    private CameraSpacePoint[] depthVertices = null;
    private int depthWidth;
    private int depthHeight;
    // Mouse Variables
    private Point mouseOrigin;
    private Point mouseStart;
    // Property Variables
    public event PropertyChangedEventHandler PropertyChanged;



    // Constructors
    public Overlay() {
      // get the kinectSensor object
      this.kinectSensor = KinectSensor.GetDefault();

      // create the colorFrameDescription from the ColorFrameSource using Bgra format
      FrameDescription depthFrameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;
      this.depthBitmap = new WriteableBitmap(depthFrameDescription.Width, depthFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);
      depthVertices = new CameraSpacePoint[depthFrameDescription.Width * depthFrameDescription.Height];

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

      // Pan + Zoom event handlers
      WPFWindow.MouseWheel += Overlay_MouseWheel;
      canvas.MouseLeftButtonDown += canvas_MouseLeftButtonDown;
      canvas.MouseLeftButtonUp += canvas_MouseLeftButtonUp;
      canvas.MouseMove += canvas_MouseMove;

      // add depth image to our canvas
      depthCanvasImage.Source = this.depthBitmap;
      canvas.Children.Add(depthCanvasImage);

      LoadSavedFaceMesh(@"data\kirk.fml");
    }

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
              SimpleDrawDepth(depthFrame);
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
        // Grab the person's face (padded a little) and use oxford to do the initial 2D recognition
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



    // Methods
    private void SimpleDrawDepth(DepthFrame frame) {
      depthWidth = frame.FrameDescription.Width;
      depthHeight = frame.FrameDescription.Height;
      if (depthWidth == this.depthBitmap.PixelWidth && depthHeight == this.depthBitmap.PixelHeight) {
        this.depthBitmap.Lock();
        depthData = new ushort[depthWidth * depthHeight];
        byte[] pixelData = new byte[depthWidth * depthHeight * (PixelFormats.Bgr32.BitsPerPixel + 7) / 8];
        int stride = depthWidth * PixelFormats.Bgr32.BitsPerPixel / 8;

        frame.CopyFrameDataToArray(depthData);

        this.kinectSensor.CoordinateMapper.MapDepthFrameToCameraSpace(depthData, depthVertices);

        int colorIndex = 0;
        var length = depthData.Length;
        for (int i = 0; i < length; ++i) {
          ushort z = depthData[i];
          if (z > this.maxDepth || z < minDepth) {
            pixelData[colorIndex++] = 0; // Blue
            pixelData[colorIndex++] = 0; // Green
            pixelData[colorIndex++] = 0; // Red
            ++colorIndex;
            continue;
          }
          byte intensity = (byte)((z - minDepth) * multiplier);

          pixelData[colorIndex++] = intensity; // Blue
          pixelData[colorIndex++] = intensity; // Green
          pixelData[colorIndex++] = intensity; // Red

          ++colorIndex;
        }

        var box = new Int32Rect(0, 0, this.depthBitmap.PixelWidth, this.depthBitmap.PixelHeight);
        this.depthBitmap.WritePixels(box, pixelData, stride, 0);
        this.depthBitmap.AddDirtyRect(box);
        this.depthBitmap.Unlock();
      }
    }

    private void UpdateFacePoints() {
      if (highDefinitionFaceModel == null) return;

      var vertices = highDefinitionFaceModel.CalculateVerticesForAlignment(highdefinitionFaceAlignment);
      if (vertices.Count > 0) {
        var count = defaultVertices.Count;
        for (int i = 0; i < count; i++) {
          // align saved vertice with live face
          tempVertice.X = savedVertices[i].X - (defaultVertices[i].X - vertices[i].X);
          tempVertice.Y = savedVertices[i].Y - (defaultVertices[i].Y - vertices[i].Y);
          tempVertice.Z = savedVertices[i].Z - (defaultVertices[i].Z - vertices[i].Z);

          var ellipse = savedDots[i];
          DepthSpacePoint point = this.kinectSensor.CoordinateMapper.MapCameraPointToDepthSpace(tempVertice);
          // do nothing if we cannot properly map the vertice to 2D space
          if (float.IsInfinity(point.X) || float.IsInfinity(point.Y)) continue;

          if (checkPointMatches == 0) {
            // reset dot to blue
            ellipse.Fill = Brushes.Blue;
            int depthPosition = ((Convert.ToInt32(point.Y) * depthWidth) + Convert.ToInt32(point.X));
            if (depthPosition >= 0 && depthPosition < depthVertices.Length) {
              // NOTE: this depthVertice is a depth value from the point cloud converted
              //       to a CameraSpacePoint (aka vertice).
              var depthVertice = depthVertices[depthPosition];
              if (depthVertice.X >= tempVertice.X - tollerance
                  && depthVertice.Y >= tempVertice.Y - tollerance
                  && depthVertice.Z >= tempVertice.Z - tollerance
                  && depthVertice.X <= tempVertice.X + tollerance
                  && depthVertice.Y <= tempVertice.Y + tollerance
                  && depthVertice.Z <= tempVertice.Z + tollerance) {
                // change dot to red if it's vertice was within the set tollerance for the point on the live face
                ellipse.Fill = Brushes.Red;
              }
            }
          }

          System.Windows.Controls.Canvas.SetLeft(ellipse, point.X);
          System.Windows.Controls.Canvas.SetTop(ellipse, point.Y);
        }

        checkPointMatches = ++checkPointMatches % 10;
      }
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

  }
}
