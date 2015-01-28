
namespace EasyUPnP
{
    public static class DeviceTypes
    {
        public const string MEDIASERVER = "urn:schemas-upnp-org:device:MediaServer:1";
        public const string INTERNET_GATEWAY_DEVICE = "urn:schemas-upnp-org:device:InternetGatewayDevice:1";
    }
    
    public static class ServiceTypes
    {
        public const string CONNECTIONMANAGER = "urn:schemas-upnp-org:service:ConnectionManager:1";
        public const string CONTENTDIRECTORY = "urn:schemas-upnp-org:service:ContentDirectory:1";
        public const string MEDIARECEIVERREGISTRAR = "urn:microsoft.com:service:X_MS_MediaReceiverRegistrar:1";
    }

    public static class Classes
    {
        public const string MUSICTRACK = "object.item.audioItem.musicTrack";
        public const string VIDEO = "object.item.videoItem.movie";
        public const string PHOTO = "object.item.imageItem.photo";
    }
}
