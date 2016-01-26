导出博客园的文章成 Markdown 文件存储的工具。     

基于 C# 语言，测试调用为 C# Console 程序。
# 支持的功能
* 可以循环抓取自己博客园的所有文章导出到 Markdown 文件进行保存；
* 在 Markdown 的头部保存了原文章的标题、发表时间、文章分类、文章 tag 元素；
* 文章中的代码块会抽取出来包含在 `codeblock` 中，你也可以修改源码保存成其他的格式块；
* 保存的文件名就是原文章的路径，如果你的文章都设置了 `EntryName`，那生成的文件名就会非常的友好；
* 文章中的图片可选进行本地保存，命名的格式为源文件名，并可在原文中将链接进行图床前缀的替换，你可以修改源码按照自己的格式进行保存。

导出函数的定义以及参数。

```
/// <summary>/// 导出博客园的文章成本地 Markdown 进行保存/// </summary>/// <param name="pageStart">博客起始页码，即 http://www.cnblogs.com/parry/default.html?page={0} </param>/// <param name="pageEnd">博客结束页码，即 http://www.cnblogs.com/parry/default.html?page={0} </param>/// <param name="isSaveImage">是否将文章中的图片保存到本地，保存后文件夹在程序运行的 images 文件夹</param>/// <param name="imagePrefixUrl">替换文章中的图片为自己图床的前缀 Url</param>/// <param name="isAddMoreSeparateLine">在抓取到的文章 separateLineLocation（参数） 处添加<!--more-->分隔符，用于博客展示文章时用于抽取描述以及阅读更多使用。</param>/// <param name="separateLineLocation">添加分隔符的位置</param>/// <returns>是否执行完成</returns>public static bool ExportToMarkdown(int pageStart, int pageEnd, bool isSaveImage, string imagePrefixUrl = "", bool isAddMoreSeparateLine = false, int separateLineLocation = 300){
	
}
```

抓取保存后文件预览。

![](http://7xqdjc.com1.z0.glb.clouddn.com/blog_670d31b91107e48bdaf1f033d055a987.png)

# 基本原理
1. 循环抓取博客的列表，获取到文章的链接；
2. 循环文章的链接，进行抓取，提取元素；
3. 保存抓取到的元素进行格式化并保存。

# 几个知识点
## 将 HTML 转换成 Markdown
这里使用了一个开源的组件 [Html2Markdown](https://github.com/baynezy/Html2Markdown) ,在控制台安装组件后就可以使用了，主要支持两个方法。
对字符串进行转换

```
var html = "Something to <strong>convert</strong>";
var converter = new Converter();
var markdown = converter.Convert(html);
```

对文件进行转换

```
var path = "file.html";
var converter = new Converter();
var markdown = converter.ConvertFile(path);
```

注意项目中添加此组件引用的方法：    

1. 项目中已经迁入了 bin 文件夹，其中包含 `Html2Markdown.dll` 和 `HtmlAgilityPack.dll (Html2Markdown的依赖)`，直接添加到项目中引用即可；    

2. 在包管理控制台中使用 `Install-Package Html2Markdown` 进行安装。

## 注意 Mac 和 Windows 以及 Linux 下的换行的区别
具体的区别可以看这里，可以根据自己的情况对源码进行修改。

unix、windows、mac 的换行习惯
> unix / linux：用 LF (\n) 表示一行结束。

> mac：用 CR (\r) 表示一行结束。

> windows：用 CR LF (\r\n) 和起来表示一行结束。

## 文章分类、tag 的获取
分析后发现通过模拟请求 API 获取即可，需要的参数通过正则匹配获取，返回数据为 Unicode 进行转码提取。

## 文章中图片保存
你可以修改源码开启或关闭此功能，使用文章中文件名作为保存到本地的文件名，并将文章中的图片前缀进行了替换，你可以替换成你自己新的图床地址。输出的图片文件在程序启动的 `images` 文件夹。

# 注意的问题
需要注意的问题是，项目中可能因为新旧文章中某些格式的变化导致抓取出来的 Markdown 格式可能稍有偏差，以及图片、代码块的处理，你需要去根据自己的博客去进行对应的调整后使用。

