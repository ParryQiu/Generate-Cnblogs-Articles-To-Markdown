using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Common.Helper;
using Html2Markdown;

namespace Generate_Cnblogs_Articles_To_Markdown_Files
{
    public class CnblogsHelper
    {

        public const string mCodeblockBegin = "{% codeblock lang:csharp%}";
        public const string mCodeblockEnd = "{% endcodeblock %}";
        /// <summary>
        /// 导出博客园的文章成本地 Markdown 进行保存
        /// </summary>
        /// <param name="pageStart">博客起始页码，即 http://www.cnblogs.com/parry/default.html?page={0} </param>
        /// <param name="pageEnd">博客结束页码，即 http://www.cnblogs.com/parry/default.html?page={0} </param>
        /// <param name="isSaveImage">是否将文章中的图片保存到本地，保存后文件夹在程序运行的 images 文件夹</param>
        /// <param name="imagePrefixUrl">替换文章中的图片为自己图床的前缀 Url</param>
        /// <param name="isAddMoreSeparateLine">在抓取到的文章 separateLineLocation（参数） 处添加<!--more-->分隔符，用于博客展示文章时用于抽取描述以及阅读更多使用。</param>
        /// <param name="separateLineLocation">添加分隔符的位置</param>
        /// <returns>是否执行完成</returns>
        public static bool ExportToMarkdown(int pageStart, int pageEnd, bool isSaveImage, string imagePrefixUrl = "", bool isAddMoreSeparateLine = false, int separateLineLocation = 300)
        {
            for (var page = pageStart; page <= pageEnd; page++)
            {
                var pagesUrl = string.Format("http://www.cnblogs.com/parry/default.html?page={0}", page);
                //抓取所有的文章内容链接地址，进行循环抓取并存储
                var regex = new Regex(@"class=""postTitle"">\s+<a.*?href=""(?<href>.*?)"">",
                    RegexOptions.Singleline | RegexOptions.Multiline);
                var matches = regex.Matches(NetworkHelper.GetHtmlFromGet(pagesUrl, Encoding.UTF8));
                foreach (Match match in matches)
                {
                    var articleUrl = match.Groups["href"].ToString();
                    var regexArticle =
                        new Regex(
                            @"<div\s+id=""topics"">.*?id=""cb_post_title_url"".*?>(?<title>.*?)</a>.*?<div\s+id=""cnblogs_post_body"">(?<articlecontent>.*?)</div><div\s+(?:id=""MySignature""></div>)?\s+<div\s+class=""clear""></div>.*?id=""post-date"">(?<date>.*?)</span>",
                            RegexOptions.Singleline | RegexOptions.Multiline);
                    var content = NetworkHelper.GetHtmlFromGet(articleUrl, Encoding.UTF8);
                    var regexAppName = new Regex("currentBlogApp\\s+=\\s+'(?<appName>.*?)'", RegexOptions.Singleline | RegexOptions.Multiline);
                    var matchAppName = regexAppName.Match(content);
                    var appName = string.Empty;
                    if (matchAppName.Success)
                    {
                        appName = matchAppName.Groups["appName"].ToString();
                    }
                    var matchArticle = regexArticle.Match(content);
                    if (matchArticle.Success)
                    {
                        var title = matchArticle.Groups["title"].ToString().Trim();
                        var date = matchArticle.Groups["date"].ToString().Trim();
                        var articleContent = matchArticle.Groups["articlecontent"].ToString();
                        if (isSaveImage)
                        {
                            articleContent = ProcessArticleImage(articleContent, imagePrefixUrl); //对文章中的图片进行保存，根据情况可以不处理，如何有自己的图床，那么保存下来后替换掉图床前缀就可以了。
                        }

                        articleContent = ProcessArticleCode(articleContent);
                        articleContent = articleContent.Replace("<div id=\"parrycontent\">", string.Empty).Replace("</div>", string.Empty); //博客标记的特殊处理
                        var regexId = new Regex(@"cb_blogId=(?<blogid>\d+),cb_entryId=(?<entryid>\d+)",
                            RegexOptions.Singleline | RegexOptions.Multiline);
                        int blogId = 0, postId = 0;
                        var matchId = regexId.Match(content);
                        if (matchId.Success)
                        {
                            int.TryParse(matchId.Groups["blogid"].ToString(), out blogId);
                            int.TryParse(matchId.Groups["entryid"].ToString(), out postId);
                        }

                        var categoryTags = GetArticleCategory(appName, blogId, postId);
                        var fileName = GetFileName(title, date);
                        var filePath = Application.StartupPath + "\\output\\" + fileName;
                        var mdContent = string.Format("---\r\ntitle: {0}\r\ndate: {1}\r\n{2}\r\n---\r\n{3}", title, date,
                            categoryTags, articleContent);
                        var converter = new Converter();
                        var markdown = converter.Convert(mdContent);
                        int tmpseparateLineLocation = separateLineLocation;
                        //注意此处的作用是在抓取到的文章 300 字符处添加<!--more-->分隔符，用于博客展示文章时用于抽取描述以及阅读更多使用。                       
                        if (isAddMoreSeparateLine && markdown.Length > (separateLineLocation + 1))
                        {
                            int indexb = 0, indexe = 0;
                            while (indexe < separateLineLocation)
                            {
                                indexb = markdown.IndexOf(mCodeblockBegin, indexe);
                                if (indexb == -1)
                                {
                                    break;//there are no codes in the arcticle.
                                }
                                indexe = markdown.IndexOf(mCodeblockEnd, indexb);
                                //if the code block is truncated,adjust the separateLineLocation
                                if ((indexb <= separateLineLocation && separateLineLocation <= indexb + mCodeblockBegin.Length)
                                    || (indexe <= separateLineLocation && separateLineLocation <= indexe + mCodeblockEnd.Length))
                                {
                                    separateLineLocation = indexe + mCodeblockEnd.Length;
                                    break;
                                }
                            }
                            markdown = markdown.Substring(0, separateLineLocation) + "\r\n<!--more-->\r\n" +
                                       markdown.Substring(separateLineLocation + 1);
                            separateLineLocation = tmpseparateLineLocation;
                        }

                        //保存文件
                        var streamWriter = new StreamWriter(filePath);
                        streamWriter.Write(markdown);
                        streamWriter.Close();
                        Console.WriteLine(fileName + " have been generated..");
                    }
                }
            }
            return true;
        }


