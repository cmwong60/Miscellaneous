using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using System.IO;
using Utils;

namespace EncodeMessage
{
  class Program
  {
    static void Main(string[] args)
    {
      string key = "key";
      if(args.Length < 2)
      {
        Console.WriteLine("Usage:\n  EncodeMessage -e|-d bitmapFile [message]");
        Console.WriteLine("  -e: to encode message into bitmap.");
        Console.WriteLine("  -d: to decode message in bitmap.");
        Console.ReadLine();
        return;
      }

      if (args[0].ToLower() == "-d")
      {
        Bitmap bm = new Bitmap(args[1]);
        if (bm != null)
        {
          var msg = BitmapUtils.DecodeMessageFrom(bm);
          if (string.IsNullOrEmpty(msg))
            ShowMessageAndPauseForReturnPressed("Failed to decode message");
          else
            ShowMessageAndPauseForReturnPressed(msg);
        }
        else
        {
          ShowMessageAndPauseForReturnPressed("Invalid bitmap {0}", args[2]);
        }

        return;
      }

      if (args[0].ToLower() == "-e")
      {
        if(args.Length != 3)
        {
          ShowMessageAndPauseForReturnPressed("No message to encode !!");
          return;
        }

        if (!File.Exists(args[1]))
        {
          ShowMessageAndPauseForReturnPressed("File {0} not found.", args[1]);
          return;
        }

        if (!BitmapUtils.EncodeMessageTo(args[2], args[1]))
        {
          ShowMessageAndPauseForReturnPressed("Failed to encode messsage.");
          return;
        }

        string bmFileName = string.Format(@"{0}\{1}_encoded{2}", Path.GetDirectoryName(args[1]),
                          Path.GetFileNameWithoutExtension(args[1]), Path.GetExtension(args[1]));

        if (!File.Exists(bmFileName))
        {
          ShowMessageAndPauseForReturnPressed("File {0} not found.", bmFileName);
          return;
        }

        Bitmap bm = new Bitmap(bmFileName);
        var msg = BitmapUtils.DecodeMessageFrom(bm);
        if(string.IsNullOrEmpty(msg) || msg != args[2])
        {
          ShowMessageAndPauseForReturnPressed("File to decode message from {0}.", bmFileName);
          return;
        }
          
      }

      Console.ReadLine();
    }

    static void ShowMessageAndPauseForReturnPressed(string fmt, params object[] args)
    {
      Console.WriteLine(fmt, args);
      Console.ReadLine();
    }
  }

  //static class Packet
  //{
  //  const int PACKET_LEN = 256;

  //  public static bool EncodeMsgTo(string msg, string bmpFileName)
  //  {
  //    var bytes = UnicodeEncoding.Unicode.GetBytes(msg);
  //    if (bytes.Length >= PACKET_LEN)
  //      return false;

  //    byte[] packet = new byte[PACKET_LEN];

  //    packet[0] = (byte)bytes.Length;

  //    Array.Copy(bytes, 0, packet, 1, bytes.Length);
  //    if (packet == null)
  //      return false;

  //    return ResourceData.EncodeMessage(packet, bmpFileName);
  //  }

  //  public static string DecodeMsg(Bitmap bmp)
  //  {
  //    var packetBytes = ResourceData.DecodeMessageFromBitmap(PACKET_LEN, "icon", bmp);
  //    if (packetBytes != null && packetBytes.Length == PACKET_LEN)
  //    {
  //      int len = packetBytes[0];
  //      if (packetBytes.Length < len + 1)
  //        return null;

  //      var msgBytes = new byte[len];

  //      Array.Copy(packetBytes, 1, msgBytes, 0, len);
  //      return UnicodeEncoding.Unicode.GetString(msgBytes);
  //    }

  //    return null;
  //  }

  //  public static string DecodeMsg(String bitmapFileName)
  //  {
  //    var bmp = new Bitmap(bitmapFileName);
  //    return DecodeMsg(bmp);
  //  }
  //}
}
