using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Windows.Data.Json;
using Windows.Storage;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using VideoJournal.Common;
using System.ComponentModel;
using Newtonsoft.Json;

using System.Diagnostics;

namespace VideoJournal.Data
{
    /// <summary>
    /// Generic item data model.
    /// </summary>
    public class VlogDataItem : INotifyPropertyChanged
    {
        public VlogDataItem(String uniqueId, String title, String subtitle, String imagePath, String description, String filename, String content)
        {
            this.UniqueId = uniqueId;
            this.Title = title;
            this.Subtitle = subtitle;
            this.Description = description;
            this.ImagePath = imagePath;
            this.Filename = filename;
            this.Content = content;
        }

        public string UniqueId { get; private set; }
        public string Title { get; private set; }
        public string Subtitle { get; private set; }
        public string Description { get; private set; }
        public string Filename { get; private set; }
        public string Content { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private string imagePath;
        public string ImagePath
        {
            get 
            {
                return this.imagePath;
            }
            set
            {
                this.imagePath = value;
                NotifyPropertyChanged();
            }
        }

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public override string ToString()
        {
            return this.Title;
        }
    }

    /// <summary>
    /// Generic group data model.
    /// </summary>
    public class VlogDataGroup : INotifyPropertyChanged
    {
        public VlogDataGroup(String uniqueId, String title, String subtitle, String imagePath, String description)
        {
            this.UniqueId = uniqueId;
            this.Title = title;
            this.Subtitle = subtitle;
            this.Description = description;
            this.ImagePath = imagePath;
            this.Items = new ObservableCollection<VlogDataItem>();
        }

        public string UniqueId { get; private set; }
        public string Title { get; private set; }
        public string Subtitle { get; private set; }
        public string Description { get; private set; }
        public ObservableCollection<VlogDataItem> Items { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private string imagePath;
        public string ImagePath
        {
            get
            {
                return this.imagePath;
            }
            set
            {
                this.imagePath = value;
                NotifyPropertyChanged();
            }
        }

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public override string ToString()
        {
            return this.Title;
        }
    }

    /// <summary>
    /// Creates a collection of groups and items with content read from a static json file.
    /// 
    /// VlogDataSource initializes with data read from a static json file included in the 
    /// project.  This provides vlog data at both design-time and run-time.
    /// </summary>
    public sealed class VlogDataSource
    {
        private const string DATA_FILENAME = "VlogData.json";
        private Uri DATA_FOLDER_URI = new Uri("ms-appdata:///local");
        private Uri DATA_URI = new Uri("ms-appdata:///local/" + DATA_FILENAME); //new Uri("ms-appx:///DataModel/VlogData.json");

        private bool dataChangedSinceLastSave = false;
        private bool saving = false;

        private static VlogDataSource _vlogDataSource = new VlogDataSource();

        private ObservableCollection<VlogDataGroup> _groups = new ObservableCollection<VlogDataGroup>();
        public ObservableCollection<VlogDataGroup> Groups
        {
            get { return this._groups; }
        }

        public VlogDataSource()
        {
            this.Groups.CollectionChanged += Groups_CollectionChanged;
        }

        void Groups_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            dataChangedSinceLastSave = true;
        }

        public static async Task<IEnumerable<VlogDataGroup>> GetGroupsAsync()
        {
            await _vlogDataSource.GetVlogDataAsync();

            return _vlogDataSource.Groups;
        }

        public static async Task<VlogDataGroup> GetGroupAsync(string uniqueId)
        {
            await _vlogDataSource.GetVlogDataAsync();
            // Simple linear search is acceptable for small data sets
            var matches = _vlogDataSource.Groups.Where((group) => group.UniqueId.Equals(uniqueId));
            if (matches.Count() == 1) return matches.First();
            return null;
        }

