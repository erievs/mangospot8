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
using System.Diagnostics;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Windows.Media.Imaging;
using static MangoSpot8.SpotifyModal;
using System.Windows.Threading;
using Microsoft.Phone.BackgroundAudio;
using System.IO.IsolatedStorage;
using System.IO;
using BackgroundAudioAgent;
using static BackgroundAudioAgent.AudioPlayer;

namespace MangoSpot8
{
    public partial class SongPage : PhoneApplicationPage
    {

        private bool isPlaying = false;
        private string songId;

        private DispatcherTimer _progressTimer;

        private string ThubnailURL;
        private new string Title;
        private string Aurthor;

        private AudioPlayer _audioPlayer;

        private AudioTrack currentTrack;

        public SongPage()
        {
            InitializeComponent();
            SetupApplicationBar();

            AudioPlayer audioPlayer = new AudioPlayer();

            _audioPlayer = audioPlayer;

            _progressTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _progressTimer.Tick += UpdateProgressBar;
        }

        public async Task<string> SearchTrackAsync(string trackName, string artistName)
        {
            string innerTubeUrl = $"https://www.youtube.com/youtubei/v1/search?key={Settings.InnerTubeAPIKey}";

            try
            {
                using (var httpClient = new HttpClient())
                {
                    var query = $"{trackName} by {artistName} category: music";

                    var searchData = new SearchData
                    {
                        Query = query,
                        Context = new Context
                        {
                            Client = new Client
                            {
                                Hl = "en",
                                Gl = "US",
                                ClientName = "WEB",
                                ClientVersion = "2.20211122.09.00"
                            }
                        },
                        Params = "EgZ2A2h0AQ=="
                    };

                    var searchContent = new StringContent(SerializeSearchData(searchData), Encoding.UTF8, "application/json");

                    var searchResponse = await httpClient.PostAsync(innerTubeUrl, searchContent);

                    if (searchResponse.IsSuccessStatusCode)
                    {
                        var searchJson = await searchResponse.Content.ReadAsStringAsync();
                        System.Diagnostics.Debug.WriteLine($"Search JSON Response: {searchJson}");

                        return ExtractVideoIdWithFallback(searchJson);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to search for track: {searchResponse.StatusCode}");
                        return null;
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Network error occurred: {ex.Message}");
                return null;
            }
            catch (TaskCanceledException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Request timeout: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"An error occurred: {ex.Message}");
                return null;
            }
        }

        public string ExtractVideoIdWithFallback(string json)
        {
            JObject jsonObject = JObject.Parse(json);

            var videoRenderers = jsonObject
                .SelectTokens("$..videoRenderer")
                .ToList(); 

            if (videoRenderers != null && videoRenderers.Count > 0)
            {
                foreach (var videoRenderer in videoRenderers)
                {
                    var videoId = videoRenderer["videoId"]?.ToString();
                    var titleRuns = videoRenderer["title"]?["runs"] as JArray;

                    if (!string.IsNullOrEmpty(videoId) && titleRuns != null && titleRuns.Count > 0)
                    {
                        string title = titleRuns[0]["text"].ToString();

                        if (title.IndexOf("Music Video", StringComparison.OrdinalIgnoreCase) == -1)
                        {
                            System.Diagnostics.Debug.WriteLine($"Found video ID: {videoId} for title: {title}");
                            return videoId;
                        }
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine("No valid video ID found in the JSON response.");
            return null;
        }

        private string SerializeSearchData(SearchData searchData)
        {
            var jsonObject = new JObject
            {
                ["query"] = searchData.Query,
                ["context"] = new JObject
                {
                    ["client"] = new JObject
                    {
                        ["hl"] = searchData.Context.Client.Hl,
                        ["gl"] = searchData.Context.Client.Gl,
                        ["clientName"] = searchData.Context.Client.ClientName,
                        ["clientVersion"] = searchData.Context.Client.ClientVersion
                    }
                },
                ["params"] = searchData.Params
            };

            return jsonObject.ToString();
        }


        public async Task FetchSongMetadataAsync(string songId)
        {
            try
            {
                string apiUrl = $"https://api.spotify.com/v1/tracks/{songId}";  

                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + Settings.AccessToken); 

                    var response = await httpClient.GetAsync(apiUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseJson = await response.Content.ReadAsStringAsync();

                        try
                        {
                            JObject jsonObject = JObject.Parse(responseJson);

                            string songTitle = jsonObject.SelectToken("name")?.ToString();
                            string songAuthor = jsonObject.SelectToken("artists[0].name")?.ToString();
                            string albumImageUrl = jsonObject.SelectToken("album.images[0].url")?.ToString();

                            if (string.IsNullOrEmpty(songTitle) || string.IsNullOrEmpty(songAuthor) || string.IsNullOrEmpty(albumImageUrl))
                            {
                                System.Diagnostics.Debug.WriteLine("One or more metadata fields are missing.");
                                return;
                            }

                            ThubnailURL = albumImageUrl;
                            Title = songTitle;
                            Aurthor = songAuthor;

                            Dispatcher.BeginInvoke(() =>
                            {
                                SongTitle.Text = songTitle;  
                                SongAurthor.Text = songAuthor;
                                BitmapImage bitmapImage = new BitmapImage(new Uri(albumImageUrl, UriKind.Absolute));
                                AlbumImage.Source = bitmapImage;
                            });

                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error extracting metadata: {ex.Message}");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to fetch song metadata: {response.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"An error occurred while fetching song metadata: {ex.Message}");
            }
            finally
            {

            }

        }

        public async Task<string> FetchAudioUrlAsync(string videoId)
        {
            try
            {
                string playerUrl = "https://www.youtube.com/youtubei/v1/player?key=" + Settings.InnerTubeAPIKey;

                using (var httpClient = new HttpClient())
                {
                    var playerData = new
                    {
                        videoId = videoId,
                        context = new
                        {
                            client = new
                            {
                                hl = "en",
                                gl = "US",
                                clientName = "IOS",
                                clientVersion = "19.29.1",
                                deviceMake = "Apple",
                                deviceModel = "iPhone",
                                osName = "iOS",
                                osVersion = "17.5.1.21F90",
                                userAgent = "com.google.ios.youtube/19.29.1 (iPhone16,2; U; CPU iOS 17_5_1 like Mac OS X;)"
                            }
                        }
                    };

                    var playerContent = new StringContent(JsonConvert.SerializeObject(playerData), Encoding.UTF8, "application/json");
                    var playerResponse = await httpClient.PostAsync(playerUrl, playerContent);

                    if (playerResponse.IsSuccessStatusCode)
                    {
                        var playerJson = await playerResponse.Content.ReadAsStringAsync();
                        return ExtractAudioUrl(playerJson);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to fetch audio URL: {playerResponse.StatusCode}");
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"An error occurred while fetching audio URL: {ex.Message}");
                return null;
            }
        }

        private string ExtractAudioUrl(string json)
        {
            System.Diagnostics.Debug.WriteLine("Received JSON: " + json);

            try
            {
                JObject jsonObject = JObject.Parse(json);

                JArray adaptiveFormatsArray = (JArray)jsonObject.SelectToken("streamingData.adaptiveFormats");

                if (adaptiveFormatsArray != null)
                {

                    int primaryItag = Settings.LowQualityAudio == true ? 139 : 140;
                    int secondaryItag = 140;

                    foreach (JObject formatObject in adaptiveFormatsArray)
                    {
                        int itag = (int)formatObject.SelectToken("itag");

                        if (itag == primaryItag || itag == secondaryItag)
                        {
                            var url = formatObject.SelectToken("url");

                            if (url != null)
                            {
                                return url.ToString();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error parsing JSON: " + ex.Message);
            }

            return null;
        }

        private void SetupApplicationBar()
        {
            ApplicationBar = new ApplicationBar
            {
                BackgroundColor = Color.FromArgb(0xFF, 0xFA, 0x89, 0x2D),
                ForegroundColor = Colors.White
            };

            var playPauseButton = new ApplicationBarIconButton(new Uri("/Assets/PlayButton.png", UriKind.Relative))
            {
                Text = "Play"
            };

            playPauseButton.Click += PlayPauseButton_Click;

            ApplicationBar.Buttons.Add(playPauseButton);

        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (NavigationContext.QueryString.ContainsKey("songId"))
            {
                songId = NavigationContext.QueryString["songId"];
                Debug.WriteLine("SongId retrieved: " + songId);
                await FetchSongMetadataAsync(songId);

            
                StartTrackHere(Title, Aurthor, ThubnailURL);
                System.Diagnostics.Debug.WriteLine($"Track assigned: {BackgroundAudioPlayer.Instance.Track?.Title}");
            }
            else
            {
                Debug.WriteLine("No songId found in query string.");
            }
        }

        private void UpdateProgressBar(object sender, EventArgs e)
        {
           if (BackgroundAudioPlayer.Instance.Track != null)
            {
                TimeSpan currentPosition = BackgroundAudioPlayer.Instance.Position;
                TimeSpan trackDuration = BackgroundAudioPlayer.Instance.Track.Duration;
                
                if (trackDuration.TotalSeconds > 0 && !double.IsNaN(currentPosition.TotalSeconds) && !double.IsInfinity(currentPosition.TotalSeconds))
                {
                    double progressPercentage = (currentPosition.TotalSeconds / trackDuration.TotalSeconds) * 100;

                    progressPercentage = Math.Max(0, Math.Min(100, progressPercentage));

                    SongProgressBar.Value = progressPercentage;
                    CurrentTime.Text = currentPosition.ToString(@"mm\:ss");
                    EndTime.Text = trackDuration.ToString(@"mm\:ss");
                }
                else
                {
                    SongProgressBar.Value = 0;
                    CurrentTime.Text = "00:00";
                    EndTime.Text = "00:00";
                }
            } 
        }

        private void SaveTrackMetadataToFile(string trackName, string artistName, string audioUrl)
        {
            string jsonFilePath = "tracks.json";

            using (var isoStore = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (isoStore.FileExists(jsonFilePath))
                {
                    isoStore.DeleteFile(jsonFilePath);
                }

                var trackMetadata = new List<TrackMetadata>
                {
                    new TrackMetadata
                    {
                        SongID = songId,
                        Url = audioUrl,
                        Title = trackName,
                        Artist = artistName,
                        ThumbnailUrl = ThubnailURL,  
                        Album = "Album Name"
                    }
                };

                using (var stream = new IsolatedStorageFileStream(jsonFilePath, FileMode.Create, isoStore))
                using (var writer = new StreamWriter(stream))
                {
                    string json = Newtonsoft.Json.JsonConvert.SerializeObject(trackMetadata);
                    writer.Write(json);
                }
            }
        }

        public async void StartTrackHere(string trackName, string artistName, string thumbnailUrl = null)
        {
            try
            {
                string videoId = await SearchTrackAsync(trackName, artistName);

                string audioUrl = await FetchAudioUrlAsync(videoId);

                if (string.IsNullOrEmpty(audioUrl))
                {
                    System.Diagnostics.Debug.WriteLine("Invalid or empty audio URL.");
                    return;
                }

                SaveTrackMetadataToFile(trackName, artistName, audioUrl);

                _audioPlayer.LoadAndPlayTrackFromMain(BackgroundAudioPlayer.Instance);

                System.Diagnostics.Debug.WriteLine($"Audio URL: {audioUrl}");

                UpdatePlayPauseButton("Pause");
                _progressTimer.Start();
                System.Diagnostics.Debug.WriteLine("Playback started successfully.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during StartTrackHere: {ex.Message}");
            }
        }

        private void OnBackgroundAudioPlayerError(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("An error occurred in BackgroundAudioPlayer.");
        }

        private void PauseTrack()
        {
            isPlaying = false;
            UpdatePlayPauseButton("Play");
            BackgroundAudioPlayer.Instance.Pause();
            _progressTimer.Stop();
        }

        private void PlayTrack()
        {
            isPlaying = true;
            UpdatePlayPauseButton("Pause");
            BackgroundAudioPlayer.Instance.Play();
            _progressTimer.Start();
        }


        private void UpdatePlayPauseButton(string text)
        {
            var playPauseButton = ApplicationBar.Buttons[0] as ApplicationBarIconButton;
            if (playPauseButton != null)
            {
                playPauseButton.Text = text;
                playPauseButton.IconUri = new Uri(text == "Pause" ? "/Assets/PauseButton.png" : "/Assets/PlayButton.png", UriKind.Relative);
            }
        }

        private void PlayPauseButton_Click(object sender, EventArgs e)
        {
            var button = sender as ApplicationBarIconButton;

            System.Diagnostics.Debug.WriteLine("Track Title: " + Title ?? "Title is null");
            System.Diagnostics.Debug.WriteLine("Track Author: " + Aurthor ?? "Author is null");

            if (isPlaying)
            {
                PauseTrack();
            }
            else
            {
                PlayTrack();
            }
        }


    }
}