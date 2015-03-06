using VideoJournal.Common;
using VideoJournal.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
using Windows.UI.Core;

using System.Diagnostics;

// The Item Detail Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234232

namespace VideoJournal
{
    /// <summary>
    /// A page that displays details for a single item within a group.
    /// </summary>
    public sealed partial class ItemDetailPage : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        private MediaCapture captureManager;
        private bool previewing = false;

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

        public ItemDetailPage()
        {
            this.InitializeComponent();
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += navigationHelper_LoadState;
            this.navigationHelper.SaveState += navigationHelper_SaveState;

            Window.Current.VisibilityChanged += Current_VisibilityChanged;

            Application.Current.Resuming += (sender, o) => OnResuming(sender, o);
            Application.Current.Suspending += (sender, args) => OnSuspending(sender, args);
        }

        public async void CaptureButton_Click(object sender, RoutedEventArgs e)
        {
            var camera = new CameraCaptureUI();
            var result = await camera.CaptureFileAsync(CameraCaptureUIMode.Photo);
            if (result != null)
            {
                Debug.WriteLine("Photo taken!");
            }
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
            // TODO: Create an appropriate data model for your problem domain to replace the sample data
            var item = await SampleDataSource.GetItemAsync((String)e.NavigationParameter);
            this.DefaultViewModel["Item"] = item;

            await StartCapture();

            //Debug.WriteLine("Loaded!");
        }

        private async void Current_VisibilityChanged(object sender, VisibilityChangedEventArgs e)
        {
            if (e.Visible)
            {
                await StartCapture();
            }
            else
            {
                await CleanupCaptureResources();
            }
        }

        private async Task StartCapture()
        {
            if (previewing)
                return; // Already started.

            //Debug.WriteLine("Start capture!");
            captureManager = new MediaCapture();
            await captureManager.InitializeAsync();

            capturePreview.Source = captureManager;
            capturePreview.FlowDirection = FlowDirection.RightToLeft;
            await captureManager.StartPreviewAsync();

            previewing = true;
        }

        private async Task CleanupCaptureResources()
        {
            if (!previewing)
                return; // Already cleaned up, or haven't started.

            //Debug.WriteLine("Cleanup!");

            await captureManager.StopPreviewAsync();
            capturePreview.Source = null;
            captureManager.Dispose();

            previewing = false;
        }

        private async void navigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
            //Debug.WriteLine("SaveState ran!");
            await CleanupCaptureResources();    
        }

        private async void OnResuming(object sender, object o)
        {
            //Debug.WriteLine("OnResuming ran!");
            await CleanupCaptureResources();
            await StartCapture();
        }

        private async void OnSuspending(object sender, SuspendingEventArgs e)
        {
            //Debug.WriteLine("OnSuspend ran!");
            var deferral = e.SuspendingOperation.GetDeferral();

            //cleanup camera resources
            await CleanupCaptureResources();

            deferral.Complete();
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
    }
}