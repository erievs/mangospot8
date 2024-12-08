using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using static MangoSpot8.SpotifyModal;
using Windows.UI.Popups;
using Newtonsoft.Json.Linq;
using System.Windows;
using System.Diagnostics;

namespace MangoSpot8
{
    public static class TokenManager
    {
        public static async Task ProcessAccessCodeAsync(string accessCode)
        {
            System.Diagnostics.Debug.WriteLine($"Extracted Access Code: {accessCode}");
            string jsonResponse = await GetAccessTokenAsync(accessCode);

            System.Diagnostics.Debug.WriteLine($"JSON Response: {jsonResponse}");

            if (!string.IsNullOrEmpty(jsonResponse))
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("Parsing JSON...");
                    var tokenData = JObject.Parse(jsonResponse);

                    string accessToken = tokenData["access_token"]?.ToString();
                    string refreshToken = tokenData["refresh_token"]?.ToString();
                    
                    System.Diagnostics.Debug.WriteLine($"Extracted Refresh Token: {refreshToken}");

                    if (!string.IsNullOrEmpty(accessToken) && !string.IsNullOrEmpty(refreshToken))
                    {
                        Settings.AccessToken = accessToken;
                        Settings.RefreshToken = refreshToken;

                        bool isValid = await ValidateAccessCodeAsync(accessToken);

                        if (isValid)
                        {
                            Utils.ShowMessage("Access Token has been set successfully.");
                            Utils.NavigateToMainPage();
                        }
                        else
                        {
                            Utils.ShowMessage("Access Token is invalid. Please try again.");
                        }
                    }
                    else
                    {
                        Utils.ShowMessage("Failed to retrieve Access Token. Please try again. It may have expired!");
                    }
                }
                catch (JsonException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"JSON Parsing Error: {ex.Message}");
                    Utils.ShowMessage("Error parsing the token response.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Unexpected error: {ex.Message}");
                    Utils.ShowMessage("An unexpected error occurred.");
                }
            }
            else
            {
                Utils.ShowMessage("Failed to retrieve Access Token. Please try again.");
            }
        }

        public static async Task<string> GetAccessTokenAsync(string authorizationCode)
        {
            using (var client = new HttpClient())
            {
                string clientId = Settings.ClientID;
                string clientSecret = Settings.ClientSecret;
                string redirectUri = "http://localhost:3000/callback";

                var tokenRequestBody = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "grant_type", "authorization_code" },
                    { "code", authorizationCode },
                    { "redirect_uri", redirectUri },
                    { "client_id", clientId },
                    { "client_secret", clientSecret }
                });

                var bodyString = await tokenRequestBody.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Token Request Body: {bodyString}");

                try
                {
                    var response = await client.PostAsync("https://accounts.spotify.com/api/token", tokenRequestBody);

                    System.Diagnostics.Debug.WriteLine($"Response Status Code: {response.StatusCode}");

                    var responseContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"Response Content: {responseContent}");

                    if (!response.IsSuccessStatusCode)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error Response: {responseContent}");
                        return null;
                    }

                    return responseContent;
                }
                catch (HttpRequestException e)
                {
                    System.Diagnostics.Debug.WriteLine($"Request error: {e.Message}");
                    return null;
                }
                catch (JsonException e)
                {
                    System.Diagnostics.Debug.WriteLine($"JSON error: {e.Message}");
                    return null;
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine($"Unexpected error: {e.Message}");
                    return null;
                }
            }
        }

        public static async Task<bool> ValidateAccessCodeAsync(string accessToken)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, "https://api.spotify.com/v1/me");
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                    System.Diagnostics.Debug.WriteLine("Sending request to Spotify API:");
                    System.Diagnostics.Debug.WriteLine($"URL: {request.RequestUri}");
                    System.Diagnostics.Debug.WriteLine($"Authorization Header: {request.Headers.Authorization}");

                    var response = await httpClient.SendAsync(request);
                    string responseBody = await response.Content.ReadAsStringAsync();

                    System.Diagnostics.Debug.WriteLine($"Response Body: {responseBody}");

                    if (response.StatusCode == HttpStatusCode.Unauthorized || responseBody.Contains("Invalid access token"))
                    {
                        // Clear the access token if it's invalid
                        Settings.AccessToken = string.Empty;
                        System.Diagnostics.Debug.WriteLine("Access token cleared due to Invalid access token error.");

                        // Optionally, attempt to refresh the token if needed
                        string newAccessToken = await RefreshAccessTokenAsync();
                        if (!string.IsNullOrEmpty(newAccessToken))
                        {
                            // Log the new access token
                            System.Diagnostics.Debug.WriteLine($"New access token obtained: {newAccessToken}");

                            // Set the new access token to Settings.AccessToken
                            Settings.AccessToken = newAccessToken;

                            // Log that the new access token has been set
                            System.Diagnostics.Debug.WriteLine($"Settings.AccessToken updated: {Settings.AccessToken}");

                            // Retry the request with the new access token
                            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newAccessToken);
                            response = await httpClient.SendAsync(request);
                        }
                        else
                        {
                            // Handle case where token refresh fails
                            System.Diagnostics.Debug.WriteLine("Token refresh failed. No new access token obtained.");
                        }
                    }

                    System.Diagnostics.Debug.WriteLine($"Response Status: {response.StatusCode}");
                    return response.IsSuccessStatusCode;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error validating access code: {ex.Message}");
                return false;
            }
        }


        public static async Task<string> RefreshAccessTokenAsync()
        {
            if (string.IsNullOrEmpty(Settings.RefreshToken))
            {
                System.Diagnostics.Debug.WriteLine("Refresh token is null or empty.");
                return null;
            }

            using (HttpClient httpClient = new HttpClient())
            {
                HttpRequestMessage tokenRequest = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token");

                FormUrlEncodedContent body = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "refresh_token"),
                    new KeyValuePair<string, string>("refresh_token", Settings.RefreshToken),
                    new KeyValuePair<string, string>("client_id", Settings.ClientID),
                    new KeyValuePair<string, string>("client_secret", Settings.ClientSecret)
                });

                tokenRequest.Content = body;

                try
                {
                    HttpResponseMessage response = await httpClient.SendAsync(tokenRequest);

                    if (response.IsSuccessStatusCode)
                    {

                        string jsonResponse = await response.Content.ReadAsStringAsync();

                        System.Diagnostics.Debug.WriteLine($"Raw JSON Response: {jsonResponse}");

                        JObject json = JObject.Parse(jsonResponse);

                        string accessToken = json["access_token"]?.ToString();
                        string tokenType = json["token_type"]?.ToString();
                        int expiresIn = json["expires_in"]?.ToObject<int>() ?? 0;
                        string scope = json["scope"]?.ToString();

                        System.Diagnostics.Debug.WriteLine($"Access Token: {accessToken}");
                        System.Diagnostics.Debug.WriteLine($"Token Type: {tokenType}");
                        System.Diagnostics.Debug.WriteLine($"Expires In: {expiresIn} seconds");
                        System.Diagnostics.Debug.WriteLine($"Scope: {scope}");

                        if (!string.IsNullOrEmpty(accessToken))
                        {
                            Settings.AccessToken = accessToken;
                            System.Diagnostics.Debug.WriteLine($"Updated Settings.AccessToken: {Settings.AccessToken}");
                            return accessToken;
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("Access token missing or invalid in response.");
                            return null;
                        }
                    }
                    else
                    {
                        string errorContent = await response.Content.ReadAsStringAsync();
                        System.Diagnostics.Debug.WriteLine($"Error refreshing access token: {response.ReasonPhrase}");
                        System.Diagnostics.Debug.WriteLine($"Response Content: {errorContent}");
                        return null;
                    }
                }
                catch (HttpRequestException e)
                {
                    System.Diagnostics.Debug.WriteLine($"Request error: {e.Message}");
                    return null;
                }
                catch (JsonException jsonEx)
                {
                    System.Diagnostics.Debug.WriteLine($"JSON error: {jsonEx.Message}");
                    return null;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Unexpected error: {ex.Message}");
                    return null;
                }
            }
        }


    }

}
