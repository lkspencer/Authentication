namespace Trainer {
  using Emgu.CV;
  using Emgu.CV.Face;
  using Emgu.CV.Structure;
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text;
  using System.Threading.Tasks;

  public class RecognizeFace {

    public RecognizeFace(string recognizerType) {
      this.recognizerType = recognizerType;
    }

    private bool _IsTrained = false;
    private string recognizerType;

    /*
    public string Recognise(Image<Gray, Byte> Input_image, int Eigen_Thresh = -1) {
      FaceRecognizer recognizer = new EigenFaceRecognizer(80, double.PositiveInfinity);
      if (_IsTrained) {
        FaceRecognizer.PredictionResult ER = recognizer.Predict(Input_image);
        //Only use the post threshold rule if we are using an Eigen Recognizer 
        //since Fisher and LBHP threshold set during the constructor will work correctly
        switch (recognizerType) {
          case ("EMGU.CV.EigenFaceRecognizer"):
            if (Eigen_Distance > Eigen_threshold) return Eigen_label;
            else return "Unknown";
          case ("EMGU.CV.LBPHFaceRecognizer"):
          case ("EMGU.CV.FisherFaceRecognizer"):
          default:
            return Eigen_label; //the threshold set in training controls unknowns
        }
      }
    }
    */

  }
}
