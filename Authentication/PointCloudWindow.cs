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
    private int vbo_depth;
    private int vbo_depth_colors;
    private int vbo_hdface;
    private int vbo_hdface_colors;
    private int depth_verticeCount;
    private int hdface_verticeCount;
    private Matrix4 cameraMatrix;
    private float[] mouseSpeed = new float[2];
    private Vector2 mouseDelta;
    private Vector3 location;
    private Vector3 up = Vector3.UnitY;
    private float pitch = 6.339f;
    private float facing = 7.859f;
    private bool wdown = false;
    private bool adown = false;
    private bool sdown = false;
    private bool ddown = false;
    private bool escdown = false;
    private bool updown = false;
    private bool downdown = false;
    private bool leftdown = false;
    private bool rightdown = false;
    private Vector3[] depthVectors;
    private Vector3[] hdFaceVectors;
    private int[] depthColors;
    private int[] hdFaceColors;
    private TextWriter tw;
    private bool showMask = true;
    private bool showPointCloud = true;
    private float hdDotSize = 3.0f;
    private CameraSpacePoint[] hdFaceMask;
    private float minx = 0;
    private float maxx = 0;
    private float miny = 0;
    private float maxy = 0;
    private float minz = 0;
    private float maxz = 0;



    public PointCloudWindow(Overlay overlay) : base(1024, 768) {
      // not exactly sure what this does, but the mask dots don't look right
      // without this line in here.
      GL.Enable(EnableCap.DepthTest);

      // setup Overlay class' event handlers
      overlay.OnVerticesUpdated += Overlay_OnVerticesUpdated;
      overlay.OnHdFaceUpdated += Overlay_OnHdFaceUpdated;
      overlay.OnTwoDMatchFound += Overlay_OnTwoDMatchFound;

      // setup on screen text
      tw = new TextWriter(new Size(1024, 768), new Size(500, 190));
      tw.AddLine("Camera Angle", new System.Drawing.PointF(10.0f, 10.0f), Brushes.White);
      tw.AddLine("facing, pitch", new System.Drawing.PointF(10.0f, 30.0f), Brushes.White);
      tw.AddLine("Camera Location", new System.Drawing.PointF(10.0f, 60.0f), Brushes.White);
      tw.AddLine("X: Y: Z", new System.Drawing.PointF(10.0f, 80.0f), Brushes.White);
      tw.AddLine("Dot Match:", new System.Drawing.PointF(10.0f, 110.0f), Brushes.White);
      tw.AddLine("Name:", new System.Drawing.PointF(10.0f, 140.0f), Brushes.White);
      tw.AddLine("Line Matches:", new System.Drawing.PointF(10.0f, 170.0f), Brushes.White);
    }






    // Event Handlers
    protected override void OnLoad(EventArgs e) {
      base.OnLoad(e);
      // background color
      GL.ClearColor(Color.Black);

      cameraMatrix = Matrix4.Identity;
      location = new Vector3(-0.0029f, 0f, -0.5f);
      mouseDelta = new Vector2();

      // center mouse on the game window
      //System.Windows.Forms.Cursor.Position = new Point(Bounds.Left + Bounds.Width / 2, Bounds.Top + Bounds.Height / 2);

      // setup GameWindow event handlers
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
      if (e.Key == Key.Up) updown = true;
      if (e.Key == Key.Down) downdown = true;
      if (e.Key == Key.Left) leftdown = true;
      if (e.Key == Key.Right) rightdown = true;
      if (e.Key == Key.Z) {
        hdDotSize += 1.0f;
        hdDotSize = hdDotSize > 10 ? 10 : hdDotSize;
      }
      if (e.Key == Key.X) {
        hdDotSize -= 1.0f;
        hdDotSize = hdDotSize < 1 ? 1 : hdDotSize;
      }
      if (e.Key == Key.Number1) {
        facing = 7.859f;
        pitch = 6.339f;
        location.X = -0.0029f;
        location.Y = 0.0f;
        location.Z = -0.5f;
      }
      if (e.Key == Key.Number2) {
        facing = 6.8789f;
        pitch = 5.7089f;
        location.X = -4.0382f;
        location.Y = 2.9999f;
        location.Z = -0.6394f;
      }
    }

    protected void OnKeyUp(object sender, KeyboardKeyEventArgs e) {
      if (e.Key == Key.W) wdown = false;
      if (e.Key == Key.A) adown = false;
      if (e.Key == Key.S) sdown = false;
      if (e.Key == Key.D) ddown = false;
      if (e.Key == Key.Escape) escdown = false;
      if (e.Key == Key.Up) updown = false;
      if (e.Key == Key.Down) downdown = false;
      if (e.Key == Key.Left) leftdown = false;
      if (e.Key == Key.Right) rightdown = false;
      if (e.Key == Key.Q) showMask = !showMask;
      if (e.Key == Key.E) showPointCloud = !showPointCloud;
    }

    protected void OnMouseWheel(object sender, MouseWheelEventArgs e) {
      if (e.Delta > 0)
        location.Y += 0.1f;
      else
        location.Y -= 0.1f;
    }

    protected void OnMouseMove(object sender, MouseMoveEventArgs e) {
      mouseDelta = new Vector2(e.XDelta, e.YDelta);
    }

    protected override void OnRenderFrame(FrameEventArgs e) {
      base.OnRenderFrame(e);
      GL.MatrixMode(MatrixMode.Modelview);
      GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
      GL.LoadMatrix(ref cameraMatrix);


      // Draw hd face
      if (showMask) {
        GL.PointSize(hdDotSize);
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo_hdface_colors);
        GL.ColorPointer(4, ColorPointerType.UnsignedByte, sizeof(int), IntPtr.Zero);
        GL.EnableClientState(ArrayCap.ColorArray);

        GL.EnableClientState(ArrayCap.VertexArray);
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo_hdface);
        GL.VertexPointer(3, VertexPointerType.Float, Vector3.SizeInBytes, new IntPtr(0));
        GL.DrawArrays(PrimitiveType.Points, 0, hdface_verticeCount);
      }


      // Draw point cloud
      if (showPointCloud) {
        GL.PointSize(1.0f);
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo_depth_colors);
        GL.ColorPointer(4, ColorPointerType.UnsignedByte, sizeof(int), IntPtr.Zero);
        GL.EnableClientState(ArrayCap.ColorArray);

        GL.EnableClientState(ArrayCap.VertexArray);
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo_depth);
        GL.VertexPointer(3, VertexPointerType.Float, Vector3.SizeInBytes, new IntPtr(0));
        GL.DrawArrays(PrimitiveType.Points, 0, depth_verticeCount);
      }


      // Draw text
      tw.Draw();


      SwapBuffers();
    }

    protected override void OnUpdateFrame(FrameEventArgs e) {
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

      /*
      mouseSpeed[0] *= 0.9f;
      mouseSpeed[1] *= 0.9f;
      mouseSpeed[0] += mouseDelta.X / 1000f;
      mouseSpeed[1] += mouseDelta.Y / 1000f;
      mouseDelta = new Vector2();

      facing += mouseSpeed[0];
      pitch += mouseSpeed[1];
      //*/

      if (updown) {
        pitch += 0.01f;
      }
      if (downdown) {
        pitch -= 0.01f;
      }
      if (leftdown) {
        facing -= 0.01f;
      }
      if (rightdown) {
        facing += 0.01f;
      }

      Vector3 lookatPoint = new Vector3((float)Math.Cos(facing), (float)Math.Sin(pitch), (float)Math.Sin(facing));
      cameraMatrix = Matrix4.LookAt(location, location + lookatPoint, up);
      tw.Update(1, String.Format("facing: {0}, pitch: {1}", facing, pitch));
      tw.Update(3, String.Format("X: {0}, Y: {1}, Z: {2}", location.X, location.Y, location.Z));
      if (escdown) Exit();
    }

    protected override void OnResize(EventArgs e) {
      base.OnResize(e);

      GL.Viewport(ClientRectangle.X, ClientRectangle.Y, ClientRectangle.Width, ClientRectangle.Height);

      Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView((float)Math.PI / 4, Width / (float)Height, 1.0f, 64.0f);
      GL.MatrixMode(MatrixMode.Projection);
      GL.LoadMatrix(ref projection);
    }

    private void Overlay_OnVerticesUpdated(CameraSpacePoint[] cameraSpacePoints, int[] colors) {
      if (!showPointCloud) return;

      minx = -10000;
      maxx = 10000;
      miny = -10000;
      maxy = 10000;
      minz = 10000;
      maxz = 10000;
      if (hdFaceMask != null) {
        var masklength = hdFaceMask.Length;
        for (int i = 0; i < masklength; i++) {
          if (i == 0) {
            minx = hdFaceMask[i].X;
            maxx = hdFaceMask[i].X;
            miny = hdFaceMask[i].Y;
            maxy = hdFaceMask[i].Y;
          }
          if (hdFaceMask[i].X > maxx) maxx = hdFaceMask[i].X;
          if (hdFaceMask[i].X < minx) minx = hdFaceMask[i].X;
          if (hdFaceMask[i].Y > maxy) maxy = hdFaceMask[i].Y;
          if (hdFaceMask[i].Y < miny) miny = hdFaceMask[i].Y;
          if (hdFaceMask[i].Z > maxz) maxz = hdFaceMask[i].Z;
          if (hdFaceMask[i].Z < minz) minz = hdFaceMask[i].Z;
        }
        maxy += 0.1f;
        maxz -= 0.01f;
        minz -= 0.01f;
      }


      var length = cameraSpacePoints.Length;
      depthColors = colors;
      if (depthVectors == null) {
        depthVectors = new Vector3[length];
        depth_verticeCount = length;
        for (int i = 0; i < length; i++) {
          if (cameraSpacePoints[i].X >= minx && cameraSpacePoints[i].X <= maxx
            && cameraSpacePoints[i].Y >= miny && cameraSpacePoints[i].Y <= maxy
            && cameraSpacePoints[i].Z >= minz && cameraSpacePoints[i].Z <= maxz) {
          depthVectors[i] = new Vector3(cameraSpacePoints[i].X, cameraSpacePoints[i].Y, cameraSpacePoints[i].Z);
          } else {
            depthVectors[i] = new Vector3(10000, 10000, 10000);
          }
        }

        // start editing   vbo_depth   buffer
        GL.GenBuffers(1, out vbo_depth);
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo_depth);
        GL.BufferData<Vector3>(BufferTarget.ArrayBuffer,
                               new IntPtr(depthVectors.Length * BlittableValueType.StrideOf(depthVectors)),
                               depthVectors, BufferUsageHint.StaticDraw);


        // start editing   vbo_depth_colors   buffer
        GL.GenBuffers(1, out vbo_depth_colors);
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo_depth_colors);
        GL.BufferData(BufferTarget.ArrayBuffer,
                      new IntPtr(depthColors.Length * 4),
                      depthColors, BufferUsageHint.StaticDraw);
      } else {
        for (int i = 0; i < length; i++) {
          if (cameraSpacePoints[i].X >= minx && cameraSpacePoints[i].X <= maxx
              && cameraSpacePoints[i].Y >= miny && cameraSpacePoints[i].Y <= maxy
              && cameraSpacePoints[i].Z >= minz && cameraSpacePoints[i].Z <= maxz) {
            depthVectors[i].X = cameraSpacePoints[i].X;
            depthVectors[i].Y = cameraSpacePoints[i].Y;
            depthVectors[i].Z = cameraSpacePoints[i].Z;
          } else {
            depthVectors[i].X = 10000;
            depthVectors[i].Y = 10000;
            depthVectors[i].Z = 10000;
          }

        }

        // start editing   vbo_depth   buffer
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo_depth);
        // clear out old memory. I think this is what allows us to redraw every time we get a new array of CameraSpacePoints
        GL.BufferData<Vector3>(BufferTarget.ArrayBuffer,
                               IntPtr.Zero,
                               depthVectors, BufferUsageHint.StaticDraw);
        GL.MapBuffer(BufferTarget.ArrayBuffer, BufferAccess.ReadWrite);
        GL.BufferData<Vector3>(BufferTarget.ArrayBuffer,
                               new IntPtr(depthVectors.Length * BlittableValueType.StrideOf(depthVectors)),
                               depthVectors, BufferUsageHint.StaticDraw);


        // start editing   vbo_depth_colors   buffer
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo_depth_colors);
        // clear out old memory. I think this is what allows us to redraw every time we get a new array of CameraSpacePoints
        GL.BufferData(BufferTarget.ArrayBuffer,
                               IntPtr.Zero,
                               depthColors, BufferUsageHint.StaticDraw);
        GL.MapBuffer(BufferTarget.ArrayBuffer, BufferAccess.ReadWrite);
        GL.BufferData(BufferTarget.ArrayBuffer,
                               new IntPtr(depthColors.Length * 4),
                               depthColors, BufferUsageHint.StaticDraw);
      }
    }

    private void Overlay_OnHdFaceUpdated(CameraSpacePoint[] cameraSpacePoints, int[] colors, int matched, int lineMatches, string name) {
      hdFaceMask = cameraSpacePoints;
      if (!showMask) return;
      if (matched > 0) tw.Update(4, String.Format("Dot Match: {0}", matched));
      var length = cameraSpacePoints.Length;
      hdFaceColors = colors;
      tw.Update(5, "Name: " + name);
      tw.Update(6, String.Format("Line Matches: {0}", lineMatches));
      // tip of the nose
      //hdFaceColors[18] = 0x00ffff;
      // top of the nose
      //hdFaceColors[24] = 0x00ffff;
      if (hdFaceVectors == null) {
        hdFaceVectors = new Vector3[length];
        hdface_verticeCount = length;
        for (int i = 0; i < length; i++) {
          hdFaceVectors[i] = new Vector3(cameraSpacePoints[i].X, cameraSpacePoints[i].Y, cameraSpacePoints[i].Z);
        }

        // start editing   vbo_hdface   buffer
        GL.GenBuffers(1, out vbo_hdface);
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo_hdface);
        GL.BufferData<Vector3>(BufferTarget.ArrayBuffer,
                               new IntPtr(hdFaceVectors.Length * BlittableValueType.StrideOf(hdFaceVectors)),
                               hdFaceVectors, BufferUsageHint.StaticDraw);


        // start editing   vbo_hdface_colors   buffer
        GL.GenBuffers(1, out vbo_hdface_colors);
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo_hdface_colors);
        GL.BufferData(BufferTarget.ArrayBuffer,
                new IntPtr(hdFaceColors.Length * 4),
                hdFaceColors, BufferUsageHint.StaticDraw);
      } else {
        for (int i = 0; i < length; i++) {
          if (float.IsInfinity(cameraSpacePoints[i].X) || float.IsNaN(cameraSpacePoints[i].X)) continue;
          if (float.IsInfinity(cameraSpacePoints[i].Y) || float.IsNaN(cameraSpacePoints[i].Y)) continue;
          if (float.IsInfinity(cameraSpacePoints[i].Z) || float.IsNaN(cameraSpacePoints[i].Z)) continue;
          hdFaceVectors[i].X = cameraSpacePoints[i].X;
          hdFaceVectors[i].Y = cameraSpacePoints[i].Y;
          hdFaceVectors[i].Z = cameraSpacePoints[i].Z;
        }

        // start editing   vbo_hdface   buffer
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo_hdface);
        // clear out old memory. I think this is what allows us to redraw every time we get a new array of CameraSpacePoints
        GL.BufferData<Vector3>(BufferTarget.ArrayBuffer,
                               IntPtr.Zero,
                               hdFaceVectors, BufferUsageHint.StaticDraw);
        GL.MapBuffer(BufferTarget.ArrayBuffer, BufferAccess.ReadWrite);
        GL.BufferData<Vector3>(BufferTarget.ArrayBuffer,
                               new IntPtr(hdFaceVectors.Length * BlittableValueType.StrideOf(hdFaceVectors)),
                               hdFaceVectors, BufferUsageHint.StaticDraw);


        // start editing   vbo_hdface_colors   buffer
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo_hdface_colors);
        // clear out old memory. I think this is what allows us to redraw every time we get a new array of CameraSpacePoints
        GL.BufferData<Vector3>(BufferTarget.ArrayBuffer,
                               IntPtr.Zero,
                               hdFaceVectors, BufferUsageHint.StaticDraw);
        GL.MapBuffer(BufferTarget.ArrayBuffer, BufferAccess.ReadWrite);
        GL.BufferData(BufferTarget.ArrayBuffer,
                               new IntPtr(hdFaceColors.Length * 4),
                               hdFaceColors, BufferUsageHint.StaticDraw);
      }
    }

    private void Overlay_OnTwoDMatchFound(string name) {
      tw.Update(5, String.Format("Name: {0}", name));
    }



    // Methods
    private Vector3[] LoadFml(string path) {
      var jss = new JavaScriptSerializer();

      if (!File.Exists(path)) return new Vector3[0];
      using (var file = new StreamReader(path)) {
        var data = file.ReadLine();
        if (string.IsNullOrWhiteSpace(data)) return new Vector3[0];
        return jss.Deserialize<Vector3[]>(data);
      }
    }

    public static void Start(Overlay overlay) {
      using (PointCloudWindow pcw = new PointCloudWindow(overlay)) {
        pcw.Run(60);
      }
    }

  }
}
