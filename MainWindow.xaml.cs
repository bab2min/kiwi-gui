using Microsoft.Win32;
using System;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.IO;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Windows.Documents;

namespace KiwiGui
{
    public class RichTextBoxHelper : DependencyObject
    {
        public static List<List<KiwiCS.Token>> GetDocumentKiwiToken(DependencyObject obj)
        {
            return (List<List<KiwiCS.Token>>)obj.GetValue(DocumentKiwiTokenProperty);
        }

        public static void SetDocumentKiwiToken(DependencyObject obj, List<List<KiwiCS.Token>> value)
        {
            obj.SetValue(DocumentKiwiTokenProperty, value);
        }

        public static readonly DependencyProperty DocumentKiwiTokenProperty = DependencyProperty.RegisterAttached(
            "DocumentKiwiToken",
            typeof(List<List<KiwiCS.Token>>),
            typeof(RichTextBoxHelper),
            new FrameworkPropertyMetadata
            {
                BindsTwoWayByDefault = true,
                PropertyChangedCallback = (obj, e) =>
                {
                    Brush brushDef = new SolidColorBrush(Color.FromRgb(0, 0, 0));
                    Brush brushMorph = new SolidColorBrush(Color.FromRgb(0, 150, 0));
                    Brush brushTag = new SolidColorBrush(Color.FromRgb(0, 0, 150));

                    var richTextBox = (RichTextBox)obj;
                    var items = (List<List<KiwiCS.Token>>)richTextBox.GetValue(DocumentKiwiTokenProperty);
                    var doc = new FlowDocument();

                    Run t;
                    Paragraph para = new Paragraph();
                    foreach (var item in items)
                    {
                        int c = 0;
                        if (para.Inlines.Count > 0)
                        {
                            para.Inlines.Add(new LineBreak());
                            para.Inlines.Add(new LineBreak());
                        }

                        foreach (var m in item)
                        {
                            if (c++ > 0)
                            {
                                t = new Run(" + ");
                                t.Foreground = brushDef;
                                para.Inlines.Add(t);
                            }
                            Bold b = new Bold();
                            b.Inlines.Add(m.form);
                            b.Foreground = brushMorph;
                            para.Inlines.Add(b);
                            t = new Run("/");
                            t.Foreground = brushDef;
                            para.Inlines.Add(t);
                            b = new Bold();
                            b.Inlines.Add(m.tag);
                            b.Foreground = brushTag;
                            para.Inlines.Add(b);
                        }
                    }
                    doc.Blocks.Add(para);

                    richTextBox.Document = doc;
                }
            }
        );
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        KiwiCS.Kiwi instKiwi;
        KiwiCS.ModelType modelType = KiwiCS.ModelType.SBG;
        bool useTypoCorrection = false;
        bool useMultiDict = true;
        ObservableCollection<AnalyzeResult> resultData;

        private class AnalyzeResult
        {
            public int Id { get; set; }
            public string Input { get; set; }
            public List<List<KiwiCS.Token>> Result { get; set; }

            public AnalyzeResult(int Id, string Input, List<List<KiwiCS.Token>> Result)
            {
                this.Id = Id;
                this.Input = Input;
                this.Result = Result;
            }

