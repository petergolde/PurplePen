/* Copyright (c) 2006-2008, Peter Golde
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are
 * met:
 *
 * 1. Redistributions of source code must retain the above copyright
 * notice, this list of conditions and the following disclaimer.
 *
 * 2. Redistributions in binary form must reproduce the above copyright
 * notice, this list of conditions and the following disclaimer in the
 * documentation and/or other materials provided with the distribution.
 *
 * 3. Neither the name of Peter Golde, nor "Purple Pen", nor the names
 * of its contributors may be used to endorse or promote products
 * derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF
 * MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING,
 * BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
 * SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
 * WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
 * NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE
 * USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY
 * OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using PurplePen.Livelox.ApiContracts;

namespace PurplePen.Livelox
{
    class LiveloxApiClient : IDisposable
    {
        public OAuth2TokenInformation TokenInformation { get; private set; }
        public bool Aborted { get; private set; }

        private readonly Action<IAbortable> requestCreatedCallback;
        private readonly Action<IAbortable> requestCompletedCallback;
        private readonly TimeSpan timeout;
        private readonly HttpClient httpClient;

        private const string baseUrl = "https://api.livelox.com";
        private const string applicationJson = "application/json";
        private const string clientId = "PurplePen";
        private const string scope = "events.import";

        public LiveloxApiClient(OAuth2TokenInformation tokenInformation, Action<IAbortable> requestCreatedCallback = null, Action<IAbortable> requestCompletedCallback = null, TimeSpan? timeout = null)
        {
            TokenInformation = tokenInformation;
            this.requestCreatedCallback = requestCreatedCallback;
            this.requestCompletedCallback = requestCompletedCallback;
            this.timeout = timeout ?? TimeSpan.FromMinutes(1);
            httpClient = new HttpClient();
        }

        public void Abort()
        {
            Aborted = true;
        }

        // Dispose managed resources.
        public void Dispose()
        {
            httpClient?.Dispose();
        }

        public void AskForUserConsent(Form form, TimeSpan? refreshTokenLifeLength, Action<LiveloxApiCall<User>> callback, Action<string> progressReportCallback)
        {
            // Generates state and PKCE values.
            string state = RandomDataBase64Url(32);
            string codeVerifier = RandomDataBase64Url(32);
            string codeChallenge = Base64UrlEncodeNoPadding(Sha256(codeVerifier));
            const string codeChallengeMethod = "S256";

            // Creates a redirect URI using an available port on the loopback address.
            var redirectUri = $"http://{IPAddress.Loopback}:{GetRandomUnusedPort()}/";

            // Creates an HttpListener to listen for requests on that redirect URI.
            var httpListener = new HttpListener();
            httpListener.Prefixes.Add(redirectUri);
            httpListener.Start();

            progressReportCallback(LiveloxResources.RedirectingToLivelox);

            // Creates the OAuth 2.0 authorization request.
            string authorizationRequest = string.Format("{0}/oauth2/authorize?response_type=code&scope={1}&redirect_uri={2}&client_id={3}&state={4}&code_challenge={5}&code_challenge_method={6}{7}",
                baseUrl,
                Uri.EscapeDataString(scope),
                Uri.EscapeDataString(redirectUri),
                Uri.EscapeDataString(clientId),
                Uri.EscapeDataString(state),
                Uri.EscapeDataString(codeChallenge),
                Uri.EscapeDataString(codeChallengeMethod),
                refreshTokenLifeLength == null ? null : "&refresh_token_life_length=" + Uri.EscapeDataString(((int)refreshTokenLifeLength.Value.TotalSeconds).ToString())
            );

            // Opens request in the browser.
            System.Diagnostics.Process.Start(authorizationRequest);

            progressReportCallback(LiveloxResources.WaitingForLiveloxConsentResponse);

            // Waits for the OAuth authorization response.
            var httpListenerWrapper = new HttpListenerWrapper()
            {
                HttpListener = httpListener
            };
            var asyncCallback = new AsyncCallback(HttpListenerGetContextCallback);

            httpListenerWrapper.Callback = context =>
            {
                try
                {
                    // Brings this app back to the foreground.
                    form.InvokeOnUiThread(form.Activate);

                    // Sends an HTTP response to the browser.
                    var response = context.Response;
                    var responseString = $"<!doctype html><html><head><meta charset=\"UTF-8\"></head><body>{LiveloxResources.PleaseReturnToPurplePen}</body></html>";
                    var buffer = Encoding.UTF8.GetBytes(responseString);
                    response.ContentLength64 = buffer.Length;
                    response.ContentEncoding = Encoding.UTF8;
                    var responseOutput = response.OutputStream;
                    responseOutput.Write(buffer, 0, buffer.Length);
                    responseOutput.Close();
                    httpListener.Stop();

                    progressReportCallback(LiveloxResources.ProcessingLiveloxConsentResponse);

                    // Checks for errors.
                    if (context.Request.QueryString.Get("error") != null)
                    {
                        throw new OAuth2Exception($"OAuth authorization error: {context.Request.QueryString.Get("error")}.");
                    }

                    if (context.Request.QueryString.Get("code") == null
                        || context.Request.QueryString.Get("state") == null)
                    {
                        throw new OAuth2Exception("Malformed authorization response. " + context.Request.QueryString);
                    }

                    // extracts the authorization code
                    var code = context.Request.QueryString.Get("code");
                    var incomingState = context.Request.QueryString.Get("state");

                    // Compares the received state to the expected value, to ensure that
                    // this app made the request which resulted in authorization.
                    if (incomingState != state)
                    {
                        throw new OAuth2Exception($"Received request with invalid state ({incomingState})");
                    }

                    // Starts the code exchange at the Token Endpoint.
                    var parameters = new Dictionary<string, string>()
                    {
                        { "code", code },
                        { "redirect_uri", redirectUri },
                        { "client_id", clientId },
                        { "code_verifier", codeVerifier },
                        { "scope", "" },
                        { "grant_type", "authorization_code" }
                    };

                    // sends the request
                    var tokenRequest = CreatePostFormUrlencodedRequest($"{baseUrl}/oauth2/token", parameters);

                    progressReportCallback(LiveloxResources.CreatingTokens);

                    ReadTokenResponse(tokenRequest, tokenCall =>
                    {
                        if (!tokenCall.Success)
                        {
                            callback(new LiveloxApiCall<User>()
                            {
                                Exception = tokenCall.Exception,
                                Client = this
                            });
                        }


                        progressReportCallback(LiveloxResources.LoadingUserInfo);
                        TokenInformation = tokenCall.Result;

                        GetUserInfo(userInfoCall =>
                        {
                            if (!userInfoCall.Success)
                            {
                                callback(new LiveloxApiCall<User>()
                                {
                                    Exception = userInfoCall.Exception,
                                    Client = this
                                });
                            }

                            var userInfo = userInfoCall.Result;
                            callback(new LiveloxApiCall<User>()
                            {
                                Result = new User()
                                {
                                    PersonId = userInfo.PersonId,
                                    FirstName = userInfo.FirstName,
                                    LastName = userInfo.LastName,
                                    TokenInformation = TokenInformation,
                                },
                                Client = this
                            });
                        });
                    });
                }
                catch (Exception ex)
                {
                    callback(new LiveloxApiCall<User>()
                    {
                        Exception = ex,
                        Client = this
                    });
                }
            };

            if (Aborted)
            {
                return;
            }

            httpListener.BeginGetContext(asyncCallback, httpListenerWrapper);
        }

        public LiveloxApiCall<UserInfo> GetUserInfo(Action<LiveloxApiCall<UserInfo>> callback)
        {
            return CallApi(() => CreateGetRequest($"{baseUrl}/oauth2/userinfo"), callback);
        }

        public LiveloxApiCall<ImportableEvent> GetImportableEvent(string importableEventId, Action<LiveloxApiCall<ImportableEvent>> callback)
        {
            return CallApi(() => CreateGetRequest($"{baseUrl}/importableEvents/{importableEventId}?includeImportedEvent=true"), callback);
        }

        public LiveloxApiCall<ImportableEventLink> CreateImportableEvent(ImportableEvent importableEvent, Action<LiveloxApiCall<ImportableEventLink>> callback)
        {
            return CallApi(() => CreatePostJsonRequest($"{baseUrl}/importableEvents", importableEvent), callback);
        }

        public LiveloxApiCall<ImportableEventLink> UpdateImportableEvent(string importableEventId, ImportableEvent importableEvent, Action<LiveloxApiCall<ImportableEventLink>> callback)
        {
            return CallApi(() => CreatePutJsonRequest($"{baseUrl}/importableEvents/{importableEventId}", importableEvent), callback);
        }

        public LiveloxApiCall<ImportableEventLink> ImportImportableEvent(string importableEventId, Action<LiveloxApiCall<ImportableEventLink>> callback)
        {
            return CallApi(() => CreatePutJsonRequest($"{baseUrl}/importableEvents/{importableEventId}/import", (object)null), callback);
        }

        public LiveloxApiCall<LiveloxApiNullResponse> UploadFile(string importableEventId, string fileName, byte[] data, Action<LiveloxApiCall<LiveloxApiNullResponse>> callback)
        {
            return CallApi(() => CreatePostFileRequest($"{baseUrl}/importableEvents/{importableEventId}/files/{fileName}", data), callback);
        }

        public LiveloxApiCall<LiveloxApiNullResponse> RevokeToken(string token, string tokenTypeHint, Action<LiveloxApiCall<LiveloxApiNullResponse>> callback)
        {
            var parameters = new Dictionary<string, string>()
            {
                { "token", token },
                { "token_type_hint", tokenTypeHint }
            };

            return CallApi(() => CreatePostFormUrlencodedRequest($"{baseUrl}/oauth2/revoke", parameters), callback);
        }

        private LiveloxApiCall<T> CallApi<T>(Func<HttpRequestMessage> requestCreator, Action<LiveloxApiCall<T>> callback) where T : new()
        {
            // calls API with access token expiration awareness

            if (TokenInformation == null)
            {
                throw new OAuth2Exception("No token information present");
            }

            var call = new LiveloxApiCall<T>()
            {
                RequestContext = new LiveloxApiRequestContext()
                {
                    RequestCreator = requestCreator,
                    RetryCount = 0
                },
                Callback = callback,
                Client = this
            };

            if (TokenInformation?.ExpirationTime < DateTime.UtcNow)
            {
                // token about to expire, request a new
                RequestNewAccessToken(call, callback);
            }
            else
            {
                GetResponse(call);
            }
            return call;
        }

        private void RequestNewAccessToken<T>(LiveloxApiCall<T> call, Action<LiveloxApiCall<T>> callback) where T : new()
        {
            RequestNewAccessToken(TokenInformation.RefreshToken, requestNewAccessTokenCall =>
            {
                if (requestNewAccessTokenCall.Success)
                {
                    var newTokenInformation = requestNewAccessTokenCall.Result;
                    var settingsProvider = new SettingsProvider();
                    var existingSettings = settingsProvider.LoadSettings();
                    var existingUser = existingSettings.Users.FirstOrDefault(o => o.TokenInformation.RefreshToken == TokenInformation.RefreshToken);
                    if (existingUser != null)
                    {
                        existingUser.TokenInformation = newTokenInformation;
                        existingSettings.Users = new[] { existingUser }
                            .Concat(existingSettings.Users.Where(u => u != existingUser))
                            .ToArray();
                        settingsProvider.SaveSettings(existingSettings);
                    }
                    TokenInformation = newTokenInformation;
                    call.RequestContext.RetryCount++;
                    GetResponse(call);
                }
                else
                {
                    call.Exception = requestNewAccessTokenCall.Exception;
                    callback(call);
                }
            });
        }

        private void GetResponse<T>(LiveloxApiCall<T> call) where T : new()
        {
            if (Aborted)
            {
                return;
            }
            call.RegisterTimeout(timeout);

            // now, we're creating the request, when all tokens are fresh
            var request = call.RequestContext.CreateRequest();
            httpClient.SendAsync(request, call.CancellationSource.Token).ContinueWith(task =>
            {
                ResponseCallback(task, call);
            });
            requestCreatedCallback?.Invoke(call);
        }

        private void ResponseCallback<T>(Task<HttpResponseMessage> task, LiveloxApiCall<T> call) where T : new()
        {
            try
            {
                if (task.IsCanceled)
                {
                    if (call.CancellationSource?.IsCancellationRequested == true)
                    {
                        call.MarkTimedOut();
                        call.Exception = new TimeoutException(LiveloxResources.Timeout);
                    }
                    else
                    {
                        call.Exception = new OperationCanceledException();
                    }
                    call.Callback(call);
                    return;
                }

                if (task.IsFaulted)
                {
                    call.Exception = task.Exception?.InnerException ?? task.Exception;
                    call.Callback(call);
                    return;
                }

                call.Response = task.Result;

                if (!call.Response.IsSuccessStatusCode)
                {
                    // is the access token expired?
                    if (call.Response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        IEnumerable<string> wwwAuthValues;
                        bool hasWwwAuth = call.Response.Headers.TryGetValues("WWW-Authenticate", out wwwAuthValues);
                        if (hasWwwAuth && string.Join(" ", wwwAuthValues).Contains("The access token expired"))
                        {
                            if (call.RequestContext.RetryCount > 0) // safety mechanism to prevent infinite loop
                            {
                                call.Exception = new OAuth2Exception("The access token could not be refreshed.");
                            }
                            else
                            {
                                call.Response.Dispose();
                                RequestNewAccessToken(call, call.Callback);
                                return;
                            }
                        }
                        else
                        {
                            call.Exception = new StatusCodeException(call.Response.StatusCode);
                        }
                    }
                    else
                    {
                        call.Exception = new StatusCodeException(call.Response.StatusCode);
                    }

                    call.Response.Dispose();
                    call.Callback(call);
                    return;
                }

                call.Result = ReadResponsePayload<T>(call.Response);
                call.Response.Dispose();
                call.Callback(call);
            }
            catch (Exception ex)
            {
                call.Exception = ex;
                call.Callback(call);
            }
            finally
            {
                requestCompletedCallback?.Invoke(call);
            }
        }

        private static void HttpListenerGetContextCallback(IAsyncResult asyncResult)
        {
            var wrapper = (HttpListenerWrapper)asyncResult.AsyncState;
            var context = wrapper.HttpListener.EndGetContext(asyncResult);
            wrapper.Callback(context);
        }

        private void AddAuthorizationHeader(HttpRequestMessage request)
        {
            if(TokenInformation != null)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", TokenInformation.AccessToken);
            }
        }

        private static T ReadResponsePayload<T>(HttpResponseMessage response) where T : new()
        {
            // kind of a hack to handle void returns
            if (typeof(T) == typeof(LiveloxApiNullResponse))
            {
                return new T();
            }

            using (var responseStream = response.Content.ReadAsStreamAsync().Result)
            {
                using (var responseReader = new StreamReader(responseStream))
                {
                    var serializer = new JsonSerializer();

                    using (var jsonTextReader = new JsonTextReader(responseReader))
                    {
                        return serializer.Deserialize<T>(jsonTextReader);
                    }
                }
            }
        }

        private HttpRequestMessage CreateGetRequest(string url)
        {
            return CreateRequestWithHeaders(url, HttpMethod.Get, contentType: null);
        }

        private HttpRequestMessage CreatePostJsonRequest<T>(string url, T payload)
        {
            var request = CreateRequestWithHeaders(url, HttpMethod.Post);
            if (payload != null)
            {
                var json = JsonConvert.SerializeObject(payload);
                request.Content = new StringContent(json, Encoding.UTF8, applicationJson);
            }
            else
            {
                request.Content = new ByteArrayContent(Array.Empty<byte>());
            }
            return request;
        }

        private HttpRequestMessage CreatePostFileRequest(string url, byte[] payload)
        {
            var request = CreateRequestWithHeaders(url, HttpMethod.Post, contentType: null, accept: null);
            if (payload != null)
            {
                request.Content = new ByteArrayContent(payload);
            }
            else
            {
                request.Content = new ByteArrayContent(Array.Empty<byte>());
            }
            return request;
        }

        private HttpRequestMessage CreatePutJsonRequest<T>(string url, T payload)
        {
            var request = CreateRequestWithHeaders(url, HttpMethod.Put, contentType: payload == null ? null : applicationJson);
            if (payload != null)
            {
                var json = JsonConvert.SerializeObject(payload);
                request.Content = new StringContent(json, Encoding.UTF8, applicationJson);
            }
            else
            {
                request.Content = new ByteArrayContent(Array.Empty<byte>());
            }
            return request;
        }

        private HttpRequestMessage CreateDeleteRequest(string url)
        {
            return CreateRequestWithHeaders(url, HttpMethod.Delete, contentType: null, accept: null);
        }

        private HttpRequestMessage CreatePostFormUrlencodedRequest(string url, IEnumerable<KeyValuePair<string, string>> parameters)
        {
            var request = CreateRequestWithHeaders(url, HttpMethod.Post, contentType: null);
            request.Content = new FormUrlEncodedContent(parameters);
            return request;
        }

        private HttpRequestMessage CreateRequestWithHeaders(string url, HttpMethod httpMethod, string contentType = applicationJson, string accept = applicationJson)
        {
            var request = new HttpRequestMessage(httpMethod, url);
            AddAuthorizationHeader(request);
            request.Headers.Add("Culture", "en-US");
            if (accept != null)
            {
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(accept));
            }
            // Note: Content-Type is set on HttpContent, not on the request headers.
            // It will be set when Content is assigned in the calling methods.
            return request;
        }

        private void RequestNewAccessToken(string refreshToken, Action<LiveloxApiCall<OAuth2TokenInformation>> callback)
        {
            var parameters = new Dictionary<string, string>()
            {
                { "grant_type", "refresh_token" },
                { "refresh_token", refreshToken },
                { "client_id", clientId }
            };

            // sends the request
            var tokenRequest = CreatePostFormUrlencodedRequest($"{baseUrl}/oauth2/token", parameters);

            ReadTokenResponse(tokenRequest, callback);
        }

        private void ReadTokenResponse(HttpRequestMessage tokenRequest, Action<LiveloxApiCall<OAuth2TokenInformation>> callback)
        {
            // gets the response
            var call = new LiveloxApiCall<OAuth2TokenInformation>()
            {
                Callback = callback,
                RequestContext = new LiveloxApiRequestContext()
                {
                    Request = tokenRequest
                },
                Client = this
            };
            httpClient.SendAsync(tokenRequest, call.CancellationSource.Token).ContinueWith(task =>
            {
                ReadTokenResponseCallback(task, call);
            });
            requestCreatedCallback?.Invoke(call);
        }

        private void ReadTokenResponseCallback(Task<HttpResponseMessage> task, LiveloxApiCall<OAuth2TokenInformation> call)
        {
            try
            {
                if (task.IsCanceled)
                {
                    call.Exception = new OperationCanceledException();
                    call.Callback(call);
                    return;
                }

                if (task.IsFaulted)
                {
                    call.Exception = task.Exception?.InnerException ?? task.Exception;
                    call.Callback(call);
                    return;
                }

                call.Response = task.Result;

                if (!call.Response.IsSuccessStatusCode)
                {
                    OAuth2Exception oauth2Exception;
                    using (var reader = new StreamReader(call.Response.Content.ReadAsStreamAsync().Result))
                    {
                        var responseText = reader.ReadToEnd();
                        oauth2Exception = new OAuth2Exception(call.Response.StatusCode, responseText, new HttpRequestException($"Response status code does not indicate success: {(int)call.Response.StatusCode} ({call.Response.ReasonPhrase})."));
                    }
                    call.Response.Dispose();
                    call.Exception = oauth2Exception;
                    call.Callback(call);
                    return;
                }

                using (var reader = new StreamReader(call.Response.Content.ReadAsStreamAsync().Result))
                {
                    var responseText = reader.ReadToEnd();

                    var tokenEndpointParameters = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseText);

                    var accessToken = tokenEndpointParameters["access_token"];
                    var refreshToken = tokenEndpointParameters["refresh_token"];
                    var expiresIn = tokenEndpointParameters["expires_in"];

                    call.Result = new OAuth2TokenInformation()
                    {
                        AccessToken = accessToken,
                        RefreshToken = refreshToken,
                        ExpirationTime = DateTime.UtcNow.AddSeconds(Convert.ToInt32(expiresIn)).AddSeconds(-30) // to avoid trouble when the access token is just about to expire
                    };
                    call.Callback(call);
                }
                call.Response.Dispose();
            }
            catch (Exception ex)
            {
                call.Exception = ex;
                call.Callback(call);
            }
            finally
            {
                requestCompletedCallback?.Invoke(call);
            }
        }

        // ref http://stackoverflow.com/a/3978040
        private static int GetRandomUnusedPort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        /// <summary>
        /// Returns URI-safe data with a given input length.
        /// </summary>
        /// <param name="length">Input length (nb. output will be longer)</param>
        /// <returns></returns>
        private static string RandomDataBase64Url(int length)
        {
            RandomNumberGenerator rng = RandomNumberGenerator.Create();
            byte[] bytes = new byte[length];
            rng.GetBytes(bytes);
            return Base64UrlEncodeNoPadding(bytes);
        }

        /// <summary>
        /// Returns the SHA256 hash of the input string.
        /// </summary>
        /// <param name="inputString"></param>
        /// <returns></returns>
        private static byte[] Sha256(string inputString)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(inputString);
            SHA256 sha256 = SHA256.Create();
            return sha256.ComputeHash(bytes);
        }

        /// <summary>
        /// Base64url no-padding encodes the given input buffer.
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        private static string Base64UrlEncodeNoPadding(byte[] buffer)
        {
            var base64 = Convert.ToBase64String(buffer);

            // Converts base64 to base64url.
            base64 = base64.Replace("+", "-");
            base64 = base64.Replace("/", "_");
            // Strips padding.
            base64 = base64.Replace("=", "");

            return base64;
        }

        private class HttpListenerWrapper
        {
            public Action<HttpListenerContext> Callback { get; set; }
            public HttpListener HttpListener { get; set; }
        }
    }
}
