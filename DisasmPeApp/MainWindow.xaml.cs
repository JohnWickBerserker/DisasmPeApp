using DisasmPeApp.ViewModels;
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

namespace DisasmPeApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.SubList.MouseDoubleClick += SubList_MouseDoubleClick;
        }

        private void SubList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var list = (ListBox)sender;
            var itemRaw = list.Items[list.SelectedIndex];
            var item = (SubViewModel)itemRaw;
            this.DisasmTextBox.ScrollToLine(item.Line);
        }
    }
}
