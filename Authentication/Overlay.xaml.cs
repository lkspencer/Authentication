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
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;

    // EMGU documentation link for our reference: http://www.emgu.com/wiki/files/3.0.0-alpha/document/html/b72c032d-59ae-c36f-5e00-12f8d621dfb8.htm
    public partial class Overlay : Window, INotifyPropertyChanged {
    // MainWindow Variables
    private KinectSensor kinectSensor = null;
    private DepthFrameReader depthFrameReader = null;
    BodyFrameReader bodyFrameReader = null;
    IList<Body> bodies = null;
    private WriteableBitmap threeDBitmap = null;
    private HighDefinitionFaceFrameSource highDefinitionFaceSource = null;
    private HighDefinitionFaceFrameReader highDefinitionFaceReader = null;
    private FaceAlignment highdefinitionFaceAlignment = null;
    private FaceModel highDefinitionFaceModel = null;
    private List<System.Windows.Shapes.Ellipse> points = new List<System.Windows.Shapes.Ellipse>();
    private ushort minDepth = 2500;
    private ushort maxDepth = 3000;
    private double multiplier;
    public event PropertyChangedEventHandler PropertyChanged;
    private Body currentTrackedBody = null;
    private ulong currentTrackingId = 0;
    private ushort[] depthData;
    private Image depthCanvasImage = new Image();



    // Constructors
    public Overlay() {
      // get the kinectSensor object
      this.kinectSensor = KinectSensor.GetDefault();

      // create the colorFrameDescription from the ColorFrameSource using Bgra format
      FrameDescription depthFrameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;

      // create the writeable bitmap to display our frames
      this.threeDBitmap = new WriteableBitmap(depthFrameDescription.Width, depthFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);

      this.highDefinitionFaceModel = new FaceModel();
      this.highdefinitionFaceAlignment = new FaceAlignment();
      this.highDefinitionFaceSource = new HighDefinitionFaceFrameSource(this.kinectSensor);
      this.highDefinitionFaceSource.TrackingQuality = FaceAlignmentQuality.High;
      this.highDefinitionFaceSource.FaceModel = this.highDefinitionFaceModel;
      // Start capturing face model to use later


      // open the reader for the face frames
      this.bodyFrameReader = this.kinectSensor.BodyFrameSource.OpenReader();
      this.depthFrameReader =  this.kinectSensor.DepthFrameSource.OpenReader();
      this.highDefinitionFaceReader = this.highDefinitionFaceSource.OpenReader();

      // wire handlers for frame arrivals
      this.bodyFrameReader.FrameArrived += this.BodyFrameReader_FrameArrived;
      this.depthFrameReader.FrameArrived += this.DepthFrameReader_FrameArrived;
      this.highDefinitionFaceReader.FrameArrived += this.HighDefinitionFaceFrameReader_FrameArrived;

      // open the sensor
      this.kinectSensor.Open();

      // use the window object as the view model in this simple example
      this.DataContext = this;

      InitializeComponent();
      depthCanvasImage.Source = this.threeDBitmap;
      canvas.Children.Add(depthCanvasImage);
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
      }
    }

    private void DepthFrameReader_FrameArrived(object sender, DepthFrameArrivedEventArgs e) {
      //*
      using (var depthFrame = e.FrameReference.AcquireFrame()) {
        if (depthFrame != null) {
          SimpleDrawDepth(depthFrame);
        }
      }
      //*/
    }

    private void HighDefinitionFaceFrameReader_FrameArrived(object sender, HighDefinitionFaceFrameArrivedEventArgs e) {
      //*
      using (var frame = e.FrameReference.AcquireFrame()) {
        if (frame != null && frame.IsFaceTracked) {
          frame.GetAndRefreshFaceAlignmentResult(highdefinitionFaceAlignment);
          UpdateFacePoints();
        }
      }
      //*/
    }



    // Methods
    private void SimpleDrawDepth(DepthFrame frame) {
      this.threeDBitmap.Lock();
      int width = frame.FrameDescription.Width;
      int height = frame.FrameDescription.Height;
      //ushort[] depthData = new ushort[width * height];
      depthData = new ushort[width * height];
      byte[] pixelData = new byte[width * height * (PixelFormats.Bgr32.BitsPerPixel + 7) / 8];
      int stride = width * PixelFormats.Bgr32.BitsPerPixel / 8;
      multiplier = (255.0 / (this.maxDepth - this.minDepth));

      frame.CopyFrameDataToArray(depthData);
      CameraSpacePoint[] vertices = new CameraSpacePoint[depthData.Length];
      this.kinectSensor.CoordinateMapper.MapDepthFrameToCameraSpace(depthData, vertices);
      /*
      var length = vertices.Length;
      for (int i = 0; i < length; i++) {
        vertices[i] = vertices[i].RotateY(10);
      }
      DepthSpacePoint[] depths = new DepthSpacePoint[depthData.Length];
      this.kinectSensor.CoordinateMapper.MapCameraPointsToDepthSpace(vertices, depths);
      */
      /*
      if (facePoints != null) {
        var xnose = facePoints[FacePointType.Nose].X;
        var xlefteye = facePoints[FacePointType.EyeLeft].X;
        var ynose = facePoints[FacePointType.Nose].Y;
        var ylefteye = facePoints[FacePointType.EyeLeft].Y;

      }
      */


      int colorIndex = 0;
      for (int depthIndex = 0; depthIndex < depthData.Length; ++depthIndex) {
        ushort z = depthData[depthIndex];
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

      if (frame.FrameDescription.Width == this.threeDBitmap.PixelWidth && frame.FrameDescription.Height == this.threeDBitmap.PixelHeight) {
        var box = new Int32Rect(0, 0, this.threeDBitmap.PixelWidth, this.threeDBitmap.PixelHeight);
        this.threeDBitmap.WritePixels(box, pixelData, stride, 0);
        this.threeDBitmap.AddDirtyRect(box);
      }

      this.threeDBitmap.Unlock();
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

        //*
        for (int index = 0; index < vertices.Count; index++) {
          CameraSpacePoint vertice = vertices[index];
          DepthSpacePoint point = this.kinectSensor.CoordinateMapper.MapCameraPointToDepthSpace(vertice);

          if (float.IsInfinity(point.X) || float.IsInfinity(point.Y)) continue;

          //vertice.Z = depthData[(this.threeDBitmap.PixelWidth * Convert.ToInt32(point.Y)) + Convert.ToInt32(point.X)];
          //point = this.kinectSensor.CoordinateMapper.MapCameraPointToDepthSpace(vertice);
          //if (float.IsInfinity(point.X) || float.IsInfinity(point.Y)) continue;

          System.Windows.Shapes.Ellipse ellipse = points[index];

          System.Windows.Controls.Canvas.SetLeft(ellipse, point.X);
          System.Windows.Controls.Canvas.SetTop(ellipse, point.Y);
        }
        //*/
      }
    }

    public void NotifyPropertyChanged(string propertyName) {
      if (PropertyChanged != null) {
        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
      }
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

        private void uiScaleSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //TransformGroup transformGroup = (TransformGroup)canvas.LayoutTransform;
            //ScaleTransform transform = (ScaleTransform)transformGroup.Children[0];

            //double zoom = e.NewValue;
            //transform.ScaleX = zoom;
            //transform.ScaleY = zoom;
        }
    }
}
