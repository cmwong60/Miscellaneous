using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.IO;
using Ionic.Zip;

namespace BackupUtils
{
  class Program
  {
    static void Main(string[] args)
    {
      if(args.Length == 0 )
      {
        Console.WriteLine("Usage:" + Environment.NewLine +
                          "  BackupUtil folderName -f -o ouputFolder" + Environment.NewLine +
                          " -f for full backup otherwise icremental.");
        return;
      }

      if(args[0].ToLower() == "dumppassword")
      {
        Console.WriteLine(Utils.BitmapUtils.DecodeMessageFrom(Properties.Resources.Icon));
        return;
      }

      bool incremental = Array.FindIndex(args, s => s.ToLower() == "-f") < 0;

      var index = Array.FindIndex(args, s => s.ToLower() == "-o");
      string outFolder = ".";
      if(index > 0 && args.Length > (index + 1))
      {
        outFolder = args[index + 1];
      }

      outFolder = Path.GetFullPath(outFolder);
      if(!Directory.Exists(outFolder))
      {
        Directory.CreateDirectory(outFolder);
      }

      string rootFolder = Path.GetFullPath(args[0]);
      if(!Directory.Exists(rootFolder))
      {
        Console.WriteLine("Folder {0} does not exist !");
        return;
      }

      string zipFileName = Path.Combine(outFolder, MakeZipName(rootFolder, incremental));
      Console.WriteLine("Backing up folder {0} to {1}", rootFolder, zipFileName);
      using (var zip = new ZipFile())
      {
        zip.Password = Utils.BitmapUtils.DecodeMessageFrom(Properties.Resources.Icon);
        int nFiles = 0;
        bool bok = AddDirectory(zip, rootFolder, rootFolder, incremental, ref nFiles);
        if(bok)
        {
          var dtStr = DateTime.Now.ToString("yyyyMMddHHmmss");
          zip.AddEntry(string.Format("BackupLog{0}.txt", dtStr), 
                                        string.Format("Folder : {0}" + Environment.NewLine +
                                                      "Date   : {1}" + Environment.NewLine +
                                                      "Type   : {2}" + Environment.NewLine +
                                                      "N files: {3}" + Environment.NewLine, 
                                                      rootFolder, dtStr, incremental ? "Incremental" : "Full", nFiles));
          double prevPercent = 0;
          //zip.SaveProgress += (s, e) =>
          //  {
          //    if (e.TotalBytesToTransfer == 0)
          //      return;

          //    double percent = (double)e.BytesTransferred / (double)e.TotalBytesToTransfer * 100d;
          //    if (percent - prevPercent > 1.0)
          //    {
          //      Console.WriteLine("{0} : {1}% completed", zipFileName, percent.ToString("F0"));
          //      prevPercent = percent;
          //    }
          //  };
          zip.Save(zipFileName);
          Console.WriteLine("Back up successful!");
        }
        else
        {
          Console.WriteLine("Back up fail!");
        }
      }
    }

    static void zip_AddProgress(object sender, AddProgressEventArgs e)
    {
      throw new NotImplementedException();
    }

    static bool AddDirectory(ZipFile zf, string directory, string rootFolder, bool incremental, ref int nFiles)
    {

      var fs = Directory.GetFiles(directory);
      try
      {
        foreach (var f in fs)
        {
          var attr = File.GetAttributes(f);
          if (incremental && !attr.HasFlag(FileAttributes.Archive))
            continue;

          var fp = Path.GetFullPath(f);
          string rp = Path.GetDirectoryName(fp);
          int index = rp.ToLower().IndexOf(rootFolder.ToLower());
          if (index >= 0)
          {
            if (rp.Length > rootFolder.Length)
              rp = rp.Substring(index + rootFolder.Length);
            else
              rp = string.Empty;
          }
          zf.AddFile(f, rp);
          File.SetAttributes(f, attr & ~FileAttributes.Archive);
          ++nFiles;
        }
      }
      catch(Exception ex)
      {
          Console.WriteLine("Exception: {0}", ex.Message);
          return false;
      }

      foreach(var d in Directory.GetDirectories(directory))
      {
        bool bok = AddDirectory(zf, d, rootFolder, incremental, ref nFiles);
        if(!bok)
          return false;
      }

      return true;
    }

    public static string MakeZipName(string folderName, bool incremental)
    {
      var n = folderName.Replace('\\', '%');
      n = n.Replace(':', '^');
      return string.Format("{0}-{1}-{2}.zip", n, DateTime.Now.ToString("yyyyMMdd-hhmmss"), incremental ? "INC" : "FULL");
    }
           
  }
}
