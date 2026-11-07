using Newtonsoft.Json.Linq;
using System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Markup;

namespace BiliLite.Helpers
{
    public static class ControlHelper
    {
        public static RichTextBlock StringToRichText(string txt,JObject emote)
        {
            try
            {
                if (txt != null)
                {
                    //处理特殊字符
                    var input = txt.Replace("&", "&amp;");
                    input = input.Replace("<", "&lt;");
                    input = input.Replace(">", "&gt;");
                    input = input.Replace("\r\n", "<LineBreak/>");
                    input = input.Replace("\n", "<LineBreak/>");

                    //链接、av号、表情的顺序固定在 CommentRichText 里，表情必须最后拼才不会被打断
                    input = CommentRichText.BuildMarkup(input, emote, u => Utils.ProcessURL(u), name =>
                        LogHelper.Log("评论表情数据不完整，保留原文 " + name, LogType.INFO));

                    //生成xaml
                    var xaml = string.Format(@"<RichTextBlock HorizontalAlignment=""Stretch"" TextWrapping=""Wrap""  xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                                            xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"" xmlns:d=""http://schemas.microsoft.com/expression/blend/2008""
    xmlns:mc = ""http://schemas.openxmlformats.org/markup-compatibility/2006"" LineHeight=""20"">
                                          <Paragraph>{0}</Paragraph>
                                      </RichTextBlock>", input);
                    var p = (RichTextBlock)XamlReader.Load(xaml);
                    return p;
                }

                return PlainText(txt);
            }
            catch (Exception ex)
            {
                //保留纯文本兜底，但必须留痕：静默降级是这条评论再也看不到图片的原因
                var snippet = txt != null && txt.Length > 60 ? txt.Substring(0, 60) : txt;
                LogHelper.Log("评论富文本生成失败，已回退纯文本：" + snippet, LogType.ERROR, ex);
                return PlainText(txt);
            }

        }

        private static RichTextBlock PlainText(string txt)
        {
            var tx = new RichTextBlock();
            Paragraph paragraph = new Paragraph();
            Run run = new Run() { Text = txt };
            paragraph.Inlines.Add(run);
            tx.Blocks.Add(paragraph);
            return tx;
        }
    }
}
