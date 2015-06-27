namespace FaceApiClient {
  using Microsoft.ProjectOxford.Face;
  using System.Windows;

  public partial class App : Application {

    private static FaceServiceClient instance;
    private static int callCount = 0;



    public static void Initialize(string subscriptionKey) {
      instance = new FaceServiceClient(subscriptionKey);
    }



    public static FaceServiceClient Instance {
      get {
        callCount++;
        return instance;
      }
    }

    public static int CallCount {
      get {
        return callCount;
      }
    }

    public static void ResetCallCount() {
      callCount = 0;
    }

  }
}
