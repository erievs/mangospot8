using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Media;
using System.Windows.Input;
using System.Diagnostics;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net.Http;
using System.Windows.Media.Imaging;
using Microsoft.Phone.BackgroundAudio;
using System.IO.IsolatedStorage;
using static MangoSpot8.SpotifyModal;
using System.IO;
using BackgroundAudioAgent;
using static BackgroundAudioAgent.AudioPlayer;

namespace MangoSpot8
{
    public partial class MainPage : PhoneApplicationPage
    {

        private bool isWhatsNewLoaded = false;

        public MainPage()
        {

            InitializeComponent();

            ValidateAndHandleAccessToken();

            UpdateMetadataDisplay();

            DataContext = App.ViewModel;

        }

        public async void ValidateAndHandleAccessToken()
        {
            string accessToken = Settings.AccessToken;

            bool isValid = await TokenManager.ValidateAccessCodeAsync(accessToken);

            if (!isValid)
            {
                await TokenManager.RefreshAccessTokenAsync();
            }
           
        }

        public async Task GetNewReleasesAsync()
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {Settings.AccessToken}");

            var url = "https://api.spotify.com/v1/browse/new-releases?limit=8";

            try
            {
                var response = await client.GetAsync(url);

                Debug.WriteLine($"Response Status Code: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();

                    Debug.WriteLine($"JSON Response: {jsonResponse}");

                    dynamic newReleases = JsonConvert.DeserializeObject(jsonResponse);

                    Debug.WriteLine($"Number of albums: {newReleases.albums.items.Count}");

                    for (int i = 0; i < newReleases.albums.items.Count; i++)
                    {
                        var album = newReleases.albums.items[i];
                        string albumName = album.name;
                        string artistName = album.artists[0].name;
                        string releaseDate = album.release_date;
                        string albumImage = album.images[0].url;
                        string albumId = album.id ?? "Unknown Album ID";

                        Debug.WriteLine($"Album {i + 1}: {albumName}, Artist: {artistName}, Release Date: {releaseDate}");
                        Debug.WriteLine($"Cover Image URL: {albumImage}");
                        Debug.WriteLine($"ID : {albumId}");


                        SetAlbumDetails(i, albumName, artistName, albumImage, albumId);
                    }
                }
                else
                {
                    Debug.WriteLine($"Error: {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception: {ex.Message}");
            }
            finally
            {
                BubbleLoadingAnimationWhatsNewScoobyDoo.Stop();
                LoadingPanelNew.Visibility = Visibility.Collapsed;
                WhatsNew.Visibility = Visibility.Visible;
                isWhatsNewLoaded = true;
            }
        }

        private void AlbumImage_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            var stackPanel = (StackPanel)sender;

            var albumId = (string)stackPanel.Tag;

            if (albumId != null)
            {
                NavigationService.Navigate(new Uri("/ViewAlbum.xaml?mediaType=album&mediaId=" + albumId, UriKind.Relative));
            }
            else
            {
                Debug.WriteLine("Album ID not found in StackPanel Tag.");
            }
        }

        private void SetAlbumDetails(int index, string albumName, string artistName, string albumImage, string albumId)
        {
            BitmapImage albumImageSource = new BitmapImage(new Uri(albumImage, UriKind.Absolute));

            switch (index)
            {
                case 0:
                    AlbumName1.Text = albumName;
                    Author1.Text = artistName;
                    AlbumImage1.Source = albumImageSource;
                    Album1.Tag = albumId; 
                    break;
                case 1:
                    AlbumName2.Text = albumName;
                    Author2.Text = artistName;
                    AlbumImage2.Source = albumImageSource;
                    Album2.Tag = albumId;
                    break;
                case 2:
                    AlbumName3.Text = albumName;
                    Author3.Text = artistName;
                    AlbumImage3.Source = albumImageSource;
                    Album3.Tag = albumId;
                    break;
                case 3:
                    AlbumName4.Text = albumName;
                    Author4.Text = artistName;
                    AlbumImage4.Source = albumImageSource;
                    Album4.Tag = albumId;
                    break;
                case 4:
                    AlbumName5.Text = albumName;
                    Author5.Text = artistName;
                    AlbumImage5.Source = albumImageSource;
                    Album5.Tag = albumId;  
                    break;
                case 5:
                    AlbumName6.Text = albumName;
                    Author6.Text = artistName;
                    AlbumImage6.Source = albumImageSource;
                    Album6.Tag = albumId;  
                    break;
                case 6:
                    AlbumName7.Text = albumName;
                    Author7.Text = artistName;
                    AlbumImage7.Source = albumImageSource;
                    Album7.Tag = albumId; 
                    break;
                default:
                    break;
            }
        }

