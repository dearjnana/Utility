﻿using System;
using DevExpress.XtraEditors;

namespace Insight.Utils.Controls.Nim
{
    public partial class TimeLabel : XtraUserControl
    {
        /// <summary>
        /// 显示消息时间
        /// </summary>
        public DateTime time
        {
            set
            {
                var ts = DateTime.Now - value;
                if (ts.TotalDays > 365)
                {
                    labTime.Text = value.ToString("yyyy-MM-dd hh:mm:ss");
                }
                else if (ts.TotalDays > 30)
                {
                    labTime.Text = value.ToString("MM-dd hh:mm:ss");
                }
                else if (ts.TotalHours > 12)
                {
                    labTime.Text = value.ToString("dd hh:mm:ss");
                }
                else
                {
                    labTime.Text = value.ToString("hh:mm:ss");
                }
            }
        }

        /// <summary>
        /// 构造方法
        /// </summary>
        public TimeLabel()
        {
            InitializeComponent();
        }
    }
}