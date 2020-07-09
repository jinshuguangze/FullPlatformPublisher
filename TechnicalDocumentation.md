# 今日头条技术编辑器页面文档<!-- omit in toc --> 

---

- [Markdown标题相关](#markdown标题相关)
- [Markdown段落字体相关](#markdown段落字体相关)
- [Markdown列表](#markdown列表)
- [Markdown区块引用](#markdown区块引用)
- [Markdown代码](#markdown代码)
- [Markdown链接](#markdown链接)
- [Markdown图片](#markdown图片)
- [Markdown表格](#markdown表格)
- [Markdown高级技巧](#markdown高级技巧)
- [头条编辑器额外支持](#头条编辑器额外支持)

## Markdown标题相关
只存在H1标题，但是H2标题也能正常显示。
Markdown里面的H2变成编辑器里面的H1，Markdown里面的H3到H6变成编辑器里面的H2。
H1标题可分为四种情况：
![mark](http://evaluations.oss-cn-hangzhou.aliyuncs.com/common/20200204/225636162.png)
![mark](http://evaluations.oss-cn-hangzhou.aliyuncs.com/common/20200204/225642265.png)
![mark](http://evaluations.oss-cn-hangzhou.aliyuncs.com/common/20200204/225711420.png)
![mark](http://evaluations.oss-cn-hangzhou.aliyuncs.com/common/20200204/225717003.png)
**目标：暂时只做第一种的适配**


## Markdown段落字体相关
1. **段落**
支持换行`<br>`和换段落`<p></p>`两种形式，在文章中记得应用，头条自己编辑器不支持只换行，但可以显示。

2. **字体**
只存在粗体`<strong></strong>`，不存在斜体。
**Markdown粗体->编辑器粗体：**
`**的你打偶啊**`
![mark](http://evaluations.oss-cn-hangzhou.aliyuncs.com/common/20200205/165527990.png)
**Markdown斜体->编辑器粗体：**
`*的会两年多偶爱你啊*`
同上
**Markdown斜粗体->编辑器粗体：**
`***的会两年多偶爱你啊***`
同上

3. **分割线**
支持分割线，记得常用。
分割线如果在正文的头或者尾，直接去掉。

4. **删除线**
不支持删除线。
编辑器里加粗后改为在删除线文字后面自动加上`（划掉）`。

5. **下划线**
Markdown本身不支持下划线`<u></u>`，但是编辑器里面支持，所以缺失。
已经用作网址支持。

6. **脚注**
编辑器不支持站内锚点，也不支持站外链接。
取消所有上角标，改成高亮，而注释改成：
![mark](http://evaluations.oss-cn-hangzhou.aliyuncs.com/common/20200205/153514538.png)
平时写文章的时候，应该是：
```markdown
balavbal[^1]，大部队阿布迪阿布都[^2]
[^1]:[balbavbal的维基百科，xxx环节](www.baidu.com)
[^2]:[《丹迪安迪哦按单》@骑士优格，第二段](www.toutiao.com)
```
在检测时，如果是头条站内链接，则加上站内链接
![mark](http://evaluations.oss-cn-hangzhou.aliyuncs.com/common/20200205/003103363.png)
**目标：暂时不检测站内链接**

7. **上标**
编辑器不支持，比如：
5^2^改成5^2
**目标：暂时不修改**

8. **下标**
编辑器不支持，比如：
H~2~O改成H_2_O
**目标：暂时不修改**

9. **颜文字**
编辑器支持，但是不显示，可以考虑转成颜文字，比如：
:smile:改成:D
**目标：暂时不修改**

10. **高亮文字**
编辑器不支持，转化为编辑器支持的高亮：
将==mark==的html代码`<p><mark>marked</mark></p>`转化为支持的高亮:
![mark](http://evaluations.oss-cn-hangzhou.aliyuncs.com/common/20200206/153151045.png)

## Markdown列表
1. **无序列表**
支持，头条支持多个无序列表镶嵌，更改方式与下同：
**目标：暂时不调整**

2. **有序列表**
支持，头条支持多个有序列表镶嵌，但要做如下更改：
从：
![mark](http://evaluations.oss-cn-hangzhou.aliyuncs.com/common/20200205/022015598.png)
改成
![mark](http://evaluations.oss-cn-hangzhou.aliyuncs.com/common/20200205/022032138.png)
**目标：暂时不调整**

3. **混合列表**
编辑器不支持混合列表（无论是有序还是无序在外面），但是Markdown支持。遇到时可以转化为普通数字镶嵌，非列表式。
**目标：暂时不实现**

## Markdown区块引用
1. **区块引用**
完全兼容。
![mark](http://evaluations.oss-cn-hangzhou.aliyuncs.com/common/20200205/024810584.png)
![mark](http://evaluations.oss-cn-hangzhou.aliyuncs.com/common/20200205/024905370.png)
![mark](http://evaluations.oss-cn-hangzhou.aliyuncs.com/common/20200205/024847201.png)
同样有三种模式：
![mark](http://evaluations.oss-cn-hangzhou.aliyuncs.com/common/20200205/024948119.png)
![mark](http://evaluations.oss-cn-hangzhou.aliyuncs.com/common/20200205/025008843.png)
![mark](http://evaluations.oss-cn-hangzhou.aliyuncs.com/common/20200205/025029085.png)
**目标：暂时不做修改**

2. **区块嵌套**
编辑器不支持嵌套。
**AVA：区块嵌套**
**目标：暂时不做适配**

3. **区块列表嵌套**
编辑器不支持嵌套。
**AVA：区块列表嵌套**
**目标：暂时不做适配**

## Markdown代码
1. **代码片段**
编辑器不支持，将`<code></code>`改为高亮。
比如`JAVA`改为==JAVA==
更改后有一些细节表现不一样。
**目标：细节不一致的暂时不做处理**

2. **代码区块**
编辑器支持，但支持完毕后会多出一行，待修复。
![mark](http://evaluations.oss-cn-hangzhou.aliyuncs.com/common/20200205/102044743.png)
**目标：暂时不做处理**

## Markdown链接
1. **单纯网址**
`www.baidu.com`
编辑器支持，但是不支持发表，故转化为其他形式（下划线）。
![mark](http://evaluations.oss-cn-hangzhou.aliyuncs.com/common/20200205/153730471.png)

2. **尖括号网址**
`<http://www.baidu.com>`
与上同。

3. **名称链接**
`[百度](www.baidu.com)`
同上，改为其他形式（下划线+高亮）
![mark](http://evaluations.oss-cn-hangzhou.aliyuncs.com/common/20200206/153024925.png)
最开始表格不用处理

4. **高级名称链接**
```
[Google][1]
[1]: www.google.com/
```
同上。

## Markdown图片

支持，改为即可
```html
<div class="pgc-img"><img src="http://evaluations.oss-cn-hangzhou.aliyuncs.com/common/20200205/102044743.png"><p class="pgc-img-caption">备注是这里</p></div>
```
图片套链接，链接注入到图片备注后方

## Markdown表格
不支持，一律不用，改用图片的形式。

## Markdown高级技巧
1. **HTML元素**
避免使用，编辑器几乎全部不支持,待验证。
**暂时不验证**

2. **转义**
支持，正常使用即可。

3. **公式**
不支持，一律不用，改用图片的形式。

4. **各类流程图**
不支持，一律不用，改用图片的形式。

## 头条编辑器额外支持
1. **插入音频**
<audio controls="" preload="none">
      <source src="http://qiniu.cloud.fandong.me/Music_iP%E8%B5%B5%E9%9C%B2%20-%20%E7%A6%BB%E6%AD%8C%20%28Live%29.mp3">
</audio>
改为：
```html
<div __syl_tag="true" contenteditable="false" draggable="true"><templ style="display:none"><p>{!-- PGC_VOICE:{"upload_id":"v02024f10000botta8i6tgqek2aovalg","title":"Steelheart - She's Gone (Official Video).mp3","duration":291.134694,"content":"来自金属光泽游戏评测","source":"pgc_audio"} --}</p></templ><mask><div class="syl-plugin-audio" data-data="{&quot;uploadId&quot;:&quot;v02024f10000botta8i6tgqek2aovalg&quot;,&quot;title&quot;:&quot;Steelheart - She's Gone (Official Video).mp3&quot;,&quot;duration&quot;:291.134694,&quot;content&quot;:&quot;来自金属光泽游戏评测&quot;,&quot;source&quot;:&quot;pgc_audio&quot;}"><div class="container"><div class="success"><div class="left"><span class="icon-bg"><span class="icon"></span></span></div><div class="right"><p class="two-line">Steelheart - She's Gone (Official Video).mp3</p><span class="two-line">音频尚未发布，暂时无法播放</span></div><span class="duration">04:51</span></div><div class="audio-icon"></div><div class="audio-title" data-id="v02024f10000botta8i6tgqek2aovalg" data-name="Steelheart - She's Gone (Official Video).mp3"><i>Steelheart - She's Gone (Official Video).mp3</i><span class="audio-time">04:51</span></div><div class="audio-content">来自金属光泽游戏评测&#8203;</div><span class="ttcore-remove-blot"></span></div></div></mask></div>
```
以上内容尚未简化。
音频套链接，链接注入到图片备注后方
**暂不支持。**

2. **插入视频**
<video controls="" preload="none" poster="http://img.blog.fandong.me/2017-08-26-Markdown-Advance-Video.jpg" width=100%>
      <source src="http://img.blog.fandong.me/2017-08-26-Markdown-Advance-Video.mp4" type="video/mp4">
</video>
改为：
```html
<div __syl_tag="true" contenteditable="false" draggable="true"><templ style="display:none"><p>{!-- PGC_VIDEO:{"sp":"toutiao","vid":"v02004c40000bottlkaepr14tadrqr1g","vu":"v02004c40000bottlkaepr14tadrqr1g","vname":"这就是你分身的借口.127995926.mp4","vposter":"http://p1.pstatp.com/large/2f3e60006915ad64a1e5e","thumb_url":"2f3e60006915ad64a1e5e"} --}</p></templ><mask><div class="syl-plugin-video" data-data="{&quot;vid&quot;:&quot;v02004c40000bottlkaepr14tadrqr1g&quot;,&quot;vu&quot;:&quot;v02004c40000bottlkaepr14tadrqr1g&quot;,&quot;vname&quot;:&quot;这就是你分身的借口.127995926.mp4&quot;,&quot;vposter&quot;:&quot;http://p1.pstatp.com/large/2f3e60006915ad64a1e5e&quot;,&quot;thumb_url&quot;:&quot;2f3e60006915ad64a1e5e&quot;,&quot;duration&quot;:78.614}"><div class="container"><span class="ttcore-remove-blot"></span><div class="success"><img alt="文字视频" src="http://p1.pstatp.com/large/2f3e60006915ad64a1e5e" data-videoheight="0" data-videowidth="0"><div class="cover">更换封面</div><div class="tip">01:18  视频尚未发布，暂时无法播放</div></div></div></div></mask></div>
```
视频套链接，链接注入到图片备注后方

3. **商品推广**
**暂不作支持。**

4. **插入投票**
可使用markdown里面的表格做通用兼容。
**暂时不支持。**

5. **插入小程序**
**暂不作支持。**