        private void UpdateMetadataDisplay()
        {
            Debug.WriteLine("Second Section selected");

            string jsonFilePath = "tracks.json";
            List<TrackMetadata> trackMetadataList = new List<TrackMetadata>();

            using (var isoStore = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (isoStore.FileExists(jsonFilePath))
                {
                    using (var stream = new IsolatedStorageFileStream(jsonFilePath, FileMode.Open, isoStore))
                    using (var reader = new StreamReader(stream))
                    {
                        string json = reader.ReadToEnd();
                        trackMetadataList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<TrackMetadata>>(json);
                    }
                }
            }

            if (trackMetadataList.Count > 0)
            {

                var track = trackMetadataList[0];

                Metadata.Visibility = Visibility.Visible;

                SongTitle.Text = track.Title;
                SongAurthor.Text = track.Artist;

                Metadata.Tag = track.SongID;

                if (track.ThumbnailUrl != null)
                {
                    CurrentPlaying.Source = new BitmapImage(new Uri(track.ThumbnailUrl, UriKind.Absolute));
                }

                Uri trackUrl = new Uri(track.Url, UriKind.Absolute);

                Debug.WriteLine($"Playing track: {track.Title} by {track.Artist}, URL: {trackUrl}");
            }
            else
            {
                Debug.WriteLine("No track metadata found.");
            }
        }

        private void Metadata_tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            string songId = Metadata.Tag.ToString();
            
            NavigationService.Navigate(new Uri($"/SongPage.xaml?songId={Uri.EscapeDataString(songId)}", UriKind.Relative));
        }

        private async void Panorama_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int selectedIndex = ((Panorama)sender).SelectedIndex;

            Debug.WriteLine("Selected Panorama Item Index: " + selectedIndex);

            switch (selectedIndex)
            {
                case 0:
                    Debug.WriteLine("First Section selected");
                    break;
                case 1:

                    Debug.WriteLine("Second Section selected");

                    UpdateMetadataDisplay();


                    break;
                case 2:
                    Debug.WriteLine("Third Section selected");

                    if (!isWhatsNewLoaded)
                    {
                        LoadingPanelNew.Visibility = Visibility.Visible;
                        WhatsNew.Visibility = Visibility.Collapsed;
                        BubbleLoadingAnimationWhatsNewScoobyDoo.Begin();
                        await GetNewReleasesAsync();
                    }

                    break;
                default:
                    Debug.WriteLine("Unknown section selected");
                    break;
            }
        }

        private void searchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            searchBox.Background = new SolidColorBrush(Color.FromArgb(255, 250, 137, 45));
        }
        private void searchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            searchBox.Background = new SolidColorBrush(Color.FromArgb(255, 250, 137, 45));
        }

        private void searchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string searchText = searchBox.Text;
                PerformSearch(searchText);
            }
        }

        private void PerformSearch(string query)
        {
            if (!string.IsNullOrWhiteSpace(query))
            {
                NavigationService.Navigate(new Uri("/SearchPage.xaml?query=" + Uri.EscapeDataString(query), UriKind.Relative));
            }
            else
            {
                MessageBox.Show("Please enter a search term.");
            }
        }

        private void PlaylistTapped(object sender, System.Windows.Input.GestureEventArgs e)
        {
            NavigationService.Navigate(new Uri("/PlaylistPage.xaml", UriKind.Relative));
        }

        private void SettingsTapped(object sender, System.Windows.Input.GestureEventArgs e)
        {
            NavigationService.Navigate(new Uri("/SettingsPage.xaml", UriKind.Relative));
        }
        

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (!App.ViewModel.IsDataLoaded)
            {
                App.ViewModel.LoadData();
            }
        }
    }
}