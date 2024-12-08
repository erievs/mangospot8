using System;
using System.Diagnostics;
using System.Net.Http;
using System.Windows.Controls;
using System.Windows.Navigation;
using Newtonsoft.Json.Linq;
using static MangoSpot8.SpotifyModal;
using Microsoft.Phone.Controls;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;

namespace MangoSpot8
{
    public partial class ViewAlbum : PhoneApplicationPage
    {

        private const int TracksPerPage = 50;
        private int currentOffset = 0;

        private ObservableCollection<SongItem> SongItems = new ObservableCollection<SongItem>();

        public string accessToken = Settings.AccessToken;

        private bool isLoading = false;

        public ViewAlbum()
        {
            InitializeComponent();
            SongItemsControl.ItemsSource = SongItems;
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

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            Debug.WriteLine("Navigated to ViewAlbum page. Starting data query...");

            string mediaType, mediaId;
            if (NavigationContext.QueryString.TryGetValue("mediaType", out mediaType) &&
                NavigationContext.QueryString.TryGetValue("mediaId", out mediaId))
            {
                Debug.WriteLine($"Media Type: {mediaType}, Media ID: {mediaId}");

                if (mediaType == "album")
                {
                    FetchAlbumMetadata(mediaId);
                    FetchAlbumSongs(mediaId, currentOffset);
                }
                else if (mediaType == "playlist")
                {
                    FetchPlaylistMetadata(mediaId);
                    FetchPlaylistSongs(mediaId, currentOffset);
                }
                else if (mediaType == "liked_songs")
                {
                    FetchLikedSongsMetadata();
                    FetchLikedSongs(currentOffset);
                }
            }
            else
            {
                Debug.WriteLine("Missing mediaType or mediaId in navigation.");
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            SongItems.Clear();
        }
        
        private void SongsItemsControl_ManipulationDelta(object sender, System.Windows.Input.ManipulationDeltaEventArgs e)
        {
            if (SongScrollViewer != null)
            {
                Debug.WriteLine("VerticalOffset: " + SongScrollViewer.VerticalOffset);
                Debug.WriteLine("ViewportHeight: " + SongScrollViewer.ViewportHeight);
                Debug.WriteLine("ExtentHeight: " + SongScrollViewer.ExtentHeight);

                if (SongScrollViewer.VerticalOffset + SongScrollViewer.ViewportHeight >= SongScrollViewer.ExtentHeight - 200)
                {
                    Debug.WriteLine("Near the bottom, loading more songs.");

                    if (!isLoading)
                    {
                        string mediaType, mediaId;
                        if (NavigationContext.QueryString.TryGetValue("mediaType", out mediaType) &&
                            NavigationContext.QueryString.TryGetValue("mediaId", out mediaId))
                        {
                            if (mediaType == "album")
                            {
                                FetchAlbumSongs(mediaId, currentOffset);
                            }
                            else if (mediaType == "playlist")
                            {
                                FetchPlaylistSongs(mediaId, currentOffset);
                            }
                        }
                    }
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

        private async void FetchAlbumMetadata(string albumId)
        {
            try
            {
                Debug.WriteLine($"Fetching metadata for Album ID: {albumId}");
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);

                    var response = await client.GetAsync($"https://api.spotify.com/v1/albums/{albumId}");
                    if (response.IsSuccessStatusCode)
                    {
                        var responseBody = await response.Content.ReadAsStringAsync();
                        var albumData = JObject.Parse(responseBody);

                        var albumMetadata = new MediaMetadata
                        {
                            ImageUrl = albumData.SelectToken("images[0].url")?.ToString(),
                            Author = albumData.SelectToken("artists[0].name")?.ToString(),
                            Title = albumData.SelectToken("name")?.ToString()
                        };

                        UpdateMetadataUI(albumMetadata);
                    }
                    else
                    {
                        Debug.WriteLine($"Error fetching album metadata: {response.StatusCode} - {response.ReasonPhrase}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception fetching album metadata: {ex.Message}");
            }
        }

        private async void FetchLikedSongsMetadata()
        {
            try
            {
                Debug.WriteLine("Fetching metadata for Liked Songs");
   
                    var likedSongsMetadata = new MediaMetadata
                    {
                        ImageUrl = "https://misc.scdn.co/liked-songs/liked-songs-300.png",
                        Author = "You",
                        Title = " Liked Songs"
                    };

                    UpdateMetadataUI(likedSongsMetadata);                  
              
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception fetching liked songs metadata: {ex.Message}");
            }
        }

        private async void FetchPlaylistMetadata(string playlistId)
        {
            try
            {
                Debug.WriteLine($"Fetching metadata for Playlist ID: {playlistId}");
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);

                    var response = await client.GetAsync($"https://api.spotify.com/v1/playlists/{playlistId}");
                    if (response.IsSuccessStatusCode)
                    {
                        var responseBody = await response.Content.ReadAsStringAsync();
                        var playlistData = JObject.Parse(responseBody);

                        var playlistMetadata = new MediaMetadata
                        {
                            ImageUrl = playlistData.SelectToken("images[0].url")?.ToString(),
                            Author = playlistData.SelectToken("owner.display_name")?.ToString(),
                            Title = playlistData.SelectToken("name")?.ToString()
                        };

                        UpdateMetadataUI(playlistMetadata);
                    }
                    else
                    {
                        Debug.WriteLine($"Error fetching playlist metadata: {response.StatusCode} - {response.ReasonPhrase}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception fetching playlist metadata: {ex.Message}");
            }
        }

        private async void FetchLikedSongs(int offset)
        {
            try
            {
                if (isLoading) return;
                isLoading = true;

                string url = $"https://api.spotify.com/v1/me/tracks?offset={offset}&limit={TracksPerPage}";
                Debug.WriteLine($"Fetching liked songs, URL: {url}");

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);

                    var response = await client.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        var responseBody = await response.Content.ReadAsStringAsync();
                        var likedSongsData = JObject.Parse(responseBody);

                        var songs = likedSongsData.SelectToken("items")
                        .Select(item => new SongItem
                        {
                            SongName = item.SelectToken("track.name")?.ToString(),
                            Author = item.SelectToken("track.artists[0].name")?.ToString(),
                            Name = item.SelectToken("track.name")?.ToString(),
                            SongId = item.SelectToken("track.id")?.ToString()
                        }).ToList();

                        foreach (var song in songs)
                        {
                            SongItems.Add(song);
                        }

                        if (songs.Count == TracksPerPage)
                        {
                            currentOffset += TracksPerPage;
                        }

                    }
                    else
                    {
                        Debug.WriteLine($"Error fetching liked songs: {response.StatusCode} - {response.ReasonPhrase}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception fetching liked songs: {ex.Message}");
            }
            finally
            {
                isLoading = false;
            }
        }

        private async void FetchAlbumSongs(string albumId, int offset)
        {
            try
            {
                if (isLoading) return;
                isLoading = true;

                string url = $"https://api.spotify.com/v1/albums/{albumId}/tracks?offset={offset}&limit={TracksPerPage}";
                Debug.WriteLine($"Fetching songs for Album ID: {albumId}, URL: {url}");

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);

                    var response = await client.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        var responseBody = await response.Content.ReadAsStringAsync();
                        var albumData = JObject.Parse(responseBody);

                        var songs = albumData.SelectToken("items")
                        .Select(song => new SongItem
                        {
                            SongName = song.SelectToken("name")?.ToString(),
                            Author = song.SelectToken("artists[0].name")?.ToString(),
                            Name = song.SelectToken("name")?.ToString(),
                            SongId = song.SelectToken("id")?.ToString()
                        }).ToList();

                        foreach (var song in songs)
                        {
                            SongItems.Add(song);
                        }

                        if (songs.Count == TracksPerPage)
                        {
                            currentOffset += TracksPerPage;
                        }

                    }
                    else
                    {
                        Debug.WriteLine($"Error fetching album songs: {response.StatusCode} - {response.ReasonPhrase}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception fetching album songs: {ex.Message}");
            }
            finally
            {
                isLoading = false;
            }
        }

        private async void FetchPlaylistSongs(string playlistId, int offset)
        {
            try
            {
                if (isLoading) return;
                isLoading = true;

                string url = $"https://api.spotify.com/v1/playlists/{playlistId}/tracks?offset={offset}&limit={TracksPerPage}";
                Debug.WriteLine($"Fetching songs for Playlist ID: {playlistId}, URL: {url}");

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);

                    var response = await client.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        var responseBody = await response.Content.ReadAsStringAsync();

                        Debug.WriteLine($"JSON Response: {responseBody}");

                        var playlistData = JObject.Parse(responseBody);

                        var songs = playlistData.SelectToken("items")
                            .Select(song => new SongItem
                            {
                                SongName = song.SelectToken("track.name")?.ToString(),
                                Author = song.SelectToken("track.artists[0].name")?.ToString(),
                                Name = song.SelectToken("track.name")?.ToString(),
                                SongId = song.SelectToken("track.id")?.ToString(),
                            }).ToList();

                        foreach (var song in songs)
                        {
                            SongItems.Add(song);
                        }

                        if (songs.Count == TracksPerPage)
                        {
                            currentOffset += TracksPerPage;
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"Error fetching playlist songs: {response.StatusCode} - {response.ReasonPhrase}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception fetching playlist songs: {ex.Message}");
            }
            finally
            {
                isLoading = false;
            }
        }

        private void Song_Clicked(object sender, System.Windows.Input.GestureEventArgs e)
        {
            var stackPanel = sender as StackPanel;
            if (stackPanel != null)
            {
                string songId = stackPanel.Tag as string;
                if (!string.IsNullOrEmpty(songId))
                {
                    Debug.WriteLine($"Navigating to SongPage with songId: {songId}");

                    NavigationService.Navigate(new Uri($"/SongPage.xaml?songId={Uri.EscapeDataString(songId)}", UriKind.Relative));
                }
                else
                {
                    Debug.WriteLine("SongId is null or empty.");
                }
            }
            else
            {
                Debug.WriteLine("Sender is not a StackPanel.");
            }
        }

        private void UpdateMetadataUI(MediaMetadata metadata)
        {
            Debug.WriteLine("Updating UI with metadata...");

            if (!string.IsNullOrEmpty(metadata.ImageUrl))
            {
                AlbumImage.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(metadata.ImageUrl, UriKind.Absolute));
            }

            Aurthor.Text = metadata.Author ?? "Unknown Author";
            Title.Text = metadata.Title ?? "Unknown Title";
        }
    }

}