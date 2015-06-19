﻿namespace Trainer {
  using DirectShowLib;
  using Emgu.CV;
  using Emgu.CV.Structure;
  using System;
  using System.Collections.Generic;
  using System.Drawing.Imaging;
  using System.IO;
  using System.Windows;
  using System.Windows.Media.Imaging;


  // EMGU documentation link for our reference: http://www.emgu.com/wiki/files/3.0.0-alpha/document/html/b72c032d-59ae-c36f-5e00-12f8d621dfb8.htm
  public partial class MainWindow : Window {
    // MainWindow Variables
    private Capture capture = null;
    private bool tryUseCuda = false;
    private bool tryUseOpenCL = true;
    private BitmapImage bitmapImage;
    private bool initialized = false;
    private List<System.Drawing.Rectangle> faces = new List<System.Drawing.Rectangle>();
    private List<System.Drawing.Rectangle> eyes = new List<System.Drawing.Rectangle>();
    private int frameNumber = 0;
    private System.Drawing.Rectangle rec = new System.Drawing.Rectangle();
    private Mat image = new Mat();
    private long detectionTime;
    private double resize = 0.5;
    private RecognizeFace recognizeFace;

    private bool training = false;
    private int trainingCount = 0;
    private System.Drawing.Rectangle trainingRectangle = new System.Drawing.Rectangle();
    private List<Image<Gray, Byte>> trainingFaces = new List<Image<Gray, byte>>();

    private bool predicting = false;



    // Constructors
    public MainWindow() {
      InitializeComponent();
      bitmapImage = new BitmapImage();
      this.Closing += MainWindow_Closing;
      //recognizeFace = new RecognizeFace("EMGU.CV.EigenFaceRecognizer");
      recognizeFace = new RecognizeFace("EMGU.CV.LBPHFaceRecognizer");
      //recognizeFace = new RecognizeFace("EMGU.CV.FisherFaceRecognizer");

      InitializeDeviceList();
      // This will cause the change event to fire and it will start the capture process
      DeviceList.SelectedIndex = 0;
    }




    // Event Handlers
    private void ProcessFrame(object sender, EventArgs e) {
      // pull the image captured from the web camera
      try {
        capture.Retrieve(image);
      } catch (Exception ex) {
        MessageBox.Show(ex.Message);
        return;
      }

      // only look for a face if the frameNumber equals 0
      if (frameNumber == 0) {
        // create a smaller image for the face detection
        Image<Bgr, Byte> img = new Image<Bgr, Byte>(640, 480);
        img.ConvertFrom(image);
        img = img.Resize(resize, Emgu.CV.CvEnum.Inter.Linear);
        // uncomment this line to overwrite the image variable to see the scaled image in our form
        //   instead of the full image.
        //image = img.Mat;

        faces.Clear();
        eyes.Clear();
        // The cuda cascade classifier doesn't seem to be able to load "haarcascade_frontalface_default.xml"
        //   file in this release disabling CUDA module for now

        //* DetectFace.Detect is a very expensive process. We might need to consider not doing this on every frame
        DetectFace.Detect(
          img.Mat,
          "haarcascade_frontalface_default.xml", "haarcascade_eye.xml",
          faces, eyes,
          tryUseCuda,
          tryUseOpenCL,
          out detectionTime);
        //*/

        if (training && faces != null && faces.Count > 0) {
          trainingRectangle = faces[0];
          trainingRectangle.Width = Convert.ToInt32(faces[0].Width * (1 / resize));
          trainingRectangle.Height = Convert.ToInt32(faces[0].Height * (1 / resize));
          trainingRectangle.X = Convert.ToInt32(faces[0].X * (1 / resize));
          trainingRectangle.Y = Convert.ToInt32(faces[0].Y * (1 / resize));
          var face = new Image<Gray, Byte>(640, 480);
          face.ConvertFrom(image);
          face.ROI = trainingRectangle;
          face = face.Resize(100, 100, Emgu.CV.CvEnum.Inter.Cubic);
          trainingFaces.Add(face);
          trainingCount++;
          if (trainingCount == 10) {
            StopTraining();
          }
        } else if (predicting && faces != null && faces.Count > 0) {
          predicting = false;
          trainingRectangle = faces[0];
          trainingRectangle.Width = Convert.ToInt32(faces[0].Width * (1 / resize));
          trainingRectangle.Height = Convert.ToInt32(faces[0].Height * (1 / resize));
          trainingRectangle.X = Convert.ToInt32(faces[0].X * (1 / resize));
          trainingRectangle.Y = Convert.ToInt32(faces[0].Y * (1 / resize));
          img = new Image<Bgr, Byte>(640, 480);
          img.ConvertFrom(image);
          img.ROI = trainingRectangle;
          img = img.Resize(100, 100, Emgu.CV.CvEnum.Inter.Cubic);
          var gray = img.Convert<Gray, Byte>();
          MessageBox.Show(recognizeFace.Recognise(gray, 70));
        }
      }
      // increment frameNumber till we get to 10 and then reset it (faster
      //   logic than requiring an additional if since we're inside a loop like
      //   situation.
      frameNumber = ++frameNumber % 10;
      foreach (System.Drawing.Rectangle face in faces) {
        // math here is used to create a properly sized rectangle proportionate to the larger image
        //   since the face detection was performed on a smaller scaled image.
        rec.Width = Convert.ToInt32(face.Width * (1 / resize));
        rec.Height = Convert.ToInt32(face.Height * (1 / resize));
        rec.X = Convert.ToInt32(face.X * (1 / resize));
        rec.Y = Convert.ToInt32(face.Y * (1 / resize));
        CvInvoke.Rectangle(image, rec, new Bgr(System.Drawing.Color.Red).MCvScalar, 2);
      }
      foreach (System.Drawing.Rectangle eye in eyes) {
        // math here is used to create a properly sized rectangle proportionate to the larger image
        //   since the face detection was performed on a smaller scaled image.
        rec.Width = Convert.ToInt32(eye.Width * (1 / resize));
        rec.Height = Convert.ToInt32(eye.Height * (1 / resize));
        rec.X = Convert.ToInt32(eye.X * (1 / resize));
        rec.Y = Convert.ToInt32(eye.Y * (1 / resize));
        CvInvoke.Rectangle(image, rec, new Bgr(System.Drawing.Color.Blue).MCvScalar, 2);
      }

      using (MemoryStream memory = new MemoryStream()) {
        // thread safe image processing that updates the MainWindow's ImageViewer's source
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

    private void Train_Click(object sender, RoutedEventArgs e) {
      trainingFaces.Clear();
      training = true;
      frameNumber = 0;
      trainingCount = 0;
    }

    private void DeviceList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) {
      try {
        if (capture != null) capture.Stop();
      } catch (Exception ex) {
        MessageBox.Show(ex.Message);
        return;
      }
      // start capturing images from the web camera
      try {
        capture = new Capture(DeviceList.SelectedIndex);
      } catch (NullReferenceException excpt) {
        MessageBox.Show(excpt.Message);
      }
      if (capture != null) {
        capture.ImageGrabbed -= ProcessFrame;
        capture.ImageGrabbed += ProcessFrame;
        capture.Start();
      }
    }

    private void Predict_Click(object sender, RoutedEventArgs e) {
      predicting = true;
    }



    // Methods
    private void InitializeDeviceList() {
      List<KeyValuePair<int, string>> ListCamerasData = new List<KeyValuePair<int, string>>();
      //-> Find systems cameras with DirectShow.Net dll 
      DsDevice[] systemCamereas = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);
      int deviceIndex = 0;
      foreach (DsDevice camera in systemCamereas) {
        ListCamerasData.Add(new KeyValuePair<int, string>(deviceIndex, camera.Name));
        deviceIndex++;
      }
      DeviceList.Items.Clear();
      DeviceList.ItemsSource = ListCamerasData;
      DeviceList.DisplayMemberPath = "Value";
      DeviceList.SelectedValuePath = "Key";
    }

    private void StopTraining() {
      training = false;
      Image<Gray, Byte>[] images = null;
      int[] labels = new int[10];
      Dispatcher.Invoke((Action)(() => {
        images = trainingFaces.ToArray();
        int i = 0;
        foreach (var face in trainingFaces) {
          if (i < 10) {
            face.Save(@"data\" + NameTextBox.Text + "0" + i + ".bmp");
          } else {
            face.Save(@"data\" + NameTextBox.Text + i + ".bmp");
          }
          labels[i] = i;
          i++;
        }
      }));
      recognizeFace.Train(images, labels);
    }

  }
}
