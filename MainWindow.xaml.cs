﻿using Microsoft.Win32;
using System;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.IO;
using System.ComponentModel;
using System.Windows.Threading;

namespace KiwiGui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        KiwiCS instKiwi;
        
        public MainWindow()
        {
            InitializeComponent();
            Splash splashScreen = new Splash();
            splashScreen.Show();
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += (s, args) =>
            {
                int version = KiwiCS.Version();
                Dispatcher.Invoke(DispatcherPriority.Normal, new Action(()=>
                {
                    VersionInfo.Header = String.Format("Kiwi 버전 {0}.{1}.{2}", version / 100 % 10, version / 10 % 10, version % 10);
                    Title += " v" + String.Format("{0}.{1}.{2}", version / 100 % 10, version / 10 % 10, version % 10);
                }));
                instKiwi = new KiwiCS("model/", 0, 1);
                instKiwi.prepare();
            };
            bw.RunWorkerCompleted += (s, args) =>
            {
                splashScreen.Close();
                if (args.Error != null)
                {
                    MessageBox.Show(this, "Kiwi 형태소 분석기를 초기화하는 데 실패했습니다. 모델 파일이 없거나 인자가 잘못되었습니다.\n오류 메세지: "
                    + args.Error.Message, "Kiwi 오류", MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                    return;
                }
                Show();
            };
            bw.RunWorkerAsync();
            
            App.monitor.TrackScreenView("Kiwi_MainWindow");
            Hide();
        }

        public static string GetFileText(string path)
        {
            string ftxt = File.ReadAllText(path);
            if (ftxt.IndexOf('\xFFFD') >= 0)
            {
                ftxt = File.ReadAllText(path, Encoding.Default);
            }
            return ftxt;
        }

        public void extractWords(string filePath)
        {
            using (StreamReader reader = new StreamReader(filePath, Encoding.UTF8, true))
            {
                KiwiCS.ExtractedWord[] res = instKiwi.extractWords((id)=>
                {
                    if (id == 0) reader.BaseStream.Seek(0, SeekOrigin.Begin);
                    return reader.ReadLine();
                });
                foreach(var r in res)
                {
                    Console.Out.WriteLine(r.word);
                }
            }
        }

        private void MenuItem_Open(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "텍스트 파일|*.txt|모든 파일|*.*";
            ofd.Title = "분석할 텍스트 파일 열기";

            if (ofd.ShowDialog() != true) return;
            InputTxt.Text = GetFileText(ofd.FileName);
            App.monitor.TrackAtomicFeature("Kiwi_Menu", "Open", ofd.FileName);
        }

        private void MenuItem_Save(object sender, RoutedEventArgs e)
        {
            SaveFileDialog ofd = new SaveFileDialog();
            ofd.Filter = "텍스트 파일|*.txt|모든 파일|*.*";
            ofd.Title = "분석 결과를 저장할 파일 경로";

            if (ofd.ShowDialog() != true) return;
            string res = new TextRange(ResultBlock.Document.ContentStart, ResultBlock.Document.ContentEnd).Text;
            File.WriteAllText(ofd.FileName, res);
            App.monitor.TrackAtomicFeature("Kiwi_Menu", "Save", res);
        }

        private void MenuItem_Batch(object sender, RoutedEventArgs e)
        {
            BatchDlg dlg = new BatchDlg();
            dlg.Owner = this;
            dlg.instKiwi = instKiwi;
            dlg.ShowDialog();
        }

        private void MenuItem_Close(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MenuItem_Homepage(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://lab.bab2min.pe.kr/kiwi");
        }

        private void MenuItem_GitHub(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/bab2min/kiwi");
        }

        private void MenuItem_Blog(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://bab2min.tistory.com/category/%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%98%EB%B0%8D/NLP");
        }

        private void AnalyzeBtn_Click(object sender, RoutedEventArgs e)
        {
            ResultBlock.Document.Blocks.Clear();
            App.monitor.TrackAtomicFeature("Kiwi_Menu", "Analyze", InputTxt.Text);
            string[] lines = TypeCmb.SelectedIndex == 0 ? InputTxt.Text.Trim().Split('\n') : new string[]{ InputTxt.Text.Trim() };
            int topN = TopNCmb.SelectedIndex + 1;
            Brush brushDef = new SolidColorBrush(Color.FromRgb(0, 0, 0));
            Brush brushMorph = new SolidColorBrush(Color.FromRgb(0, 150, 0));
            Brush brushTag = new SolidColorBrush(Color.FromRgb(0, 0, 150));
            bool content = false;
            foreach (var line in lines)
            {
                if (line.Length == 0) continue;
                content = true;
                var res = instKiwi.analyze(line.Trim(), topN);
                Run t = new Run(line.Trim());
                t.Foreground = brushDef;
                Paragraph para = new Paragraph();
                para.Inlines.Add(t);
                foreach (var r in res)
                {
                    para.Inlines.Add(new LineBreak());
                    int c = 0;
                    foreach (var m in r.morphs)
                    {
                        if (c++ > 0)
                        {
                            t = new Run(" + ");
                            t.Foreground = brushDef;
                            para.Inlines.Add(t);
                        }
                        Bold b = new Bold();
                        b.Inlines.Add(m.Item1);
                        b.Foreground = brushMorph;
                        para.Inlines.Add(b);
                        t = new Run("/");
                        t.Foreground = brushDef;
                        para.Inlines.Add(t);
                        b = new Bold();
                        b.Inlines.Add(m.Item2);
                        b.Foreground = brushTag;
                        para.Inlines.Add(b);
                    }
                }
                ResultBlock.Document.Blocks.Add(para);
            }
            MenuSave.IsEnabled = content;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
        }

    }
}
