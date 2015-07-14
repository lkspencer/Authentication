namespace Microsoft.Kinect {
  using MathNet.Spatial.Euclidean;
  using MathNet.Spatial.Units;

  public static class CameraSpacePointExtensions {
    public static CameraSpacePoint RotateX(this CameraSpacePoint target, double theta) {
      var vector = new Vector3D(target.X, target.Y, target.Z);
      var rotated = vector.Rotate(UnitVector3D.XAxis, Angle.FromRadians(theta));
      target.X = (float)rotated.X;
      target.Y = (float)rotated.Y;
      target.Z = (float)rotated.Z;
      return target;
    }
    public static CameraSpacePoint RotateY(this CameraSpacePoint target, double theta) {
      var vector = new Vector3D(target.X, target.Y, target.Z);
      var rotated = vector.Rotate(UnitVector3D.YAxis, Angle.FromRadians(theta));
      target.X = (float)rotated.X;
      target.Y = (float)rotated.Y;
      target.Z = (float)rotated.Z;
      return target;
    }
    public static CameraSpacePoint RotateZ(this CameraSpacePoint target, double theta) {
      var vector = new Vector3D(target.X, target.Y, target.Z);
      var rotated = vector.Rotate(UnitVector3D.ZAxis, Angle.FromRadians(theta));
      target.X = (float)rotated.X;
      target.Y = (float)rotated.Y;
      target.Z = (float)rotated.Z;
      return target;
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
