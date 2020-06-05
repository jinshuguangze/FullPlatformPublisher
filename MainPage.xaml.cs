using HtmlAgilityPack;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace FullPlatformPublisher
{
    public sealed partial class MainPage : Page
    {
        // 当前打开的md文件
        private static OpenedFile TheOpenedFile = new OpenedFile();

        // 上传阿里云的EndPoint
        private static string EndPoint = "oss-accelerate.aliyuncs.com";

        //  上传阿里云的AccessKeyID
        private static string AccessKeyID = "LTAI4GH9HkPLzXemuDHJBXwn";

        //  上传阿里云的AccessKeySecret
        private static string AccessKeySecret = "zjIQE82LiPGVVMYiHCkVaWgZVCVhTF";

        // 上传阿里云的Bucket
        private static string Bucket = "evaluations";

        // 存储数据的根文件夹名称
        public static string Root = "Papers";

        // 存储原始图片的文件夹名称
        public static string OriginalImages = "images_original";

        // 存储处理后图片的文件夹名称
        public static string ProcessedImages = "images_processed";

        // 支持图像类型
        public static string[] SupportImageTypes =
        {
            ".png", ".jpg",".jpeg", ".jfif", ".gif", ".bmp", ".tif", ".tiff"
        };

        // 内部图像类型识别头
        public static Dictionary<string, string> ImageHeadCode = new Dictionary<string, string>
        {
            {"13780", ".png"},
            {"255216", ".jpg"},
            {"7173", ".gif"},
            {"6677", ".bmp"},
            {"7373", ".tif"}
        };

        // 初始化
        public MainPage()
        {
            this.InitializeComponent();
        }

        // 点击打开按钮
        private async void button_open_Click(object sender, RoutedEventArgs e)
        {
            // 创建文件选择器
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.List;
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.ComputerFolder;
            picker.FileTypeFilter.Add(".html");
            Windows.Storage.StorageFile file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                // 存储文件字段
                TheOpenedFile.File = file;

                // 显示打开的html文件
                string preHtml = await Windows.Storage.FileIO.ReadTextAsync(file);
                webView_viewer.NavigateToString(preHtml);

                // html通用预处理
                TheOpenedFile.Html = htmlPreprocessing(preHtml);
            }
        }

        // html预处理函数
        private static string htmlPreprocessing(string html)
        {
            try
            {
                int a, b = 0;
                // 获取首标题
                a = html.IndexOf("<h1", StringComparison.CurrentCulture) + 3;
                b = html.IndexOf("<!-- omit in toc --></h1>", StringComparison.CurrentCulture) - 1;
                TheOpenedFile.Title = html.Substring(a, b - a + 1).Split('>')[1].Trim('\n', '\r', ' ');

                // 去掉html两端多余信息
                a = html.IndexOf("</h1>", StringComparison.CurrentCulture) + 5;
                b = html.IndexOf("</div>", StringComparison.CurrentCulture) - 1;
                html = html.Substring(a, b - a + 1).Trim('\n', '\r', ' ');

                // 给html加上一个通用的父集
                html = html.Insert(0, "<div>");
                html = html.Insert(html.Length, "</div>");
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e);
                System.Diagnostics.Debug.WriteLine("预处理异常！");
            }
            return html;
        }

        // 点击同步按钮
        private async void button_synchronize_ClickAsync(object sender, RoutedEventArgs e)
        {
            if (checkBox_toutiao.IsChecked == true)
            {
                if (TheOpenedFile.Html == null)
                {
                    return;
                }

                // 处理头条html
                TheOpenedFile.ToutiaoHtml = toutiaoHtmlProcessing(TheOpenedFile.Html);

                // 设置粘贴板内容
                DataPackage ToutiaoHtmlDataPackage = new DataPackage();
                ToutiaoHtmlDataPackage.SetText(TheOpenedFile.ToutiaoHtml);
                Clipboard.SetContent(ToutiaoHtmlDataPackage);
            }

            // ---------暂时没想好怎么处理---------
            if (TheOpenedFile.File.FileType.Equals(".md"))
            {
                // 获取当前文件夹
                StorageFolder currentFolder = await TheOpenedFile.File.GetParentAsync();

                // 获取md文件所有本地图片名称
                string mdText = await Windows.Storage.FileIO.ReadTextAsync(TheOpenedFile.File);
                ArrayList mdImagePathArray = new ArrayList();
                ArrayList mdImageNoteArray = new ArrayList();
                foreach (Match match in Regex.Matches(mdText, "!\\[[^\\]]*\\]\\([^\\)]+\\)"))
                {
                    // 获取图片评论数组
                    string mdImageNote = match.Value.Substring(match.Value.IndexOf("[") + 1
                        , match.Value.IndexOf("]") - match.Value.IndexOf("[") - 1);
                    mdImageNoteArray.Add(mdImageNote);

                    // 获取图片地址数组
                    string mdImagePath = match.Value.Substring(match.Value.LastIndexOf("(") + 1
                        , match.Value.LastIndexOf(")") - match.Value.LastIndexOf("(") - 1);
                    mdImagePath = mdImagePath.Split(' ')[0];
                    if (!(mdImagePath.StartsWith("http://") || mdImagePath.StartsWith("https://")))
                    {
                        mdImagePathArray.Add(mdImagePath);
                    }
                    else
                    {
                        mdImagePathArray.Add("");
                    }
                }

                // 读取logo图像素材
                CanvasDevice canvasDevice = new CanvasDevice(true);
                StorageFile logoFile = await getFileFromUri(currentFolder, "logo0.png");
                CanvasBitmap logoImage = await CanvasBitmap.LoadAsync(canvasDevice, await logoFile.OpenAsync(FileAccessMode.Read));

                // 本地图像处理
                int i = 0;
                foreach (string mdImagePath in mdImagePathArray)
                {
                    // 读取素材图像
                    StorageFile imageFile = await getFileFromUri(currentFolder, mdImagePath);
                    if (imageFile == null)
                    {
                        // 这里跳过去之前需要处理下
                        continue;
                    }
                    CanvasBitmap basicImage = await CanvasBitmap.LoadAsync(canvasDevice, await imageFile.OpenAsync(FileAccessMode.Read));

                    // 进行拼合，极限情况：横条：长度大于三倍高度；竖条：高度大于两倍长度。此时都基于长度放缩。
                    double basicWidth = basicImage.Size.Width;
                    double basicHeight = basicImage.Size.Height;
                    using (CanvasRenderTarget canvasRenderTarget = new CanvasRenderTarget(basicImage
                        , new Windows.Foundation.Size(basicWidth + 0.02 * basicHeight, 1.28 * basicHeight)))
                    {
                        using (CanvasDrawingSession session = canvasRenderTarget.CreateDrawingSession())
                        {
                            session.Clear(Colors.White);
                            session.DrawImage(basicImage, (float)(0.01 * basicHeight), (float)(0.01 * basicHeight));
                            session.DrawImage(logoImage, new Rect(0.01 * basicHeight, 1.02 * basicHeight, 0.25 * basicHeight, 0.25 * basicHeight)
                                , new Rect(0.0, 0.0, logoImage.Size.Width, logoImage.Size.Height), (float)1.0, CanvasImageInterpolation.HighQualityCubic);
                            session.DrawText("这张图片的介绍巴拉巴拉巴拉巴拉巴拉巴拉巴拉巴拉巴拉巴拉巴拉巴拉巴拉巴拉巴拉巴拉巴拉巴拉"
                                , (float)(0.27 * basicHeight), (float)(1.02 * basicHeight), (float)(1.02 * basicWidth - 0.28 * basicHeight)
                                , (float)(0.25 * basicHeight), Colors.Black, new CanvasTextFormat());
                            session.Flush();
                        }
                        StorageFile storageFile = await (await currentFolder.CreateFolderAsync("fortest", CreationCollisionOption.OpenIfExists))
                        .CreateFileAsync("test.gif", CreationCollisionOption.GenerateUniqueName);
                        await canvasRenderTarget.SaveAsync(await storageFile.OpenAsync(FileAccessMode.ReadWrite), CanvasBitmapFileFormat.Gif);
                    }

                    //// 上传图片至阿里云
                    //i++;
                    //OssClient client = new OssClient(EndPoint, AccessKeyID, AccessKeySecret);
                    //string absoluteImagePath = currentFolder.Path + "/" + mdImagePath;
                    //string uploadPath = "/autoUpload/" + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    //    + "/" + i + absoluteImagePath.Substring(absoluteImagePath.LastIndexOf('.'));
                    //try
                    //{
                    //    client.PutObject(Bucket, uploadPath, absoluteImagePath);
                    //    System.Diagnostics.Debug.WriteLine("位于" + absoluteImagePath + "的图片上传成功！");
                    //}
                    //catch
                    //{
                    //    System.Diagnostics.Debug.WriteLine("位于" + absoluteImagePath + "的图片上传失败！");
                    //}

                    //// 下载到本地
                    //await downloadImageUri(new Uri("https://" + Bucket + "." + EndPoint + uploadPath)
                    //    , await currentFolder.CreateFolderAsync(ProcessedImages, CreationCollisionOption.OpenIfExists));
                    //// 替换掉md文件中所有的图片链接

                    //// 替换掉html文件中所有的图片链接

                }
            }
        }

        // 今日头条html处理
        private static string toutiaoHtmlProcessing(string toutiaoHtml)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(toutiaoHtml);

            // 头条标题处理：h2和h1标签固定为h1，其余固定为h2
            // TODO：H1标签和H2标签提取时，有三种形式
            var headerNodes = doc.DocumentNode
                .SelectNodes("//h1 | //h2 | //h3 | //h4 | //h5 | //h6");
            if (headerNodes != null)
            {
                foreach (HtmlNode node in headerNodes)
                {
                    switch (node.Name)
                    {
                        case "h1":
                            node.Attributes.RemoveAll();
                            node.Attributes.Add("class", "pgc-h-arrow-right");
                            break;

                        case "h2":
                            node.Name = "h1";
                            node.Attributes.RemoveAll();
                            node.Attributes.Add("class", "pgc-h-arrow-right");
                            break;

                        default:
                            node.Name = "h2";
                            break;
                    }
                }
            }

            // 字体更改：粗体不变，斜体改成粗体，斜粗体改成粗体
            var italicNodes = doc.DocumentNode
                .SelectNodes("//em");
            if (italicNodes != null)
            {
                foreach (HtmlNode node in italicNodes)
                {
                    if (node.FirstChild.Name.Equals("strong"))
                    {
                        node.ParentNode.RemoveChild(node, true);
                    }
                    else
                    {
                        node.Name = "strong";
                    }
                }
            }

            // 分割线和多余字符处理：如果分割线在文章首尾，直接去掉，直到首尾没有分割线为止
            // 如果首尾是多余字符，也直接去掉，直到首尾没有多余字符为止
            do
            {
                HtmlNode parentNode = doc.DocumentNode
                .SelectSingleNode("//div");
                if (parentNode != null
                    && !parentNode.HasAttributes)
                {
                    if (parentNode.FirstChild.Name.Equals("hr")
                            || parentNode.FirstChild.Name.Equals("br"))
                    {
                        parentNode.FirstChild.Remove();
                        continue;
                    }

                    if (parentNode.LastChild.Name.Equals("hr")
                            || parentNode.LastChild.Name.Equals("br"))
                    {
                        parentNode.LastChild.Remove();
                        continue;
                    }
                    parentNode.InnerHtml = parentNode.InnerHtml.Trim('\r', '\n', '\t', ' ');
                    break;
                }
                else
                {
                    break;
                }
            } while (true);

            // 删除线处理：如果文章中有删除线，则删除删除线，并在文字后面加（划掉）
            var strikeThroughNodes = doc.DocumentNode
                .SelectNodes("//s");
            if (strikeThroughNodes != null)
            {
                foreach (HtmlNode node in strikeThroughNodes)
                {
                    node.Name = "strong";
                    node.InnerHtml = node.InnerHtml.Insert(node.InnerHtml.Length, "（划掉）");
                }
            }

            // 脚注处理：加脚注参考资料加高亮，脚注用个人主页链接，网站用括号括起来，并且下方有下划线，结尾有↩︎
            var footItemNodes = doc.DocumentNode.
            SelectNodes("//section[@class='footnotes']/ol[@class='footnotes-list']/li[@class='footnote-item']");
            ArrayList referenceWebList = new ArrayList();
            ArrayList referenceWordList = new ArrayList();
            if (footItemNodes != null)
            {
                foreach (HtmlNode footItemNode in footItemNodes)
                {
                    if (footItemNode.GetAttributeValue("id", "").StartsWith("fn")
                        && footItemNode.FirstChild.Name.Equals("p")
                        && !footItemNode.FirstChild.HasAttributes
                        && footItemNode.FirstChild.FirstChild.Name.Equals("a")
                        && !footItemNode.FirstChild.FirstChild.GetAttributeValue("href", "").Equals(""))
                    {
                        referenceWebList.Add(footItemNode.FirstChild.FirstChild.GetAttributeValue("href", ""));
                        referenceWordList.Add(footItemNode.FirstChild.FirstChild.InnerHtml);
                    }
                }
            }

            HtmlNode footNotesNode = doc.DocumentNode.SelectSingleNode("//section[@class='footnotes']");
            if (footNotesNode != null && referenceWebList != null && referenceWordList != null)
            {
                HtmlNode referenceTitleNode = doc.CreateElement("p");
                referenceTitleNode.Attributes.Add("class", "pgc-end-literature");
                HtmlNode referenceTitleStrongNode = doc.CreateElement("strong");
                referenceTitleStrongNode.Attributes.Add("class", "highlight-text");
                referenceTitleStrongNode.AppendChild(HtmlTextNode.CreateNode("参考资料："));
                referenceTitleNode.AppendChild(referenceTitleStrongNode);

                for (int i = 0; i < referenceWebList.Count; i++)
                {
                    HtmlNode referenceContentNode = doc.CreateElement("p");
                    referenceContentNode.Attributes.Add("class", "pgc-end-literature");
                    HtmlNode referenceContentLabelNode = doc.CreateElement("a");
                    referenceContentLabelNode.PrependChild(HtmlTextNode.CreateNode("[" + (i + 1) + "]"));
                    referenceContentLabelNode.Attributes
                        .Add("href", "https://www.toutiao.com/c/user/1543336195526035/#mid=1644840249419783");
                    referenceContentLabelNode.Attributes.Add("Deal", "");
                    referenceContentNode.AppendChild(referenceContentLabelNode);
                    referenceContentNode.InnerHtml = referenceContentNode.InnerHtml
                            .Insert(referenceContentNode.InnerHtml.Length
                            , "：" + referenceWordList[i]);

                    string referenceWeb = referenceWebList[i].ToString();
                    if (!referenceWeb.StartsWith("https://"))
                    {
                        if (!referenceWeb.StartsWith("http://"))
                        {
                            referenceWeb = referenceWeb.Insert(0, "https://");
                        }
                        else if (referenceWeb.Equals("http://www.toutiao.com")
                            || referenceWeb.StartsWith("http://www.toutiao.com/"))
                        {
                            referenceWeb = referenceWeb.Insert(4, "s");
                        }
                    }
                    if (referenceWeb.Equals("https://www.toutiao.com")
                        || referenceWeb.StartsWith("https://www.toutiao.com/"))
                    {
                        referenceContentNode.InnerHtml = referenceContentNode.InnerHtml
                            .Insert(referenceContentNode.InnerHtml.Length
                            , "（<a href=" + referenceWeb + " Deal=\"\"><u>" + referenceWeb + "</u></a>↩︎）");
                    }
                    else
                    {
                        referenceContentNode.InnerHtml = referenceContentNode.InnerHtml
                            .Insert(referenceContentNode.InnerHtml.Length
                            , "<br>（<u>" + referenceWeb + "</u>↩︎）");
                    }
                    footNotesNode.ParentNode.AppendChild(referenceContentNode);
                }
                footNotesNode.ParentNode.ReplaceChild(referenceTitleNode, footNotesNode);
            }


            // 上标处理：普通上标变成^的形式，脚注上标将标注变成链接，链接定点是个人主页
            var superScriptNodes = doc.DocumentNode
                .SelectNodes("//sup");
            if (superScriptNodes != null)
            {
                foreach (HtmlNode node in superScriptNodes)
                {
                    if (node.GetAttributeValue("class", "").Equals("footnote-ref")
                        && node.FirstChild.Name.Equals("a")
                        && node.FirstChild.GetAttributeValue("href", "").StartsWith("#fn")
                        && node.FirstChild.GetAttributeValue("id", "").StartsWith("fnref")
                        && node.FirstChild.FirstChild.Name.Equals("#text")
                        && node.FirstChild.FirstChild.InnerHtml.StartsWith("[")
                        && node.FirstChild.FirstChild.InnerHtml.EndsWith("]")
                        && !node.FirstChild.FirstChild.HasChildNodes)
                    {
                        node.FirstChild.Attributes.RemoveAll();
                        node.FirstChild.Attributes
                            .Add("href", "https://www.toutiao.com/c/user/1543336195526035/#mid=1644840249419783");
                        node.FirstChild.Attributes.Add("Deal", "");
                        node.ParentNode.RemoveChild(node, true);
                    }
                    else if (!node.HasAttributes)
                    {
                        node.InnerHtml = node.InnerHtml.Insert(0, "^(");
                        node.InnerHtml = node.InnerHtml.Insert(node.InnerHtml.Length, ")");
                        node.ParentNode.RemoveChild(node, true);
                    }
                }
            }

            // 下标处理：变成下划线代替
            var subScriptNodes = doc.DocumentNode
                .SelectNodes("//sub");
            if (subScriptNodes != null)
            {
                foreach (HtmlNode node in subScriptNodes)
                {
                    node.InnerHtml = node.InnerHtml.Insert(0, "_(");
                    node.InnerHtml = node.InnerHtml.Insert(node.InnerHtml.Length, ")");
                    node.ParentNode.RemoveChild(node, true);
                }
            }

            // 表情处理：暂不做处理
            // TODO:做成颜文字

            // 高亮文字处理：转化为编辑器支持的高亮
            var highLightNodes = doc.DocumentNode
                .SelectNodes("//mark");
            if (highLightNodes != null)
            {
                foreach (HtmlNode node in highLightNodes)
                {
                    node.Name = "strong";
                    node.Attributes.Add("class", "highlight-text");
                }
            }

            // 嵌套列表处理：同类嵌套时顺序改变，非同类嵌套时变为同类嵌套
            // 里面有引用嵌套的全部使用引号和文字代替，列表全部进行去回车整合
            var listNodes = doc.DocumentNode
                    .SelectNodes("//ol | //ul");
            if (listNodes != null)
            {
                foreach (HtmlNode node in listNodes)
                {
                    do
                    {
                        HtmlNode listQuoteNode = node.SelectSingleNode(".//blockquote");
                        if (listQuoteNode != null)
                        {
                            string temp = listQuoteNode.OuterHtml
                                .Replace("<blockquote>", "<br>“")
                                .Replace("</blockquote>", "”<br>")
                                .Replace("<p>", "").Replace("</p>", "");
                            temp = temp.Insert(0, "<div>");
                            temp = temp.Insert(temp.Length, "</div>");
                            listQuoteNode.ParentNode.InsertAfter(HtmlNode.CreateNode(temp), listQuoteNode);
                            listQuoteNode.Remove();
                        }
                        else
                        {
                            break;
                        }
                    } while (true);
                    node.InnerHtml = node.InnerHtml
                        .Replace("<div>", "")
                        .Replace("</div>", "")
                        .Replace("\n", "");
                }
            }

            do
            {
                HtmlNode listRepeatNode = doc.DocumentNode
                    .SelectSingleNode("//li/ol | //li/ul");
                if (listRepeatNode != null)
                {
                    if (listRepeatNode.Name.Equals(listRepeatNode.ParentNode.ParentNode.Name))
                    {
                        HtmlNode temp = HtmlNode.CreateNode(listRepeatNode.OuterHtml);
                        int n = listRepeatNode.ParentNode.GetAttributeValue("Num", 0);
                        if (n == 0)
                        {
                            listRepeatNode.ParentNode.Attributes.Add("Num", "0");
                        }
                        listRepeatNode.ParentNode.SetAttributeValue("Num", (n + 1).ToString());
                        HtmlNode insertIndexNode = listRepeatNode.ParentNode;
                        for (int i = 0; i < n; i++)
                        {
                            insertIndexNode = insertIndexNode.NextSibling;
                        }
                        listRepeatNode.ParentNode.ParentNode.InsertAfter(temp, insertIndexNode);
                        listRepeatNode.Remove();
                    }
                    else if (listRepeatNode.Name.Equals("ol"))
                    {
                        listRepeatNode.Name = "ul";
                        var checkNodes = listRepeatNode.SelectNodes(".//ol");
                        if (checkNodes != null)
                        {
                            foreach (HtmlNode checkNode in checkNodes)
                            {
                                checkNode.Name = "ul";
                            }
                        }
                    }
                    else
                    {
                        listRepeatNode.Name = "ol";
                        var checkNodes = listRepeatNode.SelectNodes(".//ul");
                        if (checkNodes != null)
                        {
                            foreach (HtmlNode checkNode in checkNodes)
                            {
                                checkNode.Name = "ol";
                            }
                        }
                    }
                }
                else
                {
                    break;
                }
            } while (true);

            var listDestroyNodes = doc.DocumentNode
                .SelectNodes("//li");
            if (listDestroyNodes != null)
            {
                foreach (HtmlNode node in listDestroyNodes)
                {
                    node.Attributes.RemoveAll();
                }
            }

            //区块嵌套处理1：用引号代替
            do
            {
                HtmlNode blockQuoteRepeatNode = doc.DocumentNode
                .SelectSingleNode("//blockquote/blockquote");
                if (blockQuoteRepeatNode != null)
                {
                    string temp = blockQuoteRepeatNode.OuterHtml
                        .Replace("<blockquote>", "<br>“")
                        .Replace("</blockquote>", "”<br>")
                        .Replace("<p>", "").Replace("</p>", "");
                    temp = temp.Insert(0, "<div>");
                    temp = temp.Insert(temp.Length, "</div>");
                    blockQuoteRepeatNode.ParentNode.InsertAfter(HtmlNode.CreateNode(temp), blockQuoteRepeatNode);
                    blockQuoteRepeatNode.Remove();
                }
                else
                {
                    break;
                }
            } while (true);

            // 区块引用处理：全部改成头条模式的区块引用，去除多余标签
            // TODO：三种模式安排上
            var blockQuoteNodes = doc.DocumentNode
            .SelectNodes("//blockquote");
            if (blockQuoteNodes != null)
            {
                foreach (HtmlNode node in blockQuoteNodes)
                {
                    node.SetAttributeValue("class", "pgc-blockquote-quote");
                    node.InnerHtml = node.InnerHtml
                        .Replace("<div>", "")
                        .Replace("</div>", "")
                        .Replace("<p>", "")
                        .Replace("</p>", "")
                        .Replace("\n", "");
                }
            }

            // 区块嵌套处理2：如果里面有列表，则劈开引用
            do
            {
                HtmlNode blockQuoteListNode = doc.DocumentNode
                    .SelectSingleNode("//blockquote/ol | //blockquote/ul");
                if (blockQuoteListNode != null)
                {
                    blockQuoteListNode.Attributes.Add("Pos", "");
                    HtmlNode ancestorNode = blockQuoteListNode.ParentNode.ParentNode;
                    HtmlNode parentNode = blockQuoteListNode.ParentNode;
                    string[] temp = parentNode.InnerHtml.Split(blockQuoteListNode.OuterHtml);
                    blockQuoteListNode.Attributes.Remove("Pos");
                    ancestorNode.InsertBefore(HtmlNode
                        .CreateNode("<blockquote class=\"pgc-blockquote-quote\">"
                        + temp[0] + "</blockquote>"), parentNode);
                    ancestorNode.InsertBefore(HtmlNode
                        .CreateNode(blockQuoteListNode.OuterHtml), parentNode);
                    ancestorNode.InsertBefore(HtmlNode
                        .CreateNode("<blockquote class=\"pgc-blockquote-quote\">"
                        + temp[1] + "</blockquote>"), parentNode);
                    parentNode.Remove();
                }
                else
                {
                    break;
                }
            } while (true);



            // 代码块处理：将单行代码块变成高亮
            // TODO：多行代码行到编辑器里会多一行
            var codeBlockNodes = doc.DocumentNode
                .SelectNodes("//code");
            if (codeBlockNodes != null)
            {
                foreach (HtmlNode node in codeBlockNodes)
                {
                    if (node.ParentNode.Name.Equals("pre")
                        && node.ParentNode.GetAttributeValue("data-role", "").Equals("codeBlock")
                        && !node.ParentNode.GetAttributeValue("data-info", "").Equals("")
                        && node.ParentNode.GetAttributeValue("class", "").StartsWith("language-"))
                    {
                        // TODO
                    }
                    else
                    {
                        node.Name = "strong";
                        node.Attributes.Add("class", "highlight-text");
                    }
                }
            }

            // 网址处理：如果是直接网址，有括号；如果不是，前面有字，后面括号
            // 特别地，如果是头条内部网址且是直接网址，既有括号也有链接
            var webLinkNodes = doc.DocumentNode
                .SelectNodes("//a[@href]");
            if (webLinkNodes != null)
            {
                foreach (HtmlNode node in webLinkNodes)
                {
                    if (!node.GetAttributeValue("Deal", " ").Equals(" "))
                    {
                        node.Attributes.Remove("Deal");
                        continue;
                    }
                    string webLink = node.GetAttributeValue("href", "");

                    // 判断是否是站内锚点
                    if (webLink.StartsWith("#"))
                    {
                        node.ParentNode.RemoveChild(node, true);
                        continue;
                    }

                    // 判断是否是http协议开头的头条网址
                    if (!webLink.StartsWith("https://"))
                    {
                        if (!webLink.StartsWith("http://"))
                        {
                            webLink = webLink.Insert(0, "https://");
                            node.SetAttributeValue("href", webLink);
                        }
                        else if (webLink.Equals("http://www.toutiao.com")
                            || webLink.StartsWith("http://www.toutiao.com/"))
                        {
                            webLink = webLink.Insert(4, "s");
                            node.SetAttributeValue("href", webLink);
                        }
                    }

                    // 如果链接最后一个节点是图像，则在图像备注上注明网址
                    if (node.LastChild != null
                        && node.LastChild.Name.Equals("img")
                        && !node.LastChild.GetAttributeValue("src", "").Equals(""))
                    {
                        node.LastChild.SetAttributeValue("alt",
                            node.LastChild.GetAttributeValue("alt", "") + "（"
                            + node.GetAttributeValue("href", "") + "↩︎）");
                        node.ParentNode.RemoveChild(node, true);
                        continue;
                    }


                    // 判断是否是头条内部网址
                    if (node.GetAttributeValue("href", "").Equals("https://www.toutiao.com")
                        || node.GetAttributeValue("href", "").StartsWith("https://www.toutiao.com/"))
                    {
                        // 判断是否是直接网址
                        if (node.InnerText.Equals(node.GetAttributeValue("href", ""))
                            || ("https://" + node.InnerText).Equals(node.GetAttributeValue("href", ""))
                            || (node.InnerText.Length >= 22
                            && node.InnerText.Insert(4, "s").Equals(node.GetAttributeValue("href", ""))))
                        {
                            HtmlNode temp = HtmlNode.CreateNode("（<u>" + node.InnerHtml + "</u>↩︎）");
                            node.RemoveAllChildren();
                            node.PrependChild(temp);
                        }
                    }
                    else
                    {
                        // 判断是否是直接网址
                        if (!node.InnerText.Equals(node.GetAttributeValue("href", ""))
                            && !("http://" + node.InnerText).Equals(node.GetAttributeValue("href", ""))
                            && !("https://" + node.InnerText).Equals(node.GetAttributeValue("href", "")))
                        {
                            node.ParentNode.InsertBefore(HtmlNode
                                .CreateNode("<div>" + node.InnerHtml + "</div>"), node);
                            node.RemoveAllChildren();
                            node.InnerHtml = node.GetAttributeValue("href", "");
                        }
                        node.Name = "u";
                        node.Attributes.RemoveAll();
                        node.ParentNode.InsertBefore(HtmlTextNode.CreateNode("（"), node);
                        node.ParentNode.InsertAfter(HtmlTextNode.CreateNode("↩︎）"), node);
                    }
                }
            }
            HtmlNode rootNode = doc.DocumentNode.SelectSingleNode("div");
            if (rootNode != null && rootNode.ParentNode.Name.Equals("#document"))
            {
                rootNode.InnerHtml = rootNode.InnerHtml
                .Replace("<div>", "").Replace("</div>", "");
            }

            // 图像处理：换成编辑器自己的图像处理节点
            var imageNodes = doc.DocumentNode
                .SelectNodes("//img[@src and @alt]");
            if (imageNodes != null)
            {
                foreach (HtmlNode node in imageNodes)
                {
                    HtmlNode temp = HtmlNode
                        .CreateNode("<div class=\"pgc-img\"><img src=\""
                        + node.GetAttributeValue("src", "")
                        + "\"><p class=\"pgc-img-caption\">"
                        + node.GetAttributeValue("alt", "")
                        + "</p></div>");
                    node.ParentNode.ReplaceChild(temp, node);
                }
            }

            StringWriter writer = new StringWriter();
            doc.Save(writer);

            return (writer.ToString());

            // 音频处理：暂不做处理
            // TODO：做有可能的支持

            // 视频处理：暂不做处理
            // TODO：做有可能的支持

            // 商品推广处理：暂不做处理
            // TODO：做有可能的支持

            // 投票处理：暂不做处理
            // TODO：做有可能的支持

            // 小程序处理：暂不做处理
            // TODO：做有可能的支持

            // 在线网页处理：暂不做处理
            // TODO：在线给已外链添加<u></u>
        }

        // 新建文件
        private void button_new_Click(object sender, RoutedEventArgs e)
        {

        }

        // 初始化网格视图
        private async void grid_files_LoadedAsync(object sender, RoutedEventArgs e)
        {
            StorageFolder root = ApplicationData.Current.RoamingFolder;
            StorageFolder papersFolder = await
                root.CreateFolderAsync(Root, CreationCollisionOption.OpenIfExists);
            addGridElementAsync(Root);
        }

        // 在网格视图上加载
        private async void addGridElementAsync(string uri)
        {
            // 去除所有网格子窗口
            grid_files.Children.Clear();

            // 路径文字变化
            textBox_path.Text = uri;

            // 获取文件夹路径
            StorageFolder currentFolder = await getFolderFromUri(uri);
            if (currentFolder == null)
            {
                addGridElementAsync(Root);
            }

            // 获取所有文件夹里的项目
            var items = await currentFolder.GetItemsAsync();

            // 文件夹项目里行与列定义
            int row = 0;
            int column = 0;

            // 文件夹项目类型与图标绑定
            foreach (IStorageItem item in items)
            {
                string fileType = "default";
                Image image = new Image();
                if (item.IsOfType(StorageItemTypes.Folder))
                {
                    if ((await (item as StorageFolder).GetItemsAsync()).Count != 0)
                    {
                        fileType = "folder";
                    }
                    else
                    {
                        fileType = "folder_null";
                    }

                    // 图标加入双击事件绑定，打开文件夹
                    image.DoubleTapped += (sender, e) =>
                    {
                        addGridElementAsync(uri + "/" + item.Name);
                    };
                }
                else
                {
                    StorageFile file = item as StorageFile;
                    fileType = file.FileType.Substring(1);

                    // 图标加入双击事件绑定，打开相关文件
                    image.DoubleTapped += async (sender, e) =>
                    {
                        if (Array.Exists<string>(SupportImageTypes, s => s.Equals("." + fileType)))
                        {
                            try
                            {
                                webView_viewer.NavigateToString(
                                    "<img src =\"data:image/" + fileType
                                    + ";base64," + await toBase64(file) + "\" width=\"400\">");
                            }
                            catch
                            {
                                webView_viewer.NavigateToString("<p>图像打开失败！</p>");
                                System.Diagnostics.Debug.WriteLine(
                                    "图像" + file.Name + "打开失败，它的路径为：" + file.Path);
                            }
                        }
                        else
                        {
                            if (fileType.Equals("md"))
                            {
                                // 存储文件字段
                                TheOpenedFile.File = file;
                            }
                            else if (fileType.Equals("html"))
                            {
                                // 存储文件字段
                                TheOpenedFile.File = file;

                                // 显示打开的html文件
                                string preHtml = await Windows.Storage.FileIO.ReadTextAsync(file);
                                webView_viewer.NavigateToString(preHtml);

                                // html通用预处理
                                TheOpenedFile.Html = htmlPreprocessing(preHtml);
                            }

                            try
                            {
                                webView_viewer.NavigateToString(await
                                        Windows.Storage.FileIO.ReadTextAsync(file));
                            }
                            catch
                            {
                                System.Diagnostics.Debug.WriteLine(
                                    "文件" + file.Name + "打开失败，它的路径为：" + file.Path);
                            }
                        }
                    };
                }
                Uri ico_uri = new Uri("ms-appx:///Assets/icons/" + fileType + ".ico");
                image.Source = new BitmapImage(ico_uri);

                // 验证图标文件是否存在，不存在则加载默认图标
                try
                {
                    await StorageFile.GetFileFromApplicationUriAsync(ico_uri);
                }
                catch (FileNotFoundException fnfe)
                {
                    image.Source = new BitmapImage(new Uri("ms-appx:///Assets/icons/default.ico"));
                    System.Diagnostics.Debug.WriteLine(fnfe);
                    System.Diagnostics.Debug.WriteLine(fileType + "格式的文件丢失图标支持！");
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e);
                }
                finally
                {
                    // 文件名称控件
                    TextBlock textBlock = new TextBlock()
                    {
                        Text = item.Name,
                        FontSize = 10,
                        TextWrapping = TextWrapping.Wrap,
                        TextAlignment = TextAlignment.Center
                    };

                    // ListBox装载以上两个控件
                    ListBox listBox = new ListBox()
                    {
                        Width = 120,
                        Height = 160
                    };
                    listBox.Items.Add(image);
                    listBox.Items.Add(textBlock);

                    // 设置在Grid中的行列排布
                    Grid.SetRow(listBox, row);
                    Grid.SetColumn(listBox, column);
                    grid_files.Children.Add(listBox);
                    if ((column += 1) > 3)
                    {
                        row += 1;
                        column = 0;
                    }
                }
            }
        }

        // 按URL获取当前路径文件夹，起始文件夹为起始远程文件夹
        private async Task<StorageFolder> getFolderFromUri(string uri)
        {
            // 获取根目录
            StorageFolder root = ApplicationData.Current.RoamingFolder;
            if (uri == null)
            {
                addGridElementAsync(Root);
                return await root.GetFolderAsync(Root);
            }

            // 获取文件夹
            string[] pathes = uri.Split('/');
            try
            {
                foreach (string path in pathes)
                {
                    root = await root.GetFolderAsync(path);
                }
                return root;
            }
            catch (FileNotFoundException fnfe)
            {
                System.Diagnostics.Debug.WriteLine(fnfe);
                System.Diagnostics.Debug.WriteLine("找不到文件路径！");
                addGridElementAsync(Root);
                return null;
            }
            catch (ArgumentException ae)
            {
                System.Diagnostics.Debug.WriteLine(ae);
                System.Diagnostics.Debug.WriteLine("路径格式错误！");
                addGridElementAsync(Root);
                return null;
            }
        }

        // 按URL获取当前路径文件夹，给出当前起始文件夹
        private async Task<StorageFolder> getFolderFromUri(StorageFolder currentFolder, string uri)
        {
            // 判断条件
            if (currentFolder == null)
            {
                return await getFolderFromUri(uri);
            }
            if (uri == null)
            {
                addGridElementAsync(Root);
                return await getFolderFromUri(Root);
            }

            // 获取文件夹
            string[] pathes = uri.Split('/');
            StorageFolder folder = currentFolder;
            try
            {
                foreach (string path in pathes)
                {
                    folder = await folder.GetFolderAsync(path);
                }
                return folder;
            }
            catch (FileNotFoundException fnfe)
            {
                System.Diagnostics.Debug.WriteLine(fnfe);
                System.Diagnostics.Debug.WriteLine("找不到文件路径！");
                addGridElementAsync(Root);
                return null;
            }
            catch (ArgumentException ae)
            {
                System.Diagnostics.Debug.WriteLine(ae);
                System.Diagnostics.Debug.WriteLine("路径格式错误！");
                addGridElementAsync(Root);
                return null;
            }
        }

        // 按URL获取当前路径文件，起始文件夹为起始远程文件夹
        private async Task<StorageFile> getFileFromUri(string uri)
        {
            // 得到上一级文件夹目录
            StorageFolder currentFolder;
            if (uri.Contains("/"))
            {
                currentFolder = await getFolderFromUri(uri.Substring(0, uri.LastIndexOf("/")));
                uri = uri.Substring(uri.LastIndexOf("/") + 1);
            }
            else
            {
                currentFolder = ApplicationData.Current.RoamingFolder;
            }
            if (currentFolder == null)
            {
                return null;
            }

            // 尝试得到文件
            try
            {
                return await currentFolder.GetFileAsync(uri);
            }
            catch (FileNotFoundException fnfe)
            {
                System.Diagnostics.Debug.WriteLine(fnfe);
                System.Diagnostics.Debug.WriteLine("找不到文件路径！");
                return null;
            }
            catch (ArgumentException ae)
            {
                System.Diagnostics.Debug.WriteLine(ae);
                System.Diagnostics.Debug.WriteLine("路径格式错误！");
                return null;
            }
        }

        // 按URL获取当前路径文件，给出当前起始文件夹
        private async Task<StorageFile> getFileFromUri(StorageFolder currentFolder, string uri)
        {
            // 得到上一级文件夹目录
            if (uri.Contains("/"))
            {
                currentFolder = await getFolderFromUri(currentFolder, uri.Substring(0, uri.LastIndexOf("/")));
                uri = uri.Substring(uri.LastIndexOf("/") + 1);
            }
            if (currentFolder == null)
            {
                return await getFileFromUri(uri);
            }

            // 尝试得到文件
            try
            {
                return await currentFolder.GetFileAsync(uri);
            }
            catch (FileNotFoundException fnfe)
            {
                System.Diagnostics.Debug.WriteLine(fnfe);
                System.Diagnostics.Debug.WriteLine("找不到文件路径！");
                return null;
            }
            catch (ArgumentException ae)
            {
                System.Diagnostics.Debug.WriteLine(ae);
                System.Diagnostics.Debug.WriteLine("路径格式错误！");
                return null;
            }
        }

        // 将图像加载进webview而进行Base64编码
        private async Task<String> toBase64(StorageFile image)
        {
            ImageProperties properties = await image.Properties.GetImagePropertiesAsync();
            WriteableBitmap bitmap = new WriteableBitmap((int)properties.Width, (int)properties.Height);
            bitmap.SetSource(await image.OpenReadAsync());

            byte[] imageBytes = bitmap.PixelBuffer.ToArray();
            uint height = (uint)bitmap.PixelWidth;
            uint width = (uint)bitmap.PixelHeight;
            double dpiX = 96;
            double dpiY = 96;

            using (var encoded = new InMemoryRandomAccessStream())
            {
                BitmapEncoder encoder = await BitmapEncoder
                    .CreateAsync(BitmapEncoder.PngEncoderId, encoded);
                encoder.SetPixelData(BitmapPixelFormat.Bgra8,
                    BitmapAlphaMode.Straight, height, width, dpiX, dpiY, imageBytes);
                await encoder.FlushAsync();
                encoded.Seek(0);

                byte[] bytes = new byte[encoded.Size];
                await encoded.AsStream().ReadAsync(bytes, 0, bytes.Length);

                return Convert.ToBase64String(bytes);
            }
        }

        // 点击返回按钮
        private void button_return_Click(object sender, RoutedEventArgs e)
        {
            string path = textBox_path.Text;
            if (path != null)
            {
                int index = path.LastIndexOf("/");
                if (index > 0)
                {
                    addGridElementAsync(path.Remove(index));
                }
                else
                {
                    addGridElementAsync(Root);
                }
            }
        }

        // 点击剪贴板监管按钮
        private void toggleSwitch_spy_Toggled(object sender, RoutedEventArgs e)
        {
            if (toggleSwitch_spy.IsOn)
            {
                Clipboard.ContentChanged += clipboard_ContentChangedAsync;
            }
            else
            {
                Clipboard.ContentChanged -= clipboard_ContentChangedAsync;
            }
        }

        // 剪贴板监控按钮事件
        private async void clipboard_ContentChangedAsync(object sender, object e)
        {
            // 获取剪贴板内容
            DataPackage dataPackage = new DataPackage();
            DataPackageView dataPackageView = Clipboard.GetContent();

            // 获取当前路径文件夹和存放图像文件夹
            StorageFolder currentFolder = await getFolderFromUri(textBox_path.Text);
            if (currentFolder == null)
            {
                return;
            }
            StorageFolder originalImagesFolder = await currentFolder
                .CreateFolderAsync(OriginalImages, CreationCollisionOption.OpenIfExists);
            StorageFolder processedImagesFolder = await currentFolder
                .CreateFolderAsync(ProcessedImages, CreationCollisionOption.OpenIfExists);

            // Markdown语句
            string markdown = string.Empty;

            // 如果剪贴板内容是图像
            if (dataPackageView.Contains(StandardDataFormats.Bitmap))
            {
                // 如果图像保存不成功，退出
                RandomAccessStreamReference imageView = null;
                try
                {
                    imageView = await dataPackageView.GetBitmapAsync();
                }
                catch
                {
                    System.Diagnostics.Debug.WriteLine("剪贴板图像保存出错！");
                    return;
                }

                // 如果图像为意外为空，退出
                if (imageView == null)
                {
                    return;
                }

                // 将图像流复制到文件流中，另存为图像
                StorageFile imageFile = await originalImagesFolder
                    .CreateFileAsync((await obtainImageNameAsync(originalImagesFolder)) + ".bmp"
                    , CreationCollisionOption.GenerateUniqueName);
                try
                {
                    using (var imageRead = await imageView.OpenReadAsync())
                    {
                        using (var imageWrite = await imageFile.OpenAsync(FileAccessMode.ReadWrite))
                        {
                            await imageRead.AsStreamForRead().CopyToAsync(imageWrite.AsStreamForWrite());
                            imageWrite.Seek(0);
                        }
                        imageRead.Seek(0);
                    }
                }
                catch
                {
                    System.Diagnostics.Debug.WriteLine("图像流保存出错！位置在：" + imageFile.Path);
                    return;
                }

                // 设置成原本图像扩展名，如果失败，直接返回
                if ((await setImageRealExtension(imageFile)) == null)
                {
                    return;
                }

                // 工厂化处理图像文件，如果失败，直接返回
                imageFile = await factoryImageProcess(imageFile, processedImagesFolder);
                if (imageFile == null)
                {
                    return;
                }

                // 获取Markdown语句
                markdown = "![](" + processedImagesFolder.Name + "/" + imageFile.Name + ")\n";
            }

            // 如果剪贴板内容是Html
            else if (dataPackageView.Contains(StandardDataFormats.Html))
            {
                // 如果html意外为空，退出
                string html = await dataPackageView.GetHtmlFormatAsync();
                if (html == null)
                {
                    return;
                }

                // 选择html中所有图像并下载
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(html);
                var imageNodes = doc.DocumentNode.SelectNodes("//img");
                if (imageNodes == null)
                {
                    return;
                }
                else
                {
                    foreach (HtmlNode node in imageNodes)
                    {
                        string imageUri = node.GetAttributeValue("src", "");

                        // 下载图像url，如果失败，直接返回
                        StorageFile imageFile = await downloadImageUri(new Uri(imageUri), originalImagesFolder);
                        if (imageFile == null)
                        {
                            return;
                        }

                        // 工厂化处理图像文件，如果失败，直接返回
                        imageFile = await factoryImageProcess(imageFile, processedImagesFolder);
                        if (imageFile == null)
                        {
                            return;
                        }

                        // 获取Markdown语句
                        markdown += "![](" + processedImagesFolder.Name + "/" + imageFile.Name + ")\n";
                    }
                }
            }

            // 如果剪贴板内容是字符串
            // BUG:在谷歌浏览器搜索框复制图像url的时候，会下载两次
            else if (dataPackageView.Contains(StandardDataFormats.Text))
            {
                // 由于设置文本后，会再一次触发扳机，所以加入尝试语句
                string text = null;
                try
                {
                    text = await dataPackageView.GetTextAsync();
                }
                catch
                {
                    return;
                }

                // 如果是图像url则下载到本地，如果不是直接退出
                if (text != null && (text.StartsWith("http://") || text.StartsWith("https://")))
                {
                    // 下载图像url，如果失败，直接返回
                    StorageFile imageFile = await downloadImageUri(new Uri(text), originalImagesFolder);
                    if (imageFile == null)
                    {
                        return;
                    }

                    // 工厂化处理图像文件，如果失败，直接返回
                    imageFile = await factoryImageProcess(imageFile, processedImagesFolder);
                    if (imageFile == null)
                    {
                        return;
                    }

                    // 获取Markdown语句
                    markdown = "![](" + processedImagesFolder.Name + "/" + imageFile.Name + ")\n";
                }
                else
                {
                    return;
                }
            }

            // 如果剪贴板内容是文件夹和文件
            else if (dataPackageView.Contains(StandardDataFormats.StorageItems))
            {
                // 如果文件复制不成功，退出
                IReadOnlyList<IStorageItem> items = null;
                try
                {
                    items = await dataPackageView.GetStorageItemsAsync();
                }
                catch
                {
                    System.Diagnostics.Debug.WriteLine("文件复制失败");
                    return;
                }

                // 如果文件因为意外为空，退出
                if (items == null)
                {
                    return;
                }

                // 判断每个文件是否都是图像文件，如果是，则全部复制，不是则直接退出
                foreach (IStorageItem storageItem in items)
                {
                    if (!storageItem.IsOfType(StorageItemTypes.File)
                        || !Array.Exists<string>(SupportImageTypes
                        , s => s.Equals((storageItem as StorageFile).FileType)))
                    {
                        return;
                    }
                }
                foreach (IStorageItem storageItem in items)
                {
                    StorageFile imageFile = storageItem as StorageFile;
                    StorageFile imageFileCopy = await imageFile.CopyAsync(originalImagesFolder
                        , (await obtainImageNameAsync(originalImagesFolder)) + imageFile.FileType
                        , NameCollisionOption.GenerateUniqueName);

                    // 设置成原本图像扩展名，如果失败，直接返回
                    if ((await setImageRealExtension(imageFileCopy)) == null)
                    {
                        return;
                    }

                    // 工厂化处理图像文件，如果失败，直接返回
                    imageFileCopy = await factoryImageProcess(imageFileCopy, processedImagesFolder);
                    if (imageFileCopy == null)
                    {
                        return;
                    }

                    // 获取Markdown语句
                    markdown += "![](" + processedImagesFolder.Name + "/" + imageFileCopy.Name + ")\n";
                }
            }

            // 刷新当前文件夹视图
            addGridElementAsync(textBox_path.Text);

            // 设置Markdown文本
            dataPackage.SetText(markdown);
            bool success = false;
            while (!success)
            {
                try
                {
                    Clipboard.Clear();
                    Clipboard.SetContent(dataPackage);
                    Clipboard.Flush();
                    success = true;
                }
                catch
                {
                }
            }
        }

        // 在当前文件夹下获取一个合适的图像名称
        private async Task<string> obtainImageNameAsync(StorageFolder currentFolder)
        {
            // 获取当前文件夹所有文件名称
            var cureentfiles = await currentFolder.GetFilesAsync();
            ArrayList cureentNames = new ArrayList();
            foreach (StorageFile file in cureentfiles)
            {
                if (file.Name.Contains("."))
                {
                    cureentNames.Add(file.Name.Substring(0, file.Name.LastIndexOf('.')));
                }
            }

            // 获取新的图像命名
            int imageName = 1;
            while (true)
            {
                if (cureentNames.Contains(imageName.ToString()))
                {
                    imageName++;
                }
                else
                {
                    break;
                }
            }

            return imageName.ToString();
        }

        // 获取图像流格式
        private string getImageStreamType(Stream imageStream)
        {
            string headCode = string.Empty;
            headCode += imageStream.ReadByte().ToString();
            headCode += imageStream.ReadByte().ToString();
            string imageType = null;
            ImageHeadCode.TryGetValue(headCode, out imageType);
            return imageType;
        }

        // 设置图像文件为它的真正格式
        private async Task<string> setImageRealExtension(StorageFile image)
        {
            // 获取图像文件真正格式
            string realExtension = null;
            try
            {
                using (var imageStream = await image.OpenStreamForReadAsync())
                {
                    realExtension = getImageStreamType(imageStream);
                    imageStream.Close();
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e);
                System.Diagnostics.Debug.WriteLine("获取图像格式失败！图像的路径为："
                    + image.Path);
                return null;
            }

            // 如果判断不为图像，删除原图像，返回null
            if (realExtension == null)
            {
                System.Diagnostics.Debug.WriteLine("不支持的图像文件头！图像的路径为："
                    + image.Path);
                await image.DeleteAsync();
                return null;
            }

            // 将原图像重命名至真实后缀
            string imageName = image.Name.Substring(0, image.Name.LastIndexOf('.'));
            string originalExtension = image.Name.Substring(image.Name.LastIndexOf('.'));
            if (!originalExtension.Equals(realExtension))
            {
                await image.RenameAsync(imageName + realExtension, NameCollisionOption.GenerateUniqueName);
            }
            return realExtension;
        }

        // 工厂化图像处理文件
        private async Task<StorageFile> factoryImageProcess(StorageFile image, StorageFolder folder)
        {
            // 创建解码流
            SoftwareBitmap softwareBitmap;
            using (var streamDecoder = await image.OpenAsync(FileAccessMode.Read))
            {
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(streamDecoder);
                softwareBitmap = await decoder.GetSoftwareBitmapAsync();
            }

            // 创建输出图像
            StorageFile outputImage = await folder
                    .CreateFileAsync(image.Name.Substring(0, image.Name.LastIndexOf('.')) + ".gif"
                    , CreationCollisionOption.GenerateUniqueName);

            // 创建编码流
            using (var streamEncoder = await outputImage.OpenAsync(FileAccessMode.ReadWrite))
            {
                BitmapEncoder encoder = await BitmapEncoder
                    .CreateAsync(BitmapEncoder.GifEncoderId, streamEncoder);
                encoder.SetSoftwareBitmap(softwareBitmap);

                // 将图像固定为1920宽度，并使用Cubic插值（不一定必须）
                //encoder.BitmapTransform.ScaledWidth = 1920;
                //encoder.BitmapTransform.ScaledHeight = (uint)(Math.Round(
                //    ((double)softwareBitmap.PixelHeight / (double)softwareBitmap.PixelWidth) * (double)1920));
                //encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Cubic;

                // 尝试写入编码流
                try
                {
                    await encoder.FlushAsync();
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e);
                    System.Diagnostics.Debug.WriteLine("创建工厂化图像格式失败！目标图像的路径为：" + image.Path);
                    return null;
                }
            }
            return outputImage;
        }

        // 下载图像Uri到本地
        private async Task<StorageFile> downloadImageUri(Uri uri, StorageFolder folder)
        {

            // 验证url是否可连接
            WebResponse webResponse = null;
            try
            {
                HttpWebRequest webRequest = HttpWebRequest.Create(uri) as HttpWebRequest;
                webResponse = webRequest.GetResponse();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e);
                System.Diagnostics.Debug.WriteLine("Uri格式不可读！Uri为：" + uri.ToString());
                return null;
            }
            if (webResponse == null)
            {
                System.Diagnostics.Debug.WriteLine("远程文件响应失败！Uri为：" + uri.ToString());
                return null;
            }

            // 验证url是否为支持的远程图片格式
            string imageType = null;
            try
            {
                using (Stream imageStream = webResponse.GetResponseStream())
                {
                    imageType = getImageStreamType(imageStream);
                    imageStream.Close();
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e);
                System.Diagnostics.Debug.WriteLine("获取远程文件流失败！Uri为：" + uri.ToString());
                return null;
            }
            if (imageType == null)
            {
                System.Diagnostics.Debug.WriteLine("远程文件流格式不受支持!Uri为：" + uri.ToString());
                return null;
            }

            // 下载远程文件
            StorageFile imageFile = await folder
                .CreateFileAsync((await obtainImageNameAsync(folder)) + imageType
                , CreationCollisionOption.GenerateUniqueName);
            BackgroundDownloader downloader = new BackgroundDownloader();
            DownloadOperation download = downloader.CreateDownload(uri, imageFile);
            await download.StartAsync();
            return imageFile;
        }

        // 点击刷新按钮刷新文件夹界面
        private void button_flush_Click(object sender, RoutedEventArgs e)
        {
            addGridElementAsync(textBox_path.Text);
        }

        // 点击文件夹按钮打开当前文件夹对应的资源管理器目录
        private async void button_openPath_ClickAsync(object sender, RoutedEventArgs e)
        {
            StorageFolder currentFolder = await getFolderFromUri(textBox_path.Text);
            if (currentFolder == null)
            {
                await Launcher.LaunchFolderAsync(await getFolderFromUri(Root));
            }
            else
            {
                await Launcher.LaunchFolderAsync(currentFolder);
            }
        }
    }

    // 当前打开的文件
    public class OpenedFile
    {
        // 通用字段
        public StorageFile File { get; internal set; }

        // html字段
        // 文章标题
        public string Title { get; internal set; }

        // 原始文章主体
        public string Html { get; internal set; }

        // 今日头条文章主体
        public string ToutiaoHtml { get; internal set; }

        // md字段
    }
}
