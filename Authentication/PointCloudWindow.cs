namespace Trainer {
  using Microsoft.Kinect;
  using OpenTK;
  using OpenTK.Graphics.OpenGL;
  using OpenTK.Input;
  using System;
  using System.Drawing;
  using System.IO;
  using System.Web.Script.Serialization;

  public class PointCloudWindow : GameWindow {
    private int vbo;
    private int verticeCount;
    private Matrix4 cameraMatrix;
    private float[] mouseSpeed = new float[2];
    private Vector2 mouseDelta;
    private Vector3 location;
    private Vector3 up = Vector3.UnitY;
    private float pitch = 0.0f;
    private float facing = 0.0f;
    private bool wdown = false;
    private bool adown = false;
    private bool sdown = false;
    private bool ddown = false;
    private bool escdown = false;
    private Vector3[] depthVectors;



    public PointCloudWindow(Overlay overlay) : base(1024, 768) {
      GL.Enable(EnableCap.DepthTest);
      overlay.OnVerticesUpdated += Overlay_OnVerticesUpdated;
    }




    // Event Handlers
    protected override void OnLoad(EventArgs e) {
      base.OnLoad(e);
      GL.ClearColor(Color.Black);
      GL.PointSize(3f);
      //CreateVertexBuffer();

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
      base.OnResize(e);

      GL.Viewport(ClientRectangle.X, ClientRectangle.Y, ClientRectangle.Width, ClientRectangle.Height);

      Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView((float)Math.PI / 4, Width / (float)Height, 1.0f, 64.0f);
      GL.MatrixMode(MatrixMode.Projection);
      GL.LoadMatrix(ref projection);
    }

    private void Overlay_OnVerticesUpdated(CameraSpacePoint[] cameraSpacePoints) {
      var length = cameraSpacePoints.Length;
      bool first = false;
      if (depthVectors == null) {
        depthVectors = new Vector3[length];
        verticeCount = depthVectors.Length;
        first = true;
        for (int i = 0; i < length; i++) {
          depthVectors[i] = new Vector3(cameraSpacePoints[i].X, cameraSpacePoints[i].Y, cameraSpacePoints[i].Z);
        }
        GL.GenBuffers(1, out vbo);
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        GL.BufferData<Vector3>(BufferTarget.ArrayBuffer,
                               new IntPtr(depthVectors.Length * Vector3.SizeInBytes),
                               depthVectors, BufferUsageHint.StaticDraw);
      } else {
        for (int i = 0; i < length; i++) {
          depthVectors[i].X = cameraSpacePoints[i].X;
          depthVectors[i].Y = cameraSpacePoints[i].Y;
          depthVectors[i].Z = cameraSpacePoints[i].Z;
        }
        /*
        GL.GenBuffers(1, out vbo);
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        //*/
        GL.BufferData<Vector3>(BufferTarget.ArrayBuffer,
                         IntPtr.Zero,
                         depthVectors, BufferUsageHint.StaticDraw);
        var ptr = GL.MapBuffer(BufferTarget.ArrayBuffer, BufferAccess.ReadWrite);
        GL.BufferData<Vector3>(BufferTarget.ArrayBuffer,
                         new IntPtr(depthVectors.Length * Vector3.SizeInBytes),
                         depthVectors, BufferUsageHint.StaticDraw);
      }
    }



    // Methods
    private Vector3[] LoadFml(string path) {
      var jss = new JavaScriptSerializer();

      if (!File.Exists(path)) return new Vector3[0];
      using (var file = new StreamReader(path)) {
        var data = file.ReadLine();
        if (string.IsNullOrWhiteSpace(data)) return new Vector3[0];
        var vectors = jss.Deserialize<Vector3[]>(data);
        return vectors;
      }
    }

    private void CreateVertexBuffer() {
      var vertices = LoadFml(@"data\kirk.fml");
      verticeCount = vertices.Length;
      GL.GenBuffers(1, out vbo);
      GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
      GL.BufferData<Vector3>(BufferTarget.ArrayBuffer,
                             new IntPtr(vertices.Length * Vector3.SizeInBytes),
                             vertices, BufferUsageHint.StaticDraw);
    }

    public static void Start(Overlay overlay) {
      using (PointCloudWindow pcw = new PointCloudWindow(overlay)) {
        pcw.Run(60);
      }
    }

  }
}
