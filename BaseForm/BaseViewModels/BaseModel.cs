﻿using System;
using System.ComponentModel;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using Insight.Utils.Common;

namespace Insight.Utils.BaseViewModels
{
    public class BaseModel<TV> where TV : XtraForm, new()
    {
        /// <summary>
        /// 回调
        /// </summary>
        public event CallbackHandle callbackEvent;

        /// <summary>
        /// 处理回调事件的委托
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public delegate void CallbackHandle(object sender, CallbackEventArgs e);

        /// <summary>
        /// 对话框视图
        /// </summary>
        protected readonly TV view;

        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="title">View标题</param>
        protected BaseModel(string title)
        {
            view = new TV {Text = title};
        }

        /// <summary>
        /// 回调方法
        /// </summary>
        /// <param name="methodName">方法名称</param>
        /// <param name="param">回调参数</param>
        protected void callback(string methodName, object param = null)
        {
            callbackEvent?.Invoke(this, new CallbackEventArgs(methodName, param));
        }

        /// <summary>
        /// 按钮点击事件路由
        /// </summary>
        /// <param name="methodName">回调方法名称</param>
        protected void buttonClick(string methodName)
        {
            var method = GetType().GetMethod(methodName);
            if (method == null)
            {
                Messages.showError("对不起，该功能尚未实现！");
            }
            else
            {
                method.Invoke(this, null);
            }
        }

        /// <summary>
        /// 关闭对话框
        /// </summary>
        protected void close()
        {
            view.DialogResult = DialogResult.OK;
            view.Close();
        }

        /// <summary>
        /// 关闭对话框确认
        /// </summary>
        /// <param name="args">取消事件参数</param>
        protected void closeConfirm(CancelEventArgs args)
        {
            const string msg = "您确定要放弃所做的变更，并关闭对话框吗？";
            args.Cancel = view.DialogResult != DialogResult.OK && !Messages.showConfirm(msg);
        }
    }

    /// <summary>
    /// 回调事件参数类
    /// </summary>
    public class CallbackEventArgs : EventArgs
    {
        /// <summary>
        /// 回调方法名称
        /// </summary>
        public string methodName { get; }

        /// <summary>
        /// 
        /// </summary>
        public object param { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="methodName">回调方法名称</param>
        /// <param name="param">回调参数</param>
        public CallbackEventArgs(string methodName, object param)
        {
            this.methodName = methodName;
            this.param = param;
        }
    }
}