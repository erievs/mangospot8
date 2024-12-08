using System.IO.IsolatedStorage;

namespace MangoSpot8
{
    public static class Settings
    {

        private static readonly IsolatedStorageSettings localSettings = IsolatedStorageSettings.ApplicationSettings;

        public static string ClientID { get; set; } = "67b353570c814eed91bd60678460cac1";
        public static string ClientSecret { get; set; } = "1ba00551ebf042cc9218ba2a02c48070";
        public static string RedirectUri { get; set; } = "http://localhost:3000/callback";

        public const string Version = "Version: Beta 1.0.0";

        private static string accessToken;
        private static bool shuffleTracks;
        private static string refreshToken;
        private static bool preventSleep;
        private static bool? lowQualityAudio;

        public static string InnerTubeAPIKey = "AIzaSyAO_FJ2SlqU8Q4STEHLGCilw_Y9_11qcW8";

        public static bool PreventSleep
        {
            get
            {
                if (localSettings.Contains("PreventSleep"))
                {
                    return (bool)localSettings["PreventSleep"];
                }
                return preventSleep;
            }
            set
            {
                preventSleep = value;
                localSettings["PreventSleep"] = value;
                localSettings.Save();
            }
        }

        public static string AccessToken
        {
            get
            {
                if (accessToken == null && localSettings.Contains("AccessToken"))
                {
                    accessToken = localSettings["AccessToken"] as string;
                }
                return accessToken;
            }
            set
            {
                accessToken = value;
                localSettings["AccessToken"] = value;
                localSettings.Save();
            }
        }

        public static bool? LowQualityAudio
        {
            get
            {

                if (lowQualityAudio == null && localSettings.Contains("lowQualityAudio"))
                {

                    lowQualityAudio = localSettings["lowQualityAudio"] as bool?;
                }
                return lowQualityAudio;
            }
            set
            {

                lowQualityAudio = value;
                localSettings["lowQualityAudio"] = value;
                localSettings.Save();
            }
        }

        public static string RefreshToken
        {
            get
            {
                if (refreshToken == null && localSettings.Contains("RefreshToken"))
                {
                    refreshToken = localSettings["RefreshToken"] as string;
                }
                return refreshToken;
            }
            set
            {
                refreshToken = value;
                localSettings["RefreshToken"] = value;
                localSettings.Save();
            }
        }

        public static bool IsLoggedIn => !string.IsNullOrEmpty(AccessToken);

        public static void ClearAccessToken()
        {
            if (localSettings.Contains("AccessToken"))
            {
                localSettings.Remove("AccessToken");
                localSettings.Save();
            }
            accessToken = null;
        }

        public static void ClearRefreshToken()
        {
            if (localSettings.Contains("RefreshToken"))
            {
                localSettings.Remove("RefreshToken");
                localSettings.Save();
            }
            refreshToken = null;
        }

        public static void ClearTokens()
        {
            ClearAccessToken();
            ClearRefreshToken();
        }
    }
}