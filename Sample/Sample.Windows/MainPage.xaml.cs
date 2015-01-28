using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using EasyUPnP;
using Windows.UI;
using System.Collections.ObjectModel;
using System.Threading;

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
                listMediaServers.Items.Clear();

                _upnpService = new UPnPService();
                _upnpService.OnMediaServerFound += _upnpService_OnMediaServerFound;
                _upnpService.OnUPnPDiscoveryCompleted += _upnpService_OnUPnPDiscoveryCompleted;
                await _upnpService.AddOnlineMediaServerAsync(new Uri("http://easysoft.hu/home/index.php?host_id=meehi&port=9000"), new CancellationToken());
                //await _upnpService.AddOnlineMediaServerAsync(new Uri("http://yourexternalip.com:9000"), new CancellationToken());
                Notify("UPnP discovery started...", Colors.Green);
                btnStartDiscovery.IsEnabled = false;
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

        private void _upnpService_OnMediaServerFound(object sender, UPnPService.MediaServerFoundEventArgs e)
        {
            listMediaServers.DataContext = _upnpService.MediaServers;
        }

        private void Notify(string message, Color color)
        {
            txtNotify.Visibility = Windows.UI.Xaml.Visibility.Visible;
            txtNotify.Foreground = new SolidColorBrush(color);
            txtNotify.Text = message;
        }
    }
}
