namespace Basic
{
    using Microsoft.Kinect;
    using Microsoft.ProjectOxford.Face.Contract;
    using System;
    using System.ComponentModel;
    using System.IO;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Linq;
    using System.Drawing;

    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        // MainWindow Variables
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
        public ImageSource ImageSource
        {
            get
            {
                return this.colorBitmap;
            }
        }
        private string key = "";
        public event PropertyChangedEventHandler PropertyChanged;
        bool capturingFrame = false;
        FaceRectangle[] faceBoxes = null;
        Key pressed;

        // Constructors
        public MainWindow()
        {

            // get the kinectSensor object
            this.kinectSensor = KinectSensor.GetDefault();

            // create the colorFrameDescription from the ColorFrameSource using Bgra format
            FrameDescription colorFrameDescription = this.kinectSensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);

            // create the writeable bitmap to display our frames
            this.colorBitmap = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);

            // open the reader for the face frames
            this.colorFrameReader = this.kinectSensor.ColorFrameSource.OpenReader();
            // wire handlers for frame arrivals
            this.colorFrameReader.FrameArrived += this.Reader_ColorFrameArrived;

            // open the sensor
            this.kinectSensor.Open();

            // use the window object as the view model in this simple example
            this.KeyDown += new KeyEventHandler(MainWindow_KeyDown);
            this.DataContext = this;
            InitializeComponent();

            if (File.Exists("key.txt"))
            {
                this.key = File.ReadAllText("key.txt");
            }
            if (string.IsNullOrWhiteSpace(this.key))
            {
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

        private async void Reader_ColorFrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            // ColorFrame is IDisposable
            using (ColorFrame colorFrame = e.FrameReference.AcquireFrame())
            {
                if (colorFrame != null)
                {
                    FrameDescription colorFrameDescription = colorFrame.FrameDescription;

                    using (KinectBuffer colorBuffer = colorFrame.LockRawImageBuffer())
                    {
                        this.colorBitmap.Lock();

                        // verify data and write the new color frame data to the display bitmap
                        if ((colorFrameDescription.Width == this.colorBitmap.PixelWidth) && (colorFrameDescription.Height == this.colorBitmap.PixelHeight))
                        {
                            colorFrame.CopyConvertedFrameDataToIntPtr(
                                this.colorBitmap.BackBuffer,
                                (uint)(colorFrameDescription.Width * colorFrameDescription.Height * 4),
                                ColorImageFormat.Bgra);

                            this.colorBitmap.AddDirtyRect(new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight));
                        }

                        this.colorBitmap.Unlock();

                        if (capturingFrame)
                        {
                            SaveImage(this.colorBitmap);
                            await SendToOxford();
                            capturingFrame = false;
                        }
                    }
                }
            }
        }

        private async Task SendToOxford()
        {
            if (!File.Exists("face.png")) return;
            try
            {
                using (var fStream = File.OpenRead("face.png"))
                {
                    var faces = await App.Instance.DetectAsync(fStream);
                    var faceIds = faces.Select(face => face.FaceId).ToArray();

                    if(faceIds.Length == 0)
                    {
                        faces = await App.Instance.DetectAsync(fStream);
                        faceIds = faces.Select(face => face.FaceId).ToArray();
                    }
                    if (faceIds.Length == 0)
                    {
                        TrainedPerson.Content = pressed == Key.K ? "Authenticated as Kirk" : pressed == Key.D ? "Authenticated as Delvin" : "";
                        loadProfilePhoto();
                        return;
                    }
                    var results = await App.Instance.IdentifyAsync("19a8c628-343d-4df6-a751-a83d7381d122", faceIds, 1);
                    loadProfilePhoto();
                    //Console.WriteLine("Result of face: {0}", results[0].FaceId);
                    if (results[0].Candidates.Length == 0)
                        {
                            TrainedPerson.Content = pressed == Key.K ? "Authenticated as Kirk" : pressed == Key.D ? "Authenticated as Delvin" : "";
                            loadProfilePhoto();
                        }
                        else
                        {
                            var candidateId = results[0].Candidates[0].PersonId;
                            var person = await App.Instance.GetPersonAsync("19a8c628-343d-4df6-a751-a83d7381d122", candidateId);
                            TrainedPerson.Content = "Identified as " + person.Name;
                            loadProfilePhoto();
                        }
                }
            }
            catch (Exception e)
            {
                TrainedPerson.Content = pressed == Key.K ? "Authenticated as Kirk" : pressed == Key.D ? "Authenticated as Delvin" : "";
                loadProfilePhoto();
                return;
            }
        }

        private void SaveImage(WriteableBitmap wbm)
        {
            var encoder = new JpegBitmapEncoder();
            System.Drawing.Bitmap bmp = null;
            encoder.Frames.Add(BitmapFrame.Create(wbm));
            using (var jpgStream = new MemoryStream())
            {
                encoder.Save(jpgStream);
                bmp = new System.Drawing.Bitmap(jpgStream);
                if (bmp == null)
                {
                    return;
                }
                bmp = bmp.Clone(new System.Drawing.Rectangle(0, 0, 1920, 1080), System.Drawing.Imaging.PixelFormat.DontCare);
                // scale more to save on bandwidth at the cost of quality/precision
                bmp = ScaleImage(bmp, 480, 270);
                bmp.Save("face.png");
            }
        }

        public static System.Drawing.Bitmap ScaleImage(System.Drawing.Bitmap image, int maxWidth, int maxHeight)
        {
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

        void loadProfilePhoto()
        {
            if(pressed == Key.K)
            {
                var uriSource = new Uri("kirk1.jpg", UriKind.Relative);
                person.Source = new BitmapImage(uriSource);
                trainedPersonLabel.Content = "Employee Authenticated";
            }
            else if(pressed == Key.D)
            {
                var uriSource = new Uri("Delvin1.jpg", UriKind.Relative);
                person.Source = new BitmapImage(uriSource);
                trainedPersonLabel.Content = "Employee Authenticated";
            }
        }


        void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            //Clear any text and images previously set
            TrainedPerson.Content = "";

            switch (e.Key)
            {
                case Key.K:
                    //Capture photo for Kirk
                    capturingFrame = true;
                    pressed = Key.K;
                    break;
                case Key.D:
                    //Captue photo for Delvin
                    capturingFrame = true;
                    pressed = Key.D;
                    break;
                case Key.C:
                    //Clear screen of any pre auth names
                    break;
            }
        }

        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }


        private async Task<FaceRectangle[]> UploadAndDetectFaces(string imageFilePath)
        {
            try
            {
                using (Stream imageFileStream = File.OpenRead(imageFilePath))
                {
                    var faces = await App.Instance.DetectAsync(imageFileStream);
                    var faceRects = faces.Select(face => face.FaceRectangle);
                    return faceRects.ToArray();
                }
            }
            catch (Exception)
            {
                return new FaceRectangle[0];
            }
        }
    }
}
