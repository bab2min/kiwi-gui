using KiwiCS;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;

namespace KiwiGui
{
    /// <summary>
    /// Interaction logic for ModelExplorer.xaml
    /// </summary>
    public partial class ModelExplorer : Window
    {
        private struct MorphemeItem
        {
            public KiwiCS.Morpheme data;
            public uint id;

            public string Form { get => data.form; }
            public string Tag { get => data.tag; }
            public string POS { get => KiwiCS.Kiwi.TagToPOS(data.tag); }
            public string SenseId { get => data.senseId > 0 ? data.senseId.ToString() : ""; }
            public string Misc { get => data.dialect > 0 ? "방언" : "";  }
            public MorphemeItem(KiwiCS.Morpheme morpheme, uint id)
            {
                this.data = morpheme;
                this.id = id;
            }
        }


        private ObservableCollection<MorphemeItem> searchedMorphemes;

        // Debounce timer fields
        private DispatcherTimer searchDebounceTimer;
        private const int DebounceMilliseconds = 250;

        private List<uint> navigationHistory = new List<uint>();
        private int currentHistoryIndex = 0;

        public ModelExplorer()
        {
            InitializeComponent();
        }

        private void SearchForm_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (searchDebounceTimer == null) return;

            if (searchDebounceTimer.IsEnabled)
            {
                searchDebounceTimer.Stop();
            }
            searchDebounceTimer.Start();
        }

        private void SearchDebounceTimer_Tick(object sender, EventArgs e)
        {
            PerformSearch(SearchForm.Text ?? string.Empty);
        }

        private void PerformSearch(string query)
        {
            searchDebounceTimer.Stop();
            try
            {
                if (Owner == null || SearchForm == null) return;
                var mainWin = Owner as MainWindow;
                if (mainWin == null) return;
                var kiwi = mainWin.GetKiwiInstance();
                if (kiwi == null) return;

                var morphemes = kiwi.FindMorphemesWithPrefix(query, null, -1, 50);
                for (int i = 0; i < morphemes.Length; i++)
                {
                    var info = kiwi.GetMorphemeInfo(morphemes[i]);
                    if (i < searchedMorphemes.Count)
                    {
                        searchedMorphemes[i] = new MorphemeItem(info, morphemes[i]);
                    }
                    else
                    {
                        searchedMorphemes.Add(new MorphemeItem(info, morphemes[i]));
                    }
                }

                if (morphemes.Length < searchedMorphemes.Count)
                {
                    for (int j = searchedMorphemes.Count - 1; j >= morphemes.Length; j--)
                    {
                        searchedMorphemes.RemoveAt(j);
                    }
                }
            }
            catch
            {
                // Ignore errors
            }
        }