            public AnalyzeResult(int Id, string Input, List<KiwiCS.Token> Result)
            {
                this.Id = Id;
                this.Input = Input;
                this.Result = new List<List<KiwiCS.Token>>();
                this.Result.Add(Result);
            }
        }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            try
            {
                string version = KiwiCS.Kiwi.Version();
                VersionInfo.Header = String.Format("Kiwi 버전 {0}", version);
                Title += " v" + version;
                var builder = new KiwiCS.KiwiBuilder("model/", 0,
                    KiwiCS.Option.LoadDefaultDict
                    | KiwiCS.Option.LoadTypoDict
                    | (useMultiDict ? KiwiCS.Option.LoadMultiDict : 0),
                    modelType);
                if (useTypoCorrection)
                {
                    instKiwi = builder.Build(new KiwiCS.TypoTransformer(KiwiCS.DefaultTypoSet.BasicTypoSetWithContinual));
                }
                else
                {
                    instKiwi = builder.Build();
                }
                ResultBlock.DataContext = resultData = new ObservableCollection<AnalyzeResult>();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Kiwi 형태소 분석기를 초기화하는 데 실패했습니다. 모델 파일이 없거나 인자가 잘못되었습니다.\n오류 메세지: "
                    + ex.Message, "Kiwi 오류", MessageBoxButton.OK, MessageBoxImage.Error);
                MessageBox.Show(this, "Kiwi 형태소 분석기를 초기화하는 데 실패했습니다. 모델 파일이 없거나 인자가 잘못되었습니다.\n오류 메세지: "
                    + ex.Message, "Kiwi 오류", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private void UpdateKiwiModel()
        {
            var builder = new KiwiCS.KiwiBuilder("model/", 0, 
                KiwiCS.Option.LoadDefaultDict 
                | KiwiCS.Option.LoadTypoDict 
                | (useMultiDict ? KiwiCS.Option.LoadMultiDict : 0), 
                modelType);
            if (useTypoCorrection)
            {
                instKiwi = builder.Build(new KiwiCS.TypoTransformer(KiwiCS.DefaultTypoSet.BasicTypoSetWithContinual));
            }
            else
            {
                instKiwi = builder.Build();
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
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
            string res = "";
            foreach(var r in resultData)
            {
                res += r.Input;
                res += "\n";
                int c = 0;
                foreach(var l in r.Result)
                {
                    if (c++ > 0) res += "\n";
                    int d = 0;
                    foreach(var m in l)
                    {
                        if(d++ > 0) res += " + ";
                        res += m.form + "/" + m.tag;
                    }
                }
                res += "\n\n";
            }
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

        private IEnumerable<AnalyzeResult> AnalyzeText(string text, int mode, KiwiCS.Match match, int topN = 1)
        {
            if (mode == 2)
            {
                int rid = 0;
                foreach(var line in text.Trim().Split('\n'))
                {
                    var trimmed = line.Trim();
                    var res = instKiwi.Analyze(trimmed, topN, match);
                    List<List<KiwiCS.Token>> s = new List<List<KiwiCS.Token>>();
                    foreach (var r in res)
                    {
                        if (r.morphs.Length == 0) continue;
                        s.Add(new List<KiwiCS.Token>(r.morphs));
                    }
                    if (trimmed.Length == 0) continue;

                    yield return new AnalyzeResult(++rid, trimmed, s);
                }
            }
            else if(mode == 3)
            {
                var res = instKiwi.Analyze(text, topN, match);
                List<List<KiwiCS.Token>> s = new List<List<KiwiCS.Token>>();
                foreach (var r in res)
                {
                    if (r.morphs.Length == 0) continue;
                    s.Add(new List<KiwiCS.Token>(r.morphs));
                }
                yield return new AnalyzeResult(1, text.Trim(), s);
            }
            else
            {
                var r = instKiwi.Analyze(text, 1, match)[0];
                int wp = 0, sp = 0, cp = 0, rid = 0, c = 0;
                List<KiwiCS.Token> s = new List<KiwiCS.Token>();
                
                foreach (var m in r.morphs)
                {
                    if(mode == 0 ? (wp != m.wordPosition || sp != m.sentPosition) : (sp != m.sentPosition) )
                    {
                        yield return new AnalyzeResult(++rid, text.Substring(cp, (int)m.chrPosition - cp).Trim(), s);
                        cp = (int)m.chrPosition;
                        c = 0;
                        s = new List<KiwiCS.Token>();
                    }
                    s.Add(m);
                    wp = (int)m.wordPosition;
                    sp = (int)m.sentPosition;
                }

                if (s.Count > 0)
                {
                    yield return new AnalyzeResult(++rid, text.Substring(cp), s);
                }
            }
        }
        private void UpdateAnalyzeResult()
        {
            if (InputTxt == null || ResultBlock == null) return;
            //ResultBlock.Document.Blocks.Clear();

            resultData.Clear();

            App.monitor.TrackAtomicFeature("Kiwi_Menu", "Analyze", InputTxt.Text);
            int topN = TopNCmb.SelectedIndex + 1;
            instKiwi.IntegrateAllomorph = IntegratedAllomorph.IsChecked.Value;
            
            KiwiCS.Match match = 0;
            if (NormalizeCoda.IsChecked.Value) match |= KiwiCS.Match.NormalizeCoda;
            if (ZCoda.IsChecked.Value) match |= KiwiCS.Match.ZCoda;
            if (MatchUrl.IsChecked.Value) match |= KiwiCS.Match.Url;
            if (MatchEmail.IsChecked.Value) match |= KiwiCS.Match.Email;
            if (MatchHashtag.IsChecked.Value) match |= KiwiCS.Match.Hashtag;
            if (MatchMention.IsChecked.Value) match |= KiwiCS.Match.Mention;
            if (MatchSerial.IsChecked.Value) match |= KiwiCS.Match.Serial;
            if (JoinNounPrefix.IsChecked.Value) match |= KiwiCS.Match.JoinNounPrefix;
            if (JoinNounSuffix.IsChecked.Value) match |= KiwiCS.Match.JoinNounSuffix;
            if (JoinVerbSuffix.IsChecked.Value) match |= KiwiCS.Match.JoinVerbSuffix;
            if (JoinAdjSuffix.IsChecked.Value) match |= KiwiCS.Match.JoinAdjSuffix;
            if (JoinAdvSuffix.IsChecked.Value) match |= KiwiCS.Match.JoinAdvSuffix;
            if (SaisiotTrue.IsChecked.Value) match |= KiwiCS.Match.SplitSaisiot;
            if (SaisiotFalse.IsChecked.Value) match |= KiwiCS.Match.MergeSaisiot;

            bool hasContent = false;
            foreach(var r in AnalyzeText(InputTxt.Text, TypeCmb.SelectedIndex, match, topN))
            {
                hasContent = true;
                resultData.Add(r);
            }
            MenuSave.IsEnabled = hasContent;
        }
        private void AnalyzeBtn_Click(object sender, RoutedEventArgs e)
        {
            UpdateAnalyzeResult();
        }

        private void UpdateAnalyzeResultFromEvent(object sender, RoutedEventArgs e)
        {
            if (AutoAnalyze == null || !AutoAnalyze.IsChecked) return;
            UpdateAnalyzeResult();
        }

        private void TypeCmb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AutoAnalyze == null || !AutoAnalyze.IsChecked) return;
            UpdateAnalyzeResult();
        }

        private void MenuItem_Wrap_Checked(object sender, RoutedEventArgs e)
        {
            if (InputTxt == null) return;
            InputTxt.HorizontalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Hidden;
            InputTxt.TextWrapping = TextWrapping.Wrap;
        }

        private void MenuItem_Wrap_Unchecked(object sender, RoutedEventArgs e)
        {
            if (InputTxt == null) return;
            InputTxt.HorizontalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto;
            InputTxt.TextWrapping = TextWrapping.NoWrap;
        }

        private void ResultBlock_CellEditEnding(object sender, System.Windows.Controls.DataGridCellEditEndingEventArgs e)
        {
            e.Cancel = true;
        }

        private void MenuKNLM_Checked(object sender, RoutedEventArgs e)
        {
            if (InputTxt == null) return;
            if (modelType == KiwiCS.ModelType.KNLM) return;
            MenuKNLM.IsChecked = true;
            MenuSBG.IsChecked = false;
            modelType = KiwiCS.ModelType.KNLM;
            UpdateKiwiModel();
            UpdateAnalyzeResult();
        }

        private void MenuSBG_Checked(object sender, RoutedEventArgs e)
        {
            if (InputTxt == null) return;
            if (modelType == KiwiCS.ModelType.SBG) return;
            MenuKNLM.IsChecked = false;
            MenuSBG.IsChecked = true;
            modelType = KiwiCS.ModelType.SBG;
            UpdateKiwiModel();
            UpdateAnalyzeResult();
        }

        private void MenuTypo_Checked(object sender, RoutedEventArgs e)
        {
            if (InputTxt == null) return;
            useTypoCorrection = true;
            UpdateKiwiModel();
            UpdateAnalyzeResult();
        }

        private void MenuTypo_Unchecked(object sender, RoutedEventArgs e)
        {
            if (InputTxt == null) return;
            useTypoCorrection = false;
            UpdateKiwiModel();
            UpdateAnalyzeResult();
        }

        private void MenuMulti_Checked(object sender, RoutedEventArgs e)
        {
            if (InputTxt == null) return;
            useMultiDict = true;
            UpdateKiwiModel();
            UpdateAnalyzeResult();
        }

        private void MenuMulti_Unchecked(object sender, RoutedEventArgs e)
        {
            if (InputTxt == null) return;
            useMultiDict = false;
            UpdateKiwiModel();
            UpdateAnalyzeResult();
        }
    }

}
