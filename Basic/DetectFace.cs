namespace Basic{
  using Emgu.CV;
  using System.Drawing;

  public class DetectFace {

    public static Rectangle[] Detect (Mat image, string eyeFilePath, string faceFilePath) {

      using (var eye = new CascadeClassifier(eyeFilePath)) {
        using (var face = new CascadeClassifier(faceFilePath)) {
          CvInvoke.UseOpenCL = CvInvoke.HaveOpenCLCompatibleGpuDevice;
          using (var gray = new UMat()) {
            CvInvoke.CvtColor(image, gray, Emgu.CV.CvEnum.ColorConversion.Bgr2Gray);
            CvInvoke.EqualizeHist(gray, gray);
            return face.DetectMultiScale(gray);
          }
        }
      }
    }

  }
}
