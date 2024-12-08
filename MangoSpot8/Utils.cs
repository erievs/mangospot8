using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Windows.Networking.Connectivity;

namespace MangoSpot8
{
    class Utils
    {

        public static void ShowMessage(string message)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                MessageBox.Show(message);
            });
        }

        public static void NavigateToMainPage()
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                var frame = Application.Current.RootVisual as Frame;
                if (frame != null)
                {
                    frame.Navigate(new Uri("/MainPage.xaml", UriKind.Relative));
                }
            });
        }

        public static string ExtractCode(string content)
        {
            string code = string.Empty;

            if (!string.IsNullOrEmpty(content) && content.Contains("callback?code="))
            {
                try
                {
                    var parts = content.Split(new[] { "callback?code=" }, StringSplitOptions.None);
                    if (parts.Length > 1)
                    {
                        code = parts[1].Split('&')[0];
                        Debug.WriteLine($"callback?code=: {content}");
                        Debug.WriteLine($"Extracted Code: {code}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error extracting code: {ex.Message}");
                }
            }
            else
            {
                Debug.WriteLine("Invalid content or missing callback?code=");
            }

            return code;
        }


        public static string GetLocalIPAddress()
        {
            var hostNames = NetworkInformation.GetHostNames();

            foreach (var hostName in hostNames)
            {
                if (hostName.IPInformation != null && hostName.Type == Windows.Networking.HostNameType.Ipv4)
                {
                    string ipAddress = hostName.CanonicalName;

                    string[] ipParts = ipAddress.Split('.');

                    if (ipParts.Length == 4)
                    {

                        return ipAddress;
                    }
                }
            }

            return "No valid IP Address found.";
        }


    }
}
