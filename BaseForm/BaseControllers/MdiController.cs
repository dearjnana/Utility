﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DevExpress.XtraBars;
using FastReport;
using Insight.Utils.BaseForms;
using Insight.Utils.BaseViewModels;
using Insight.Utils.Common;
using Insight.Utils.Entities;
using Insight.Utils.Entity;

namespace Insight.Utils.BaseControllers
{
    public class MdiController<T, TV, TM> : BaseController where TV : BaseMdi, new() where TM : BaseMdiModel<T, TV>, new()
    {
        private readonly List<BarButtonItem> buttons;
        private int waits;
        private DateTime wait;

        /// <summary>
        /// MDI Model
        /// </summary>
        protected readonly TM mdiModel;

        /// <summary>
        /// MDI View
        /// </summary>
        public readonly TV mdiView;

        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="module">模块信息</param>
        protected MdiController(ModuleDto module)
        {
            mdiModel = new TM();
            mdiModel.initModule(module);
            mdiView = mdiModel.view;

            buttons = (from a in BaseModel.getActions(module.id)
                select new BarButtonItem
                {
                    AllowDrawArrow = a.funcInfo.beginGroup,
                    Caption = a.name,
                    Enabled = a.permit,
                    Name = a.funcInfo.method,
                    Tag = a.permit,
                    Glyph = Util.getImage(a.funcInfo.iconUrl),
                    PaintStyle = a.funcInfo.hideText ? BarItemPaintStyle.Standard : BarItemPaintStyle.CaptionGlyph
                }).ToList();
            buttons.ForEach(i => i.ItemClick += (sender, args) => buttonClick(args.Item.Name));
            buttons.ForEach(i => mdiView.ToolBar.ItemLinks.Add(i, i.AllowDrawArrow));
        }

        /// <summary>
        /// 设计报表
        /// </summary>
        /// <typeparam name="TE">类型</typeparam>
        /// <param name="tid">模板ID</param>
        /// <param name="name">数据源名称</param>
        /// <param name="data">数据</param>
        /// <param name="param">参数集合</param>
        protected void design<TE>(string tid, string name, List<TE> data, Dictionary<string, object> param = null)
        {
            if (string.IsNullOrEmpty(tid))
            {
                Messages.showError("未配置打印模板，请先在选项中设置对应的打印模板！");
                return;
            }

            var report = mdiModel.buildReport(tid, name, data, param);
            if (report == null)
            {
                Messages.showError("初始化报表失败！");
                return;
            }

            report.Design();
        }

        /// <summary>
        /// 打印预览
        /// </summary>
        /// <param name="tid">模板ID</param>
        /// <param name="id">数据ID</param>
        protected void preview(string tid, string id = null)
        {
            if (string.IsNullOrEmpty(tid))
            {
                Messages.showError("未配置打印模板，请先在选项中设置对应的打印模板！");
                return;
            }

            var print = mdiModel.buildReport(tid, id);
            print?.ShowPrepared(true);
        }

        /// <summary>
        /// 打印预览
        /// </summary>
        /// <typeparam name="TE">数据类型</typeparam>
        /// <param name="set">打印设置</param>
        protected void preview<TE>(PrintSetting<TE> set)
        {
            if (string.IsNullOrEmpty(set.templateId))
            {
                Messages.showError("未配置打印模板，请先在选项中设置对应的打印模板！");
                return;
            }

            var report = mdiModel.buildReport(set.templateId, set.dataName, set.data, set.parameter);
            if (report == null || !report.Prepare())
            {
                Messages.showError("生成报表失败！");
                return;
            }

            if (set.pagesOnSheet != PagesOnSheet.One)
            {
                report.PrintSettings.PrintMode = PrintMode.Scale;
                report.PrintSettings.PagesOnSheet = set.pagesOnSheet;
            }
            else
            {
                report.PrintSettings.PrintMode = set.printMode;
            }

            report.ShowPrepared(true);
        }

