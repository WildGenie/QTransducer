﻿
using QDasTransfer.Forms;
using QTrans;
using QTrans.Classes;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace QDasTransfer
{
    public partial class NewMainForm : Form
    {
        TransferBase trans;
        /// <summary>
        /// 用于定位上次在文件夹选择容体中的打开的文件夹。
        /// </summary>
        string lastSelectedFolder;
        ParamaterData pd;
        int fileCountSuccessful = 0;

        public NewMainForm()
        {
            InitializeComponent();
            trans = values.transducer;
            pd = trans.pd;
        }


        private void lkAddFile_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {

        }

        private void lkAddFolder_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            FolderBrowserDialog fb = new FolderBrowserDialog();
            fb.SelectedPath = lastSelectedFolder;
            if (fb.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                lastSelectedFolder = fb.SelectedPath;
                InputPath ip = new InputPath(lastSelectedFolder, 1);
                AddDistinctToListBox(ip);
            }
        }

        private void NewMainForm_Load(object sender, EventArgs e)
        {
            lvInputList.Items.Clear();
            foreach (InputPath ip in pd.InputPaths)
            {
                ListViewItem lvi = new ListViewItem(ip.ToStrings());
                lvi.Tag = ip;
                lvi.ImageIndex = ip.Type;
                lvInputList.Items.Add(lvi);
            }

            try
            {
                if (pd.InputListViewWidth.Count >= 2)
                {
                    pd.InputListViewWidth[0].SetData(lvInputList);
                    pd.InputListViewWidth[1].SetData(lvLogs);
                }
            }
            catch { } 

            tiLock.Visible = false; 
             
            trans.TransFileComplete += Trans_TransFileComplete;

            this.Text += " " + trans.CompanyName;
            Reset();
        }

        private void Trans_TransFileComplete(object sender, QTrans.Classes.TransLog e)
        {
            if (e == null)
                return;

            if (e.LogType == LogType.Success)
            {
                ListViewItem lvi = new ListViewItem(new string[] { e.Date.ToString(), e.Input, e.LogType.ToString(), e.Output, e.Message });
                lvi.ImageIndex = 0;
                lvResults.Items.Add(lvi);
                fileCountSuccessful++;
            }
            else
            {
                ListViewItem lvi = new ListViewItem(new string[] { e.Date.ToString(), e.Input, e.LogType.ToString() });
                lvi.ImageIndex = 3;
                lvResults.Items.Add(lvi);
                fileCountSuccessful++;
            }
        }

        private void tiStart_Click(object sender, EventArgs e)
        {
            selectedonly = false;
            transduce();
        }

        Thread transThread;

        private void processTransducing()
        {
            int transed = 0;
            while (true)
            {
                //这三个时间分别表示当前时间，开始时间和结束时间。
                DateTime dt = DateTime.Now;
                DateTime start = new DateTime(dt.Year, dt.Month, dt.Day, pd.StartTime.Hour, pd.StartTime.Minute, pd.StartTime.Second);
                DateTime end = new DateTime(dt.Year, dt.Month, dt.Day, pd.EndTime.Hour, pd.EndTime.Minute, pd.EndTime.Second);

                //只有在开始和结束时间范围内才会开始下一次转换。
                if (pd.SupportAutoTransducer && !(dt >= start && dt <= end))
                {
                    Thread.Sleep(1);
                    continue;
                }

                try
                {
                    for (int i = 0; i < lvInputList.Items.Count; i++)
                    {
                        if (selectedonly && !lvInputList.Items[i].Checked)
                            continue;

                        InputPath ip = lvInputList.Items[i].Tag as InputPath;
                        lvInputList.Items[i].BackColor = Color.LightGray;

                        // process transducing.
                        trans.ProcessInput(ip);

                        lvInputList.Items[i].BackColor = Color.White;
                        ip.LastTranducingTime = DateTime.Now;
                        lvInputList.Items[i].SubItems[2].Text = ip.LastTranducingTime.ToString();
                        transed += trans.LogList.Count(l => l.LogType == LogType.Success);
                        //处理完成后，把日志添加到日志列表中去。日志存放在trans.LogList中。
                        for (int j = 0; j < trans.LogList.Count; j++)
                        {
                            TransLog log = trans.LogList[j];
                            //编号，0.时间，1.事件，2.原因，3.输出文件
                            lvLogs.Items.Insert(0, new ListViewItem(log.GetStrings()));
                            //Funs.AddLog(log.LogType.ToString(), log.Content, log.Output);
                            Funs.AddLog(log);
                        }
                    }

                    Thread.Sleep(100);
                    Application.DoEvents();
                }
                catch (Exception e1)
                {
                    Funs.AddLog("TRANS", "NONE", e1.Message);
                }



                if (!pd.SupportAutoTransducer || !pd.AutoTransducerAvaliable)
                {
                    Reset();
                    MessageBox.Show("转换完成。\n共转换 " + transed + "个文件。", "转换完成", MessageBoxButtons.OK);
                    break;
                }

                trans.LogList.Clear();
                Application.DoEvents();
                Thread.Sleep(pd.getCircleInterval() * 1000);
            }
        }

        public  void AddDistinctToListBox(InputPath ip)
        {
            ListView lv = this.lvInputList;
            string[] strs = ip.ToStrings();
            foreach (ListViewItem item in lv.Items)
            {
                if (item.Text == ip.path)
                    return;
            }

            ListViewItem lvi = new ListViewItem(strs);
            lvi.Tag = ip;
            lvi.ImageIndex = ip.Type;
            lv.Items.Add(lvi);

            pd.InputPaths.Add(ip);
        }

        private void Reset()
        {
            tiStart.Enabled = true;
            tiStop.Enabled = false;
            selectedonly = false;

            for (int i = 0; i < lvInputList.Items.Count; i++)
            {
                InputPath ip = (InputPath)lvInputList.Items[i].Tag;
                if (ip.Type == 0)
                {
                    lvInputList.Items[i].SubItems[3].Text = File.Exists(lvInputList.Items[i].Text) ? "○" : "X";
                }
                else if (ip.Type == 1)
                {
                    string[] files = Directory.GetFiles(lvInputList.Items[i].Text);
                    int fc = 0;
                    foreach (string file in files)
                    {
                        if (File.Exists(file) && pd.ValidateExtention(file))
                            fc++;
                    }
                    lvInputList.Items[i].SubItems[3].Text = fc > 0 ? "○" : "X";
                }
            }


        }

        private void tiSystemConfig_Click(object sender, EventArgs e)
        {
            ConfigForm cf = new ConfigForm();
            cf.pd = pd.Clone();
            cf.ShowDialog();
            if (cf.needSave)
            {
                pd = cf.pd; 
            }
        }

        private void NewMainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            pd.InputPaths.Clear();
            for (int i = 0; i < lvInputList.Items.Count; i++)
                pd.InputPaths.Add(lvInputList.Items[i].Tag as InputPath);

            pd.InputListViewWidth.Clear();
            pd.InputListViewWidth.Add(new ListViewData(lvInputList));
            pd.InputListViewWidth.Add(new ListViewData(lvLogs));
        }

        private void tiLock_Click(object sender, EventArgs e)
        {

        }

        private void tiStop_Click(object sender, EventArgs e)
        {
            transThread.Suspend();
            if (MessageBox.Show("确定要停止吗？", "停止任务确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
            {
                transThread.Resume();
            }
            else
            {
                Reset();
            }
        }

        private void tiOpenOutputFolder_Click(object sender, EventArgs e)
        {
            Funs.OpenFolderInWindows(pd.OutputFolder);
        }

        private void tiStart_ButtonClick(object sender, EventArgs e)
        {
            selectedonly = false;
            transduce();
        }

        private void transduce()
        {
            if (lvInputList.Items.Count == 0)
            {
                MessageBox.Show("请选择输入文件。");
                tbMain.SelectedIndex = 0;
                return;
            }

            //建立输出目录 
            if (!Funs.CreateFolder(pd.OutputFolder))
            {
                MessageBox.Show("输出文件夹创建失败。", "路径创建失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            //建立临时目录 
            if (!Funs.CreateFolder(pd.TempFolder))
            {
                MessageBox.Show("输出文件夹创建失败。", "路径创建失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            //如果开启自动转换，那么就会创建对应的备份目录。
            if (pd.ProcessSourceFileType == 0)
            {
                try
                {
                    Directory.CreateDirectory(pd.FolderForSuccessed);
                    Directory.CreateDirectory(pd.FolderForFailed);
                }
                catch
                {
                    MessageBox.Show("备份文件夹设置不正确，未能创建成功，请重新设置。", "备份文件夹设置失败",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            //复制PdfToTxt.exe文件过去，如果未能复制成功，则同样提示失败。
            if (pd.PdfMode)
            {
                //可执行的转换程序。
                string exepath = pd.PdfDllFolder + "\\PdfToTxt.exe";
                //如果此程序不存在，则从程序中复制过去。
                if (!File.Exists(exepath))
                    Funs.CopyResourceDataFile(exepath);

                //等待复制完成。
                Application.DoEvents();
                Thread.Sleep(100);

                //如果仍然不存在，表示复制失败。
                if (!File.Exists(exepath))
                {
                    MessageBox.Show("PDF转换模块启动失败，请关闭程序重新尝试，如果仍然无法解决，请与技术人员联系。",
                        "转换模块启动失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }



            tiStart.Enabled = false;
            tiStop.Enabled = true;
            tbMain.SelectedIndex = 0;

            transThread = new Thread(new ThreadStart(processTransducing));
            transThread.IsBackground = true;
            transThread.Start();
        }

        bool selectedonly = false;
        private void tiTransduceSelected_Click(object sender, EventArgs e)
        {
            selectedonly = true;
            transduce();
        }

        private void tiSaveConfig_Click(object sender, EventArgs e)
        {

        }

        private void tiConfig_Click(object sender, EventArgs e)
        {
            ConfigForm cf = new ConfigForm();
            cf.pd = pd;
            cf.ShowDialog();
            if (cf.needSave)
            {
                pd.Save();
            }
        }

        private void lkClearTransducedHistory_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            lvResults.Items.Clear();
        }

        private void lkAllSelections_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            for (int i = 0; i < lvInputList.Items.Count; i++)
                lvInputList.Items[i].Checked = true;

        }

        private void lkReverseSelection_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            for (int i = 0; i < lvInputList.Items.Count; i++)
                lvInputList.Items[i].Checked = !lvInputList.Items[i].Checked;
        }

        private void lkCancelAllSelections_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            for (int i = 0; i < lvInputList.Items.Count; i++)
                lvInputList.Items[i].Checked = false;
        }

        private void mnDeleteItem_Click(object sender, EventArgs e)
        {
            if (lvInputList.SelectedIndices.Count > 0)
            {
                DialogResult dr = MessageBox.Show("是否删除当前选中项？", "删除确认", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (dr == DialogResult.Yes)
                {
                    pd.InputPaths.RemoveAt(lvInputList.SelectedIndices[0]);
                    lvInputList.Items.RemoveAt(lvInputList.SelectedIndices[0]);
                }
                pd.Save();
            }
        }

        private void tiChBase_Click(object sender, EventArgs e)
        {
            if (lvInputList.SelectedIndices.Count == 0)
            {
                MessageBox.Show("请选择需要配置的文件。", "未选中文件", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }


        }

        private void lvInputList_DoubleClick(object sender, EventArgs e)
        {
            //tiChBase.PerformClick();
        }

        private void mnOpenInputFolder_Click(object sender, EventArgs e)
        {
            if (lvInputList.SelectedItems.Count > 0)
                WindGoes.FileHelper.OpenAndSelectPath(lvInputList.SelectedItems[0].Text);
        }

        private void mnOpenFile_Click(object sender, EventArgs e)
        {
            if (lvInputList.SelectedItems.Count > 0)
                WindGoes.FileHelper.OpenPath(lvInputList.SelectedItems[0].Text);
        }

        private void mnTransduce_Click(object sender, EventArgs e)
        {
            tiTransduceSelected.PerformClick();
        }

        private void lkClearLog_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            lvLogs.Items.Clear();
        }


        private void mnNewConfig_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否重置所有配置参数？", "参数重置确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                trans.pd = new ParamaterData();
                trans.Initialize();
                MessageBox.Show("配置参数已重置成功。", "重置成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

        }

        private void mnExportConfig_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "xml文档|*.xml";
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                pd.Save(sfd.FileName);
            }
        }

        private void mnSaveConfig_Click(object sender, EventArgs e)
        {
            pd.InputPaths.Clear();
            for (int i = 0; i < lvInputList.Items.Count; i++)
                pd.InputPaths.Add(lvInputList.Items[i].Tag as InputPath);

            pd.InputListViewWidth.Clear();
            pd.InputListViewWidth.Add(new ListViewData(lvInputList));
            pd.InputListViewWidth.Add(new ListViewData(lvLogs));

            pd.Save();
            MessageBox.Show("配置文件已经保存。", "保存成功。", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }


        private void lkAddManully_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            NetpathInputForm ibf = new NetpathInputForm();
            ibf.ShowDialog();
            if (ibf.Confirmed)
            {
                lastSelectedFolder = Path.GetDirectoryName(ibf.NetPath);
                InputPath ip = new InputPath(ibf.NetPath);
                AddDistinctToListBox(ip);
            }
        }

        private void tiAddFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.InitialDirectory = lastSelectedFolder;
            ofd.Filter = pd.GetExtFilter();
            ofd.FilterIndex = trans.pd.FilterIndex;
            ofd.Multiselect = true;
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                lastSelectedFolder = Path.GetDirectoryName(ofd.FileNames[0]);
                for (int i = 0; i < ofd.FileNames.Length; i++)
                {
                    InputPath ip = new InputPath(ofd.FileNames[i], 0);
                    AddDistinctToListBox(ip);
                }
                pd.Save();
            }
        }

        private void tiAddFolder_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fb = new FolderBrowserDialog();
            fb.SelectedPath = lastSelectedFolder;
            if (fb.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                lastSelectedFolder = fb.SelectedPath;
                InputPath ip = new InputPath(lastSelectedFolder, 1);
                AddDistinctToListBox(ip);
                pd.Save();
            }
        }

        private void tiAddPathManually_Click(object sender, EventArgs e)
        {
            NetpathInputForm ibf = new NetpathInputForm();
            ibf.ShowDialog();
            if (ibf.Confirmed)
            {
                lastSelectedFolder = Path.GetDirectoryName(ibf.NetPath);
                InputPath ip = new InputPath(ibf.NetPath);
                AddDistinctToListBox(ip);
                pd.Save();
            }
        }

        private void tiSelectAll_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < lvInputList.Items.Count; i++)
                lvInputList.Items[i].Checked = true;
        }

        private void tiCancelAll_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < lvInputList.Items.Count; i++)
                lvInputList.Items[i].Checked = false;
        }

        private void tiSelectReverse_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < lvInputList.Items.Count; i++)
                lvInputList.Items[i].Checked = !lvInputList.Items[i].Checked;
        }

        private void tiClearLogList_Click(object sender, EventArgs e)
        {
            lvLogs.Items.Clear();
        }

        private void tiClear_Click(object sender, EventArgs e)
        {
            lvResults.Items.Clear();
        }

        private void menuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void tiDeletePathes_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否删除已选中项？", "删除确认", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                for (int i = lvInputList.Items.Count - 1; i >= 0; i--)
                {
                    if (lvInputList.Items[i].Checked) {
                        pd.InputPaths.RemoveAt(i);
                        lvInputList.Items.RemoveAt(i);
                    }
                }
                pd.Save();
            }
        }

        private void tiOpenLogFile_Click(object sender, EventArgs e)
        {
            tiOpenLogFile.Enabled = false;
            Process.Start("notepad.exe", ".\\runtime.log");
            tiOpenLogFile.Enabled = true;
        }

        private void tiSaveResults_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "text文档|*.txt";
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < lvResults.Items.Count; i++)
                {
                    sb.Append(lvResults.Items[i].Text + "\t");
                    for (int j = 0; j < lvResults.Items[i].SubItems.Count; j++)
                    {
                        sb.Append(lvResults.Items[i].SubItems[j].Text + "\t");
                    }
                    sb.AppendLine();
                }

                File.WriteAllText(sfd.FileName, sb.ToString());
            }
        }

        private void lvInputList_MouseClick(object sender, MouseEventArgs e)
        {
            if (lvInputList.SelectedIndices.Count > 0)
            {
                lvInputList.SelectedItems[0].Checked = !lvInputList.SelectedItems[0].Checked;
            }
        }

        private void tiOpenAppFolder_Click(object sender, EventArgs e)
        {
            Process.Start("explorer.exe", ".");
        }
    }
}
