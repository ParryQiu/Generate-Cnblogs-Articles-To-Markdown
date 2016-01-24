using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Common.Helper;
using Html2Markdown;

namespace Generate_Cnblogs_Articles_To_Markdown_Files
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            //Init
            if (!Directory.Exists(Application.StartupPath + "\\output\\"))
            {
                Directory.CreateDirectory(Application.StartupPath + "\\output\\");
            }

            if (!Directory.Exists(Application.StartupPath + "\\images\\"))
            {
                Directory.CreateDirectory(Application.StartupPath + "\\images\\");
            }

            //抓取博客园文章转换成MD进行文档存储
            for (var page = 1; page <= 4; page++) //page=4 就是你博客的文章总页数
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
                    var matchArticle = regexArticle.Match(content);
                    if (matchArticle.Success)
                    {
                        Console.WriteLine("开始解析：" + articleUrl);
                        var title = matchArticle.Groups["title"].ToString().Trim();
                        var date = matchArticle.Groups["date"].ToString().Trim();
                        var articleContent = matchArticle.Groups["articlecontent"].ToString();
                        articleContent = ProcessArticleImage(articleContent); //对文章中的图片进行保存，根据情况可以不处理，如何有自己的图床，那么保存下来后替换掉图床前缀就可以了。
                        articleContent = ProcessArticleCode(articleContent);
                        articleContent =
                            articleContent.Replace("<div id=\"parrycontent\">", string.Empty)
                                .Replace("</div>", string.Empty);
                        var regexId = new Regex(@"cb_blogId=(?<blogid>\d+),cb_entryId=(?<entryid>\d+)",
                            RegexOptions.Singleline | RegexOptions.Multiline);
                        int blogId = 0, postId = 0;
                        var matchId = regexId.Match(content);
                        if (matchId.Success)
                        {
                            int.TryParse(matchId.Groups["blogid"].ToString(), out blogId);
                            int.TryParse(matchId.Groups["entryid"].ToString(), out postId);
                        }
                        var categoryTags = GetArticleCategory(blogId, postId);
                        var fileName = GetFileName(articleUrl);
                        var filePath = Application.StartupPath + "\\output\\" + fileName;
                        var mdContent = string.Format("---\r\ntitle: {0}\r\ndate: {1}\r\n{2}\r\n---\r\n{3}", title, date,
                            categoryTags, articleContent);
                        var converter = new Converter();
                        var markdown = converter.Convert(mdContent);
                        markdown = markdown.Substring(0, 300) + "\r\n<!--more-->\r\n" +
                                   markdown.Substring(301);
                        //保存文件
                        var streamWriter = new StreamWriter(filePath);
                        streamWriter.Write(markdown);
                        streamWriter.Close();

                        Console.WriteLine("保存成功：" + filePath);
                    }
                }
            }
            Console.WriteLine("##########抓取完成###########");
            Console.ReadKey();
        }

        private static string GetFileName(string articleUrl)
        {
            if (articleUrl.Length > articleUrl.LastIndexOf("/") + 1)
                return articleUrl.Substring(articleUrl.LastIndexOf("/") + 1).Replace(".html", string.Empty) + ".md";
            return "path_error";
        }

        private static string GetArticleCategory(int blogId, int postId)
        {
            var strReturn = string.Empty;
            var apiReturn =
                NetworkHelper.GetHtmlFromGet(
                    string.Format(
                        "http://www.cnblogs.com/mvc/blog/CategoriesTags.aspx?blogApp=parry&blogId={0}&postId={1}",
                        blogId, postId), Encoding.UTF8);
            var content = ConvertUnicode(apiReturn); //注意parry的参数需要替换，其实blogid不要获取，是固定的。
            var regexCategory = new Regex(@".*?category.*?>(\d+\.)?(?<cata>.*?)</a>",
                RegexOptions.Singleline | RegexOptions.Multiline);
            var regexTag = new Regex(".*?tag.*?>(?<cata>.*?)</a>", RegexOptions.Singleline | RegexOptions.Multiline);
            var matches = regexCategory.Matches(content);
            var stringBuilder = new StringBuilder();
            foreach (Match match in matches)
            {
                var catName = match.Groups["cata"].ToString();
                if (catName == "Sugars")
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
                strReturn += "tags:" + stringBuilderTags + "\r\n- 我的博客园文章";
            }
            return strReturn;
        }

        private static string ProcessArticleImage(string articleContent)
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
            if (matches.Count > 0)
            {
                return articleContent.Replace(preImagePath, "http://7xqdjc.com1.z0.glb.clouddn.com/blog_"); //自己的图床前缀
            }
            return articleContent; //自己的图床前缀
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
                    @"<span\s+style=""color:\s+#008080;"">.*?</span>", "",
                    RegexOptions.Singleline | RegexOptions.Multiline);
                resultString = Regex.Replace(resultString, "<span.*?>", "",
                    RegexOptions.Singleline | RegexOptions.Multiline);
                resultString = Regex.Replace(resultString, "</span>", "",
                    RegexOptions.Singleline | RegexOptions.Multiline);

                resultString = "\r\n{% codeblock lang:csharp%}\r\n" + resultString + "\r\n{% endcodeblock %}\r\n";
                articleContent =
                    articleContent.Replace(match.Groups["total"].ToString(), resultString)
                        .Replace("<div class=\"cnblogs_code\">", string.Empty)
                        .Replace("</div>", string.Empty);
            }
            return articleContent;
        }

        private static string ConvertUnicode(string str)
        {
            var outStr = "";
            var reg = new Regex(@"(?i)\\u([0-9a-f]{4})");
            outStr = reg.Replace(str,
                delegate(Match m1) { return ((char) Convert.ToInt32(m1.Groups[1].Value, 16)).ToString(); });
            return outStr;
        }
    }
}