namespace Microsoft.Kinect {
  using MathNet.Spatial.Euclidean;
  using MathNet.Spatial.Units;

  public static class CameraSpacePointExtensions {
    public static CameraSpacePoint RotateY(this CameraSpacePoint target, double theta) {
      var vector = new Vector3D(target.X, target.Y, target.Z);
      var rotated = vector.Rotate(UnitVector3D.YAxis, Angle.FromRadians(theta));
      return new CameraSpacePoint {
        X = (float)rotated.X,
        Y = (float)rotated.Y,
        Z = (float)rotated.Z
      };
    }

    public static CameraSpacePoint Translate(this CameraSpacePoint point, float dx, float dy, float dz) {
      return new CameraSpacePoint {
        X = point.X + dx,
        Y = point.Y + dy,
        Z = point.Z + dz
      };
    }
  }
}