        private void MorphemeList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Owner == null || MorphemeList == null) return;
            if (MorphemeList.SelectedItem == null) return;
            var selectedMorpheme = (MorphemeItem)MorphemeList.SelectedItem;
            UpdateInfo(selectedMorpheme);
            navigationHistory.RemoveRange(currentHistoryIndex, navigationHistory.Count - currentHistoryIndex);
            navigationHistory.Add(selectedMorpheme.id);
            currentHistoryIndex = navigationHistory.Count;

            BackwardBtn.IsEnabled = currentHistoryIndex > 1;
            ForwardBtn.IsEnabled = currentHistoryIndex < navigationHistory.Count;
        }
        private ListItem GenerateSimilarMorphemeItem(string form, string similarity, string id)
        {
            /*
            <ListItem>
                <Paragraph>
                    <Hyperlink NavigateUri="kiwi-morpheme://[I]">
                        <Run Text=""[F]" FontWeight="Bold"/>
                    </Hyperlink>
                    <Run Text=": [S]"/>
                </Paragraph>
            </ListItem>
            */
            var listItem = new ListItem();
            var paragraph = new Paragraph();
            var hyperlink = new Hyperlink();
            hyperlink.NavigateUri = new Uri($"kiwi-morpheme://{id}");
            hyperlink.RequestNavigate += DetailMorphemeIdLink_Click;
            var runForm = new Run(form);
            runForm.FontWeight = FontWeights.Bold;
            hyperlink.Inlines.Add(runForm);
            paragraph.Inlines.Add(hyperlink);
            var runSim = new Run(": " + similarity);
            paragraph.Inlines.Add(runSim);
            listItem.Blocks.Add(paragraph);
            return listItem;
        }
        private void UpdateInfo(MorphemeItem morpheme)
        {
            var mainWin = Owner as MainWindow;
            if (mainWin == null) return;
            var kiwi = mainWin.GetKiwiInstance();
            if (kiwi == null) return;

            DetailForm.Text = morpheme.Form;
            DetailId.Text = morpheme.id.ToString();
            DetailTag.Text = morpheme.Tag + " (" + morpheme.POS + ")";
            DetailSenseId.Text = morpheme.data.senseId.ToString();
            DetailLmMorphemeId.Text = morpheme.data.lmMorphemeId.ToString();
            DetailLmMorphemeIdLink.NavigateUri = null;
            DetailLmMorphemeIdLink.IsEnabled = false;
            if (morpheme.data.lmMorphemeId > 0 && morpheme.data.lmMorphemeId != morpheme.id)
            {
                var lmMorphemeInfo = kiwi.GetMorphemeInfo(morpheme.data.lmMorphemeId);
                if (lmMorphemeInfo.form != null && lmMorphemeInfo.form.Length > 0)
                {
                    DetailLmMorphemeId.Text += " (" + lmMorphemeInfo.form + "/" + lmMorphemeInfo.tag + ")";
                    DetailLmMorphemeIdLink.NavigateUri = new Uri($"kiwi-morpheme://{morpheme.data.lmMorphemeId}");
                    DetailLmMorphemeIdLink.IsEnabled = true;
                }
            }

            DetailOrigMorphemeId.Text = morpheme.data.origMorphemeId.ToString();
            DetailOrigMorphemeIdLink.NavigateUri = null;
            DetailOrigMorphemeIdLink.IsEnabled = false;
            if (morpheme.data.origMorphemeId > 0)
            {
                var origMorphemeInfo = kiwi.GetMorphemeInfo(morpheme.data.origMorphemeId);
                if (origMorphemeInfo.form != null && origMorphemeInfo.form.Length > 0)
                {
                    DetailOrigMorphemeId.Text += " (" + origMorphemeInfo.form + "/" + origMorphemeInfo.tag + ")";
                    DetailOrigMorphemeIdLink.NavigateUri = new Uri($"kiwi-morpheme://{morpheme.data.origMorphemeId}");
                    DetailOrigMorphemeIdLink.IsEnabled = true;
                }
            }
            DetailDialect.Text = ((int)morpheme.data.dialect).ToString() + " (" + KiwiCS.Kiwi.DialectToString(morpheme.data.dialect) + ")";

            DetailSimilarMorphemes.ListItems.Clear();
            var sims = kiwi.MostSimilarWords(morpheme.id, 10);
            for (int i = 0; i < sims.Length; i++)
            {
                var simMorphemeInfo = kiwi.GetMorphemeInfo(sims[i].Item1);
                var item = GenerateSimilarMorphemeItem($"{sims[i].Item1} ({simMorphemeInfo.form}/{simMorphemeInfo.tag})", $"{sims[i].Item2:F4}", sims[i].Item1.ToString());
                DetailSimilarMorphemes.ListItems.Add(item);
            }
        }

        private void BackwardBtn_Click(object sender, RoutedEventArgs e)
        {
            var kiwi = (Owner as MainWindow)?.GetKiwiInstance();
            if (kiwi == null) return;

            if (currentHistoryIndex <= 1) return;
            currentHistoryIndex--;
            var morphemeId = navigationHistory[currentHistoryIndex - 1];
            var morphemeInfo = kiwi.GetMorphemeInfo(morphemeId);
            UpdateInfo(new MorphemeItem(morphemeInfo, morphemeId));
            PerformSearch(morphemeInfo.form);

            BackwardBtn.IsEnabled = currentHistoryIndex > 1;
            ForwardBtn.IsEnabled = currentHistoryIndex < navigationHistory.Count;
        }

        private void ForwardBtn_Click(object sender, RoutedEventArgs e)
        {
            var kiwi = (Owner as MainWindow)?.GetKiwiInstance();
            if (kiwi == null) return;

            if (currentHistoryIndex >= navigationHistory.Count) return;
            var morphemeId = navigationHistory[currentHistoryIndex];
            currentHistoryIndex++;
            var morphemeInfo = kiwi.GetMorphemeInfo(morphemeId);
            UpdateInfo(new MorphemeItem(morphemeInfo, morphemeId));
            PerformSearch(morphemeInfo.form);

            BackwardBtn.IsEnabled = currentHistoryIndex > 1;
            ForwardBtn.IsEnabled = currentHistoryIndex < navigationHistory.Count;
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            MorphemeList.DataContext = searchedMorphemes = new ObservableCollection<MorphemeItem>();

            searchDebounceTimer = new DispatcherTimer();
            searchDebounceTimer.Interval = TimeSpan.FromMilliseconds(DebounceMilliseconds);
            searchDebounceTimer.Tick += SearchDebounceTimer_Tick;
        }

        private void DetailMorphemeIdLink_Click(object sender, RoutedEventArgs e)
        {
            uint morphemeId;
            var link = sender as Hyperlink;
            if (link == null || link.NavigateUri == null ||
                !uint.TryParse(link.NavigateUri.AbsoluteUri.Replace("kiwi-morpheme://", "").Replace("/", ""), out morphemeId))
            {
                return;
            }
            var kiwi = (Owner as MainWindow)?.GetKiwiInstance();
            if (kiwi == null) return;
            var morphemeInfo = kiwi.GetMorphemeInfo(morphemeId);
            UpdateInfo(new MorphemeItem(morphemeInfo, morphemeId));
            PerformSearch(morphemeInfo.form);

            navigationHistory.RemoveRange(currentHistoryIndex, navigationHistory.Count - currentHistoryIndex);
            navigationHistory.Add(morphemeId);
            currentHistoryIndex = navigationHistory.Count;

            BackwardBtn.IsEnabled = currentHistoryIndex > 1;
            ForwardBtn.IsEnabled = currentHistoryIndex < navigationHistory.Count;
        }
    }
}