        public static async Task<VlogDataItem> GetItemAsync(string uniqueId)
        {
            await _vlogDataSource.GetVlogDataAsync();
            // Simple linear search is acceptable for small data sets
            var matches = _vlogDataSource.Groups.SelectMany(group => group.Items).Where((item) => item.UniqueId.Equals(uniqueId));
            if (matches.Count() == 1) return matches.First();
            return null;
        }

        public static async Task AddItemAsync(String uniqueId, String title, String subtitle, String imagePath, String description, String filename, String content)
        {
            VlogDataItem newItem = new VlogDataItem(uniqueId, title, subtitle, imagePath, description, filename, content);
            newItem.ImagePath = await ApplicationHelper.GetThumbnailPath(filename);

            string groupID = ApplicationHelper.GetCurrentMonthID();

            VlogDataGroup group = await VlogDataSource.GetGroupAsync(groupID);

            if (group == null)
            {
                // Group doesn't exist yet, create it.
                VlogDataGroup newGroup = new VlogDataGroup(ApplicationHelper.GetCurrentMonthID(),
                                                           ApplicationHelper.GetCurrentMonthTitle(),
                                                           ApplicationHelper.GetCurrentMonthTitle() + " video logs",
                                                           ApplicationHelper.DEFAULT_GROUP_IMAGE_PATH,
                                                           "");
                _vlogDataSource.Groups.Add(newGroup);
                group = newGroup;
            }

            // Update group image to most recent video thumbnail
            group.ImagePath = newItem.ImagePath;

            group.Items.Add(newItem);
        }

        private async Task GetVlogDataAsync()
        {
            if (this._groups.Count != 0)
                return;

            StorageFile dataFile;
            try
            {
                dataFile = await StorageFile.GetFileFromApplicationUriAsync(DATA_URI);
            }
            catch (System.Exception)
            {
                // if file wasn't found, just return. Groups is empty.
                return;
            }

            //JsonConvert.DeserializeObject<ObservableCollection<VlogDataGroup>>(output);
            
            string jsonText = await FileIO.ReadTextAsync(dataFile);
            JsonObject jsonObject = JsonObject.Parse(jsonText);
            JsonArray jsonArray = jsonObject["Groups"].GetArray();

            foreach (JsonValue groupValue in jsonArray)
            {
                JsonObject groupObject = groupValue.GetObject();
                VlogDataGroup group = new VlogDataGroup(groupObject["UniqueId"].GetString(),
                                                        groupObject["Title"].GetString(),
                                                        groupObject["Subtitle"].GetString(),
                                                        groupObject["ImagePath"].GetString(),
                                                        groupObject["Description"].GetString());

                foreach (JsonValue itemValue in groupObject["Items"].GetArray())
                {
                    JsonObject itemObject = itemValue.GetObject();
                    group.Items.Add(new VlogDataItem(itemObject["UniqueId"].GetString(),
                                                     itemObject["Title"].GetString(),
                                                     itemObject["Subtitle"].GetString(),
                                                     itemObject["ImagePath"].GetString(),
                                                     itemObject["Description"].GetString(),
                                                     itemObject["Filename"].GetString(),
                                                     itemObject["Content"].GetString()));
                }
                this.Groups.Add(group);
            }
        }

        public static async Task SaveToFile()
        {
            if (!_vlogDataSource.dataChangedSinceLastSave || _vlogDataSource.saving)
                return; // nothing changed, so no need to save again.

            _vlogDataSource.saving = true;

            string jsonString = JsonConvert.SerializeObject(_vlogDataSource);

            StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;

            StorageFile newDataFile = await localFolder.CreateFileAsync(DATA_FILENAME, CreationCollisionOption.ReplaceExisting);

            await Windows.Storage.FileIO.WriteTextAsync(newDataFile, jsonString);

            _vlogDataSource.dataChangedSinceLastSave = false;
            _vlogDataSource.saving = false;

            Debug.WriteLine("Saved to: " + localFolder.Path + "/" + DATA_FILENAME);
        }
    }
}