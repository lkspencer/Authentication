namespace OpenGlTest {
  using Microsoft.Kinect;
  using Microsoft.Kinect.Face;
  using OpenTK;
  using OpenTK.Graphics.OpenGL;
  using OpenTK.Input;
  using System;
  using System.Collections.Generic;
  using System.Drawing;
  using System.IO;
  using System.Web.Script.Serialization;

  class Program : GameWindow {
    private int vbo;
    private int verticeCount;
    private Matrix4 cameraMatrix;
    private float[] mouseSpeed = new float[2];
    private Vector2 mouseDelta;
    private Vector3 location;
    private Vector3 up = Vector3.UnitY;
    private float pitch = 0.0f;
    private float facing = 0.0f;
    bool wdown = false;
    bool adown = false;
    bool sdown = false;
    bool ddown = false;
    bool escdown = false;
    // main kinect variables
    private KinectSensor kinectSensor = null;
    // Body Variables
    BodyFrameReader bodyFrameReader = null;
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
    // Saved HD Face Variables
    //private List<System.Windows.Shapes.Ellipse> savedDots = new List<System.Windows.Shapes.Ellipse>();
    private CameraSpacePoint[] savedVertices = null;
    // Face Frame Variables
    private FaceFrameSource faceFrameSource = null;
    private FaceFrameReader faceFrameReader = null;
    private bool faceCaptured = false;
    // Depth Variables
    private DepthFrameReader depthFrameReader = null;
    private ushort minDepth = 500;
    private ushort maxDepth = 1000;
    private double multiplier;
    private ushort[] depthData;
    //private Image depthCanvasImage = new Image();
    //private WriteableBitmap depthBitmap = null;
    private CameraSpacePoint[] depthVertices = null;
    private int depthWidth;
    private int depthHeight;
    private byte[] pixelData;
    private int stride;



    public Program() : base(1024, 768) {
      GL.Enable(EnableCap.DepthTest);
    }



    // Event Handlers
    protected override void OnLoad(EventArgs e) {
      base.OnLoad(e);
      GL.ClearColor(Color.Black);
      GL.PointSize(3f);
      CreateVertexBuffer();

      cameraMatrix = Matrix4.Identity;
      location = new Vector3(0f, 0f, 0f);
      mouseDelta = new Vector2();

      // center mouse on the game window
      //System.Windows.Forms.Cursor.Position = new Point(Bounds.Left + Bounds.Width / 2, Bounds.Top + Bounds.Height / 2);

      // setup event handlers
      MouseWheel += OnMouseWheel;
      MouseMove += OnMouseMove;
      KeyDown += OnKeyDown;
      KeyUp += OnKeyUp;


      this.kinectSensor = KinectSensor.GetDefault();
      FrameDescription depthFrameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;
      depthVertices = new CameraSpacePoint[depthFrameDescription.Width * depthFrameDescription.Height];
      depthData = new ushort[depthFrameDescription.Width * depthFrameDescription.Height];
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
      //this.faceFrameReader.FrameArrived += this.FaceFrameReader_FrameArrived;

      // open the sensor
      this.kinectSensor.Open();
    }












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
            //UpdateFacePoints();
          }
        }
      }
    }

    protected void OnKeyDown(object sender, KeyboardKeyEventArgs e) {
      if (e.Key == Key.W) wdown = true;
      if (e.Key == Key.A) adown = true;
      if (e.Key == Key.S) sdown = true;
      if (e.Key == Key.D) ddown = true;
      if (e.Key == Key.Escape) escdown = true;
    }

    protected void OnKeyUp(object sender, KeyboardKeyEventArgs e) {
      if (e.Key == Key.W) wdown = false;
      if (e.Key == Key.A) adown = false;
      if (e.Key == Key.S) sdown = false;
      if (e.Key == Key.D) ddown = false;
      if (e.Key == Key.Escape) escdown = false;
    }

    protected void OnMouseWheel(object sender, MouseWheelEventArgs e) {
      if (e.Delta > 0)
        location.Y -= 0.1f;
      else
        location.Y += 0.1f;
    }

    protected void OnMouseMove(object sender, MouseMoveEventArgs e) {
      mouseDelta = new Vector2(e.XDelta, e.YDelta);
    }

    protected override void OnRenderFrame(FrameEventArgs e) {
      base.OnRenderFrame(e);
      GL.MatrixMode(MatrixMode.Modelview);
      GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
      GL.LoadMatrix(ref cameraMatrix);

      GL.EnableVertexAttribArray(0);
      GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
      GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 10, 0);

      GL.DrawArrays(PrimitiveType.Points, 0, verticeCount);

      GL.DisableVertexAttribArray(0);

      SwapBuffers();
    }

    protected override void OnUpdateFrame(FrameEventArgs e) {
      //*
      if (float.IsNaN(facing) || float.IsInfinity(facing)) facing = 0.0f;
      if (wdown) {
        location.X += (float)Math.Cos(facing) * 0.01f;
        location.Z += (float)Math.Sin(facing) * 0.01f;
      }

      if (sdown) {
        location.X -= (float)Math.Cos(facing) * 0.01f;
        location.Z -= (float)Math.Sin(facing) * 0.01f;
      }

      if (adown) {
        location.X -= (float)Math.Cos(facing + Math.PI / 2) * 0.01f;
        location.Z -= (float)Math.Sin(facing + Math.PI / 2) * 0.01f;
      }

      if (ddown) {
        location.X += (float)Math.Cos(facing + Math.PI / 2) * 0.01f;
        location.Z += (float)Math.Sin(facing + Math.PI / 2) * 0.01f;
      }

      mouseSpeed[0] *= 0.9f;
      mouseSpeed[1] *= 0.9f;
      mouseSpeed[0] += mouseDelta.X / 1000f;
      mouseSpeed[1] += mouseDelta.Y / 1000f;
      mouseDelta = new Vector2();

      facing += mouseSpeed[0];
      pitch += mouseSpeed[1];
      Vector3 lookatPoint = new Vector3((float)Math.Cos(facing), (float)Math.Sin(pitch), (float)Math.Sin(facing));
      cameraMatrix = Matrix4.LookAt(location, location + lookatPoint, up);

      if (escdown) Exit();
      //*/
    }

    protected override void OnResize(EventArgs e) {
      //*
      base.OnResize(e);

      GL.Viewport(ClientRectangle.X, ClientRectangle.Y, ClientRectangle.Width, ClientRectangle.Height);

      Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView((float)Math.PI / 4, Width / (float)Height, 1.0f, 64.0f);
      GL.MatrixMode(MatrixMode.Projection);
      GL.LoadMatrix(ref projection);
      //*/
    }



    // Methods
    private void SimpleDrawDepth(DepthFrame frame) {
      depthWidth = frame.FrameDescription.Width;
      depthHeight = frame.FrameDescription.Height;
      frame.CopyFrameDataToArray(depthData);

      this.kinectSensor.CoordinateMapper.MapDepthFrameToCameraSpace(depthData, depthVertices);
    }

    private Vector3[] LoadFml(string path) {
      var jss = new JavaScriptSerializer();

      if (!File.Exists(@"kirk.fml")) return new Vector3[0];
      using (var file = new StreamReader(path)) {
        var data = file.ReadLine();
        if (string.IsNullOrWhiteSpace(data)) return new Vector3[0];
        var vectors = jss.Deserialize<Vector3[]>(data);
        return vectors;
      }
    }

    private void CreateVertexBuffer() {
      var vertices = LoadFml(@"kirk.fml");
      verticeCount = vertices.Length;
      GL.GenBuffers(1, out vbo);
      GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
      GL.BufferData<Vector3>(BufferTarget.ArrayBuffer,
                             new IntPtr(vertices.Length * Vector3.SizeInBytes),
                             vertices, BufferUsageHint.StaticDraw);
    }

    public static void Main(string[] args) {
      using (Program p = new Program()) {
        p.Run(60);
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

  }
}
