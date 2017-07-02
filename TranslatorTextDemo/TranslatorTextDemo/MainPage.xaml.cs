using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using Xamarin.Forms;
using System.Threading.Tasks;

namespace TranslatorTextDemo
{
	public partial class MainPage : ContentPage
	{
        // Translator Text Translation APIのサブスクリプションキー（Azureの認証キー1 or キー2）
        // ※以下をAzureの認証キーを変更すること
        private const string TEXT_TRANSLATION_API_SUBSCRIPTION_KEY = "ENTER_YOUR_CLIENT_SECRET";
        // 認証キーオブジェクト
        private AzureAuthToken tokenProvider;
        // 言語リストキャッシュ
        private string[] friendlyName = {" "};
        // 言語マッピング
        private Dictionary<string, string> languageCodesAndTitles = new Dictionary<string, string>();
        
		public MainPage()
		{
			InitializeComponent();
            // クライアントアクセストークン作成　
			tokenProvider = new AzureAuthToken(TEXT_TRANSLATION_API_SUBSCRIPTION_KEY);  // Azureの認証キー1 or 認証キー2を設定
            // 翻訳する言語一覧取得
            GetLanguagesForTranslate(); 
            // 翻訳可能な言語設定
            GetLanguageNamesMethod(tokenProvider.GetAccessToken(), friendlyName);
            // リスト要素をPickerに設定
            enumLanguages();
		}
		
        /// <summary>
        /// 言語一覧表示
        /// </summary>
        private void enumLanguages()
        {
            // 言語マッピング数
            var count = languageCodesAndTitles.Count;

            for (int i = 0; i < count; i++)
            {
                // Xamarin.Forms Picker コントロール
                LanguagePicker.Items.Add(languageCodesAndTitles.ElementAt(i).Key);
            }

            if(count != 0)
            {
                LanguagePicker.SelectedItem = "Japanese";
            }

        }

        /// <summary>
        /// 翻訳開始
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
         private void translateButton_Click(object sender, EventArgs e)
        {
            string languageCode;
            string txtToTranslate = textToTranslate.Text;

            languageCodesAndTitles.TryGetValue(LanguagePicker.SelectedItem.ToString(), out languageCode);

            // デフォルトは英語設定
            if (languageCode == null) 
            {
                languageCode = "en";
            }
            // 未入力は処理を終了
            if (txtToTranslate == null)
            {
                return;
            }
            // Translate Method
            // https://msdn.microsoft.com/en-us/library/ff512421.aspx
            //
            string uri = string.Format("http://api.microsofttranslator.com/v2/Http.svc/Translate?text=" + System.Net.WebUtility.UrlEncode(txtToTranslate) + "&to={0}", languageCode);

            WebRequest translationWebRequest = WebRequest.Create(uri);
            translationWebRequest.Headers["Authorization"] = tokenProvider.GetAccessToken();

            WebResponse response = null;
            var task = Task.Run(async () =>
            {
                response = await translationWebRequest.GetResponseAsync();
            });
            while (!task.IsCompleted)
            {
                task.Wait();
            }

            Stream stream = response.GetResponseStream();
            Encoding encode = Encoding.GetEncoding("utf-8");
            StreamReader translatedStream = new StreamReader(stream, encode);
            System.Xml.XmlDocument xTranslation = new System.Xml.XmlDocument();
            xTranslation.LoadXml(translatedStream.ReadToEnd());
            translatedTextLabel.Text= "翻訳 -->   " + xTranslation.InnerText;
        }

        /// <summary>
        /// 翻訳する言語一覧取得
        /// </summary>
        private void GetLanguagesForTranslate()
        {
           
            string uri = "http://api.microsofttranslator.com/v2/Http.svc/GetLanguagesForTranslate";
            WebRequest WebRequest = WebRequest.Create(uri);
            WebRequest.Headers["Authorization"] = tokenProvider.GetAccessToken();

            WebResponse response = null;

            try
            {
                var task = Task.Run(async () =>
                {
                    response = await WebRequest.GetResponseAsync();
                });
                while (!task.IsCompleted)
                {
                    task.Wait();
                }
                using (Stream stream = response.GetResponseStream())
                {
                    System.Runtime.Serialization.DataContractSerializer dcs = new System.Runtime.Serialization.DataContractSerializer(typeof(List<string>));
                    List<string> languagesForTranslate = (List<string>)dcs.ReadObject(stream);
                    friendlyName = languagesForTranslate.ToArray();
                }
            }
            catch
            {
                throw;
            }
            finally
            {
                if (response != null)
                {
                    response.Dispose();
                    response = null;
                }
            }
        }

        /// <summary>
        /// 翻訳可能な言語設定
        /// </summary>
        /// <param name="authToken"></param>
        /// <param name="languageCodes"></param>
        private void GetLanguageNamesMethod(string authToken, string[] languageCodes)
        {
            // GetLanguageNames Method
            // https://msdn.microsoft.com/ja-jp/library/ff512399.aspx
            //
            string uri = "http://api.microsofttranslator.com/v2/Http.svc/GetLanguageNames?locale=en";

            //
            // Webリクエスト作成
            //
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.Headers["Authorization"] = tokenProvider.GetAccessToken();
            request.ContentType = "text/xml";
            request.Method = "POST";
            System.Runtime.Serialization.DataContractSerializer dcs = new System.Runtime.Serialization.DataContractSerializer(Type.GetType("System.String[]"));

            var task = Task.Run(async () =>
            {
                using (System.IO.Stream stream = await request.GetRequestStreamAsync())
                {
                    dcs.WriteObject(stream, languageCodes);
                }
            });
            while (!task.IsCompleted)
            {
                task.Wait();
            }

            WebResponse response = null;
            try
            {
                task = Task.Run(async () =>
                {
                    response = await request.GetResponseAsync();
                });
                while (!task.IsCompleted)
                {
                    task.Wait();
                }

                using (Stream stream = response.GetResponseStream())
                {
                    string[] languageNames = (string[])dcs.ReadObject(stream);

                    for (int i = 0; i < languageNames.Length; i++)
                    {
                        languageCodesAndTitles.Add(languageNames[i], languageCodes[i]); 
                    }   
                }
            }
            catch
            {
                throw;
            }
            finally
            {
                if (response != null)
                {
                    response.Dispose();
                    response = null;
                }
            }
        }
	}
}
