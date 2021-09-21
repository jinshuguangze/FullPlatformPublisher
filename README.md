# FullPlatformPublisher

> 运营多平台自媒体账号时，每个平台对于图文的格式不一致，而且很多平台无法直接复制带有图片的html代码或者解析word文档，所以大量自媒体工作者不得不花费数小时时间在无意义的文字上传和编辑工作上。
此桌面端系统能很好的解决这个问题，不同于传统的txt文本，它的底层逻辑是基于markdown-html线的，使用markdown来轻松撰写文章并将它无损转换成适配于网页端的html代码，使用此桌面端系统自动将这些html代码解析并重排成各大自媒体平台的格式，并从网页发布器端直接内嵌进去，省去了编辑的步骤。
除此以外，图片的处理也是全自动化的，在使用截图工具（微信/QQ/自带的截图键）后，程序会检测到粘贴板图片信息的输入，并将它的流信息加上可自定义的水印并直接存储为本地图片，并将截图板里保存的信息自动替换为本地图片相对路径的markdown代码，当点击同步按钮后，这些本地图片能直接上传到图床并自动替换掉markdown里的本地路径，最后在自媒体平台看到的图片就是基于图床的外链，整个过程都是全自动的并且高度可自定义。
 
- 使用C#的UWP技术编写的桌面端系统，拥有优雅简明的UI，能够进行本地层次式数据库和文章发布前预览，使用本发布器实际应用于众多文章，比起传统的多平台手动发布与截图上传，用时仅为5%以下。
- 可将Markdown格式书写的本地文章一键发布到媒体平台上，包括腾讯号、今日头条、百家号、小黑盒、B站等，并重排列这些网站自带的Html标签使之正常生效，可处理列表、引用、图片之间的多层嵌套。
- 可监视粘贴板，随时将新增截图保存于本地层次式数据库中并返回图片Markdown引用，发布文章时上传到在线图床中并添加logo与引用文字，自动将文章中的本地图片引用替换成在线图片。

![文章发布器.png](https://i.loli.net/2021/09/22/AHLXVCoc2g9Fer7.png)

支持Markdown语法列表：
- 标题
- 分割线
- 删除线
- 脚注
- 上标
- 下标
- 高亮
- 列表
- 列表嵌套
- 引用
- 引用嵌套
- 代码块
- 网址引用
- 图像引用
