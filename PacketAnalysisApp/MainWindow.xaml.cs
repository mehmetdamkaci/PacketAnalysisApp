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

namespace PacketAnalysisApp
{

    public partial class MainWindow : Window
    {
        EnumMatchWindow enumMatchWindow = new EnumMatchWindow();
        Dictionary<string, Dictionary<int, string>> enumStruct = new Dictionary<string, Dictionary<int, string>>();
        public MainWindow()
        {
            InitializeComponent();
            enumMatchWindow.Closed += enumMatchClosed;
            enumMatchWindow.OkKaydetLog.Click += enumKaydetClick;
            enumMatchWindow.UpdatedList += EnumMatchingWindow_UpdatedList;
        }

        // -------------------- DATA GRID DETAY BLOKLARI --------------------
        private void Expander_Expanded(object sender, RoutedEventArgs e)
        {
            for (var vis = sender as Visual; vis != null; vis = VisualTreeHelper.GetParent(vis) as Visual)
                if (vis is DataGridRow)
                {
                    var row = (DataGridRow)vis;
                    row.DetailsVisibility = row.DetailsVisibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
                    break;
                }
        }

        private void Expander_Collapsed(object sender, RoutedEventArgs e)
        {
            for (var vis = sender as Visual; vis != null; vis = VisualTreeHelper.GetParent(vis) as Visual)
                if (vis is DataGridRow)
                {
                    var row = (DataGridRow)vis;
                    row.DetailsVisibility = row.DetailsVisibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
                    break;
                }
        }

        // -------------------- PENCERE MOUSE EVENTLERİ --------------------
        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private bool IsMaximized = false;
        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                if (IsMaximized)
                {
                    this.WindowState = WindowState.Normal;
                    this.Width = 1080;
                    this.Height = 720;

                    IsMaximized = false;
                }
                else
                {
                    this.WindowState = WindowState.Maximized;

                    IsMaximized = true;
                }
            }
        }

        // -------------------- Ayarlar Buton Fonksiyonu --------------------
        public void AyarlarClicked(object sender, RoutedEventArgs e)
        {            
            enumMatchWindow.ShowDialog();
        }

        public void ButtonFrekans(object sender, RoutedEventArgs e)
        {
        }
        // -------------------- FİLTRE FONKSİYONU --------------------
        public void searchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
        }
        private void enumKaydetClick(object sender, RoutedEventArgs e)
        {
            enumStruct = enumMatchWindow.enumStruct;
            updateGrid();
        }



        public void updateGrid()
        {
            paketColumn.Binding = new Binding("Value");
            dataGrid.ItemsSource = enumStruct[enumMatchWindow.paketName];
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {            
            enumMatchWindow.ShowDialog();            
        }

        private void enumMatchClosed(object sender, EventArgs e)
        {
            enumMatchWindow = new EnumMatchWindow();
            enumMatchWindow.Closed += enumMatchClosed;
            enumMatchWindow.OkKaydetLog.Click += enumKaydetClick;
            enumMatchWindow.UpdatedList += EnumMatchingWindow_UpdatedList;
            //for (int i = 0; i < enumStruct.Count; i++)
            //{
            //    Dictionary<int, string> enumValue = enumStruct[enumStruct.Keys.ElementAt(i)];

            //    textBox.Text +=  enumStruct.Keys.ElementAt(i) + "\n{";
            //    for (int j = 0; j < enumValue.Count; j++)
            //    {
            //        textBox.Text += "\t" + enumValue.Keys.ElementAt(j) + " = " + enumValue.Values.ElementAt(j) + "\n";
            //    }
            //    textBox.Text += "}\n";
            //}
        }

        private void EnumMatchingWindow_UpdatedList(Dictionary<string, Dictionary<int, string>> updatedList)
        {
            //enumStruct = updatedList;
            //textBox.Text = string.Empty;
        }
    }
}
