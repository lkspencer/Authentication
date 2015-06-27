namespace FaceApiClient {
  using System;
  using System.IO;
  using System.Linq;
  using System.Windows;
  using System.Collections.Generic;
  using Microsoft.ProjectOxford.Face.Contract;
  using System.Collections.ObjectModel;
  using System.ComponentModel;

  public partial class MainWindow : Window, INotifyPropertyChanged {
    // Constructors
    public MainWindow() {
      this.DataContext = this;
      InitializeComponent();
      if (File.Exists("key.txt")) {
        this.key = File.ReadAllText("key.txt");
      }
      if (string.IsNullOrWhiteSpace(this.key)) {
        MessageBox.Show(String.Format("{0}{1}{2}{3}",
          "Create a \"key.txt\" file in the root of the Client project. The key file should have your Azure Project Oxford key in it. It should be on a single line, no carriage return after the key.",
          "\r\n\r\n*********************************************************************************\r\n",
          "     DO NOT CHECK THE KEY.TXT FILE IN TO GITHUB!!!!!!!",
          "\r\n*********************************************************************************"));
        this.Close();
        return;
      }
      App.Initialize(this.key);
      GetGroups();
      this.FacesCount = "Faces Count: 0";
    }



    // Variables
    private string key = "";
    private PersonGroup group;
    private Person person;
    private ObservableCollection<Person> groupPersons = new ObservableCollection<Person>();
    private ObservableCollection<PersonGroup> personGroups = new ObservableCollection<PersonGroup>();
    private string groupName;
    private string facesCount;
    public event PropertyChangedEventHandler PropertyChanged;




    // Properties
    public ObservableCollection<Person> GroupPersons {
      get {
        return this.groupPersons;
      }
    }
    public ObservableCollection<PersonGroup> PersonGroups {
      get {
        return this.personGroups;
      }
    }
    public string GroupName {
      get {
        return this.groupName;
      }
      set {
        this.groupName = value;
      }
    }
    public string FacesCount {
      get {
        return this.facesCount;
      }
      set {
        this.facesCount = value;
        NotifyPropertyChanged("FacesCount");
      }
    }



    // Event Handlers
    private async void button_old_Click(object sender, RoutedEventArgs e) {
      App.ResetCallCount();
      var GroupNameold = "First Test";
      var PersonName = "Kirk";
      //System.Threading.Thread.Sleep(5000);
      var groups = await App.Instance.GetPersonGroupsAsync();
      var group = groups.Where(g => g.Name == GroupNameold).FirstOrDefault();
      if (group == null) {
        var personGroupId = Guid.NewGuid().ToString();
        //System.Threading.Thread.Sleep(5000);
        await App.Instance.CreatePersonGroupAsync(personGroupId, GroupNameold);
        //System.Threading.Thread.Sleep(5000);
        group = await App.Instance.GetPersonGroupAsync(personGroupId);
      }
      if (group == null) return;

      //System.Threading.Thread.Sleep(5000);
      var persons = await App.Instance.GetPersonsAsync(group.PersonGroupId);
      var kirk = persons.Where(p => p.Name == PersonName).FirstOrDefault();
      if (kirk == null) {
        List<Guid> faceIds = new List<Guid>();
        using (var fStream = File.OpenRead(@"C:\Users\kirk_000\Desktop\kirk\IMG_20150527_103454.jpg")) {
          //System.Threading.Thread.Sleep(5000);
          var faces = await App.Instance.DetectAsync(fStream);
          faceIds.AddRange(faces.Select(f => f.FaceId));
        }
        using (var fStream = File.OpenRead(@"C:\Users\kirk_000\Desktop\kirk\IMG_20140801_152108.jpg")) {
          //System.Threading.Thread.Sleep(5000);
          var faces = await App.Instance.DetectAsync(fStream);
          faceIds.AddRange(faces.Select(f => f.FaceId));
        }
        using (var fStream = File.OpenRead(@"C:\Users\kirk_000\Desktop\kirk\IMG_20130622_183858.jpg")) {
          //System.Threading.Thread.Sleep(5000);
          var faces = await App.Instance.DetectAsync(fStream);
          faceIds.AddRange(faces.Select(f => f.FaceId));
        }
        //System.Threading.Thread.Sleep(5000);
        var personId = (await App.Instance.CreatePersonAsync(group.PersonGroupId, faceIds.ToArray(), PersonName)).PersonId.ToString();
        MessageBox.Show(personId);
      } else {
        //System.Threading.Thread.Sleep(5000);
        var trainingStatus = await App.Instance.GetPersonGroupTrainingStatusAsync(group.PersonGroupId);
        if (trainingStatus.Status != "succeeded") {
          // This PersonGroup has not been trained yet
          //System.Threading.Thread.Sleep(5000);
          await App.Instance.TrainPersonGroupAsync(group.PersonGroupId);
          while (true) {
            System.Threading.Thread.Sleep(1000);
            trainingStatus = await App.Instance.GetPersonGroupTrainingStatusAsync(group.PersonGroupId);
            if (trainingStatus.Status == "running") continue;

            // This code only runs once the status changes away from "running"
            if (trainingStatus.Status != "succeeded") {
              MessageBox.Show("There was an error!");
            }
            break;
          }
        }
        //using (var fStream = File.OpenRead(@"C:\Users\kirk_000\Desktop\Laura\IMG_20150418_193313.jpg")) {
        using (var fStream = File.OpenRead(@"C:\Users\kirk_000\Desktop\Laura\20131011_203854.jpg")) {
          //System.Threading.Thread.Sleep(5000);
          var faces = await App.Instance.DetectAsync(fStream);
          if (faces == null || faces.Length == 0) {
            MessageBox.Show(String.Format("No face found in image: {0}", fStream.Name));
          } else {
            //System.Threading.Thread.Sleep(5000);
            var identifyResults = await App.Instance.IdentifyAsync(group.PersonGroupId, faces.Select(f => f.FaceId).ToArray());
            foreach (var result in identifyResults) {
              foreach (var candidate in result.Candidates) {
                if (candidate.Confidence > 0.5) {
                  //System.Threading.Thread.Sleep(5000);
                  var person = await App.Instance.GetPersonAsync(group.PersonGroupId, candidate.PersonId);
                  MessageBox.Show(String.Format("Name: {0}, API Calls: {1}", person.Name, App.CallCount));
                  return;
                }
              }
            }
          }
        }
        /*
        using (var fStream = File.OpenRead(@"C:\Users\kirk_000\Desktop\kirk\IMG_20150624_2.jpg")) {
          System.Threading.Thread.Sleep(5000);
          var faces = await App.Instance.DetectAsync(fStream);
          System.Threading.Thread.Sleep(5000);
          var identifyResults = await App.Instance.IdentifyAsync(group.PersonGroupId, faces.Select(f => f.FaceId).ToArray());
          foreach (var result in identifyResults) {
            foreach (var candidate in result.Candidates) {
              if (candidate.Confidence > 0.5) {
                System.Threading.Thread.Sleep(5000);
                var person = await App.Instance.GetPersonAsync(group.PersonGroupId, candidate.PersonId);
                MessageBox.Show(String.Format("Name: {0}, API Calls: {1}", person.Name, App.CallCount));
              }
            }
          }
        }
        using (var fStream = File.OpenRead(@"C:\Users\kirk_000\Desktop\kirk\IMG_20150624_3.jpg")) {
          System.Threading.Thread.Sleep(5000);
          var faces = await App.Instance.DetectAsync(fStream);
          System.Threading.Thread.Sleep(5000);
          var identifyResults = await App.Instance.IdentifyAsync(group.PersonGroupId, faces.Select(f => f.FaceId).ToArray());
          foreach (var result in identifyResults) {
            foreach (var candidate in result.Candidates) {
              if (candidate.Confidence > 0.5) {
                System.Threading.Thread.Sleep(5000);
                var person = await App.Instance.GetPersonAsync(group.PersonGroupId, candidate.PersonId);
                MessageBox.Show(String.Format("Name: {0}, API Calls: {1}", person.Name, App.CallCount));
              }
            }
          }
        }
        using (var fStream = File.OpenRead(@"C:\Users\kirk_000\Desktop\kirk\kirk.jpg")) {
          System.Threading.Thread.Sleep(5000);
          var faces = await App.Instance.DetectAsync(fStream);
          System.Threading.Thread.Sleep(5000);
          var identifyResults = await App.Instance.IdentifyAsync(group.PersonGroupId, faces.Select(f => f.FaceId).ToArray());
          foreach (var result in identifyResults) {
            foreach (var candidate in result.Candidates) {
              if (candidate.Confidence > 0.5) {
                System.Threading.Thread.Sleep(5000);
                var person = await App.Instance.GetPersonAsync(group.PersonGroupId, candidate.PersonId);
                MessageBox.Show(String.Format("Name: {0}, API Calls: {1}", person.Name, App.CallCount));
              }
            }
          }
        }
        //*/
      }
    }

    private async void AddGroup_Click(object sender, RoutedEventArgs e) {
      var personGroupId = Guid.NewGuid().ToString();
      await App.Instance.CreatePersonGroupAsync(personGroupId, GroupName);
      GetGroups();
    }
    private async void DeleteGroup_Click(object sender, RoutedEventArgs e) {
      if (group == null) return;

      var result = MessageBox.Show(String.Format("Delete {0}?", group.Name), "Delete?", MessageBoxButton.YesNo);
      if (result == MessageBoxResult.No) return;

      await App.Instance.DeletePersonGroupAsync(group.PersonGroupId);
      GetGroups();
    }
    private void GroupsListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) {
      group = GroupsListBox.SelectedItem as PersonGroup;
      if (group == null) return;
      GetPersons();
    }
    private void AddPerson_Click(object sender, RoutedEventArgs e) {
      if (group == null) {
        MessageBox.Show("Please select a group first");
        return;
      }
      var addPersonDialog = new AddPerson(group);
      addPersonDialog.ShowDialog();
      if (group == null) return;
      Train();
      GetPersons();
    }
    private async void DeletePerson_Click(object sender, RoutedEventArgs e) {
      if (group == null || person == null) return;

      var result = MessageBox.Show(String.Format("Delete {0}?", person.Name), "Delete?", MessageBoxButton.YesNo);
      if (result == MessageBoxResult.No) return;

      await App.Instance.DeletePersonAsync(group.PersonGroupId, person.PersonId);
      Train();
      GetPersons();
    }
    private void PeopleListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) {
      person = PeopleListBox.SelectedItem as Person;
      if (person == null) return;
      this.FacesCount = String.Format("Faces Count: {0}", person.FaceIds.Length);
    }
    private async void TestImage_Click(object sender, RoutedEventArgs e) {
      if (group == null) {
        MessageBox.Show("Please select a group first");
        return;
      }
      App.ResetCallCount();
      Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
      dlg.DefaultExt = ".jpg"; // Default file extension
      dlg.Filter = "Images (.jpg)|*.jpg"; // Filter files by extension 

      // Show open file dialog box
      Nullable<bool> dialogResult = dlg.ShowDialog();

      // Process open file dialog box results 
      if (dialogResult == true) {
        string filename = dlg.FileName;
        using (var fStream = File.OpenRead(filename)) {
          //System.Threading.Thread.Sleep(5000);
          var faces = await App.Instance.DetectAsync(fStream, false, true, true);
          if (faces == null || faces.Length == 0) {
            MessageBox.Show(String.Format("No face found in image: {0}", fStream.Name));
          } else {
            //System.Threading.Thread.Sleep(5000);
            var identifyResults = await App.Instance.IdentifyAsync(group.PersonGroupId, faces.Select(f => f.FaceId).ToArray());
            var found = 0;
            var names = "";
            foreach (var result in identifyResults) {
              foreach (var candidate in result.Candidates) {
                if (candidate.Confidence > 0.5) {
                  //System.Threading.Thread.Sleep(5000);
                  var person = await App.Instance.GetPersonAsync(group.PersonGroupId, candidate.PersonId);
                  var attributes = faces.Where(f => f.FaceId == result.FaceId).Select(f => f.Attributes).FirstOrDefault();
                  names += String.Format("{0}({1}) - {2}{3}", person.Name, attributes.Gender, attributes.Age, ", ");
                  found++;
                }
              }
            }
            if (found > 0) {
              MessageBox.Show(String.Format("Name{0}: {1}", (found > 1 ? "s" : ""), names.Substring(0, names.Length - 2)));
            } else {
              MessageBox.Show("No match found for this person");
            }
          }
        }

      }

    }


    // Methods
    private async void GetGroups() {
      var groups = await App.Instance.GetPersonGroupsAsync();
      this.personGroups.Clear();
      foreach (var pg in groups) {
        this.personGroups.Add(pg);
      }
      this.FacesCount = "Faces Count: 0";
    }
    private async void GetPersons() {
      var persons = await App.Instance.GetPersonsAsync(group.PersonGroupId);
      this.groupPersons.Clear();
      foreach (var p in persons) {
        this.groupPersons.Add(p);
      }
      this.FacesCount = "Faces Count: 0";
    }
    private async void Train() {
      if (group == null) return;
      //System.Threading.Thread.Sleep(5000);
      await App.Instance.TrainPersonGroupAsync(group.PersonGroupId);
      while (true) {
        System.Threading.Thread.Sleep(1000);
        var trainingStatus = await App.Instance.GetPersonGroupTrainingStatusAsync(group.PersonGroupId);
        if (trainingStatus.Status == "running") continue;

        // This code only runs once the status changes away from "running"
        if (trainingStatus.Status != "succeeded") {
          MessageBox.Show("There was an error!");
        }
        break;
      }
    }
    public void NotifyPropertyChanged(string propertyName) {
      if (PropertyChanged != null) {
        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
      }
    }

  }
}
