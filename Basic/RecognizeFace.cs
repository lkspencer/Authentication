namespace Basic {
  using Emgu.CV;
  using Emgu.CV.Face;
  using Emgu.CV.Structure;
  using System;
  using System.Threading;
  using System.Threading.Tasks;
  using System.Windows;

  public class RecognizeFace {
    // RecognizeFace Variables
    private bool isTrained = false;
    private string recognizerType;
    private int Eigen_threshold = 80;
    private FaceRecognizer recognizer;
    public delegate void FaceRecognition(string result);
    public event FaceRecognition FaceRecognitionArrived;
    public delegate void FaceTraining(bool trained);
    public event FaceTraining FaceTrainingComplete;
    private Thread thread;



    // Constructors
    public RecognizeFace(string recognizerType) {
      this.recognizerType = recognizerType;
      switch (recognizerType) {
        case ("EMGU.CV.EigenFaceRecognizer"):
          recognizer = new EigenFaceRecognizer(10, double.PositiveInfinity);
          break;
        case ("EMGU.CV.LBPHFaceRecognizer"):
          recognizer = new LBPHFaceRecognizer(1, 8, 8, 8, 100);//50
          break;
        case ("EMGU.CV.FisherFaceRecognizer"):
          recognizer = new FisherFaceRecognizer(0, 3500);//4000
          break;
      }
    }



    // Methods
    public async Task TrainAsync<TColor, TDepth>(Image<TColor, TDepth>[] images, int[] labels, bool save = true) where TColor : struct, IColor where TDepth : new() {
      isTrained = false;
      if (images == null || images.Length == 0 || labels == null || labels.Length == 0 || labels.Length != images.Length) {
        FaceTrainingComplete(false);
        return;
      }
      try {
        await Task.Run(() => {
          recognizer.Train(images, labels);
          if (save) recognizer.Save(@"data\database.xml");
        });
        isTrained = true;
      } catch (Exception ex) {
        MessageBox.Show(ex.Message);
      }
      FaceTrainingComplete(isTrained);
    }

    public void Recognise(Image<Gray, Byte> Input_image, int Eigen_Thresh = -1) {
      if (thread == null || thread.ThreadState == ThreadState.Stopped) {
        thread = new Thread(() => {
          if (!isTrained) FaceRecognitionArrived("Not Trained Yet");

          var LBPH_threshold = Eigen_Thresh;
          var Fisher_threshold = Eigen_Thresh;
          FaceRecognizer.PredictionResult predictionResult = new FaceRecognizer.PredictionResult();
          predictionResult = recognizer.Predict(Input_image);
          switch (recognizerType) {
            case ("EMGU.CV.EigenFaceRecognizer"):
              if (predictionResult.Distance /*Eigen_Distance*/ < Eigen_Thresh) FaceRecognitionArrived(String.Format("EigenMatch, Distance: {0}, Threshold {1}", predictionResult.Distance, Eigen_Thresh));
              else FaceRecognitionArrived(String.Format("Unknown, Distance: {0}", predictionResult.Distance));
              break;
            case ("EMGU.CV.LBPHFaceRecognizer"):
              //Note how the Eigen Distance must be below the threshold unlike as above
              if (predictionResult.Distance < LBPH_threshold) FaceRecognitionArrived(String.Format("LBPHMatch, Distance: {0}", predictionResult.Distance));
              else FaceRecognitionArrived(String.Format("Unknown, Distance: {0}", predictionResult.Distance));
              break;
            case ("EMGU.CV.FisherFaceRecognizer"):
              if (predictionResult.Distance < Fisher_threshold) FaceRecognitionArrived(String.Format("FisherMatch, Distance: {0}", predictionResult.Distance));
              else FaceRecognitionArrived(String.Format("Unknown, Distance: {0}", predictionResult.Distance));
              break;
            default:
              FaceRecognitionArrived("Unknown");
              break;
          }
        });

        thread.Start();
      }
    }

  }
}
