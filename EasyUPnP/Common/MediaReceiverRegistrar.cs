using System.Xml.Serialization;

namespace EasyUPnP
{
    [XmlRoot("scpd", Namespace = "urn:schemas-upnp-org:service-1-0")]
    public class MediaReceiverRegistrar : Services
    {
    }
}