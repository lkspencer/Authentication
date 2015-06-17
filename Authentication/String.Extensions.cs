namespace Trainer {
  using System;

  public static class StringExtensions {

    public static int[] ToIntArray(this string value) {
      byte[] bytes = new byte[value.Length * sizeof(char)];
      System.Buffer.BlockCopy(value.ToCharArray(), 0, bytes, 0, bytes.Length);
      return Array.ConvertAll(bytes, c => (int)c);
    }

  }
}
