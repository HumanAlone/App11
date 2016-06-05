using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;
using Windows.UI.Xaml.Navigation;
using App11.DataModel;
using Microsoft.WindowsAzure.MobileServices;
using Microsoft.WindowsAzure.MobileServices.SQLiteStore;
using Microsoft.WindowsAzure.MobileServices.Sync;
using SQLitePCL;


// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace App11
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private MobileServiceCollection<Data, Data> items;
        private IMobileServiceSyncTable<Data> DataTable = App.MobileService.GetSyncTable<Data>(); // offline sync

        public MainPage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            await InitLocalStoreAsync();

            // Specify a known location.
            BasicGeoposition cityPosition = new BasicGeoposition() { Latitude = 55.529634, Longitude = 37.548079 };
            Geopoint cityCenter = new Geopoint(cityPosition);

            // Set the map location.
            MyMap.Center = cityCenter;
            MyMap.ZoomLevel = 15;
            MyMap.LandmarksVisible = true;

            SQLiteConnection connection = new SQLiteConnection("localstore.db");
            using (var statement = connection.Prepare("SELECT DISTINCT DeviceId FROM Data"))
            {
                while (statement.Step() == SQLiteResult.ROW)
                {
                    listBox.Items.Add((string)statement[0]);
                }
            }
        }

        private async void button_Click_1(object sender, RoutedEventArgs e)
        {
            MapPolyline mapPolyline = new MapPolyline();
            mapPolyline.StrokeColor = Colors.Indigo;
            mapPolyline.StrokeThickness = 5;
            List<BasicGeoposition> positions = new List<BasicGeoposition>();
            SQLiteConnection connection = new SQLiteConnection("localstore.db");

            if (Calendar.Date != null)
            {
                DateTime selectedDate = Calendar.Date.Value.DateTime;
                string newDate = selectedDate.ToString("dd-MM-yyyy");
                var selec = listBox.SelectedItem;

                if (selec != null)
                    using (var statement = connection.Prepare($"SELECT Longitude, Latitude FROM Data Where DeviceId = {selec} and substr(TIMESTAMP,1,10) = '{newDate}'"))
                    {
                        while (statement.Step() == SQLiteResult.ROW)
                        {
                            positions.Add(new BasicGeoposition() { Latitude = (double)statement[1], Longitude = (double)statement[0] });
                        }
                    }

                else
                {
                    var dialog = new MessageDialog("Выберите устройство!");
                    await dialog.ShowAsync();
                    return;

                }

                try
                {
                    Geopath path = new Geopath(positions);
                    mapPolyline.Path = path;
                    MyMap.MapElements.Add(mapPolyline);
                }

                catch (Exception)
                {
                    var dialog = new MessageDialog("Нет данных за этот период!");
                    await dialog.ShowAsync();
                    return;
                }

                using (var statement = connection.Prepare($"SELECT min(Timestamp) FROM Data Where DeviceId = {selec} and substr(TIMESTAMP,1,10) = '{newDate}'"))
                {
                    while (statement.Step() == SQLiteResult.ROW)
                    {
                        textBox1.Text = "Начало движения" + Environment.NewLine + (string)statement[0];
                    }
                }

                using (var statement = connection.Prepare($"SELECT max(Timestamp) FROM Data Where DeviceId = {selec} and substr(TIMESTAMP,1,10) = '{newDate}'"))
                {
                    while (statement.Step() == SQLiteResult.ROW)
                    {
                        textBox.Text = "Конец движения" + Environment.NewLine + (string)statement[0];
                    }
                }

                using (var statement = connection.Prepare($"SELECT max(Speed) FROM Data Where DeviceId = {selec} and substr(TIMESTAMP,1,10) = '{newDate}'"))
                {
                    while (statement.Step() == SQLiteResult.ROW)
                    {
                        textBox2.Text = "Максимальная скорость" + Environment.NewLine + Math.Round(((double)statement[0] * 3.6), 2) + " км/ч";
                    }
                }

                using (var statement = connection.Prepare($"SELECT avg(Speed) FROM Data Where DeviceId = {selec} and substr(TIMESTAMP,1,10) = '{newDate}'"))
                {
                    while (statement.Step() == SQLiteResult.ROW)
                    {
                        textBox3.Text = "Средняя скорость" + Environment.NewLine + Math.Round(((double)statement[0] * 3.6), 2) + " км/ч";
                    }
                }

                using (var statement = connection.Prepare($"SELECT max(Timestamp),min(Timestamp) FROM Data Where DeviceId = {selec} and substr(TIMESTAMP,1,10) = '{newDate}'"))
                {
                    while (statement.Step() == SQLiteResult.ROW)
                    {
                        textBox4.Text = "Время в пути" + Environment.NewLine + (DateTime.Parse((string)statement[0]) - DateTime.Parse((string)statement[1])).ToString();
                    }
                }

                using (var statement = connection.Prepare($"SELECT max(Timestamp),min(Timestamp),avg(Speed) FROM Data Where DeviceId = {selec} and substr(TIMESTAMP,1,10) = '{newDate}'"))
                {
                    while (statement.Step() == SQLiteResult.ROW)
                    {
                        double a = ((double)statement[2]);
                        TimeSpan b = DateTime.Parse((string)statement[0]) - DateTime.Parse((string)statement[1]);
                        double res = (b.TotalSeconds * a) / 1000;
                        textBox5.Text = "Пройденный путь" + Environment.NewLine + Math.Round(res, 3).ToString() + " км";
                    }
                }

                using (var statement = connection.Prepare($"SELECT Timestamp, Longitude, Latitude FROM Data WHERE rowid % 25 = 0 and DeviceId = {selec} and substr(TIMESTAMP,1,10) = '{newDate}'"))
                {
                    while (statement.Step() == SQLiteResult.ROW)
                    {
                        BasicGeoposition snPosition = new BasicGeoposition() { Latitude = (double)statement[2], Longitude = (double)statement[1] };
                        // Specify a known location.

                        Geopoint snPoint = new Geopoint(snPosition);

                        // Create a MapIcon.
                        MapIcon mapIcon1 = new MapIcon();
                        mapIcon1.Location = snPoint;
                        mapIcon1.NormalizedAnchorPoint = new Point(0.5, 1.0);
                        mapIcon1.Image = RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Assets/2.png"));
                        mapIcon1.Title = "Я был тут" + Environment.NewLine + (string)statement[0];
                        mapIcon1.ZIndex = 0;

                        // Add the MapIcon to the map.
                        MyMap.MapElements.Add(mapIcon1);
                    }
                }
            }

            else
            {
                var dialog = new MessageDialog("Выберите дату!");
                await dialog.ShowAsync();
            }
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            SQLiteConnection connection = new SQLiteConnection("localstore.db");
            using (var statement = connection.Prepare($"SELECT Timestamp, Longitude, Latitude FROM Data WHERE rowid % 45 = 0"))
            {
                while (statement.Step() == SQLiteResult.ROW)
                {
                    BasicGeoposition snPosition = new BasicGeoposition() { Latitude = (double)statement[2], Longitude = (double)statement[1] };
                    // Specify a known location.

                    Geopoint snPoint = new Geopoint(snPosition);

                    // Create a MapIcon.
                    MapIcon mapIcon1 = new MapIcon();
                    mapIcon1.Location = snPoint;
                    mapIcon1.NormalizedAnchorPoint = new Point(0.5, 1.0);
                    mapIcon1.Image = RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Assets/1.png"));
                    mapIcon1.Title = "Забери товар в " + Environment.NewLine + (string)statement[0];
                    mapIcon1.ZIndex = 0;

                    // Add the MapIcon to the map.
                    MyMap.MapElements.Add(mapIcon1);
                }
            }
        }


        private async Task InitLocalStoreAsync()
        {
            if (!App.MobileService.SyncContext.IsInitialized)
            {
                var store = new MobileServiceSQLiteStore("localstore.db");
                store.DefineTable<Data>();
                await App.MobileService.SyncContext.InitializeAsync(store);
            }

            await SyncAsync();
        }

        private async Task SyncAsync()
        {
            //await App.MobileService.SyncContext.PushAsync();
            await DataTable.PullAsync("Data", DataTable.CreateQuery());
        }

        private void textBlock_SelectionChanged(object sender, RoutedEventArgs e)
        {

        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            MyMap.MapElements.Clear();
            textBox.Text = "";
            textBox1.Text = "";
            textBox2.Text = "";
            textBox3.Text = "";
            textBox4.Text = "";
            textBox5.Text = "";
        }
    }
}
