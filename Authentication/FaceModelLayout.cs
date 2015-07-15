namespace Trainer {
  using Microsoft.Kinect;
  using System;
  using System.Collections.Generic;

  public class FaceModelLayout {

    public List<Tuple<double, double>> Tolerances { get; set; }
    public CameraSpacePoint[] SavedVertices { get; set; }

  }
}
