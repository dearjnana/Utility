﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using DevExpress.XtraBars;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraGrid.Views.Grid.ViewInfo;
using FastReport;
using Insight.Utils.BaseForm;
using Insight.Utils.Client;
using Insight.Utils.Common;
using Insight.Utils.Controls;
using Insight.Utils.Entities;
using Insight.Utils.Entity;

namespace Insight.Utils.Models
{
    public class MdiModel<T> where T : BaseMdi, new()
    {
        private int waits;
        private DateTime wait;
        private GridHitInfo hitInfo = new GridHitInfo();

        /// <summary>
        /// MDI视图
        /// </summary>
        public readonly T view;

        /// <summary>
        /// 主列表分页控件
        /// </summary>
        public PageControl tab;

        /// <summary>
        /// 工具栏按钮集合
        /// </summary>
        public List<BarButtonItem> buttons;

        /// <summary>
        /// 令牌管理器
        /// </summary>
        public readonly TokenHelper tokenHelper = Setting.tokenHelper;

        /// <summary>
        /// 服务地址
        /// </summary>
        public readonly string gateway = Setting.gateway;

        /// <summary>
        /// 业务模块ID
        /// </summary>
        public readonly string moduleId;

        /// <summary>
        /// 模块选项集合
        /// </summary>
        public List<ModuleParam> moduleParams;

        /// <summary>
        /// 构造函数，初始化MDI窗体并显示
        /// </summary>
        /// <param name="nav">导航信息</param>
        public MdiModel(Navigation nav)
        {
            moduleId = nav.id;
            view = new T
            {
                ControlBox = nav.index > 0,
                MdiParent = Application.OpenForms["MainWindow"],
                Icon = Icon.FromHandle(new Bitmap(Util.getImage(nav.moduleInfo.iconUrl)).GetHicon()),
                Name = nav.moduleInfo.module,
                Text = nav.name
            };

            view.Show();
            initToolBar();
            getParams();
        }

