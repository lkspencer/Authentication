namespace Trainer {
  using Emgu.CV;
  using Emgu.CV.Structure;
  using System;
  using System.Collections.Generic;
  using System.Drawing.Imaging;
  using System.IO;
  using System.Windows;
  using System.Windows.Media.Imaging;

  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window {
    private Capture capture = null;
    private bool tryUseCuda = false;
    private bool tryUseOpenCL = true;
    private BitmapImage bitmapImage;
    //private long detectionTime;
    private bool initialized = false;
    private List<System.Drawing.Rectangle> faces = new List<System.Drawing.Rectangle>();
    private List<System.Drawing.Rectangle> eyes = new List<System.Drawing.Rectangle>();
    private int frameNumber = 0;
    private System.Drawing.Rectangle rec = new System.Drawing.Rectangle();
    private Mat image = new Mat();
    private long detectionTime;
    private double resize = 1.0;

    public MainWindow() {
      InitializeComponent();
      bitmapImage = new BitmapImage();
      this.Closing += MainWindow_Closing;
      try {
        capture = new Capture();
      } catch (NullReferenceException excpt) {   //show errors if there is any
        MessageBox.Show(excpt.Message);
      }
      if (capture != null) {
        capture.ImageGrabbed += ProcessFrame;
        capture.Start();
      }
    }
    private System.Drawing.Bitmap ResizeImage(System.Drawing.Bitmap imgToResize, System.Drawing.Size size) {
      try {
        System.Drawing.Bitmap b = new System.Drawing.Bitmap(size.Width, size.Height);
        using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage((System.Drawing.Image)b)) {
          g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
          g.DrawImage(imgToResize, 0, 0, size.Width, size.Height);
        }
        return b;
      } catch { }
      return imgToResize;
    }
    private void ProcessFrame(object sender, EventArgs e) {
      //Mat image = new Mat("lena.jpg", LoadImageType.Color); //Read the files as an 8-bit Bgr image
      //Mat image = new Mat("kirk.jpg", LoadImageType.Color); //Read the files as an 8-bit Bgr image

      
      try {
        capture.Retrieve(image);
        //capture.Stop();
      } catch (Exception ex) {
        MessageBox.Show(ex.Message);
        return;
      }
      Image<Emgu.CV.Structure.Bgr, Byte> img = new Image<Emgu.CV.Structure.Bgr, Byte>(640, 480);
      img.ConvertFrom(image);
      img = img.Resize(resize, Emgu.CV.CvEnum.Inter.Linear);
      //image = img.Mat;

      if (frameNumber == 0) {
        faces.Clear();
        eyes.Clear();
        //The cuda cascade classifier doesn't seem to be able to load "haarcascade_frontalface_default.xml" file in this release
        //disabling CUDA module for now

        //* DetectFace.Detect is a very expensive process. We might need to consider not doing this on every frame
        Dispatcher.Invoke((Action)(() => {
          DetectFace.Detect(
            img.Mat,
            "haarcascade_frontalface_default.xml", "haarcascade_eye.xml",
            faces, eyes,
            tryUseCuda,
            tryUseOpenCL,
            out detectionTime);
        }));
      //*/
      }
      frameNumber = ++frameNumber % 10;
      foreach (System.Drawing.Rectangle face in faces) {
        rec.Width = Convert.ToInt32(face.Width * (1 / resize));
        rec.Height = Convert.ToInt32(face.Height * (1 / resize));
        rec.X = Convert.ToInt32(face.X * (1 / resize));
        rec.Y = Convert.ToInt32(face.Y * (1 / resize));
        CvInvoke.Rectangle(image, rec, new Bgr(System.Drawing.Color.Red).MCvScalar, 2);
      }
      foreach (System.Drawing.Rectangle eye in eyes) {
        rec.Width = Convert.ToInt32(eye.Width * (1 / resize));
        rec.Height = Convert.ToInt32(eye.Height * (1 / resize));
        rec.X = Convert.ToInt32(eye.X * (1 / resize));
        rec.Y = Convert.ToInt32(eye.Y * (1 / resize));
        CvInvoke.Rectangle(image, rec, new Bgr(System.Drawing.Color.Blue).MCvScalar, 2);
      }

      using (MemoryStream memory = new MemoryStream()) {
        image.Bitmap.Save(memory, ImageFormat.Png);
        memory.Position = 0;
        bitmapImage.Dispatcher.Invoke((Action)(() => {
          bitmapImage = new BitmapImage();
          bitmapImage.BeginInit();
          bitmapImage.StreamSource = memory;
          bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
          bitmapImage.EndInit();
          var wb = new WriteableBitmap(bitmapImage);
          ImageViewer.Source = wb;
        }));
      }
    }

    private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
      try {
        capture.Stop();
      } catch (Exception ex) {
        MessageBox.Show(ex.Message);
        return;
      }

    }
  }
}
