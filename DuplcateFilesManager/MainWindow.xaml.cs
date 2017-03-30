using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Security.Cryptography;
using System.Collections.Specialized;
using Path = System.IO.Path;
using System.Threading.Tasks;
using System.Data.HashFunction;

namespace DuplcateFilesManager
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    public Status Status { get; set; }
    public ObservableCollection<string> FolderList { get; set; }
    public Utils.ObservableCollectionEx<CandidateFileList> FileGroups { get; set; }
    public MainWindow()
    {
      InitializeComponent();
      Status = new Status();
      FolderList = new FolderList();
      xFolders.ItemsSource = FolderList;
      FileGroups = new Utils.ObservableCollectionEx<CandidateFileList>();
      //CollectFiles(@"E:\JUNK\JJJ");
      var v = CollectionViewSource.GetDefaultView(FileGroups);
      v.Filter += (i) =>
        {
          var f = i as CandidateFileList;
          if (f != null)
            //return true; 
           return f.CandidateFiles.Count > 1;

          return false;
        };

      v.SortDescriptions.Add(new SortDescription("FileName", ListSortDirection.Ascending));
      xDuplicateGroupLB.ItemsSource = v;
      DataContext = this;

    }

    private void OnAddFolderClick(object sender, RoutedEventArgs e)
    {
      var dialog = new System.Windows.Forms.FolderBrowserDialog();
      dialog.SelectedPath = System.IO.Path.GetFullPath(Properties.Settings.Default.LastFolder);
      dialog.ShowNewFolderButton = false;
      if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
      {
        if (!string.IsNullOrEmpty(dialog.SelectedPath) && !FolderList.Contains(dialog.SelectedPath))
        {
          foreach(var f in FolderList)
          {
            if (CheckChildFolder(f, dialog.SelectedPath))
              return;
          }

          var childFolders = FolderList.Where(f => CheckChildFolder(dialog.SelectedPath, f)).ToList();
          foreach(var cf in childFolders)
          {
            FolderList.Remove(cf);
          }

          FolderList.Add(dialog.SelectedPath);
        }

        Properties.Settings.Default.LastFolder = dialog.SelectedPath;
        Properties.Settings.Default.Save();
      }
    }

    private void OnDetectClick(object sender, RoutedEventArgs e)
    {
      Status.Reset();
      var but = sender as Button;
      but.IsEnabled = false;
      FileGroups.Clear();
      Action del = () =>
        {
          foreach (var f in FolderList)
          {
            CollectFiles(f);
          }
        };

      var cb = new AsyncCallback((res) =>
      {
        if (Dispatcher.CheckAccess())
        {
          but.IsEnabled = true;
        }
        else
        {
          Dispatcher.BeginInvoke((Action)(() => but.IsEnabled = true));
        }
      });
      
      del.BeginInvoke(cb, null);

    }

    private void CollectFiles(string folder)
    {
      var files = Directory.GetFiles(folder).ToList();
      Parallel.ForEach(files, (f) =>
                {
                  ProcessFile(f);
                });

      foreach(var d in Directory.GetDirectories(folder))
      {
        CollectFiles(d);
      }

    }

    private void ProcessFile(string file)
    {
      string hash = null;
      try
      {
        var xxhash = new xxHash(64);
        {
          using (var stream = File.OpenRead(file))
          {
            var hBytes = xxhash.ComputeHash(stream);
            hash = Convert.ToBase64String(hBytes);
          } // compute the hash.
        }

        //using (var md5 = MD5.Create())
        //{
        //  using (var stream = File.OpenRead(file))
        //  {
        //    var hBytes = md5.ComputeHash(stream);
        //    hash = Convert.ToBase64String(hBytes);
        //  }
        //}
      }
      catch(Exception ex)
      {
        return;
      }

      Dispatcher.BeginInvoke((Action)(() =>
      {
        Status.IncrementFileCounts();
        Status.LastFile = file;
        var fg = FileGroups.FirstOrDefault(i => i.Hash == hash);
        if (fg == null)
        {
          fg = new CandidateFileList()
          {
            Hash = hash,
            FileName = System.IO.Path.GetFileName(file)
          };

          FileGroups.Add(fg);
        }
        else
        {
          if (fg.CandidateFiles.FirstOrDefault(i => i.Path == file) != null)
            return;
        }

        CandidateFile cf = new CandidateFile(fg)
        {
          Path = file,
          ToRemove = false
        };
        fg.AddFile(cf);
      }));

      System.Threading.Thread.Sleep(1);
    }
    private void OnCleanClick(object sender, RoutedEventArgs e)
    {
      List<CandidateFile> filesToRemove = new List<CandidateFile>();
      foreach(var fg in FileGroups)
      {
        foreach(var f in fg.CandidateFiles)
        {
          if (f.ToRemove)
            filesToRemove.Add(f);
        }
      }

      filesToRemove.ForEach(f => f.DeleteIfMarked());
    }

    private void OnExitClick(object sender, RoutedEventArgs e)
    {

    }

    private void OnItemMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      var cb = sender as CheckBox;
      if (cb == null)
        return;

      string p = cb.Content as string;
      if (string.IsNullOrEmpty(p) || !File.Exists(p))
        return;

      System.Diagnostics.Process.Start(p);
    }

    private void OnRemoveFolderClick(object sender, RoutedEventArgs e)
    {
      var f = xFolders.SelectedItem as string;
      if (f != null)
        FolderList.Remove(f);
    }

    private static bool CheckChildFolder(string parent, string child)
    {
      var parentPath = Path.GetFullPath(parent).ToLower();
      var childPath = Path.GetFullPath(child).ToLower();
      return childPath.StartsWith(parentPath);
    }

    private void OnUnselectFolder(object sender, RoutedEventArgs e)
    {
      var folder = xFolders.SelectedItem as string;
      if (!string.IsNullOrEmpty(folder))
      {
        foreach (var fg in FileGroups)
        {
          foreach (var f in fg.CandidateFiles)
          {
            if (f.Path.StartsWith(folder))
            {
              f.ToRemove = false;
            }
          }
        }
      }
    }

    private void OnUnselectAll(object sender, RoutedEventArgs e)
    {
      var folder = xFolders.SelectedItem as string;
      if (!string.IsNullOrEmpty(folder))
      {
        foreach (var fg in FileGroups)
        {
          foreach (var f in fg.CandidateFiles)
          {
            f.ToRemove = true;
          }
        }
      }
    }

    private void OnSelectFolder(object sender, RoutedEventArgs e)
    {
      var folder = xFolders.SelectedItem as string;
      if(!string.IsNullOrEmpty(folder))
      {
        foreach(var fg in FileGroups)
        {
          foreach(var f in fg.CandidateFiles)
          {
            if(f.CanSetRemove && f.Path.StartsWith(folder))
            {
              f.ToRemove = true;
            }
          }
        }
      }
    }
  }
}
