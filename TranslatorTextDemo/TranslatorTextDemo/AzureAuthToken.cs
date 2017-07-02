using System;
using System.Net;
/// Portable HttpClient for .NET Framework and Windows Phone
/// https://blogs.msdn.microsoft.com/bclteam/2013/02/18/portable-httpclient-for-net-framework-and-windows-phone/
/// System.Net.Httpは、デフォルト使えないので、リンクする必要あり
using System.Net.Http;
using System.Threading.Tasks;

namespace TranslatorTextDemo
{
    /// <summary>
    /// Client to call Cognitive Services Azure Auth Token service in order to get an access token.
    /// Exposes asynchronous as well as synchronous methods.
    /// </summary>
    public class AzureAuthToken
    {
        /// AzureトークンサービスのURL
        private static readonly Uri ServiceUrl = new Uri("https://api.cognitive.microsoft.com/sts/v1.0/issueToken");

        ///  Azureサブスクリプションキーヘッダー
        private const string OcpApimSubscriptionKeyHeader = "Ocp-Apim-Subscription-Key";

        /// Azureトークンサービスキーの有効時間 - 最大値１０分 -
        private static readonly TimeSpan TokenCacheDuration = new TimeSpan(0, 10, 0);

        /// Azureトークンサービスから取得した最後の有効なトークンの値
        private string _storedTokenValue = string.Empty;

        /// Azureトークンサービスから取得した最後のトークン時間
        private DateTime _storedTokenTime = DateTime.MinValue;

        ///  Azureサブスクリプションキー
        public string SubscriptionKey { get; }

        /// HTTPステータスコード取得(最新トークンサービス)
        public HttpStatusCode RequestStatusCode { get; private set; }

        /// <summary>
        /// クライアントアクセストークン作成
        /// </summary>
        /// <param name="key">サブスクリプション認証キー</param>
        public AzureAuthToken(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key), "サブスクリプションキーを指定してください。");
            }

            this.SubscriptionKey = key;
            this.RequestStatusCode = HttpStatusCode.InternalServerError;
        }

        /// <summary>
        /// Gets a token for the specified subscription.
        /// </summary>
        /// <returns>The encoded JWT token prefixed with the string "Bearer ".</returns>
        /// <remarks>
        /// This method uses a cache to limit the number of request to the token service.
        /// A fresh token can be re-used during its lifetime of 10 minutes. After a successful
        /// request to the token service, this method caches the access token. Subsequent 
        /// invocations of the method return the cached token for the next 5 minutes. After
        /// 5 minutes, a new token is fetched from the token service and the cache is updated.
        /// </remarks>
        public async Task<string> GetAccessTokenAsync()
        {
            if (string.IsNullOrWhiteSpace(this.SubscriptionKey))
            {
                return string.Empty;
            }

            // Re-use the cached token if there is one.
            if ((DateTime.Now - _storedTokenTime) < TokenCacheDuration)
            {
                return _storedTokenValue;
            }

            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Post;
                request.RequestUri = ServiceUrl;
                request.Content = new StringContent(string.Empty);
                request.Headers.TryAddWithoutValidation(OcpApimSubscriptionKeyHeader, this.SubscriptionKey);
                client.Timeout = TimeSpan.FromSeconds(2);
                var response = await client.SendAsync(request);
                this.RequestStatusCode = response.StatusCode;
                response.EnsureSuccessStatusCode();
                var token = await response.Content.ReadAsStringAsync();
                _storedTokenTime = DateTime.Now;
                _storedTokenValue = "Bearer " + token;
                return _storedTokenValue;
            }
        }

        /// <summary>
        /// Gets a token for the specified subscription. Synchronous version.
        /// Use of async version preferred
        /// </summary>
        /// <returns>The encoded JWT token prefixed with the string "Bearer ".</returns>
        /// <remarks>
        /// This method uses a cache to limit the number of request to the token service.
        /// A fresh token can be re-used during its lifetime of 10 minutes. After a successful
        /// request to the token service, this method caches the access token. Subsequent 
        /// invocations of the method return the cached token for the next 5 minutes. After
        /// 5 minutes, a new token is fetched from the token service and the cache is updated.
        /// </remarks>
        public string GetAccessToken()
        {
            // Re-use the cached token if there is one.
            if ((DateTime.Now - _storedTokenTime) < TokenCacheDuration)
            {
                return _storedTokenValue;
            }

            string accessToken = null;
            var task = Task.Run(async () =>
            {
                accessToken = await this.GetAccessTokenAsync();
            });

            while (!task.IsCompleted)
            {
                // System.Threading.Thread.Yield();
                task.Wait();
            }
            if (task.IsFaulted)
            {
                throw task.Exception;
            }
            if (task.IsCanceled)
            {
                throw new Exception("Timeout obtaining access token.");
            }
            return accessToken;
        }

    }
}