        /// <summary>
        /// 打印
        /// </summary>
        /// <param name="printer">打印机名称</param>
        /// <param name="tid">模板ID</param>
        /// <param name="id">数据ID</param>
        /// <param name="onSheet">合并打印模式</param>
        /// <returns>string 打印文档名称</returns>
        protected string print(string printer, string tid, string id = null, int onSheet = 0)
        {
            if (string.IsNullOrEmpty(tid))
            {
                Messages.showError("未配置打印模板，请先在选项中设置对应的打印模板！");
                return null;
            }

            var report = mdiModel.buildReport(tid, id);
            if (report == null) return null;

            if (onSheet > 0)
            {
                report.PrintSettings.PrintMode = PrintMode.Scale;
                report.PrintSettings.PagesOnSheet = PagesOnSheet.Three;
            }

            if (!string.IsNullOrEmpty(printer))
            {
                report.PrintSettings.ShowDialog = false;
                report.PrintSettings.Printer = printer;
            }

            report.PrintPrepared();

            return report.FileName;
        }

        /// <summary>
        /// 打印
        /// </summary>
        /// <typeparam name="TE">数据类型</typeparam>
        /// <param name="set">打印设置</param>
        /// <returns>string 电子影像文件名</returns>
        protected string print<TE>(PrintSetting<TE> set)
        {
            if (string.IsNullOrEmpty(set.templateId))
            {
                Messages.showError("未配置打印模板，请先在选项中设置对应的打印模板！");
                return null;
            }

            var report = mdiModel.buildReport(set.templateId, set.dataName, set.data, set.parameter);
            if (report == null || !report.Prepare())
            {
                Messages.showError("生成报表失败！");
                return null;
            }

            report.PrintSettings.Copies = set.copies;
            if (!string.IsNullOrEmpty(set.printer))
            {
                report.PrintSettings.ShowDialog = false;
                report.PrintSettings.Printer = set.printer;
            }

            if (set.pagesOnSheet != PagesOnSheet.One)
            {
                report.PrintSettings.PrintMode = PrintMode.Scale;
                report.PrintSettings.PagesOnSheet = set.pagesOnSheet;
            }
            else
            {
                report.PrintSettings.PrintMode = set.printMode;
            }

            report.PrintPrepared();

            return report.FileName;
        }

        /// <summary>
        /// 切换工具栏按钮状态
        /// </summary>
        /// <param name="dict"></param>
        protected void switchItemStatus(Dictionary<string, bool> dict)
        {
            var keys = new[] { "enable", "disable" };
            foreach (var obj in dict)
            {
                var item = buttons.SingleOrDefault(b => b.Name == obj.Key);
                if (item == null) continue;

                item.Enabled = obj.Value && (bool)item.Tag;
                if (keys.All(i => !item.Name.Contains(i))) continue;

                item.Visibility = item.Enabled ? BarItemVisibility.Always : BarItemVisibility.Never;
            }
        }

        /// <summary>
        /// 是否允许双击
        /// </summary>
        /// <param name="key">操作名称</param>
        /// <returns>是否允许双击</returns>
        protected bool allowDoubleClick(string key)
        {
            var button = buttons.SingleOrDefault(i => i.Name == key);
            return button != null && button.Enabled;
        }

        /// <summary>
        /// 显示等待提示
        /// </summary>
        protected void showWaitForm()
        {
            waits++;
            if (mdiView.Wait.IsSplashFormVisible) return;

            wait = DateTime.Now;
            mdiView.Wait.ShowWaitForm();
        }

        /// <summary>
        /// 关闭等待提示
        /// </summary>
        protected void closeWaitForm()
        {
            waits--;
            if (waits > 0) return;

            var time = (int)(DateTime.Now - wait).TotalMilliseconds;
            if (time < 800) Thread.Sleep(800 - time);

            mdiView.Wait.CloseWaitForm();
        }
    }
}