using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.IO;
using ZXing;
using System.Windows.Media.Imaging;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Diagnostics;
using Windows.Storage;
using Windows.Storage.Pickers;
using ZXing.Common;

namespace MangoSpot8
{
    public partial class LoginPage : PhoneApplicationPage
    {

        private LocalWebServer server;

        public LoginPage()
        {
            InitializeComponent();
            LoadSettings();
            DisplayLocalIPAddress();

            if(Settings.IsLoggedIn)
            {
                Utils.NavigateToMainPage();
            }

        }

        private void LoadSettings()
        {
            ClientIDTextBox.Text = Settings.ClientID;
            ClientSecretTextBox.Text = Settings.ClientSecret;
            RedirectUriTextBox.Text = Settings.RedirectUri;
        }

        private void DisplayLocalIPAddress()
        {
            string localIpAddress = Utils.GetLocalIPAddress();
            IpAddressTextBlock.Text = $"Local IP Address: {localIpAddress}:3000";
        }

        private async void StartServerButton_Click(object sender, RoutedEventArgs e)
        {
            if (LocalWebServer.IsRunning == false)
            {
                server = new LocalWebServer(3000);
                await server.Start(3000);
                server.ParametersSet += UpdateUIWithSettings;
                string localIpAddress = Utils.GetLocalIPAddress();
            }
        }

        public void UpdateUIWithSettings()
        {
            string accessCode = Settings.AccessToken;

            accessCode = Utils.ExtractCode(accessCode);

            Settings.AccessToken = accessCode;

            Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    if (!string.IsNullOrEmpty(Settings.ClientID))
                    {
                        ClientIDTextBox.Text = Settings.ClientID;
                    }
                    else
                    {
                        Debug.WriteLine("ClientID is empty or null.");
                    }

                    if (!string.IsNullOrEmpty(Settings.ClientSecret))
                    {
                        ClientSecretTextBox.Text = Settings.ClientSecret;
                    }
                    else
                    {
                        Debug.WriteLine("ClientSecret is empty or null.");
                    }

                    if (!string.IsNullOrEmpty(Settings.RedirectUri))
                    {
                        RedirectUriTextBox.Text = Settings.RedirectUri;
                    }
                    else
                    {
                        Debug.WriteLine("RedirectUri is empty or null.");
                    }

                    if (!string.IsNullOrEmpty(Settings.AccessToken))
                    {
                        AccessTokenTextBox.Text = Settings.AccessToken;
                    }
                    else
                    {
                        Debug.WriteLine("AccessToken is empty or null.");
                    }             
        
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error updating UI: {ex.Message}");
                }
            });
        }

        private void SignInButton_Click(object sender, RoutedEventArgs e)
        {
            Settings.ClientID = ClientIDTextBox.Text;
            Settings.ClientSecret = ClientSecretTextBox.Text;
            Settings.RedirectUri = ClientSecretTextBox.Text;
            AuthenticateUser();
        }

        private void AuthenticateUser()
        {
            string authUrl = GetAuthorizationUrl();
            System.Diagnostics.Debug.WriteLine($"Navigating to: {authUrl}");
            GenerateQRCode(authUrl);
            QRCodeImage.Visibility = Visibility.Visible;
        }

        private string GetAuthorizationUrl()
        {
            string clientId = Settings.ClientID;
            string redirectUri = Uri.EscapeDataString("http://localhost:3000/callback");

            string scope = Uri.EscapeDataString("user-read-private playlist-read-private user-library-read user-library-modify playlist-modify-private playlist-modify-public user-read-recently-played user-modify-playback-state user-read-playback-state");

            string authEndpoint = "https://accounts.spotify.com/authorize";

            return $"{authEndpoint}?client_id={clientId}&redirect_uri={redirectUri}&response_type=code&scope={scope}";
        }

        
        private async void SubmitCodeButton_Click(object sender, RoutedEventArgs e)
        {
            string accessCode = Settings.AccessToken;

            AccessTokenTextBox.Text = accessCode;

            if (!string.IsNullOrEmpty(accessCode))
            {
                await TokenManager.ProcessAccessCodeAsync(accessCode);
            }
        }

        private void GenerateQRCode(string data)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(data))
                {
                    MessageBox.Show("Invalid data for QR Code. Please enter valid data.");
                    return;
                }
                var writer = new BarcodeWriter
                {
                    Format = BarcodeFormat.QR_CODE,
                    Options = new ZXing.Common.EncodingOptions
                    {
                        Height = 250,
                        Width = 250,
                        Margin = 1
                    }
                };
                var bitMatrix = writer.Encode(data);
                var qrCodeImage = new WriteableBitmap(bitMatrix.Width, bitMatrix.Height);
                for (int y = 0; y < bitMatrix.Height; y++)
                {
                    for (int x = 0; x < bitMatrix.Width; x++)
                    {
                        bool isBlack = bitMatrix[x, y];
                        int color = (int)(isBlack ? 0xFF000000 : 0xFFFFFFFF);
                        qrCodeImage.Pixels[x + (y * bitMatrix.Width)] = color;
                    }
                }
                QRCodeImage.Source = qrCodeImage;
                QRCodeImage.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generating QR code: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                MessageBox.Show($"Error generating QR code: {ex.Message}");
            }
        }


    }
}