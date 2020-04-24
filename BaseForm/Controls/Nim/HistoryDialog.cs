﻿using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Insight.Utils.BaseForms;
using Insight.Utils.Common;
using NIM.Messagelog;
using NIM.Session;

namespace Insight.Utils.Controls.Nim
{
    public partial class HistoryDialog : BaseDialog
    {
        private long endTime = Util.getTimeStamp(DateTime.Now) * 1000;
        private readonly string targetId;
        private readonly Image targetHead;
        private DateTime messageTime;
        private int height;

        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="user">对方云信名片</param>
        public HistoryDialog(NimUser user)
        {
            InitializeComponent();

            Closed += (sender, args) => Dispose();
            close.Click += (sender, args) =>
            {
                DialogResult = DialogResult.OK;
                Close();
            };

            targetId = user.accid;
            targetHead = Util.getImageFromUrl(user.icon);
            getHistory();
        }

        /// <summary>
        /// 查询历史消息
        /// </summary>
        private void getHistory()
        {
            MessagelogAPI.QueryMsglogOnline(targetId, NIMSessionType.kNIMSessionTypeP2P, 6, 0, endTime, 0, false, true, true, (code, accountId, sType, result) =>
            {
                void action()
                {
                    var control = (ButtonLabel) pceHistory.Controls[endTime.ToString()];
                    if (control != null)
                    {
                        control.click -= (sender, args) => getHistory();
                        pceHistory.Controls.Remove(control);
                        height = height - control.Height;
                        pceHistory.Height = height;
                    }

                    if (result.MsglogCollection == null || result.MsglogCollection.Length == 0) return;

                    var list = result.MsglogCollection.OrderByDescending(i => i.TimeStamp).ToList();
                    foreach (var msg in list)
                    {
                        var message = new NimMessage
                        {
                            id = msg.ClientMsgID,
                            msgid = msg.ServerMsgId,
                            from = msg.SenderID,
                            to = msg.ReceiverID,
                            type = msg.MessageType.GetHashCode(),
                            body = NimUtil.getMsg(msg),
                            direction = msg.SenderID == targetId ? 1 : 0,
                            timetag = msg.TimeStamp / 1000
                        };

                        addMessage(message);
                    }

                    if (list.Count < 6) return;

                    endTime = list.Last().TimeStamp;
                    addButton();
                }

                Invoke((Action)action);
            });
        }

        /// <summary>
        /// 构造并添加消息控件到消息窗口
        /// </summary>
        /// <param name="message">云信IM点对点消息</param>
        public void addMessage(NimMessage message)
        {
            var control = new MessageBox
            {
                width = pceHistory.Width,
                message = message,
                targetHead = targetHead,
                Name = message.id,
                Dock = DockStyle.Top
            };
            pceHistory.Controls.Add(control);
            height = height + control.Size.Height;

            var time = Util.getDateTime(message.timetag);
            if (messageTime == DateTime.MinValue)
            {
                messageTime = time;
                addTime(time);
            }

            var ts = time - messageTime;
            if (ts.TotalMinutes > 15) addTime(time);

            pceHistory.Height = height;
        }

        /// <summary>
        /// 构造并添加时间控件到消息窗口
        /// </summary>
        /// <param name="time"></param>
        private void addTime(DateTime time)
        {
            messageTime = time;
            var control = new TimeLabel
            {
                time = time,
                Name = Util.newId("N"),
                Dock = DockStyle.Top
            };

            pceHistory.Controls.Add(control);

            height = height + control.Size.Height;
            pceHistory.Height = height;
        }

        /// <summary>
        /// 构造并添加时间控件到消息窗口
        /// </summary>
        private void addButton()
        {
            var control = new ButtonLabel
            {
                Name = endTime.ToString(),
                Dock = DockStyle.Top
            };
            control.click += (sender, args) => getHistory();
            pceHistory.Controls.Add(control);

            height = height + control.Size.Height;
            pceHistory.Height = height;
        }
    }
}