        /// <summary>
        /// 设计报表
        /// </summary>
        /// <typeparam name="TE">类型</typeparam>
        /// <param name="tid">模板ID</param>
        /// <param name="name">数据源名称</param>
        /// <param name="data">数据</param>
        /// <param name="param">参数集合</param>
        public void design<TE>(string tid, string name, List<TE> data, Dictionary<string, object> param = null)
        {
            if (string.IsNullOrEmpty(tid))
            {
                Messages.showError("未配置打印模板，请先在选项中设置对应的打印模板！");
                return;
            }

            var report = buildReport(tid, name, data, param);
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
        public void preview(string tid, string id = null)
        {
            if (string.IsNullOrEmpty(tid))
            {
                Messages.showError("未配置打印模板，请先在选项中设置对应的打印模板！");
                return;
            }

            var print = buildReport(tid, id);
            print?.ShowPrepared(true);
        }

        /// <summary>
        /// 打印预览
        /// </summary>
        /// <typeparam name="TE">数据类型</typeparam>
        /// <param name="set">打印设置</param>
        public void preview<TE>(PrintSetting<TE> set)
        {
            if (string.IsNullOrEmpty(set.templateId))
            {
                Messages.showError("未配置打印模板，请先在选项中设置对应的打印模板！");
                return;
            }

            var report = buildReport(set.templateId, set.dataName, set.data, set.parameter);
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
        public string print(string printer, string tid, string id = null, int onSheet = 0)
        {
            if (string.IsNullOrEmpty(tid))
            {
                Messages.showError("未配置打印模板，请先在选项中设置对应的打印模板！");
                return null;
            }

            var report = buildReport(tid, id);
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
        public string print<TE>(PrintSetting<TE> set)
        {
            if (string.IsNullOrEmpty(set.templateId))
            {
                Messages.showError("未配置打印模板，请先在选项中设置对应的打印模板！");
                return null;
            }

            var report = buildReport(set.templateId, set.dataName, set.data, set.parameter);
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
        public void switchItemStatus(Dictionary<string, bool> dict)
        {
            var keys = new[] {"enable","disable"};
            foreach (var obj in dict)
            {
                var item = buttons.SingleOrDefault(b => b.Name == obj.Key);
                if (item == null) continue;

                item.Enabled = obj.Value && (bool) item.Tag;
                if (keys.All(i => !item.Name.Contains(i))) continue;

                item.Visibility = item.Enabled ? BarItemVisibility.Always : BarItemVisibility.Never;
            }
        }

        /// <summary>
        /// 是否允许双击
        /// </summary>
        /// <param name="key">操作名称</param>
        /// <returns>是否允许双击</returns>
        public bool allowDoubleClick(string key)
        {
            var button = buttons.SingleOrDefault(i => i.Name == key);
            return button != null && button.Enabled;
        }

        /// <summary>
        /// 显示等待提示
        /// </summary>
        public void showWaitForm()
        {
            waits++;
            if (view.Wait.IsSplashFormVisible) return;

            wait = DateTime.Now;
            view.Wait.ShowWaitForm();
        }

        /// <summary>
        /// 关闭等待提示
        /// </summary>
        public void closeWaitForm()
        {
            waits--;
            if (waits > 0) return;

            var time = (int) (DateTime.Now - wait).TotalMilliseconds;
            if (time < 800) Thread.Sleep(800 - time);

            view.Wait.CloseWaitForm();
        }

        /// <summary>
        /// 鼠标点击事件
        /// </summary>
        /// <param name="gridView">GridView</param>
        /// <param name="args">MouseEventArgs</param>
        public void mouseDownEvent(GridView gridView, MouseEventArgs args)
        {
            if (args.Button != MouseButtons.Right) return;

            var point = new Point(args.X, args.Y);
            hitInfo = gridView.CalcHitInfo(point);
        }

        /// <summary>
        /// 创建右键菜单并注册事件
        /// </summary>
        /// <param name="gridView">GridView</param>
        /// <returns>ContextMenuStrip</returns>
        public ContextMenuStrip createCopyMenu(GridView gridView)
        {
            var tsmi = new ToolStripMenuItem {Text = @"复制"};
            tsmi.Click += (sender, args) =>
            {
                if (hitInfo.Column == null) return;

                var content = gridView.GetRowCellDisplayText(hitInfo.RowHandle, hitInfo.Column);
                if (string.IsNullOrEmpty(content)) return;

                Clipboard.Clear();
                Clipboard.SetData(DataFormats.Text, content);
            };

            var menu = new ContextMenuStrip();
            menu.Items.Add(tsmi);
            return menu;
        }

        /// <summary>
        /// 获取选项数据
        /// </summary>
        /// <param name="keys">选项代码集</param>
        /// <returns>ModuleParam 选项数据</returns>
        public List<ModuleParam> getParams(List<Dictionary<string, string>> keys)
        {
            var datas = new List<ModuleParam>();
            foreach (var key in keys)
            {
                var code = key.ContainsKey("code") ? key["code"] : null;
                var deptId = key.ContainsKey("deptId") ? key["deptId"] : null;
                var userId = key.ContainsKey("userId") ? key["userId"] : null;
                var byModule = key.ContainsKey("byModule");
                var data = getParam(code, deptId, userId, byModule);
                datas.Add(data);
            }

            return datas;
        }

        /// <summary>
        /// 获取选项数据
        /// </summary>
        /// <param name="key">选项代码</param>
        /// <param name="deptId">部门ID</param>
        /// <param name="userId">用户ID</param>
        /// <param name="byModule">是否模块专用</param>
        /// <returns>ModuleParam 选项数据</returns>
        public ModuleParam getParam(string key, string deptId = null, string userId = null, bool byModule = false)
        {
            var param = moduleParams.FirstOrDefault(i => i.code == key && (!byModule || i.moduleId == moduleId) 
                                                                       && i.deptId == deptId && i.userId == userId);
            if (param == null)
            {
                param = new ModuleParam
                {
                    id = Util.newId(),
                    moduleId = byModule ? moduleId : null,
                    code = key,
                    deptId = deptId,
                    userId = userId
                };
                moduleParams.Add(param);
            }

            return param;
        }

        /// <summary>
        /// 保存选项数据
        /// </summary>
        /// <returns>bool 是否成功</returns>
        public bool saveParam()
        {
            var url = $"{gateway}/commonapi/v1.0/params";
            var dict = new Dictionary<string, object> {{"list", moduleParams}};
            var client = new HttpClient<List<ModuleParam>>(tokenHelper);

            return client.put(url, dict);
        }

        /// <summary>
        /// 初始化模块工具栏
        /// </summary>
        private void initToolBar()
        {
            buttons = (from a in getActions()
                select new BarButtonItem
                {
                    AllowDrawArrow = a.funcInfo.beginGroup,
                    Caption = a.name,
                    Enabled = a.permit,
                    Name = a.funcInfo.method,
                    Tag = a.permit,
                    Glyph = Util.getImage(a.funcInfo.iconUrl),
                    PaintStyle = a.funcInfo.hideText ? BarItemPaintStyle.Standard : BarItemPaintStyle.CaptionGlyph,
                }).ToList();
            buttons.ForEach(i => view.ToolBar.ItemLinks.Add(i, i.AllowDrawArrow));
        }

        /// <summary>
        /// 生成报表
        /// </summary>
        /// <typeparam name="TE">类型</typeparam>
        /// <param name="tid">模板ID</param>
        /// <param name="name">数据源名称</param>
        /// <param name="data">数据</param>
        /// <param name="dict">参数集合</param>
        /// <returns>Report FastReport报表</returns>
        private Report buildReport<TE>(string tid, string name, List<TE> data, Dictionary<string, object> dict)
        {
            var url = $"{gateway}/commonapi/v1.0/templates/{tid}";
            var client = new HttpClient<object>(tokenHelper);
            if (!client.get(url)) return null;

            var report = new Report();
            report.LoadFromString(client.data.ToString());
            report.RegisterData(data, name);
            foreach (var i in dict ?? new Dictionary<string, object>())
            {
                report.SetParameterValue(i.Key, i.Value);
            }

            return report;
        }

        /// <summary>
        /// 生成报表
        /// </summary>
        /// <param name="tid">模板ID</param>
        /// <param name="id">数据ID</param>
        /// <returns></returns>
        private Report buildReport(string tid, string id)
        {
            var isCopy = false;
            ImageData img;
            if (string.IsNullOrEmpty(tid))
            {
                if (string.IsNullOrEmpty(id))
                {
                    Messages.showError("尚未选定需要打印的数据！请先选择数据。");
                    return null;
                }

                isCopy = true;
                img = getImageData(id);
                if (img == null)
                {
                    Messages.showError("尚未设置打印模板！请先在设置对话框中设置正确的模板。");
                    return null;
                }
            }
            else
            {
                img = buildImageData(id, tid);
            }

            if (img == null)
            {
                Messages.showError("生成打印数据错误");
                return null;
            }

            var print = new Report {FileName = img.id};
            print.LoadPrepared(new MemoryStream(img.image));

            if (isCopy) addWatermark(print, "副 本");

            return print;
        }

        /// <summary>
        /// 获取电子影像数据
        /// </summary>
        /// <param name="id">影像ID</param>
        /// <returns>ImageData 电子影像数据</returns>
        private ImageData getImageData(string id)
        {
            var url = $"{gateway}/commonapi/v1.0/images/{id}";
            var client = new HttpClient<ImageData>(Setting.tokenHelper);

            return client.get(url) ? client.data : null;
        }

        /// <summary>
        /// 生成电子影像数据
        /// </summary>
        /// <param name="id">业务数据ID</param>
        /// <param name="templateId">模板ID</param>
        /// <returns></returns>
        private ImageData buildImageData(string id, string templateId)
        {
            var url = $"{gateway}/commonapi/v1.0/images/{id ?? "null"}";
            var client = new HttpClient<ImageData>(Setting.tokenHelper);
            var dict = new Dictionary<string, object>
            {
                {"templateId", templateId},
                {"deptName", Setting.deptName}
            };

            return client.post(url, dict) ? client.data : null;
        }

        /// <summary>
        /// 增加水印
        /// </summary>
        /// <param name="fr">Report对象实体</param>
        /// <param name="str">水印文字</param>
        /// <param name="size"></param>
        /// <returns>Report对象实体</returns>
        private void addWatermark(Report fr, string str, int size = 72)
        {
            var wm = new Watermark
            {
                Enabled = true,
                Text = str,
                Font = new Font("宋体", size, FontStyle.Bold)
            };

            for (var i = 0; i < fr.PreparedPages.Count; i++)
            {
                var pag = fr.PreparedPages.GetPage(i);
                pag.Watermark = wm;
                fr.PreparedPages.ModifyPage(i, pag);
            }
        }

        /// <summary>
        /// 获取模块功能按钮集合
        /// </summary>
        /// <returns>功能按钮集合</returns>
        private IEnumerable<Function> getActions()
        {
            var url = $"{gateway}/commonapi/v1.0/navigations/{moduleId}/functions";
            var client = new HttpClient<List<Function>>(tokenHelper);

            return client.get(url) ? client.data : new List<Function>();
        }

        /// <summary>
        /// 获取选项数据
        /// </summary>
        private void getParams()
        {
            var url = $"{gateway}/commonapi/v1.0/params";
            var client = new HttpClient<List<ModuleParam>>(tokenHelper);
            if (!client.get(url)) return;

            moduleParams = client.data;
        }
    }
}