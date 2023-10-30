using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
using LiveCharts.Wpf.Charts.Base;
using NetMQ;
using NetMQ.Sockets;

namespace PacketAnalysisApp
{
    public class StringConcatConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2)
            {
                string firstValue = values[0]?.ToString() ?? string.Empty;
                string secondValue = values[1]?.ToString() ?? string.Empty;
                return firstValue + secondValue;
            }
            return Binding.DoNothing;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }


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

        TextBlock textBlock;
        DataGridRow row;

        Dictionary<string, CartesianChart> chartList = new Dictionary<string, CartesianChart>();
        Dictionary<string, LineSeries> lineSeriesList = new Dictionary<string, LineSeries>();
        Dictionary<string, ChartValues<int>> lineValuesList = new Dictionary<string, ChartValues<int>>();
        ObservableCollection<string> chartXLabels = new ObservableCollection<string>();
        CartesianChart chartDeneme;
        LineSeries lineSeries = new LineSeries();
        ChartValues<int> cv = new ChartValues<int>();

        Dictionary<string, Button> zoomButtons = new Dictionary<string, Button>();
        Dictionary<string, Button> realButtons = new Dictionary<string, Button>();
        Dictionary<string, string> chartStatuses = new Dictionary<string, string>();

        Button zoomButton = new Button();
        Button realButton = new Button();

        string chartStatus = "DEFAULT";

        public MainWindow()
        {
;
            InitializeComponent();
            // -------------------- EVENTLER --------------------
            enumMatchWindow.Closed += enumMatchClosed;
            enumMatchWindow.OkKaydetLog.Click += enumKaydetClick;
            enumMatchWindow.UpdatedList += EnumMatchingWindow_UpdatedList;


            //zoomButton.Click += Button_Click;

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

        private void zoomButtonLoaded(object sender, RoutedEventArgs e)
        {
            zoomButton = sender as Button;
            //zoomButton.Click += Button_Click;
        }

        private void realButtonLoaded(object sender, RoutedEventArgs e)
        {
            realButton = sender as Button;
            //zoomButton.Click += Button_Click;
        }

        public void createTotalPacketDict()
        {
            chartList = new Dictionary<string, CartesianChart>();
            lineSeriesList = new Dictionary<string, LineSeries>();
            lineValuesList = new Dictionary<string, ChartValues<int>>();
            chartXLabels = new ObservableCollection<string>();
            zoomButtons = new Dictionary<string, Button>();
            realButtons = new Dictionary<string, Button>();
            chartStatuses = new Dictionary<string, string>();
            totalReceivedaPacket.Clear();

            for (int i = 0; i < enumStruct[enumMatchWindow.paketName].Count; i++)
            {
                int[] deneme = { 0, 0, 0 };
                totalReceivedaPacket.Add(enumStruct[enumMatchWindow.paketName].Values.ElementAt(i), deneme);
                chartList.Add(enumStruct[enumMatchWindow.paketName].Values.ElementAt(i), new CartesianChart());
                lineSeriesList.Add(enumStruct[enumMatchWindow.paketName].Values.ElementAt(i), new LineSeries());
                lineValuesList.Add(enumStruct[enumMatchWindow.paketName].Values.ElementAt(i), new ChartValues<int>());
                zoomButtons.Add(enumStruct[enumMatchWindow.paketName].Values.ElementAt(i), new Button());
                realButtons.Add(enumStruct[enumMatchWindow.paketName].Values.ElementAt(i), new Button());
                chartStatuses.Add(enumStruct[enumMatchWindow.paketName].Values.ElementAt(i), "DEFAULT");
            }


            dataSource.Clear();
            foreach (var data in totalReceivedaPacket)
            {
                dataSource.Add(data);
            }
            //dataSource.Add(totalReceivedaPacket);

        }

        private void UpdateFrekans(object sender, EventArgs e)
        {

            chartXLabels.Add(DateTime.Now.ToString("HH:mm:ss"));
            for (int i = 0; i < totalReceivedaPacket.Count; i++)
            {
                int currentTotal = totalReceivedaPacket[enumStruct[paketName].Values.ElementAt(i)][1];
                dataGrid.Dispatcher.Invoke(new Action(() =>
                {
                    var item = dataSource.FirstOrDefault(j => j.Key == enumStruct[paketName].Values.ElementAt(i));
                    if (item.Key == null)
                    {
                    }
                    else
                    {
                        //dataSource.Remove(item);
                        int index = dataSource.IndexOf(item);
                        //item.Value[1] = totalReceivedaPacket[enumStruct[paketName].Values.ElementAt((int)bytes[0])][1];
                        //dataSource.Add(item);
                        dataSource[index] = new KeyValuePair<string, int[]>(item.Key, new int[] { currentTotal - privTotal[i], item.Value[1] });
                        //dataSource[index].Value[1] += 1;
                    }
                }));

                //int currentTotal = totalReceivedaPacket[enumStruct[paketName].Values.ElementAt(i)][1];
                totalReceivedaPacket[enumStruct[paketName].Values.ElementAt(i)][0] = currentTotal - privTotal[i];
                privTotal[i] = currentTotal;
                lineValuesList[totalReceivedaPacket.Keys.ElementAt(i)].Add(totalReceivedaPacket[enumStruct[paketName].Values.ElementAt(i)][0]);
                lineSeriesList[totalReceivedaPacket.Keys.ElementAt(i)].Values = lineValuesList[totalReceivedaPacket.Keys.ElementAt(i)];
                try 
                {
                    switch (chartStatuses[totalReceivedaPacket.Keys.ElementAt(i)])
                    {
                        case "ZOOM-":
                            chartList[totalReceivedaPacket.Keys.ElementAt(i)].Zoom = ZoomingOptions.None;
                            chartList[totalReceivedaPacket.Keys.ElementAt(i)].Pan = PanningOptions.None;
                            chartList[totalReceivedaPacket.Keys.ElementAt(i)].AxisY[0].MinValue = lineValuesList[totalReceivedaPacket.Keys.ElementAt(i)].Min();
                            chartList[totalReceivedaPacket.Keys.ElementAt(i)].AxisY[0].MaxValue = lineValuesList[totalReceivedaPacket.Keys.ElementAt(i)].Max();
                            chartList[totalReceivedaPacket.Keys.ElementAt(i)].AxisX[0].MinValue = 0;
                            chartList[totalReceivedaPacket.Keys.ElementAt(i)].AxisX[0].MaxValue = chartXLabels.Count - 1;
                            break;
                        case "REAL":
                            chartList[totalReceivedaPacket.Keys.ElementAt(i)].AxisY[0].MinValue = 0;
                            chartList[totalReceivedaPacket.Keys.ElementAt(i)].AxisY[0].MaxValue = lineValuesList[totalReceivedaPacket.Keys.ElementAt(i)].Max();
                            chartList[totalReceivedaPacket.Keys.ElementAt(i)].AxisX[0].MinValue = chartXLabels.Count - 20;
                            chartList[totalReceivedaPacket.Keys.ElementAt(i)].AxisX[0].MaxValue = chartXLabels.Count - 1;
                            break;
                        case "DEFAULT":
                            chartList[totalReceivedaPacket.Keys.ElementAt(i)].Zoom = ZoomingOptions.X;
                            chartList[totalReceivedaPacket.Keys.ElementAt(i)].Pan = PanningOptions.X;                           
                            break;
                    }
                    ////chartList["YZB_SURECLER"].AxisX[0].MouseWheel += ChartZoomEvent;
                    //chartList["YZB_SURECLER"].Zoom = ZoomingOptions.X;
                    //chartList["YZB_SURECLER"].AxisY[0].MinValue = 0;
                    //chartList["YZB_SURECLER"].AxisY[0].MaxValue = lineValuesList["YZB_SURECLER"].Max();
                    //chartList["YZB_SURECLER"].AxisX[0].MinValue = chartXLabels.Count - 20;
                    //chartList["YZB_SURECLER"].AxisX[0].MaxValue = chartXLabels.Count - 1;
                }
                catch
                {
                    continue;
                }
                //cv.Add(totalReceivedaPacket[enumStruct[paketName].Values.ElementAt(i)][0]);
                //lineSeries.Values = cv;
            }
            //int currentTotal = totalReceivedaPacket[enumStruct[paketName].Values.ElementAt((int)bytes[0])][1];
            //totalReceivedaPacket[enumStruct[paketName].Values.ElementAt((int)bytes[0])][0] = currentTotal - privTotal;
            //privTotal = totalReceivedaPacket[enumStruct[paketName].Values.ElementAt((int)bytes[0])][1];            
        }

        

        private void ChartPanEvent(object sender, MouseButtonEventArgs e )
        {

            CartesianChart chart = sender as CartesianChart;
            if (chart != null)
            {
                chartStatuses[chart.Name] = "DEFAULT";
            }

            //chartStatus = "DEFAULT";
        }


        private void ChartZoomEvent(object sender, MouseWheelEventArgs e)
        {

            CartesianChart chart = sender as CartesianChart;
            if (chart != null)
            {
                chartStatuses[chart.Name] = "DEFAULT";
            }

            //chartStatus = "DEFAULT";
        }

        // -------------------- Ayarlar Buton Fonksiyonu --------------------
        public void AyarlarClicked(object sender, RoutedEventArgs e)
        {
            timer.Stop();
            enumMatchWindow.Show();
        }

        private void LoadTextBlock(object sender, RoutedEventArgs e)
        {
            dataGrid.Dispatcher.Invoke(new Action(() =>
            {
                var selecteItem = dataGrid.SelectedItem;
                if (selecteItem != null)
                {
                    KeyValuePair<string, int[]> selectedRow = (KeyValuePair<string, int[]>)selecteItem;
                    chartList[selectedRow.Key] = sender as CartesianChart;
                    chartList[selectedRow.Key].Name = selectedRow.Key;
                    chartList[selectedRow.Key].Height = 200;
                    chartList[selectedRow.Key].Series = new SeriesCollection { lineSeriesList[selectedRow.Key] };
                    chartList[selectedRow.Key].AxisX[0].Labels = chartXLabels;
                }
            }));            

            //chartDeneme.Series = new SeriesCollection(lineSeries);

            //CartesianChart chart = sender as CartesianChart;
            //chart.DataContext = chartViewModels[selectedRow.Key];

        }

        public void ButtonDetayClicked(object sender, RoutedEventArgs e)
        {
            //MessageBox.Show("Detay Butonuna Tıklandı");
            dataGrid.Dispatcher.Invoke(new Action(() =>
            {
                //var selectedRow = dataGrid.SelectedItem;
                DataGridRow selectedRow = (DataGridRow)dataGrid.ItemContainerGenerator.ContainerFromItem(dataGrid.SelectedItem);
                if (selectedRow != null)
                {
                    KeyValuePair<string, int[]> a = (KeyValuePair<string, int[]>)selectedRow.Item;
                    row = (DataGridRow)dataGrid.ItemContainerGenerator.ContainerFromItem(a);
                    if (row.DetailsVisibility != Visibility.Visible) row.DetailsVisibility = Visibility.Visible;
                    else row.DetailsVisibility = Visibility.Collapsed;
                }
            }));

            //row = (DataGridRow)dataGrid.ItemContainerGenerator.ContainerFromItem(dataGrid.SelectedCells[0].Item);            


            //enumMatchWindow.Show();
        }
        // -------------------- FİLTRE FONKSİYONU --------------------
        List<KeyValuePair<string, int[]>> filterModeList = new List<KeyValuePair<string, int[]>>();
        public void searchBox_TextChanged(object sender, TextChangedEventArgs e)
        {

            filterModeList.Clear();
            if (searchBox.Text.Equals(""))
            {
                filterModeList.AddRange(dataSource.ToList());
            }
            else
            {
                foreach (KeyValuePair<string, int[]> packet in dataSource)
                {
                    if (packet.Key.Contains(searchBox.Text) || packet.Key.Contains(searchBox.Text.ToUpper()) || packet.Key.Contains(searchBox.Text.ToLower()))
                    {
                        filterModeList.Add(packet);
                    }
                }
            }
            dataGrid.ItemsSource = filterModeList.ToList();
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

            for (int i = 0; i < totalReceivedaPacket.Count; i++)
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


                //chartViewModels.Add(totalReceivedaPacket.Keys.ElementAt(i), new RealTimeChartViewModel());

               
            }
            pieChart.Series = piechartPaket;


            paketName = enumMatchWindow.paketName;

            paketColumn.Binding = new Binding("Key");
            frekansColumn.Binding = new Binding("Value[0]");
            toplamColumn.Binding = new Binding("Value[1]");
            dataGrid.ItemsSource = dataSource;
            //dataGrid.ItemsSource = totalReceivedaPacket.ToList();
            //dataGrid.ItemsSource = enumStruct[enumMatchWindow.paketName];

            // -------------------- FREKANS İÇİN TIMER --------------------
            privTotal = new int[totalReceivedaPacket.Count];
            privTotal = privTotal.Select(x => 0).ToArray();
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(1000);
            timer.Tick += UpdateFrekans;
            timer.Start();
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
                            //int value = new int();
                            //value = totalReceivedaPacket[enumStruct[paketName].Values.ElementAt((int)bytes[0])][1] + 1;
                            //MessageBox.Show(value.ToString());
                            //dataSource.Remove(item);
                            int index = dataSource.IndexOf(item);

                            //item.Value[1] = totalReceivedaPacket[enumStruct[paketName].Values.ElementAt((int)bytes[0])][1];
                            //dataSource.Add(item);
                            //dataSource[index] = new KeyValuePair<string, int[]>(item.Key, new int[] { item.Value[0], item.Value[1] + 1 });
                            dataSource[index].Value[1] += 1;
                        }
                        
                        //dataGrid.Items.Refresh();
                        
                        dataGrid.ItemsSource = dataSource;
                    }));
                }
            }
        }

        private void MainAppClosed(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //MessageBox.Show("zoom butonuna tıklandı");
            dataGrid.Dispatcher.Invoke(new Action(() =>
            {
                var selecteItem = dataGrid.SelectedItem;
                if (selecteItem != null)
                {
                    KeyValuePair<string, int[]> selectedRow = (KeyValuePair<string, int[]>)selecteItem;
                    chartStatuses[selectedRow.Key] = "ZOOM-";
                }
            }));
            //chartStatus = "ZOOM-";
        }
        private void realButton_Click(object sender, RoutedEventArgs e)
        {
            dataGrid.Dispatcher.Invoke(new Action(() =>
            {
                var selecteItem = dataGrid.SelectedItem;
                if (selecteItem != null)
                {
                    KeyValuePair<string, int[]> selectedRow = (KeyValuePair<string, int[]>)selecteItem;
                    chartStatuses[selectedRow.Key] = "REAL";
                }
            }));
            //chartStatus = "REAL";
        }

        private void realTimeChart_MouseWheel(object sender, MouseWheelEventArgs e)
        {

        }
    }
}