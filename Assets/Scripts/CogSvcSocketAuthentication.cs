//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license.
//
// Microsoft Cognitive Services (formerly Project Oxford): 
// https://www.microsoft.com/cognitive-services
//
// New Speech Service: 
// https://docs.microsoft.com/en-us/azure/cognitive-services/Speech-Service/
// Old Bing Speech SDK: 
// https://docs.microsoft.com/en-us/azure/cognitive-services/Speech/home
//
// Copyright (c) Microsoft Corporation
// All rights reserved.
//
// MIT License:
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace SpeechRecognitionService
{
    public class CogSvcSocketAuthentication
    {
        public static string AuthenticationUri;
        private string subscriptionKey;
        private string token;
        private Timer accessTokenRenewer;

        //Access token expires every 10 minutes. Renew it every 9 minutes.
        private const int RefreshTokenDuration = 9;

        // Set usebingspeechservice to true in  the client constructor if you want to use the old Bing Speech SDK
        // instead of the new Speech Service (new API is the default).
        public async Task<string> Authenticate(string subscriptionKey, string region, bool usebingspeechservice = false)
        {
            try
            {
                // Important: The Bing Speech service and the new Speech Service DO NOT use the same Uri
                if (!usebingspeechservice)
                {
                    AuthenticationUri = $"https://{region}.api.cognitive.microsoft.com/sts/v1.0";
                }
                else
                {
                    // The region is ignored for the old Bing Speech service
                    AuthenticationUri = "https://api.cognitive.microsoft.com/sts/v1.0";
                }

                this.subscriptionKey = subscriptionKey;
                this.token = await FetchToken(AuthenticationUri, subscriptionKey);

                // Renew the token based on a fixed interval using a Timer
                accessTokenRenewer = new Timer(new TimerCallback(OnTokenExpiredCallback),
                                               this,
                                               TimeSpan.FromMinutes(RefreshTokenDuration),
                                               TimeSpan.FromMilliseconds(-1));
                return this.token;
            }
            catch (Exception ex)
            {
                Debug.Log($"An exception occurred during authentication:{Environment.NewLine}{ex.Message}");
                return null;
            }
        }

        public string GetAccessToken()
        {
            return this.token;
        }

        private async void RenewAccessToken()
        {
            this.token = await FetchToken(AuthenticationUri, this.subscriptionKey);
            Debug.Log($"Renewed authentication token: {this.token}");
        }

        private void OnTokenExpiredCallback(object stateInfo)
        {
            try
            {
                RenewAccessToken();
            }
            catch (Exception ex)
            {
                Debug.Log($"Failed renewing access token. Details: {ex.Message}");
            }
            finally
            {
                try
                {
                    accessTokenRenewer.Change(TimeSpan.FromMinutes(RefreshTokenDuration), TimeSpan.FromMilliseconds(-1));
                }
                catch (Exception ex)
                {
                    Debug.Log($"Failed to reschedule the timer to renew access token. Details: {ex.Message}");
                }
            }
        }

        private async Task<string> FetchToken(string fetchUri, string subscriptionKey)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
                UriBuilder uriBuilder = new UriBuilder(fetchUri);
                uriBuilder.Path += "/issueToken";

                // Using ConfigureAwait(false) to configures the awaiter used to await this Task to prevent 
                // the attempt to marshal the continuation back to the original context captured.
                var result = await client.PostAsync(uriBuilder.Uri.AbsoluteUri, null).ConfigureAwait(false);
                Debug.Log("Token Uri: " + uriBuilder.Uri.AbsoluteUri);
                return await result.Content.ReadAsStringAsync();
            }
        }
    }
}
