using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Collections.Specialized;
using System.IO;

namespace DuplcateFilesManager
{
  public class Status : INotifyPropertyChanged
  {
    private int _numberOfFilesProcessed = 0;
    public string NumberOfFilesProcessedText 
    { 
      get
      {
        return string.Format("{0} files processed", _numberOfFilesProcessed);
      }
    }
  
    private string _lastfile;
    public string LastFile 
    {
      get {return _lastfile;}
      set
      {
        _lastfile = value;
        OnPropertyChanged(new PropertyChangedEventArgs("LastFile"));
      }
    }

    public void IncrementFileCounts()
    {
      ++_numberOfFilesProcessed;
      OnPropertyChanged(new PropertyChangedEventArgs("NumberOfFilesProcessedText"));
    }

    public void Reset()
    {
      _numberOfFilesProcessed = 0;
      OnPropertyChanged(new PropertyChangedEventArgs(NumberOfFilesProcessedText));
      LastFile = string.Empty;
    }

    public event PropertyChangedEventHandler PropertyChanged;
    public void OnPropertyChanged(PropertyChangedEventArgs e)
    {
      if (PropertyChanged != null)
        PropertyChanged(this, e);
    }
}

  public class FolderList : ObservableCollection<string>
  {

  }

  public class CandidateFile : INotifyPropertyChanged
  {
    private CandidateFileList _parent;
    public string Path { get; set; }

    private bool _toRemove;
    public bool ToRemove
    {
      get { return _toRemove; }
      set
      {
        if (value != _toRemove)
        {
          _toRemove = value;
          OnPropertyChanged(new PropertyChangedEventArgs("ToRemove"));
          _parent.NotifyChildren(new PropertyChangedEventArgs("CanSetRemove"));
        }
      }
    }

    public bool CanSetRemove
    {
      get
      {
        return _parent.CanSetRemove(this);
      }
    }

    public CandidateFile(CandidateFileList parent)
    {
      _parent = parent;
    }
    public event PropertyChangedEventHandler PropertyChanged;
    public void OnPropertyChanged(PropertyChangedEventArgs e)
    {
      if (PropertyChanged != null)
        PropertyChanged(this, e);
    }

    public void DeleteIfMarked()
    {
      if (_toRemove)
      {
        File.Delete(this.Path);
        _parent.RemoveFile(this);
      }
    }
  }

  public class CandidateFileList : INotifyPropertyChanged
  {
    public string Hash { get; set; }
    public string FileName { get; set; }

    public ObservableCollection<CandidateFile> CandidateFiles { get; set; }

    public CandidateFileList()
    {
      CandidateFiles = new ObservableCollection<CandidateFile>();
    }

    public void AddFile(CandidateFile cf)
    {
      CandidateFiles.Add(cf);
      OnPropertyChanged(new PropertyChangedEventArgs("CandidateFiles"));
    }

    public bool CanSetRemove(CandidateFile child)
    {
      if (!child.ToRemove)
      {
        return CandidateFiles.Count(c => c.ToRemove) < (CandidateFiles.Count - 1);
      }

      return true;
    }

    public void NotifyChildren(PropertyChangedEventArgs e)
    {
      foreach (var c in CandidateFiles)
        c.OnPropertyChanged(e);
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged(PropertyChangedEventArgs e)
    {
      if (PropertyChanged != null)
        PropertyChanged(this, e);
    }

    public void RemoveFile(CandidateFile f)
    {
      CandidateFiles.Remove(f);
    }
  }

}
