namespace Trainer {
  using Emgu.CV;
  using Emgu.CV.Face;
  using Emgu.CV.Structure;
  using System;
  using System.Windows;

  public class RecognizeFace {

    public RecognizeFace(string recognizerType) {
      this.recognizerType = recognizerType;
    }

    private bool _IsTrained = false;
    private string recognizerType;
    private int Eigen_threshold = 80;
    private string Eigen_label = "EigenMatch";

    public void Train<TColor, TDepth>(Image<TColor, TDepth>[] images, int[] labels) where TColor : struct, IColor where TDepth : new() {
      FaceRecognizer recognizer = new EigenFaceRecognizer(80, double.PositiveInfinity);
      try {
        recognizer.Train(images, labels);
        recognizer.Save(@"data\database.xml");
      } catch (Exception ex) {
        MessageBox.Show(ex.Message);
      }
    }

    public string Recognise(Image<Gray, Byte> Input_image, int Eigen_Thresh = -1) {
      FaceRecognizer recognizer = new EigenFaceRecognizer(80, double.PositiveInfinity);
      recognizer.Load(@"data\database.xml");
      if (_IsTrained) {
        FaceRecognizer.PredictionResult predictionResult = recognizer.Predict(Input_image);
        //Only use the post threshold rule if we are using an Eigen Recognizer 
        //since Fisher and LBHP threshold set during the constructor will work correctly
        switch (recognizerType) {
          case ("EMGU.CV.EigenFaceRecognizer"):
            if (predictionResult.Distance /*Eigen_Distance*/ > Eigen_Thresh) return Eigen_label;
            else return "Unknown";
          case ("EMGU.CV.LBPHFaceRecognizer"):
          case ("EMGU.CV.FisherFaceRecognizer"):
          default:
            return Eigen_label;
        }
      }
      return "";
    }

  }
}
