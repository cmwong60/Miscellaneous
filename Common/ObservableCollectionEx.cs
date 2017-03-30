using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Collections.Specialized;

namespace Utils
{
  public class ObservableCollectionEx<T> : ObservableCollection<T> where T : INotifyPropertyChanged
  {
    protected override void InsertItem(int index, T item)
    {
      base.InsertItem(index, item);
      item.PropertyChanged += item_PropertyChanged;
    }

    protected override void RemoveItem(int index)
    {
      var item = this[index];
      base.RemoveItem(index);
      item.PropertyChanged -= item_PropertyChanged;
    }

    protected override void SetItem(int index, T item)
    {
      var oldItem = this[index];
      oldItem.PropertyChanged -= item_PropertyChanged;
      base.SetItem(index, item);
      item.PropertyChanged += item_PropertyChanged;
    }

    void item_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
      // generate a "replace" event
      OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, sender, sender));
    }
  }

}