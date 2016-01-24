using System;
using System.IO;
using System.Net;
using System.Text;

namespace Common.Helper
{
    public class NetworkHelper
    {
        public static bool SavePhotoFromUrl(string fileName, string url)
        {
            var value = false;
            try
            {
                var request = (HttpWebRequest) WebRequest.Create(url);

                var response = request.GetResponse();
                request.Timeout = 360000;
                response.GetResponseStream();

                if (!response.ContentType.ToLower().StartsWith("text/"))
                {
                    value = SaveBinaryFile(response, fileName);
                }
            }
            catch (Exception err)
            {
                var aa = err.ToString();
            }
            return value;
        }

        public static bool SaveBinaryFile(WebResponse response, string fileName)
        {
            var value = true;
            var buffer = new byte[1024];
            try
            {
                if (File.Exists(fileName))
                    File.Delete(fileName);
                Stream outStream = File.Create(fileName);
                var inStream = response.GetResponseStream();

                int l;
                do
                {
                    l = inStream.Read(buffer, 0, buffer.Length);
                    if (l > 0)
                        outStream.Write(buffer, 0, l);
                } while (l > 0);

                outStream.Close();
                inStream.Close();
            }
            catch
            {
                value = false;
            }
            return value;
        }

        /// <summary>
        ///     通过GET方式获取页面的方法
        /// </summary>
        /// <param name="urlString">请求的URL</param>
        /// <param name="encoding">页面编码</param>
        /// <returns></returns>
        public static string GetHtmlFromGet(string urlString, Encoding encoding)
        {
            //定义局部变量
            HttpWebRequest httpWebRequest;
            Stream stream;
            string htmlString;

            //请求页面
            try
            {
                httpWebRequest = (HttpWebRequest) WebRequest.Create(urlString);
                httpWebRequest.Credentials = CredentialCache.DefaultNetworkCredentials;
                httpWebRequest.CookieContainer = new CookieContainer();
                httpWebRequest.ContentType = "text/html; charset=utf-8";
                httpWebRequest.Method = "GET";
                httpWebRequest.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
                httpWebRequest.Timeout = 60000;
                httpWebRequest.UserAgent =
                    "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_10_3) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/40.0.2214.115 Safari/537.36";
            }
                //处理异常
            catch (Exception ex)
            {
                throw new Exception("GetHtmlFromGet建立页面请求时发生错误！", ex);
            }
            //获取服务器的返回信息
            try
            {
                var httpWebRespones = (HttpWebResponse) httpWebRequest.GetResponse();
                stream = httpWebRespones.GetResponseStream();
            }
                //处理异常
            catch (Exception ex)
            {
                throw new Exception("GetHtmlFromGet接受服务器返回页面时发生错误！", ex);
            }
            var streamReader = new StreamReader(stream, encoding);
            //读取返回页面
            try
            {
                htmlString = streamReader.ReadToEnd();
            }
                //处理异常
            catch (Exception ex)
            {
                throw new Exception("GetHtmlFromGet读取页面数据时发生错误！", ex);
            }
            //释放资源返回结果
            streamReader.Close();
            stream.Close();
            return htmlString;
        }
    }
}