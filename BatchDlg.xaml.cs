using System.Windows;
using System.Windows.Forms;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.IO;
using System;
using static System.Net.Mime.MediaTypeNames;
using System.Linq;

namespace KiwiGui
{
    /// <summary>
    /// Interaction logic for BatchDlg.xaml
    /// </summary>
    public partial class BatchDlg : Window
    {
        public KiwiCS.Kiwi instKiwi;
        private BackgroundWorker worker = new BackgroundWorker();
        bool separateResult = false;
        protected enum AnalyzeMode
        {
            Word,
            Sentence,
            Line,
            Whole,
        }
        protected struct WorkerArgs
        {
            public List<string> fileList;
            public AnalyzeMode mode;
            public int topN;
            public bool formatTag;
            public string formatSep;
            public bool integrateAllomorph;
        }

        public BatchDlg()
        {
            InitializeComponent();
            worker.DoWork += Worker_DoWork;
            worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
            worker.ProgressChanged += Worker_ProgressChanged;
            worker.WorkerReportsProgress = true;
            worker.WorkerSupportsCancellation = true;
            App.monitor.TrackScreenView("Kiwi_BatchDlg");
        }

        private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            Prg.Value = e.ProgressPercentage;
        }

        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // First, handle the case where an exception was thrown.
            if (e.Error != null)
            {
                System.Windows.MessageBox.Show(e.Error.Message);
            }
            else if (e.Cancelled)
            {
                
            }
            else
            {
                System.Windows.MessageBox.Show("일괄 처리 완료");
            }
            StopBtn.Visibility = Visibility.Collapsed;
            StartBtn.Visibility = Visibility.Visible;
            Prg.Value = 0;
        }

        private string morphsToString(IEnumerable<KiwiCS.Token> morphs, bool formatTag, string formatSep)
        {
            string ret = "";
            foreach (var m in morphs)
            {
                if (ret.Length > 0) ret += formatSep;
                ret += m.form;
                if (formatTag) ret += "/" + m.tag;
            }
            return ret;
        }
        private void analyzeWriteResult(string input, WorkerArgs args, StreamWriter taggedOutput, StreamWriter origOutput = null)
        {
            instKiwi.IntegrateAllomorph = args.integrateAllomorph;

            if (args.mode == AnalyzeMode.Line)
            {
                foreach (var line in input.Trim().Split('\n'))
                {
                    var trimmed = line.Trim();
                    if (trimmed.Length == 0) continue;

                    var res = instKiwi.Analyze(trimmed, args.topN, KiwiCS.Match.All);
                    origOutput.WriteLine(trimmed);
                    for (int i = 0; i < res.Length; i++)
                    {
                        taggedOutput.WriteLine(morphsToString(res[i].morphs, args.formatTag, args.formatSep));
                    }
                }
            }
            else if (args.mode == AnalyzeMode.Whole)
            {
                var res = instKiwi.Analyze(input, args.topN, KiwiCS.Match.All);
                
                origOutput.WriteLine(input.Trim());
                for (int i = 0; i < res.Length; i++)
                {
                    taggedOutput.WriteLine(morphsToString(res[i].morphs, args.formatTag, args.formatSep));
                }
            }
            else
            {
                var r = instKiwi.Analyze(input, 1, KiwiCS.Match.All)[0];
                int wp = 0, sp = 0, cp = 0;
                List<KiwiCS.Token> s = new List<KiwiCS.Token>();

                foreach (var m in r.morphs)
                {
                    if (args.mode == AnalyzeMode.Word ? (wp != m.wordPosition || sp != m.sentPosition) : (sp != m.sentPosition))
                    {
                        origOutput.WriteLine(input.Substring(cp, (int)m.chrPosition - cp).Replace('\n', ' ').Replace('\r', ' ').Trim());
                        taggedOutput.WriteLine(morphsToString(s, args.formatTag, args.formatSep));
                        cp = (int)m.chrPosition;
                        s.Clear();
                    }
                    s.Add(m);
                    wp = (int)m.wordPosition;
                    sp = (int)m.sentPosition;
                }

                if (s.Count > 0)
                {
                    origOutput.WriteLine(input.Substring(cp).Replace('\n', ' ').Replace('\r', ' ').Trim());
                    taggedOutput.WriteLine(morphsToString(s, args.formatTag, args.formatSep));
                }
            }
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            WorkerArgs args = (WorkerArgs)e.Argument;
            worker.ReportProgress(1);
            int n = 0;
            foreach(string path in args.fileList)
            {
                n++;
                if(worker.CancellationPending)
                {
                    e.Cancel = true;
                    return;
                }
                if (separateResult)
                {
                    using (StreamWriter taggedOutput = new StreamWriter(path + ".tagged"), origOutput = new StreamWriter(path + ".orig"))
                    {
                        analyzeWriteResult(MainWindow.GetFileText(path), args, taggedOutput, origOutput);
                    }
                }
                else
                {
                    using (StreamWriter output = new StreamWriter(path + ".tagged"))
                    {
                        analyzeWriteResult(MainWindow.GetFileText(path), args, output, output);
                    }
                }
                
                worker.ReportProgress((int)(n * 100.0 / args.fileList.Count));
            }
        }

        private void AddBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "텍스트 파일|*.txt|모든 파일|*.*";
            ofd.Title = "분석할 텍스트 파일 열기";
            ofd.Multiselect = true;
            if (ofd.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
            foreach(string name in ofd.FileNames)
            {
                FileList.Items.Add(name);
            }
            StartBtn.IsEnabled = true;
        }

        private void AddFolderBtn_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog fd = new FolderBrowserDialog();
            if (fd.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
            FileList.Items.Add(fd.SelectedPath);
        }

        private void DelBtn_Click(object sender, RoutedEventArgs e)
        {
            List<string> sel = new List<string>();
            foreach(string i in FileList.SelectedItems)
            {
                sel.Add(i);
            }

            for (int n = FileList.Items.Count - 1; n >= 0; --n)
            {
                string v = FileList.Items[n].ToString();
                if (sel.IndexOf(v) >= 0)
                {
                    FileList.Items.RemoveAt(n);
                }
            }
            StartBtn.IsEnabled = FileList.Items.Count > 0;
        }

        private void StartBtn_Click(object sender, RoutedEventArgs e)
        {
            StopBtn.Visibility = Visibility.Visible;
            StartBtn.Visibility = Visibility.Collapsed;
            WorkerArgs args;
            args.fileList = new List<string>();
            foreach (string i in FileList.Items)
            {
                var attr = File.GetAttributes(i);
                if(Directory.Exists(i))
                {
                    foreach (string j in Directory.GetFiles(i)) args.fileList.Add(j);
                }
                else if(File.Exists(i)) args.fileList.Add(i);
            }
            args.mode = (AnalyzeMode)TypeCmb.SelectedIndex;
            args.topN = TopNCmb.SelectedIndex + 1;
            args.formatTag = FormatCmb.SelectedIndex % 2 == 1;
            args.formatSep = FormatCmb.SelectedIndex / 2 == 1 ? " + " : "\t";
            args.integrateAllomorph = IntegrateAllomorph.IsChecked.Value;
            worker.RunWorkerAsync(args);
        }

        private void StopBtn_Click(object sender, RoutedEventArgs e)
        {
            StopBtn.Visibility = Visibility.Collapsed;
            StartBtn.Visibility = Visibility.Visible;
            worker.CancelAsync();
            Prg.Value = 0;
        }

        private void FileList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            DelBtn.IsEnabled = FileList.SelectedIndex >= 0;
        }

        private void SeparateResult_Checked(object sender, RoutedEventArgs e)
        {
            if (MsgWithoutSeparation == null) return;
            MsgWithoutSeparation.Visibility = Visibility.Collapsed;
            MsgWithSeparation.Visibility = Visibility.Visible;
            separateResult = true;
        }

        private void SeparateResult_Unchecked(object sender, RoutedEventArgs e)
        {
            if (MsgWithoutSeparation == null) return;
            MsgWithoutSeparation.Visibility = Visibility.Visible;
            MsgWithSeparation.Visibility = Visibility.Collapsed;
            separateResult = false;
        }
    }
}
