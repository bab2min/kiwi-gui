using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace KiwiGui
{
    /// <summary>
    /// Interaction logic for DialectSelectDlg.xaml
    /// </summary>
    public partial class DialectSelectDlg : Window
    {
        public KiwiCS.Dialect selectedDialect;
        public DialectSelectDlg(KiwiCS.Dialect dialect)
        {
            InitializeComponent();

            Standard.IsChecked = dialect == KiwiCS.Dialect.Standard;
            All.IsChecked = dialect == KiwiCS.Dialect.All;

            Gyeonggi.IsChecked = (dialect & KiwiCS.Dialect.Gyeonggi) != 0;
            Chungcheong.IsChecked = (dialect & KiwiCS.Dialect.Chungcheong) != 0;
            Gangwon.IsChecked = (dialect & KiwiCS.Dialect.Gangwon) != 0;
            Gyeongsang.IsChecked = (dialect & KiwiCS.Dialect.Gyeongsang) != 0;
            Jeolla.IsChecked = (dialect & KiwiCS.Dialect.Jeolla) != 0;
            Jeju.IsChecked = (dialect & KiwiCS.Dialect.Jeju) != 0;
            Hwanghae.IsChecked = (dialect & KiwiCS.Dialect.Hwanghae) != 0;
            Hamgyeong.IsChecked = (dialect & KiwiCS.Dialect.Hamgyeong) != 0;
            Pyeongan.IsChecked = (dialect & KiwiCS.Dialect.Pyeongan) != 0;
            Archaic.IsChecked = (dialect & KiwiCS.Dialect.Archaic) != 0;
        }

        private void OKBtn_Click(object sender, RoutedEventArgs e)
        {
            selectedDialect = KiwiCS.Dialect.Standard;

            if (Gyeonggi.IsChecked == true) selectedDialect |= KiwiCS.Dialect.Gyeonggi;
            if (Chungcheong.IsChecked == true) selectedDialect |= KiwiCS.Dialect.Chungcheong;
            if (Gangwon.IsChecked == true) selectedDialect |= KiwiCS.Dialect.Gangwon;
            if (Gyeongsang.IsChecked == true) selectedDialect |= KiwiCS.Dialect.Gyeongsang;
            if (Jeolla.IsChecked == true) selectedDialect |= KiwiCS.Dialect.Jeolla;
            if (Jeju.IsChecked == true) selectedDialect |= KiwiCS.Dialect.Jeju;
            if (Hwanghae.IsChecked == true) selectedDialect |= KiwiCS.Dialect.Hwanghae;
            if (Hamgyeong.IsChecked == true) selectedDialect |= KiwiCS.Dialect.Hamgyeong;
            if (Pyeongan.IsChecked == true) selectedDialect |= KiwiCS.Dialect.Pyeongan;
            if (Archaic.IsChecked == true) selectedDialect |= KiwiCS.Dialect.Archaic;

            DialogResult = true;
            Close();
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private void Standard_Checked(object sender, RoutedEventArgs e)
        {
            if (All == null) return;

            All.IsChecked = false;
            Gyeonggi.IsChecked = false;
            Chungcheong.IsChecked = false;
            Gangwon.IsChecked = false;
            Gyeongsang.IsChecked = false;
            Jeolla.IsChecked = false;
            Jeju.IsChecked = false;
            Hwanghae.IsChecked = false;
            Hamgyeong.IsChecked = false;
            Pyeongan.IsChecked = false;
            Archaic.IsChecked = false;
        }

        private void All_Checked(object sender, RoutedEventArgs e)
        {
            if (All == null) return;

            Standard.IsChecked = false;
            Gyeonggi.IsChecked = true;
            Chungcheong.IsChecked = true;
            Gangwon.IsChecked = true;
            Gyeongsang.IsChecked = true;
            Jeolla.IsChecked = true;
            Jeju.IsChecked = true;
            Hwanghae.IsChecked = true;
            Hamgyeong.IsChecked = true;
            Pyeongan.IsChecked = true;
            Archaic.IsChecked = true;
        }
        private void Selection_Changed(object sender, RoutedEventArgs e)
        {
            if (All == null) return;

            Standard.IsChecked = Gyeonggi.IsChecked == false &&
                                 Chungcheong.IsChecked == false &&
                                 Gangwon.IsChecked == false &&
                                 Gyeongsang.IsChecked == false &&
                                 Jeolla.IsChecked == false &&
                                 Jeju.IsChecked == false &&
                                 Hwanghae.IsChecked == false &&
                                 Hamgyeong.IsChecked == false &&
                                 Pyeongan.IsChecked == false &&
                                 Archaic.IsChecked == false;
            All.IsChecked = Gyeonggi.IsChecked == true &&
                            Chungcheong.IsChecked == true &&
                            Gangwon.IsChecked == true &&
                            Gyeongsang.IsChecked == true &&
                            Jeolla.IsChecked == true &&
                            Jeju.IsChecked == true &&
                            Hwanghae.IsChecked == true &&
                            Hamgyeong.IsChecked == true &&
                            Pyeongan.IsChecked == true &&
                            Archaic.IsChecked == true;
        }
    }
}
