using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using LiveCharts;
using LiveCharts.Wpf;
using NetMQ;
using NetMQ.Sockets;

namespace PacketAnalysisApp
{

    public partial class MainWindow : Window
    {
        EnumMatchWindow enumMatchWindow = new EnumMatchWindow();
        Dictionary<string, Dictionary<int, string>> enumStruct = new Dictionary<string, Dictionary<int, string>>();
        Dictionary<string, int[]> totalReceivedaPacket = new Dictionary<string, int[]>();

        ObservableCollection<KeyValuePair<string, int[]>> dataSource = new ObservableCollection<KeyValuePair<string, int[]>>();

        SeriesCollection piechartPaket;

        string paketName = string.Empty;
        
        List<ChartValues<int>> chartValuesList = new List<ChartValues<int>>();

        private DispatcherTimer timer;

        byte[] bytes;
        int[] privTotal;

        public MainWindow()
        {
            InitializeComponent();
            // -------------------- EVENTLER --------------------
            enumMatchWindow.Closed += enumMatchClosed;
            enumMatchWindow.OkKaydetLog.Click += enumKaydetClick;
            enumMatchWindow.UpdatedList += EnumMatchingWindow_UpdatedList;

            // -------------------- ENUM YAPISININ OLULŞTURULMASI --------------------
            enumStruct = enumMatchWindow.enumStructMain;

            // -------------------- TOPLAM PAKET SAYISININ TUTAN YAPI --------------------
            createTotalPacketDict();

            updateGrid();

            // -------------------- ZERO MQ DATA ALMA THREAD --------------------
            Thread subscriber = new Thread(new ThreadStart(receiveData));
            subscriber.IsBackground = true;
            subscriber.Start();
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

        public void createTotalPacketDict()
        {
            totalReceivedaPacket.Clear();

            for (int i = 0; i < enumStruct[enumMatchWindow.paketName].Count; i++)
            {
                int[] deneme = { 0, 0, 0 };
                totalReceivedaPacket.Add(enumStruct[enumMatchWindow.paketName].Values.ElementAt(i), deneme);
            }


            dataSource.Clear();
            foreach (var data in totalReceivedaPacket)
            {
                dataSource.Add(data);
            }
            //dataSource.Add(totalReceivedaPacket);

            // -------------------- FREKANS İÇİN TIMER --------------------
            privTotal = new int[totalReceivedaPacket.Count];
            privTotal = privTotal.Select(x => 0).ToArray();
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(1000);
            timer.Tick += UpdateFrekans;
            timer.Start();

        }

        private void UpdateFrekans(object sender, EventArgs e)
        {

            for (int i = 0; i < totalReceivedaPacket.Count; i++)
            {                    
                int currentTotal = totalReceivedaPacket[enumStruct[paketName].Values.ElementAt(i)][1];
                totalReceivedaPacket[enumStruct[paketName].Values.ElementAt(i)][0] = currentTotal - privTotal[i];
                privTotal[i] = currentTotal;
            }



            //int currentTotal = totalReceivedaPacket[enumStruct[paketName].Values.ElementAt((int)bytes[0])][1];
            //totalReceivedaPacket[enumStruct[paketName].Values.ElementAt((int)bytes[0])][0] = currentTotal - privTotal;
            //privTotal = totalReceivedaPacket[enumStruct[paketName].Values.ElementAt((int)bytes[0])][1];            
        }

        // -------------------- Ayarlar Buton Fonksiyonu --------------------
        public void AyarlarClicked(object sender, RoutedEventArgs e)
        {   
            timer.Stop();
            enumMatchWindow.Show();
        }

        public void ButtonDetayClicked(object sender, RoutedEventArgs e)
        {
            //enumMatchWindow.Show();
        }
        // -------------------- FİLTRE FONKSİYONU --------------------
        public void searchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
        }
        private void enumKaydetClick(object sender, RoutedEventArgs e)
        {
            enumStruct = enumMatchWindow.enumStruct;
            createTotalPacketDict();
            updateGrid();
        }



        public void updateGrid()
        {
            chartValuesList = new List<ChartValues<int>>();
            piechartPaket = new SeriesCollection();
            Func<ChartPoint, string> labelPoint = chartPoint => string.Format("{0} ({1:P})", chartPoint.Y, chartPoint.Participation);

            for (int i = 0; i<totalReceivedaPacket.Count; i++)
            {
                chartValuesList.Add(new ChartValues<int> { 0 });
                PieSeries pieSeries = new PieSeries
                {
                    Title = totalReceivedaPacket.Keys.ElementAt(i),
                    Values = chartValuesList[i],
                    DataLabels = true,
                    LabelPoint = labelPoint,
                    //Fill = Brushes.DarkBlue,
                    FontSize = 12
                };
                piechartPaket.Add(pieSeries);
            }
            pieChart.Series = piechartPaket;


            paketName = enumMatchWindow.paketName;

            paketColumn.Binding = new Binding("Key");
            frekansColumn.Binding = new Binding("Value[0]");
            toplamColumn.Binding = new Binding("Value[1]");

            dataGrid.ItemsSource = dataSource;
            //dataGrid.ItemsSource = totalReceivedaPacket.ToList();
            //dataGrid.ItemsSource = enumStruct[enumMatchWindow.paketName];
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

        public void receiveData()
        {
            bytes = new byte[6];

            using (var subSocket = new SubscriberSocket())
            {

                subSocket.Options.ReceiveHighWatermark = 1000;
                subSocket.Connect("tcp://127.0.0.1:12345");
                subSocket.SubscribeToAnyTopic();

                while (true)
                {
                    bytes = ReceivingSocketExtensions.ReceiveFrameBytes(subSocket);


                    totalReceivedaPacket[enumStruct[paketName].Values.ElementAt((int)bytes[0])][1] += 1;


                    
                    
                    int idx = totalReceivedaPacket.Keys.ToList().IndexOf(enumStruct[paketName].Values.ElementAt((int)bytes[0]));


                    dataGrid.Dispatcher.Invoke(new System.Action(() =>
                    {
                        chartValuesList[idx][0] = totalReceivedaPacket[enumStruct[paketName].Values.ElementAt((int)bytes[0])][1] + 1;
                        piechartPaket[idx].Values = chartValuesList[idx];

                        //for (int i = 0; i< totalReceivedaPacket.Count; i++)
                        //{
                        //    dataSource.Add(totalReceivedaPacket.ElementAt(i));
                        //}

                        var item = dataSource.FirstOrDefault(i => i.Key == enumStruct[paketName].Values.ElementAt((int)bytes[0]));
                        if (item.Key == null)
                        {
                            dataSource.Add(new KeyValuePair<string, int[]>(enumStruct[paketName].Values.ElementAt((int)bytes[0]), totalReceivedaPacket[enumStruct[paketName].Values.ElementAt((int)bytes[0])]));
                        }
                        else
                        {
                            //dataSource.Remove(item);
                            int index = dataSource.IndexOf(item);
                            //item.Value[1] = totalReceivedaPacket[enumStruct[paketName].Values.ElementAt((int)bytes[0])][1];
                            //dataSource.Add(item);
                            dataSource[index].Value[1] += 1;
                        }
                        dataGrid.Items.Refresh();

                        dataGrid.ItemsSource = dataSource;
                    }));                    
                }
            }
        }
    }
}
