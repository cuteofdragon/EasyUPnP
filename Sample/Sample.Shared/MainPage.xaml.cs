using EasyUPnP;
using System;
using System.Threading;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Sample
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private UPnPService _upnpService;

        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void btnStartDiscovery_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                btnStartDiscovery.IsEnabled = false;
                _upnpService = new UPnPService();
                _upnpService.OnMediaServerFound += _upnpService_OnMediaServerFound;
                _upnpService.OnUPnPDiscoveryCompleted += _upnpService_OnUPnPDiscoveryCompleted;
                await _upnpService.AddOnlineMediaServerAsync(new Uri("http://easysoft.hu/home/index.php?host_id=meehi&port=9000"), new CancellationToken());
                //await _upnpService.AddOnlineMediaServerAsync(new Uri("http://your-external-ip.org:9000"), new CancellationToken());  //the way you can add your own online media server over internet
                Notify("UPnP discovery started...", Colors.Green);
                await _upnpService.StartUPnPDiscoveryAsync();
            }
            catch (Exception ex)
            {
                Notify(ex.Message, Colors.Red);
                btnStartDiscovery.IsEnabled = true;
            }
        }

        private void _upnpService_OnUPnPDiscoveryCompleted(object sender, UPnPService.UPnPDiscoveryCompletedEventArgs e)
        {
            Notify("UPnP discovery completed", Colors.Green);
            btnStartDiscovery.IsEnabled = true;
        }

        private async void _upnpService_OnMediaServerFound(object sender, UPnPService.MediaServerFoundEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, delegate
            {
                listMediaServers.ItemsSource = _upnpService.MediaServers;
            });
        }

        private void Notify(string message, Color color)
        {
            txtNotify.Visibility = Windows.UI.Xaml.Visibility.Visible;
            txtNotify.Foreground = new SolidColorBrush(color);
            txtNotify.Text = message;
        }
    }
}
