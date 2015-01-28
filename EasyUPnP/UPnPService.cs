using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.UI.Xaml;

namespace EasyUPnP
{
    public class UPnPService
    {
        #region Events

        public event EventHandler<UPnPDiscoveryCompletedEventArgs> OnUPnPDiscoveryCompleted;
        public class UPnPDiscoveryCompletedEventArgs : EventArgs
        {
            public UPnPDiscoveryCompletedEventArgs()
            {
            }
        }
        public event EventHandler<MediaServerFoundEventArgs> OnMediaServerFound;
        public class MediaServerFoundEventArgs : EventArgs
        {
            public MediaServerFoundEventArgs(MediaServer mediaServer)
            {
                MediaServer = mediaServer;
            }

            public MediaServer MediaServer { get; set; }
        }

        #endregion

        #region Private constants

        private const int UPNP_TIMEOUT = 10;  //seconds

        #endregion

        #region Private variables

        private Dictionary<string, MediaServer> _mediaServers = new Dictionary<string, MediaServer>();
        private DatagramSocket _socket;
        private DispatcherTimer _upnpListener;
        private DateTime _upnpDiscoveryStart;
        private DateTime _upnpLastMessageReceived;

        #endregion

        #region Constructor

        public UPnPService()
        {
        }

        #endregion

        #region Public properties

        public ObservableCollection<MediaServer> MediaServers
        {
            get
            {
                ObservableCollection<MediaServer> result = new ObservableCollection<MediaServer>();
                foreach (MediaServer mediaServer in _mediaServers.Values)
                    result.Add(mediaServer);

                return result;
            }
        }

        #endregion

        #region Public functions

        public async Task StartUPnPDiscoveryAsync()
        {
            try
            {
                _socket = new DatagramSocket();
                _socket.MessageReceived += _socket_MessageReceived;

                IOutputStream stream = await _socket.GetOutputStreamAsync(new HostName("239.255.255.250"), "1900");
                const string message = "M-SEARCH * HTTP/1.1\r\n" +
                                       "HOST: 239.255.255.250:1900\r\n" +
                                       "ST:upnp:rootdevice\r\n" +
                                       "MAN:\"ssdp:discover\"\r\n" +
                                       "MX:3\r\n\r\n";

                var writer = new DataWriter(stream) { UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8 };
                writer.WriteString(message);

                _upnpDiscoveryStart = DateTime.Now;
                _upnpListener = new DispatcherTimer();
                _upnpListener.Interval = new TimeSpan(0, 0, 1);  //1 second
                _upnpListener.Tick += _upnpListener_Tick;
                _upnpListener.Start();

                await writer.StoreAsync();
                await stream.FlushAsync();
                stream.Dispose();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task AddOnlineMediaServerAsync(Uri url, CancellationToken ct)
        {
            if (ct.IsCancellationRequested)
                return;
            await AddDeviceAsync(url, true);
        }

        public static async Task<bool> TestSupportedOnlineDeviceAsync(Uri url)
        {
            foreach (string description_url in SupportedOnlineDevices.Items)
            {
                Uri myUri = new Uri(url.AbsoluteUri + UseSlash(url.AbsoluteUri) + description_url);
                if (await Request.RequestUriAsync(myUri) != null)
                    return true;
            }

            return false;
        }

        #endregion

        #region Private functions

        private void _upnpListener_Tick(object sender, object e)
        {
            bool UPnPDiscoveryCompleted = false;
            if (_upnpLastMessageReceived != null)
            {
                if ((DateTime.Now - _upnpDiscoveryStart).TotalSeconds > UPNP_TIMEOUT)
                    UPnPDiscoveryCompleted = true;
            }
            else
            {
                if ((DateTime.Now - _upnpDiscoveryStart).TotalSeconds > UPNP_TIMEOUT)
                    UPnPDiscoveryCompleted = false;
            }

            if (UPnPDiscoveryCompleted)
            {
                _upnpListener.Stop();
                if (_socket != null)
                    _socket.Dispose();
                if (OnUPnPDiscoveryCompleted != null)
                    OnUPnPDiscoveryCompleted(sender, null);
            }
        }
        
        private async void _socket_MessageReceived(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)
        {
            try
            {
                _upnpLastMessageReceived = DateTime.Now;

                DataReader reader = args.GetDataReader();

                uint count = reader.UnconsumedBufferLength;
                string data = reader.ReadString(count);
                var response = new Dictionary<string, string>();
                foreach (string x in data.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
                {
                    if (x.Contains(":"))
                    {
                        string[] strings = x.Split(':');
                        response.Add(strings[0].ToLower(), x.Remove(0, strings[0].Length + 1));
                    }
                }

                if (response.ContainsKey("location"))
                {
                    Uri myUri = new Uri(response["location"]);
                    await AddDeviceAsync(myUri, false);
                }
            }
            catch
            {
            }
        }

        private async Task AddDeviceAsync(Uri aliasUrl, bool is_online_media_server)
        {
            Uri deviceDescriptionUrl = new Uri(aliasUrl.AbsoluteUri);
            if (is_online_media_server)
            {
                aliasUrl = new Uri(aliasUrl.AbsoluteUri + UseSlash(aliasUrl.AbsoluteUri));
                deviceDescriptionUrl = await DetectDeviceDescriptionUrlAsync(aliasUrl);
            }

            DeviceDescription deviceDescription = null;
            if (deviceDescriptionUrl != null)
            {
                deviceDescription = await Deserializer.DeserializeXmlAsync<DeviceDescription>(deviceDescriptionUrl);
                if (deviceDescription != null)
                {
                    switch (deviceDescription.Device.DeviceTypeText)
                    {
                        case DeviceTypes.MEDIASERVER:
                            await AddMediaServerAsync(deviceDescription, aliasUrl, deviceDescriptionUrl, is_online_media_server);
                            break;
                        case DeviceTypes.INTERNET_GATEWAY_DEVICE:
                            //await AddInternetGateWayDeviceAsync();
                            break;
                        default:
                            //await AddOtherDeviceAsync();
                            break;
                    }
                }
            }

            if (deviceDescription == null)
                await AddMediaServerAsync(null, aliasUrl, null, is_online_media_server);
        }

        private async Task AddMediaServerAsync(DeviceDescription deviceDescription, Uri aliasUrl, Uri deviceDescriptionUrl, bool is_online_media_server)
        {
            try
            {
                if (_mediaServers.ContainsKey(deviceDescriptionUrl.AbsoluteUri))
                    return;

                _mediaServers.Add(deviceDescriptionUrl.AbsoluteUri, null);
                MediaServer mediaServer = new MediaServer(deviceDescription, aliasUrl, deviceDescriptionUrl, is_online_media_server);
                await mediaServer.InitAsync();
                _mediaServers[deviceDescriptionUrl.AbsoluteUri] = mediaServer;

                if (OnMediaServerFound != null)
                    OnMediaServerFound(this, new MediaServerFoundEventArgs(mediaServer));
            }
            catch
            {
            }
        }

        private async Task<Uri> DetectDeviceDescriptionUrlAsync(Uri url)
        {
            foreach (string description_url in SupportedOnlineDevices.Items)
            {
                Uri newUri = await Request.RequestUriAsync(new Uri(url.AbsoluteUri + description_url));
                if (newUri != null)
                    return newUri;
            }
            return null;
        }

        internal static string UseSlash(string url)
        {
            if (string.IsNullOrEmpty(url))
                return "";

            if (url.Substring(url.Length - 1, 1) != "/")
                return "/";

            return "";
        }

        #endregion
    }
}
