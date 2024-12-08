using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Media;
using System.Windows.Input;
using System.Net.Http;
using static MangoSpot8.SpotifyModal;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Newtonsoft.Json.Linq;

namespace MangoSpot8
{
    public partial class SearchPage : PhoneApplicationPage
    {

        private int albumOffset = 0; 
        private int trackOffset = 0; 

        private const int ResultsPerPage = 25;

        private ObservableCollection<AlbumItem> albumsCollection;
        private ObservableCollection<SongItem> tracksCollection;

        public SearchPage()
        {
            InitializeComponent();

            albumsCollection = new ObservableCollection<AlbumItem>();
            tracksCollection = new ObservableCollection<SongItem>(); 

            AlbumItemsControl.ItemsSource = albumsCollection;
            SongItemsControl.ItemsSource = tracksCollection;

            AlbumItemsControl.ManipulationDelta += AlbumItemsControl_ManipulationDelta;
            SongItemsControl.ManipulationDelta += SongsItemsControl_ManipulationDelta;
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
                NavigationService.Navigate(new Uri("/SearchPage.xaml?query=" + Uri.EscapeDataString(searchText), UriKind.Relative));
            }
        
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            string query = null;
            if (NavigationContext.QueryString.ContainsKey("query"))
            {
                query = NavigationContext.QueryString["query"];
            }
            
            if (!string.IsNullOrEmpty(query))
            {
                searchBox.Text = query;
                PerformSearch(query);
            }
        }

        private async void PerformSearch(string query)
        {
            try
            {
                string accessToken = Settings.AccessToken;
                if (string.IsNullOrEmpty(accessToken))
                {
                    MessageBox.Show("Access token is missing. Please log in.");
                    return;
                }

                // Create a HashSet to track unique IDs (for albums and tracks)
                var seenAlbumIds = new HashSet<string>();
                var seenTrackIds = new HashSet<string>();

                // Fetch results for albums and tracks with pagination
                var albums = await SearchSpotify(query, "album", accessToken, albumOffset);
                var tracks = await SearchSpotify(query, "track", accessToken, trackOffset);

                // Add the fetched albums to the ObservableCollection, ensuring no duplicates
                foreach (var album in albums.OfType<AlbumItem>())
                {
                    // Check if the album ID is already in the collection
                    if (!seenAlbumIds.Contains(album.Name))  // Assuming album name is unique, or you can use another ID like `album.Id`
                    {
                        seenAlbumIds.Add(album.Name);  // Mark this album ID as seen
                        if (!albumsCollection.Contains(album))  // Check if the album is already in the collection
                        {
                            albumsCollection.Add(album);   // Add album to collection if it's not already there
                        }
                    }
                }

                // Add the fetched tracks to the ObservableCollection, ensuring no duplicates
                foreach (var track in tracks.OfType<SongItem>())
                {
                    // Check if the track ID is already in the collection
                    if (!seenTrackIds.Contains(track.SongName))  // Assuming song name is unique, or you can use another ID like `track.Id`
                    {
                        seenTrackIds.Add(track.SongName);  // Mark this track ID as seen
                        if (!tracksCollection.Contains(track))  // Check if the track is already in the collection
                        {
                            tracksCollection.Add(track);      // Add track to collection if it's not already there
                        }
                    }
                }

                // Optionally update the offsets for pagination if you have them.
                albumOffset += ResultsPerPage;
                trackOffset += ResultsPerPage;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error performing search: " + ex.Message);
            }
        }


        private async Task<ObservableCollection<object>> SearchSpotify(string query, string type, string accessToken, int offset)
        {
            string url = $"https://api.spotify.com/v1/search?q={Uri.EscapeDataString(query)}&type={type}&limit={ResultsPerPage}&offset={offset}";

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);
                var response = await client.GetStringAsync(url);
                var json = JsonConvert.DeserializeObject<SpotifySearchResult>(response);
                JObject jsonObject = JObject.Parse(response);

                Debug.WriteLine("Received JSON response: " + response);

                var resultCollection = new ObservableCollection<object>();

                if (type == "album")
                {

                    var albums = jsonObject.SelectToken("albums.items");
                    if (albums != null)
                    {
                        foreach (var album in albums)
                        {
                            Debug.WriteLine($"Found album: {album["name"]} (ID: {album["id"]})");

                            resultCollection.Add(new AlbumItem
                            {
                                AlbumID = album["id"]?.ToString(),
                                Name = album["name"]?.ToString(),
                                Author = album["artists"]?[0]["name"]?.ToString(),
                                ImageUrl = album["images"]?.FirstOrDefault()?["url"]?.ToString() ?? string.Empty
                            });
                        }
                    }
                }

                if (type == "track")
                {

                    var tracks = jsonObject.SelectToken("tracks.items");
                    if (tracks != null)
                    {
                        foreach (var track in tracks)
                        {
                            resultCollection.Add(new SongItem
                            {
                                SongId = track["id"]?.ToString(),
                                SongName = track["name"]?.ToString(),
                                Author = track["artists"]?[0]["name"]?.ToString() ?? "Unknown",
                                Name = track["album"]?["name"]?.ToString()
                            });
                        }
                    }
                }

                return resultCollection;
            }
        }

        private void AlbumItemsControl_ManipulationDelta(object sender, System.Windows.Input.ManipulationDeltaEventArgs e)
        {
            if (AlbumItemsControl != null && AlbumScrollViewer != null)
            {
                if (AlbumScrollViewer.VerticalOffset + AlbumScrollViewer.ViewportHeight >= AlbumScrollViewer.ExtentHeight - 100)
                {
                    albumOffset += ResultsPerPage;
                    PerformSearch(searchBox.Text);  
                }
            }
        }

        private void SongsItemsControl_ManipulationDelta(object sender, System.Windows.Input.ManipulationDeltaEventArgs e)
        {
            if (SongItemsControl != null && SongScrollViewer != null)
            {
                if (SongScrollViewer.VerticalOffset + SongScrollViewer.ViewportHeight >= SongScrollViewer.ExtentHeight - 100)
                {
                    trackOffset += ResultsPerPage;
                    PerformSearch(searchBox.Text); 
                }
            }
        }

        private void Album_Tapped(object sender, System.Windows.Input.GestureEventArgs e)
        {
            var stackPanel = sender as StackPanel;
            if (stackPanel != null)
            {
                string albumName = (stackPanel.DataContext as AlbumItem)?.Name;
                string albumId = (stackPanel.DataContext as AlbumItem)?.AlbumID;

                if (!string.IsNullOrEmpty(albumName))
                {
                    Debug.WriteLine($"Navigating to AlbumPage with albumName: {albumName}");
                    NavigationService.Navigate(new Uri($"/ViewAlbum.xaml?mediaType=album&mediaId={Uri.EscapeDataString(albumId)}", UriKind.Relative));
                }
            }
        }

        private void Song_Tapped(object sender, System.Windows.Input.GestureEventArgs e)
        {
            var stackPanel = sender as StackPanel;
            if (stackPanel != null)
            {
                string songId = (stackPanel.DataContext as SongItem)?.SongId;
                if (!string.IsNullOrEmpty(songId))
                {
                    Debug.WriteLine($"Navigating to SongPage with songId: {songId}");
                    NavigationService.Navigate(new Uri($"/SongPage.xaml?songId={Uri.EscapeDataString(songId)}", UriKind.Relative));
                }
            }
        }

        private void PlaylistTapped(object sender, System.Windows.Input.GestureEventArgs e)
        {
       
        }

        private void Pivot_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}