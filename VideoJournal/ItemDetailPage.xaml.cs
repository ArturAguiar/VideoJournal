using VideoJournal.Common;
using VideoJournal.Data;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.Storage;

using System.Diagnostics;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Data;

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

        private bool playing = false;
        private TimeSpan videoTimeSpanDuration;

        private BindingExpression sliderValueExpression;

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

            mediaElement.MediaOpened += mediaElement_MediaOpened;

            videoProgressSlider.AddHandler(PointerPressedEvent, new PointerEventHandler(videoProgressSlider_PointerPressed), true);
            videoProgressSlider.AddHandler(PointerReleasedEvent, new PointerEventHandler(videoProgressSlider_PointerReleased), true);
            sliderValueExpression = videoProgressSlider.GetBindingExpression(Slider.ValueProperty);
            videoProgressSlider.ThumbToolTipValueConverter = new TimeSpanFormatter();
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
            // TODO: Create an appropriate data model for your problem domain to replace the vlog data
            var item = await VlogDataSource.GetItemAsync((String)e.NavigationParameter);
            this.DefaultViewModel["Item"] = item;

            StorageFile vlogFile = await ApplicationHelper.GetVlogFile(item.Filename);
            var stream = await vlogFile.OpenAsync(Windows.Storage.FileAccessMode.Read);

            if (vlogFile != null)
            {
                mediaElement.SetSource(stream, vlogFile.ContentType);
            }
        }

        void mediaElement_MediaOpened(object sender, RoutedEventArgs e)
        {
            PlayVideo();
            videoProgressSlider.Maximum = mediaElement.NaturalDuration.TimeSpan.TotalMilliseconds;

            if (mediaElement.CanSeek)
                videoProgressSlider.IsEnabled = true;

            if (mediaElement.CanPause)
                playPauseButton.IsEnabled = true;

            videoTimeSpanDuration = mediaElement.NaturalDuration.TimeSpan;

            videoDurationTextBlock.Text = "";
            if (videoTimeSpanDuration.Hours > 0)
            {
                videoDurationTextBlock.Text += videoTimeSpanDuration.Hours.ToString("D2") + ":";
            }
            videoDurationTextBlock.Text += String.Format("{0:D2}:{1:D2}", videoTimeSpanDuration.Minutes, videoTimeSpanDuration.Seconds);
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

        private void playPauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (playing && mediaElement.CanPause)
            {
                PauseVideo();
            }
            else
            {
                PlayVideo();
            }
        }

        private void prevButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: implement this
        }

        private void nextButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: implement this
        }

        private void PlayVideo()
        {
            if (playing)
                return; //already playing

            mediaElement.Play();
            playing = true;
            playPauseButtonSymbol.Symbol = Symbol.Pause;
        }

        private void PauseVideo()
        {
            if (!playing)
                return; //already paused

            mediaElement.Pause();
            playing = false;
            playPauseButtonSymbol.Symbol = Symbol.Play;
        }

        private void videoProgressSlider_PointerPressed(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            PauseVideo();
            VideoSeekTo(videoProgressSlider.Value);
        }

        private void videoProgressSlider_ValueChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (playing)
                return; // the value was updated just due to the current time of the video increasing

            // the value was changed by the user
            VideoSeekTo(e.NewValue);
        }

        private void videoProgressSlider_PointerReleased(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            videoProgressSlider.SetBinding(Slider.ValueProperty, sliderValueExpression.ParentBinding);
            PlayVideo();
        }

        private void VideoSeekTo(double milliseconds)
        {
            if (mediaElement.CanSeek)
            {
                TimeSpan seekTo = TimeSpan.FromMilliseconds(milliseconds);

                mediaElement.Position = seekTo;
            }
        }        
    }


    public class TimeSpanFormatter : IValueConverter
    {
        // This converts the DateTime object to the string to display.
        public object Convert(object value, Type targetType, object parameter, string lang)
        {
            // Retrieve the format string and use it to format the value.
            TimeSpan timeSpan = TimeSpan.FromMilliseconds((double)value);

            string converted = "";
            if (timeSpan.Hours > 0)
            {
                converted += timeSpan.Hours.ToString("D2") + ":";
            }
            converted += string.Format("{0:D2}:{1:D2}", timeSpan.Minutes, timeSpan.Seconds);

            return converted;
        }

        // No need to implement converting back on a one-way binding 
        public object ConvertBack(object value, Type targetType,
            object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}