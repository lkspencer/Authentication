namespace FaceApiClient {
  using Microsoft.ProjectOxford.Face.Contract;
  using System;
  using System.Collections.Generic;
  using System.Collections.ObjectModel;
  using System.IO;
  using System.Linq;
  using System.Windows;
  using System.Windows.Controls;
  using System.Windows.Media.Imaging;

  public partial class AddPerson : Window {
    // Constructors
    public AddPerson(PersonGroup group) {
      this.DataContext = this;
      this.group = group;
      InitializeComponent();
    }



    // Variables
    private ObservableCollection<string> filePaths = new ObservableCollection<string>();
    private PersonGroup group;



    // Properties
    public ObservableCollection<string> FilePaths {
      get {
        return this.filePaths;
      }
    }



    // Event Handlers
    private void AddFace_Click(object sender, RoutedEventArgs e) {
      Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
      dlg.DefaultExt = ".jpg"; // Default file extension
      dlg.Filter = "Images (.jpg)|*.jpg"; // Filter files by extension 

      // Show open file dialog box
      Nullable<bool> result = dlg.ShowDialog();

      // Process open file dialog box results 
      if (result == true) {
        // Open document 
        string filename = dlg.FileName;
        BitmapImage image = new BitmapImage();
        image.BeginInit();
        image.UriSource = new Uri(filename);
        image.EndInit();
        ImageViewer.Source = image;
        this.filePaths.Add(filename);
      }
    }
    private void FileListBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
      if (FileListBox.SelectedItem == null) {
        ImageViewer.Source = null;
        return;
      }
      string filename = (string)FileListBox.SelectedItem;
      if (string.IsNullOrWhiteSpace(filename)) {
        ImageViewer.Source = null;
        return;
      }
      BitmapImage image = new BitmapImage();
      image.BeginInit();
      image.UriSource = new Uri(filename);
      image.EndInit();
      ImageViewer.Source = image;
    }
    private void Cancel_Click(object sender, RoutedEventArgs e) {
      this.Close();
    }
    private async void Save_Click(object sender, RoutedEventArgs e) {
      List<Guid> faceIds = new List<Guid>();
      foreach (var path in this.filePaths) {
        using (var fStream = File.OpenRead(path)) {
          //System.Threading.Thread.Sleep(5000);
          var faces = await App.Instance.DetectAsync(fStream);
          faceIds.AddRange(faces.Select(f => f.FaceId));
        }
      }
      var personId = (await App.Instance.CreatePersonAsync(group.PersonGroupId, faceIds.ToArray(), Name.Text)).PersonId.ToString();
      this.Close();
    }
    private void DeleteFace_Click(object sender, RoutedEventArgs e) {
      if (FileListBox.SelectedItem == null) return;
      var filename = FileListBox.SelectedItem as string;
      if (string.IsNullOrWhiteSpace(filename)) return;
      this.FilePaths.Remove(filename);
      ImageViewer.Source = null;
    }

  }
}
