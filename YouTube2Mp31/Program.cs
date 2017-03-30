using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using YoutubeExtractor;

namespace YouTube2Mp3
{
  class Program
  {
    class StatusLogger
    {
      int _lastLength = 0;
      int _pos = 0;
      public StatusLogger(int pos)
      {
        _pos = pos;
      }
      public void Write(string format, params object[] args)
      {
        Console.SetCursorPosition(0, _pos);
        Console.Write(new string(' ', Math.Max(Console.WindowWidth, _lastLength))); 
        Console.SetCursorPosition(0, _pos);
        var s = string.Format(format, args);
        _lastLength = s.Length;
        Console.Write(s);
      }
    }

    static void Main(string[] args)
    {
      if (args.Length < 2)
      {
        Console.WriteLine("Usage: YouTube2MP3 targetFolder url1, url2 ...");
        return;
      }

      Console.Clear();
      string targetFolder = Path.GetFullPath(args[0]);
      if (!Directory.Exists(targetFolder))
        Directory.CreateDirectory(targetFolder);

      Console.WriteLine("YouTube2MP3 Target folder: {0}", targetFolder);
      int pos = Console.CursorTop;
      var urls = args.ToList();
      urls.RemoveAt(0);
      foreach (var url in urls)
      {
        var logger = new StatusLogger(pos);
        Download(logger, url, targetFolder);
        pos += 2;
      }

      new StatusLogger(pos).Write("All videos downloaded, press return to exit.");
      Console.ReadLine();
    }

    private static bool Download(StatusLogger logger, string url, string targetFolder)
    {
      try
      {
        logger.Write("Resolving URL {0} ...", url); 
        IEnumerable<VideoInfo> videoInfos = DownloadUrlResolver.GetDownloadUrls(url);
        VideoInfo video = videoInfos
            .First(info => info.VideoType == VideoType.Mp4 && info.Resolution == 360);

        /*
         * If the video has a decrypted signature, decipher it
         */
        if (video.RequiresDecryption)
        {
          DownloadUrlResolver.DecryptDownloadUrl(video);
        }

        string name = GetSafeFilename(video.Title);
        string videoPath = Path.Combine(targetFolder, name + video.VideoExtension);
        if (File.Exists(videoPath))
          File.Delete(videoPath);

        var videoDownloader = new VideoDownloader(video, videoPath);
        double nextPercent = 5d;
        videoDownloader.DownloadProgressChanged += (sender, arg) =>
          {
            if (arg.ProgressPercentage > nextPercent)
            {
              nextPercent += 5d;
              logger.Write("Downloading {0}: {1}% completed", video.Title, arg.ProgressPercentage.ToString("F1"));
            }
          };

        videoDownloader.Execute();


        string mp3Path = Path.Combine(targetFolder, name + ".mp3");
        if (File.Exists(mp3Path))
          File.Delete(mp3Path);

        //Action del = () =>
        //{
        logger.Write("Converting {0} to MP3 {1}. ", video.Title, mp3Path);
        var ffMpeg = new NReco.VideoConverter.FFMpegConverter();
        ffMpeg.ConvertMedia(videoPath, mp3Path, "MP3");
        File.Delete(videoPath);
        logger.Write("Converting {0} to MP3 Completed.", video.Title, mp3Path);
        //};

        //del.BeginInvoke(null, null);
      }
      catch(Exception ex)
      {
        logger.Write("Exception downloading {0}, exception - {1}", url, ex.Message);
        return false;
      }

      return true;
    }

    private static string GetSafeFilename(string filename)
    {
      return string.Join("_", filename.Split(Path.GetInvalidFileNameChars()));
    }

  }
}