        #region 元素抓取
        private static string GetFileName(string articleUrl)
        {
            if (articleUrl.Length > articleUrl.LastIndexOf("/") + 1)
                return articleUrl.Substring(articleUrl.LastIndexOf("/") + 1).Replace(".html", string.Empty) + ".md";
            return "path_error";
        }
        private static string GetFileName(string title, string date)
        {
            var fileName = date + "_" + title + ".md";
            Regex regex = new Regex("[:|\\|/|*|?|>|<||]");
            return regex.Replace(fileName, "-");
        }

        private static string GetArticleCategory(string appName, int blogId, int postId)
        {
            var strReturn = string.Empty;
            var apiReturn =
                NetworkHelper.GetHtmlFromGet(string.Format("http://www.cnblogs.com/mvc/blog/CategoriesTags.aspx?blogApp={0}&blogId={1}&postId={2}", appName, blogId, postId), Encoding.UTF8);
            var content = StringHelper.ConvertUnicode(apiReturn); //注意参数 appName 需要替换，其实blogid不要获取，是固定的。
            var regexCategory = new Regex(@".*?category.*?>(\d+\.)?(?<cata>.*?)</a>",
                RegexOptions.Singleline | RegexOptions.Multiline);
            var regexTag = new Regex(".*?tag.*?>(?<cata>.*?)</a>", RegexOptions.Singleline | RegexOptions.Multiline);
            var matches = regexCategory.Matches(content);
            var stringBuilder = new StringBuilder();
            foreach (Match match in matches)
            {
                var catName = match.Groups["cata"].ToString();
                if (catName == "Sugars") //一些分类的替换，可根据需要修改
                {
                    catName = "开发技巧";
                }
                stringBuilder.AppendFormat("\r\n- {0}", catName);
            }
            if (matches.Count > 0)
            {
                strReturn = "categories:" + stringBuilder;
            }

            var stringBuilderTags = new StringBuilder();
            var matchesTag = regexTag.Matches(content);
            foreach (Match match in matchesTag)
            {
                var catName = match.Groups["cata"].ToString();
                stringBuilderTags.AppendFormat("\r\n- {0}", catName);
            }
            if (!string.IsNullOrEmpty(strReturn))
            {
                strReturn += "\r\n";
            }
            if (matchesTag.Count > 0)
            {
                strReturn += "tags:" + stringBuilderTags + "\r\n- 我的博客园文章"; //导入的文章添加了默认的 tag，可去除。
            }
            return strReturn;
        }

        private static string ProcessArticleImage(string articleContent, string imagePrefixUrl = "")
        {
            var regex = new Regex(@"<img\s+src=""(?<src>.*?)""", RegexOptions.Singleline | RegexOptions.Multiline);
            var matches = regex.Matches(articleContent);
            var preImagePath = "";
            foreach (Match match in matches)
            {
                var imagePath = match.Groups["src"].ToString();
                var imageName = imagePath.Substring(imagePath.LastIndexOf("/") + 1);
                if (string.IsNullOrEmpty(preImagePath))
                {
                    preImagePath = imagePath.Substring(0, imagePath.LastIndexOf("/") + 1);
                }
                NetworkHelper.SavePhotoFromUrl(Application.StartupPath + "\\images\\" + imageName, imagePath);
            }
            if (matches.Count > 0 && !string.IsNullOrEmpty(imagePrefixUrl))
            {
                return articleContent.Replace(preImagePath, imagePrefixUrl); //自己的图床前缀
            }
            return articleContent;
        }

        private static string ProcessArticleCode(string articleContent)
        {
            var regex =
                new Regex(
                    @"(?<total><div\s+class=""cnblogs_code"">.*?(<pre>|<div>)(?<code>.*?)(</pre>|</div>).*?</div>)",
                    RegexOptions.Singleline | RegexOptions.Multiline);
            var matches = regex.Matches(articleContent);
            foreach (Match match in matches)
            {
                var resultString = Regex.Replace(match.Groups["code"].ToString(),
                    @"<span\s+style=""color:\s+#008080;"">\s*\d+\s*", "",
                    RegexOptions.Singleline | RegexOptions.Multiline);
                resultString = Regex.Replace(resultString, "<span.*?>", "",
                    RegexOptions.Singleline | RegexOptions.Multiline);
                resultString = Regex.Replace(resultString, "</span>", "",
                    RegexOptions.Singleline | RegexOptions.Multiline);

                resultString = "\r\n" + mCodeblockBegin + "\r\n" + resultString + "\r\n" + mCodeblockEnd + "\r\n";
                articleContent = articleContent.Replace(match.Groups["total"].ToString(), resultString);

            }
            return articleContent.Replace("<div class=\"cnblogs_code\">", string.Empty)
                        .Replace("</div>", string.Empty);
        }

        #endregion
    }
}
