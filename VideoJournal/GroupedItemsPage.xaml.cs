using VideoJournal.Common;
using VideoJournal.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Media.Capture;
using Windows.Storage;

using System.Diagnostics;

// The Grouped Items Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234231

namespace VideoJournal
{
    /// <summary>
    /// A page that displays a grouped collection of items.
    /// </summary>
    public sealed partial class GroupedItemsPage : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        private List<VlogDataItem> selectedVlogs = new List<VlogDataItem>();
        private bool deletingItems = false;

        /// <summary>
        /// NavigationHelper is used on each page to aid in navigation and 
        /// process lifetime management
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        /// <summary>
        /// This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        public GroupedItemsPage()
        {
            this.InitializeComponent();
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += navigationHelper_LoadState;
            this.navigationHelper.SaveState += navigationHelper_SaveState;

            Application.Current.Suspending += (sender, args) => OnSuspending(sender, args);
        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session.  The state will be null the first time a page is visited.</param>
        private async void navigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            var vlogDataGroups = await VlogDataSource.GetGroupsAsync();
            
            this.DefaultViewModel["Groups"] = vlogDataGroups;

            SetThumbnailImages();
        }

        private async void navigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
            Debug.WriteLine("Ran SaveState!");
            await VlogDataSource.SaveToFile();
        }

        private async void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();

            Debug.WriteLine("Ran OnSuspending!");
            await VlogDataSource.SaveToFile();

            deferral.Complete();
        }

        private async void SetThumbnailImages()
        {
            StorageFolder tempFolder = Windows.Storage.ApplicationData.Current.TemporaryFolder;

            foreach (VlogDataGroup group in (IEnumerable<VlogDataGroup>)this.DefaultViewModel["Groups"])
            {
                foreach (VlogDataItem item in group.Items)
                {
                    item.ImagePath = await ApplicationHelper.GetThumbnailPath(item);
                }
            }
        }

        /// <summary>
        /// Invoked when a group header is clicked.
        /// </summary>
        /// <param name="sender">The Button used as a group header for the selected group.</param>
        /// <param name="e">Event data that describes how the click was initiated.</param>
        void Header_Click(object sender, RoutedEventArgs e)
        {
            // Determine what group the Button instance represents
            var group = (sender as FrameworkElement).DataContext;

            // Navigate to the appropriate destination page, configuring the new page
            // by passing required information as a navigation parameter
            this.Frame.Navigate(typeof(GroupDetailPage), ((VlogDataGroup)group).UniqueId);
        }

        /// <summary>
        /// Invoked when an item within a group is clicked.
        /// </summary>
        /// <param name="sender">The GridView (or ListView when the application is snapped)
        /// displaying the item clicked.</param>
        /// <param name="e">Event data that describes the item clicked.</param>
        void ItemView_ItemClick(object sender, ItemClickEventArgs e)
        {
            // Navigate to the appropriate destination page, configuring the new page
            // by passing required information as a navigation parameter
            var itemId = ((VlogDataItem)e.ClickedItem).UniqueId;
            this.Frame.Navigate(typeof(ItemDetailPage), itemId);
        }

        public async void newEntryButton_Click(object sender, RoutedEventArgs e)
        {
            var captureUI = new Windows.Media.Capture.CameraCaptureUI();
            captureUI.VideoSettings.AllowTrimming = true;
            captureUI.VideoSettings.Format = ApplicationHelper.GetVideoFormat(); //Windows.Media.Capture.CameraCaptureUIVideoFormat.Wmv;
            captureUI.VideoSettings.MaxResolution = Windows.Media.Capture.CameraCaptureUIMaxVideoResolution.HighestAvailable; //.StandardDefinition;
            //captureUI.VideoSettings.MaxDurationInSeconds = 10;

            StorageFile vlog = await captureUI.CaptureFileAsync(CameraCaptureUIMode.Video);

            if (vlog != null)
            {
                StorageFolder destinationFolder = await ApplicationHelper.GetDestinationFolder();

                string filename = ApplicationHelper.GenerateFilename();
                int counter = 1;
                bool movedSuccessfully = false;
                
                while (!movedSuccessfully)
                {
                    try
                    {
                        await vlog.MoveAsync(destinationFolder, filename, NameCollisionOption.FailIfExists);
                        movedSuccessfully = true;
                    }
                    catch (System.Exception)
                    {
                        counter++;
                        filename = ApplicationHelper.GenerateFilename("_" + counter);
                    }
                }

                string id = ApplicationHelper.GetRandomGuid(); //ApplicationHelper.GetCurrentDayID();
                string title = ApplicationHelper.GetCurrentDayTitle();

                if (counter > 1)
                {
                    //id += "_" + counter;
                    title += " #" + counter;
                }

                await VlogDataSource.AddItemAsync(id,                                           // uniqueId
                                                  title,                                        // title
                                                  "",                                           // subtitle
                                                  ApplicationHelper.DEFAULT_VLOG_IMAGE_PATH,    // imagePath
                                                  "",                                           // description
                                                  filename,                                     // filename
                                                  "");                                          // content
            }

            
        }

        #region NavigationHelper registration

        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// 
        /// Page specific logic should be placed in event handlers for the  
        /// <see cref="GridCS.Common.NavigationHelper.LoadState"/>
        /// and <see cref="GridCS.Common.NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method 
        /// in addition to page state preserved during an earlier session.

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        private async void deleteSelectionButton_Click(object sender, RoutedEventArgs e)
        {
            deletingItems = true;

            foreach (VlogDataItem vlogToDelete in this.selectedVlogs)
            {
                await VlogDataSource.DeleteItemAsync(vlogToDelete);
            }

            this.selectedVlogs.Clear();
            deleteSelectionButton.Opacity = 0;

            deletingItems = false;
        }

        private void itemGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (deletingItems)
                return; // just deleting items, no need to update the collection until it is done.

            foreach (object objAdded in e.AddedItems)
            {
                selectedVlogs.Add(objAdded as VlogDataItem);
            }

            foreach (object objRemoved in e.RemovedItems)
            {
                selectedVlogs.Remove(objRemoved as VlogDataItem);
            }

            if (this.selectedVlogs.Count > 0)
                deleteSelectionButton.Opacity = 1;
            else
                deleteSelectionButton.Opacity = 0;            
        }
    }
}