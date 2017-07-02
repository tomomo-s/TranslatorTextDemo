using System;
using System.Net;
/// Portable HttpClient for .NET Framework and Windows Phone
/// https://blogs.msdn.microsoft.com/bclteam/2013/02/18/portable-httpclient-for-net-framework-and-windows-phone/
/// System.Net.Http�́A�f�t�H���g�g���Ȃ��̂ŁA�����N����K�v����
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
        /// Azure�g�[�N���T�[�r�X��URL
        private static readonly Uri ServiceUrl = new Uri("https://api.cognitive.microsoft.com/sts/v1.0/issueToken");

        ///  Azure�T�u�X�N���v�V�����L�[�w�b�_�[
        private const string OcpApimSubscriptionKeyHeader = "Ocp-Apim-Subscription-Key";

        /// Azure�g�[�N���T�[�r�X�L�[�̗L������ - �ő�l�P�O�� -
        private static readonly TimeSpan TokenCacheDuration = new TimeSpan(0, 10, 0);

        /// Azure�g�[�N���T�[�r�X����擾�����Ō�̗L���ȃg�[�N���̒l
        private string _storedTokenValue = string.Empty;

        /// Azure�g�[�N���T�[�r�X����擾�����Ō�̃g�[�N������
        private DateTime _storedTokenTime = DateTime.MinValue;

        ///  Azure�T�u�X�N���v�V�����L�[
        public string SubscriptionKey { get; }

        /// HTTP�X�e�[�^�X�R�[�h�擾(�ŐV�g�[�N���T�[�r�X)
        public HttpStatusCode RequestStatusCode { get; private set; }

        /// <summary>
        /// �N���C�A���g�A�N�Z�X�g�[�N���쐬
        /// </summary>
        /// <param name="key">�T�u�X�N���v�V�����F�؃L�[</param>
        public AzureAuthToken(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key), "�T�u�X�N���v�V�����L�[���w�肵�Ă��������B");
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
