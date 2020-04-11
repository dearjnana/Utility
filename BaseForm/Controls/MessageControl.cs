﻿using System.Drawing;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using Insight.Utils.Common;

namespace Insight.Utils.Controls
{
    public partial class MessageControl : XtraUserControl
    {
        private int _width;
        private NimMessage _message;

        public int width
        {
            set
            {
                _width = (value > 1000 ? 1000 : value) - 180;
                Width = value;
            }
        }

        public NimMessage message
        {
            set
            {
                _message = value;
                switch (_message.type)
                {
                    case 0:
                        showText();
                        break;
                    case 1:
                        showImage();
                        break;
                    default:
                        _message.body = new TextMessage{msg = "此版本不支持该消息类型" };
                        showText();
                        break;
                }

            }
        }

        public Image headImage
        {
            set
            {
                var isSend = _message.direction == 0;
                if (isSend) picMe.Image = value;
                else picTarget.Image = value;

                picTarget.Visible = !isSend;
                picMe.Visible = isSend;

            }
        }

        /// <summary>
        /// 构造方法
        /// </summary>
        public MessageControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 显示文本
        /// </summary>
        private void showText()
        {
            var msg = Util.convertTo<TextMessage>(_message.body).msg;
            var x = 70;
            var y = 5;

            // 计算字符宽度
            var rw = TextRenderer.MeasureText(msg, Font).Width;
            var tw = rw < _width - 10 ? rw : _width - 10;
            labMessage.Width = tw;
            labMessage.Text = msg;

            // 计算气泡宽高
            var th = labMessage.Height;
            if (th < 50) y = (50 - th) / 2;

            pceText.Width = tw + 10;
            pceText.Height = th + 10;

            // 计算控件宽高
            var h = pceText.Height < 60 ? 60 : pceText.Height;
            Height = h + 10;

            // 发送气泡靠右
            if (_message.direction == 0)
            {
                x = Width - pceText.Width - 70;
                pceText.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            }

            pceText.Location = new Point(x, y);
            pceText.Visible = true;
        }

        /// <summary>
        /// 显示图片
        /// </summary>
        private void showImage()
        {
            var msg = Util.convertTo<FileMessage>(_message.body);
            var x = 70;

            // 计算图片宽高
            var w = msg.w < _width ? msg.w : _width;
            var h = msg.w < _width ? msg.h : msg.h * w / msg.w;
            picImage.Width = w;
            picImage.Height = h;
            picImage.EditValue = msg.url;

            // 计算控件宽高
            Height = h + 10;

            // 发送图片靠右
            if (_message.direction == 0)
            {
                x = Width - pceText.Width - 70;
                picImage.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            }

            picImage.Location = new Point(x, 5);
            picImage.Visible = true;
        }
    }

    public class NimMessage
    {
        /// <summary>
        /// 消息ID
        /// </summary>
        public string id { get; set; }

        /// <summary>
        /// 云信消息ID
        /// </summary>
        public long msgid { get; set; }

        /// <summary>
        /// 发送者accid
        /// </summary>
        public string from { get; set; }

        /// <summary>
        /// 接收者accid
        /// </summary>
        public string to { get; set; }

        /// <summary>
        /// 操作类型
        /// </summary>
        public int ope => 0;

        /// <summary>
        /// 方向：0.发送;1.接收
        /// </summary>
        public int direction { get; set; }

        /// <summary>
        /// 消息类型
        /// </summary>
        public int type { get; set; }

        /// <summary>
        /// 消息内容
        /// </summary>
        public object body { get; set; }

        /// <summary>
        /// 消息发送时间戳
        /// </summary>
        public long timetag { get; set; }
    }

    public class TextMessage
    {
        /// <summary>
        /// 消息内容
        /// </summary>
        public string msg { get; set; }
    }

    public class FileMessage
    {
        /// <summary>
        /// 图片名称
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// 文件扩展名
        /// </summary>
        public string ext { get; set; }

        /// <summary>
        /// 文件MD5值
        /// </summary>
        public string md5 { get; set; }

        /// <summary>
        /// 图片URL
        /// </summary>
        public string url { get; set; }

        /// <summary>
        /// 宽度
        /// </summary>
        public int w { get; set; }

        /// <summary>
        /// 高度
        /// </summary>
        public int h { get; set; }

        /// <summary>
        /// 字节数
        /// </summary>
        public int size { get; set; }
    }
}
