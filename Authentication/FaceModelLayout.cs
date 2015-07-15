namespace Trainer {
  using Microsoft.Kinect;
  using System;
  using System.Collections.Generic;

  public class FaceModelLayout {

    public Tolerance[] Tolerances { get; set; }
    public CameraSpacePoint[] SavedVertices { get; set; }

  }
}
