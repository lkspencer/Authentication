namespace Trainer {
  using Emgu.CV;
  using Emgu.CV.Face;
  using Emgu.CV.Structure;
  using System;
  using System.Windows;

  public class RecognizeFace {

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

    private bool isTrained = false;
    private string recognizerType;
    private int Eigen_threshold = 80;
    private FaceRecognizer recognizer;

    public void Train<TColor, TDepth>(Image<TColor, TDepth>[] images, int[] labels) where TColor : struct, IColor where TDepth : new() {
      try {
        recognizer.Train(images, labels);
        recognizer.Save(@"data\database.xml");
        isTrained = true;
      } catch (Exception ex) {
        MessageBox.Show(ex.Message);
      }
    }

    public string Recognise(Image<Gray, Byte> Input_image, int Eigen_Thresh = -1) {
      recognizer.Load(@"data\database.xml");
      var LBPH_threshold = Eigen_Thresh;
      var Fisher_threshold = Eigen_Thresh;
      if (isTrained) {
        FaceRecognizer.PredictionResult predictionResult = recognizer.Predict(Input_image);
        //Only use the post threshold rule if we are using an Eigen Recognizer 
        //since Fisher and LBHP threshold set during the constructor will work correctly
        switch (recognizerType) {
          case ("EMGU.CV.EigenFaceRecognizer"):
            if (predictionResult.Distance /*Eigen_Distance*/ > Eigen_Thresh) return "EigenMatch";
            else return "Unknown";
          case ("EMGU.CV.LBPHFaceRecognizer"):
            //Note how the Eigen Distance must be below the threshold unlike as above
            if (predictionResult.Distance < LBPH_threshold) return "LBPHMatch";
            else return "Unknown";
          case ("EMGU.CV.FisherFaceRecognizer"):
            if (predictionResult.Distance < Fisher_threshold) return "FisherMatch";
            else return "Unknown";
          default:
            return "Unknown";
        }
      }
      return "";
    }

  }
}
