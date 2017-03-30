using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.IO;

namespace Utils
{
  public static class BitmapUtils
  {
    private static double _pi = 3.14159d;
    private static double _multiplier = 10d;

    private static Random rand = new Random();

    const int PACKET_LEN = 256;

    public static bool EncodeMessageTo(string msg, string bmpFileName)
    {
      var bytes = UnicodeEncoding.Unicode.GetBytes(msg);
      if (bytes.Length >= PACKET_LEN)
        return false;

      byte[] packet = new byte[PACKET_LEN];

      packet[0] = (byte)bytes.Length;

      Array.Copy(bytes, 0, packet, 1, bytes.Length);
      if (packet == null)
        return false;

      return EncodeMessage(packet, bmpFileName);
    }

    public static string DecodeMessageFrom(Bitmap bmp)
    {
      var packetBytes = DecodeMessageFromBitmap(PACKET_LEN, "icon", bmp);
      if (packetBytes != null && packetBytes.Length == PACKET_LEN)
      {
        int len = packetBytes[0];
        if (packetBytes.Length < len + 1)
          return null;

        var msgBytes = new byte[len];

        Array.Copy(packetBytes, 1, msgBytes, 0, len);
        return UnicodeEncoding.Unicode.GetString(msgBytes);
      }

      return null;
    }

    private static bool EncodeMessage(byte[] bytesToEncode, string fileName, double seed0To1 = -1)
    {
      if (seed0To1 <= 0)
        seed0To1 = _pi / _multiplier;

      var bmp = new Bitmap(fileName);
      Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
      System.Drawing.Imaging.BitmapData bmpData = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite,
                                                                  bmp.PixelFormat); ;
      if (bmpData == null)
        return false;

      try
      {
        // Get the address of the first line.
        IntPtr ptr = bmpData.Scan0;

        // Declare an array to hold the bytes of the bitmap.
        int nBmpBytes = Math.Abs(bmpData.Stride) * bmp.Height;
        if (nBmpBytes <= bytesToEncode.Length)
          return false;

        byte[] rgbValues = new byte[nBmpBytes];
        // Copy the RGB values into the array.
        System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, nBmpBytes);

        var bits = new BitArray(bytesToEncode);
        int begin = ComputeFirstIndex(bytesToEncode.Length, rgbValues.Length, seed0To1);
        if (begin < 0)
          return false;

        int end = begin + bits.Length;
        int bitIndex = 0;
        for (int i = 0; i < rgbValues.Length; ++i)
        {
          bool flag = false;
          if (begin <= i && i < end)
          {
            flag = bits[bitIndex];
            ++bitIndex;
          }
          else
          {
            flag = (rand.Next() & 0x1) == 0x1;
          }

          if (flag)
          {
            rgbValues[i] |= 0x01;
          }
          else
          {
            rgbValues[i] &= 0xFE;
          }
        }

        // Copy the RGB values back to the bitmap
        System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, nBmpBytes);
        bmp.UnlockBits(bmpData);
        bmpData = null;
      }
      catch
      {
        return false;
      }
      finally
      {
        if (bmpData != null)
        {
          bmp.UnlockBits(bmpData);
          bmpData = null;
        }
      }

      string outFileName = string.Format(@"{0}\{1}_encoded{2}", Path.GetDirectoryName(fileName),
            Path.GetFileNameWithoutExtension(fileName), Path.GetExtension(fileName));
      bmp.Save(outFileName);
      return true;
    }

    private static byte[] DecodeMessageFromBitmap(int len, string name, Bitmap bmp, double seed0To1 = -1)
    {

      byte[] rgbValues = GetBitmapBytes(bmp);
      return DecodeMessage(len, rgbValues, seed0To1);
    }

    private static byte[] DecodeMessage(int msgByteLen, byte[] rgbValues, double seed0To1 = -1)
    {
      BitArray bits = new BitArray(msgByteLen * 8);
      int first = ComputeFirstIndex(msgByteLen, rgbValues.Length, seed0To1);
      if (first < 0)
        return null;

      for (int i = 0, index = first; i < bits.Length; ++i, ++index)
      {
        bits[i] = (rgbValues[index] & 0x01) == 0x01;
      }

      byte[] bytes = new byte[msgByteLen];
      bits.CopyTo(bytes, 0);
      return bytes;
    }

    private static int ComputeFirstIndex(int nMsgBytes, int nRgbBytes, double seed0To1 = 1)
    {
      var nMsgBits = nMsgBytes * 8;
      if (seed0To1 > 1 || nMsgBits >= nRgbBytes)
        return -1;

      if (seed0To1 < 0)
        seed0To1 = _pi / _multiplier;

      return (int)((nRgbBytes - nMsgBits) * seed0To1);
    }

    private static byte[] GetBitmapBytes(Bitmap bmp)
    {
      Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
      byte[] rgbValues = null;
      System.Drawing.Imaging.BitmapData bmpData = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite,
                                                                  bmp.PixelFormat);
      if (bmpData == null)
        return null;

      try
      {
        // Get the address of the first line.
        IntPtr ptr = bmpData.Scan0;
        int nBytes = Math.Abs(bmpData.Stride) * bmp.Height;
        rgbValues = new byte[nBytes];
        System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, nBytes);
        bmp.UnlockBits(bmpData);
        bmpData = null;
      }
      catch
      {
        return null;
      }
      finally
      {
        if (bmpData != null)
          bmp.UnlockBits(bmpData);
      }

      return rgbValues;
    }

  }
}
