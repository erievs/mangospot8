using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json.Linq;
using Microsoft.Phone.Controls;
using static MangoSpot8.SpotifyModal;
using System.Windows.Media;
using System.Diagnostics;
using System.Linq;
using System.Windows.Media.Imaging;
using System.Collections.ObjectModel;

namespace MangoSpot8
{
    public partial class PlaylistPage : PhoneApplicationPage
    {

        private string accessToken = Settings.AccessToken;  

        private ObservableCollection<PlaylistItem> playlistItems = new ObservableCollection<PlaylistItem>();

        private int currentPage = 1; 

        public PlaylistPage()
        {
            InitializeComponent();
            PlaylistItemsControl.ItemsSource = playlistItems;
            LoadPlaylists();
            FetchLikedSongs();
        }

        private void PlaylistItemsControl_ManipulationDelta(object sender, System.Windows.Input.ManipulationDeltaEventArgs e)
        {
            if (PlaylistScrollViewer != null)
            {
                Debug.WriteLine("VerticalOffset: " + PlaylistScrollViewer.VerticalOffset);
                Debug.WriteLine("ViewportHeight: " + PlaylistScrollViewer.ViewportHeight);
                Debug.WriteLine("ExtentHeight: " + PlaylistScrollViewer.ExtentHeight);

                if (PlaylistScrollViewer.VerticalOffset + PlaylistScrollViewer.ViewportHeight >= PlaylistScrollViewer.ExtentHeight - 200)
                {
                    Debug.WriteLine("Near the bottom, loading more playlists.");
                    LoadMorePlaylists();
                }
                else
                {
                    Debug.WriteLine("Not near the bottom.");
                }
            }
            else
            {
                Debug.WriteLine("ScrollViewer not found.");
            }
        }


        private static T FindChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;

            T child = null;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var visualChild = VisualTreeHelper.GetChild(parent, i);
                child = visualChild as T;
                if (child == null)
                {
                    child = FindChild<T>(visualChild);
                }

                if (child != null) break;
            }

            return child;
        }

        private async void LoadPlaylists()
        {
            await FetchPlaylists();
        }

        private async Task FetchLikedSongs()
        {
            try
            {
                using (var client = new HttpClient())
                {

                    string url = "https://api.spotify.com/v1/me/tracks?limit=1";
                    client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);

                    HttpResponseMessage response = await client.GetAsync(url);

                    if (!response.IsSuccessStatusCode)
                    {
                        string errorResponse = await response.Content.ReadAsStringAsync();
                        Debug.WriteLine("Error Response: " + errorResponse);
                        return;
                    }

                    string responseBody = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine("Full JSON Response for Liked Songs:");
                    Debug.WriteLine(responseBody);

                    JObject jsonResponse = JObject.Parse(responseBody);

                    var likedSongsData = jsonResponse.SelectToken("items");

                    var totalLikedSongs = jsonResponse.SelectToken("total")?.ToString();

                    Debug.WriteLine("Liked Songs Count: " + (likedSongsData != null ? likedSongsData.Count() : 0));

                    if (likedSongsData == null || !likedSongsData.HasValues)
                    {
                        Debug.WriteLine("No liked songs found.");
                        return;
                    }

                    var likedSongsItem = new PlaylistItem
                    {
                        Name = "Liked Songs",
                        TrackCount = totalLikedSongs + " tracks",
                        Author = "by You",
                        PlaylistID = "liked_songs",
                        ImageUrl = "default_image_url"
                    };

                    playlistItems.Insert(0, likedSongsItem);

                    Debug.WriteLine("Added Liked Songs to the list.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error fetching liked songs: " + ex.Message);
            }
        }

        private async Task FetchPlaylists()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    string url = $"https://api.spotify.com/v1/me/playlists?limit=50&offset={currentPage * 50}";
                    client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);

                    HttpResponseMessage response = await client.GetAsync(url);

                    if (!response.IsSuccessStatusCode)
                    {
                        string errorResponse = await response.Content.ReadAsStringAsync();
                        Debug.WriteLine("Error Response: " + errorResponse);
                        MessageBox.Show($"Error fetching playlists: {response.StatusCode} - {response.ReasonPhrase}");
                        return;
                    }

                    string responseBody = await response.Content.ReadAsStringAsync();

                    Debug.WriteLine("Raw JSON Response:");
                    Debug.WriteLine(responseBody);

                    JObject jsonResponse = JObject.Parse(responseBody);

                    var playlistsData = jsonResponse.SelectToken("items");

                    if (playlistsData == null || !playlistsData.HasValues)
                    {
                        MessageBox.Show("No playlists found.");
                        return;
                    }

                    Debug.WriteLine("Logging each playlist item:");
                    Debug.WriteLine(playlistsData.ToString());

                    int index = 1;
                    currentPage++;

                    foreach (var item in playlistsData)
                    {
                        var name = item.SelectToken("name")?.ToString();
                        var trackCount = item.SelectToken("tracks.total")?.ToString() ?? "0";
                        var author = item.SelectToken("owner.display_name")?.ToString();
                        var playlistID = item.SelectToken("id")?.ToString();
                        var imageUrl = item.SelectToken("images[0].url")?.ToString();

                        if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(playlistID) && !string.IsNullOrEmpty(author))
                        {
                            var existingPlaylist = playlistItems.FirstOrDefault(p => p.PlaylistID == playlistID);
                            if (existingPlaylist != null)
                            {
                                Debug.WriteLine($"Skipping duplicate playlist with PlaylistID: {playlistID}");
                                continue; 
                            }

                            var playlistItem = new PlaylistItem
                            {
                                Name = name,
                                TrackCount = trackCount + " tracks",
                                Author = "by " + author,
                                PlaylistID = playlistID,
                                ImageUrl = imageUrl
                            };

                            Debug.WriteLine($"Playlist {index}: Name = {playlistItem.Name}, TrackCount = {playlistItem.TrackCount}, Author = {playlistItem.Author}, PlaylistID = {playlistItem.PlaylistID}, ImageUrl = {playlistItem.ImageUrl}");
                            
                            playlistItems.Add(playlistItem);

                            index++;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("General Error: " + ex.Message);
                MessageBox.Show($"Error loading playlists: {ex.Message}");
            }
        }

        private void AddPlaylistToControl(PlaylistItem playlist, ItemsControl itemsControl)
        {
            playlistItems.Add(playlist);
        }
       
        private string CleanText(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            input = input.Replace("\"", "").Trim(); 
            input = new string(input.Where(c => !char.IsControl(c) && !char.IsWhiteSpace(c)).ToArray()); 
            return input;
        }


        private async void LoadMorePlaylists()
        {
            await FetchPlaylists(); 
        }

        private void PlayList_Tapped(object sender, System.Windows.Input.GestureEventArgs e)
        {
            StackPanel stackPanel = sender as StackPanel;
            if (stackPanel != null)
            {
                PlaylistItem playlistItem = stackPanel.DataContext as PlaylistItem;
                if (playlistItem != null)
                {
                    Debug.WriteLine("Playlist tapped: PlaylistID = " + playlistItem.PlaylistID);

                    string mediaType = playlistItem.PlaylistID == "liked_songs" ? "liked_songs" : "playlist";  
                    string mediaId = playlistItem.PlaylistID;

                    string navigationUri = string.Format("/ViewAlbum.xaml?mediaType={0}&mediaId={1}", mediaType, mediaId);
                    NavigationService.Navigate(new Uri(navigationUri, UriKind.Relative));
                }
            }
        }

    }
}
