using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace EasyUPnP.Utils
{
    static class Deserializer
    {
        internal static async Task<T> DeserializeXmlAsync<T>(Uri url)
        {
            try
            {
                string response = await Request.RequestStringAsync(url, Encoding.UTF8);
                if (!string.IsNullOrEmpty(response))
                    return (T)DeserializeXml<T>(response);
            }
            catch
            {
            }
            finally
            {
            }
            
            return default(T);
        }

        internal static T DeserializeXml<T>(string xml)
        {
            try
            {
                XmlSerializer ser = new XmlSerializer(typeof(T));
                XmlReaderSettings settings = new XmlReaderSettings { ConformanceLevel = ConformanceLevel.Fragment, IgnoreWhitespace = true, IgnoreComments = true };
                XmlReader rdr = XmlReader.Create(new StringReader(xml), settings);
                return (T)ser.Deserialize(rdr);
            }
            catch
            {
            }

            return default(T);
        }
    }
}
