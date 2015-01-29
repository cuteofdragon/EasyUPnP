using EasyUPnP;
using System;
using System.Collections.ObjectModel;
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
        public class BindingClass
        {
            public string DefaultIconUrl { get; set; }
            public string FriendlyName { get; set; }
            public string PresentationURL { get; set; }
            public string AliasURL { get; set; }
        }

        private UPnPService _upnpService;
        private ObservableCollection<BindingClass> _bindingClass;

        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void btnStartDiscovery_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _bindingClass = new ObservableCollection<BindingClass>();
                listMediaServers.ItemsSource = _bindingClass;
                btnStartDiscovery.IsEnabled = false;
                Notify("UPnP discovery started...", Colors.Green);

                _upnpService = new UPnPService();
                _upnpService.OnMediaServerFound += _upnpService_OnMediaServerFound;
                _upnpService.OnMediaRendererFound += _upnpService_OnMediaRendererFound;
                _upnpService.OnOtherDeviceFound += _upnpService_OnOtherDeviceFound;
                _upnpService.OnUPnPDiscoveryCompleted += _upnpService_OnUPnPDiscoveryCompleted;

                //await _upnpService.AddOnlineMediaServerAsync(new Uri("http://your-external-ip.org:9000"));  //the way you can add your own online media server over internet
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
                BindingClass bc = new BindingClass();
                bc.AliasURL = e.MediaServer.AliasURL;
                bc.DefaultIconUrl = e.MediaServer.DefaultIconUrl;
                bc.FriendlyName = "MEDIA SERVER: " + e.MediaServer.DeviceDescription.Device.FriendlyName;
                bc.PresentationURL = e.MediaServer.PresentationURL;
                _bindingClass.Add(bc);
            });
        }

        private async void _upnpService_OnMediaRendererFound(object sender, UPnPService.MediaRendererFoundEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, delegate
            {
                BindingClass bc = new BindingClass();
                bc.DefaultIconUrl = e.MediaRenderer.DefaultIconUrl;
                bc.FriendlyName = "MEDIA RENDERER: " + e.MediaRenderer.DeviceDescription.Device.FriendlyName;
                bc.PresentationURL = e.MediaRenderer.PresentationURL;
                _bindingClass.Add(bc);
            });
        }

        private async void _upnpService_OnOtherDeviceFound(object sender, UPnPService.OtherDeviceFoundEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, delegate
            {
                BindingClass bc = new BindingClass();
                bc.DefaultIconUrl = e.OtherDevice.DefaultIconUrl;
                bc.FriendlyName = "OTHER DEVICE: " + e.OtherDevice.DeviceDescription.Device.FriendlyName;
                bc.PresentationURL = e.OtherDevice.PresentationURL;
                _bindingClass.Add(bc);
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
