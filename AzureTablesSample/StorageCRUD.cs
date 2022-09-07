using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace AzureTablesSample
{
    public class StorageCRUD
    {
        public static String CreateAuthorizationHeader(String canonicalizedString)
        {
            String signature = String.Empty;

            using (HMACSHA256 hmacSha256 = new HMACSHA256(Convert.FromBase64String("AccKey")))
            {
                Byte[] dataToHmac = System.Text.Encoding.UTF8.GetBytes(canonicalizedString);
                signature = Convert.ToBase64String(hmacSha256.ComputeHash(dataToHmac));
            }

            String authorizationHeader = String.Format(
                CultureInfo.InvariantCulture,
                "{0} {1}:{2}",
                AzureStorageConstants.SharedKeyAuthorizationScheme,
                AzureStorageConstants.Account,
                signature
            );

            return authorizationHeader;
        }

        public void InsertEntity(String tableName, String artist, String title)
        {
            String requestMethod = "POST";

            String urlPath = tableName;

            String storageServiceVersion = "2012-02-12";

            String dateInRfc1123Format = DateTime.UtcNow.ToString("R", CultureInfo.InvariantCulture);
            String contentMD5 = String.Empty;
            String contentType = "application/atom+xml";
            String canonicalizedResource = String.Format("/{0}/{1}", AzureStorageConstants.Account, urlPath);
            String stringToSign = String.Format(
                    "{0}\n{1}\n{2}\n{3}\n{4}",
                    requestMethod,
                    contentMD5,
                    contentType,
                    dateInRfc1123Format,
                    canonicalizedResource);
            String authorizationHeader = CreateAuthorizationHeader(stringToSign);

            UTF8Encoding utf8Encoding = new UTF8Encoding();
            Byte[] content = utf8Encoding.GetBytes(GetRequestContentInsertXml(artist, title));

            Uri uri = new Uri(AzureStorageConstants.TableEndPoint + urlPath);
            #pragma warning disable SYSLIB0014 // Type or member is obsolete
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            #pragma warning restore SYSLIB0014 // Type or member is obsolete
            request.Accept = "application/atom+xml,application/xml";
            request.ContentLength = content.Length;
            request.ContentType = contentType;
            request.Method = requestMethod;
            request.Headers.Add("x-ms-date", dateInRfc1123Format);
            request.Headers.Add("x-ms-version", storageServiceVersion);
            request.Headers.Add("Authorization", authorizationHeader);
            request.Headers.Add("Accept-Charset", "UTF-8");

            request.Headers.Add("DataServiceVersion", "2.0;NetFx");
            request.Headers.Add("MaxDataServiceVersion", "2.0;NetFx");

            using (Stream requestStream = request.GetRequestStream())
            {
                requestStream.Write(content, 0, content.Length);
            }

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                Stream dataStream = response.GetResponseStream();
                using (StreamReader reader = new StreamReader(dataStream))
                {
                    String responseFromServer = reader.ReadToEnd();
                }
            }
        }


        private String GetRequestContentInsertXml(String artist, String title)
        {
            String defaultNameSpace = "http://www.w3.org/2005/Atom";
            String dataservicesNameSpace = "http://schemas.microsoft.com/ado/2007/08/dataservices";
            String metadataNameSpace = "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata";

            XmlWriterSettings xmlWriterSettings = new XmlWriterSettings();
            xmlWriterSettings.OmitXmlDeclaration = false;
            xmlWriterSettings.Encoding = Encoding.UTF8;

            StringBuilder entry = new StringBuilder();
            using (XmlWriter xmlWriter = XmlWriter.Create(entry))
            {
                xmlWriter.WriteProcessingInstruction("xml", "version=\"1.0\" encoding=\"UTF-8\"");
                xmlWriter.WriteWhitespace("\n");
                xmlWriter.WriteStartElement("entry", defaultNameSpace);
                xmlWriter.WriteAttributeString("xmlns", "d", null, dataservicesNameSpace);
                xmlWriter.WriteAttributeString("xmlns", "m", null, metadataNameSpace);
                xmlWriter.WriteElementString("title", null);
                xmlWriter.WriteElementString("updated", String.Format("{0:o}", DateTime.UtcNow));
                xmlWriter.WriteStartElement("author");
                xmlWriter.WriteElementString("name", null);
                xmlWriter.WriteEndElement();
                xmlWriter.WriteElementString("id", null);
                xmlWriter.WriteStartElement("content");
                xmlWriter.WriteAttributeString("type", "application/xml");
                xmlWriter.WriteStartElement("properties", metadataNameSpace);
                xmlWriter.WriteElementString("PartitionKey", dataservicesNameSpace, artist);
                xmlWriter.WriteElementString("RowKey", dataservicesNameSpace, title);
                xmlWriter.WriteElementString("Artist", dataservicesNameSpace, artist);
                xmlWriter.WriteElementString("Title", dataservicesNameSpace, title + "\n" + title);
                xmlWriter.WriteEndElement();
                xmlWriter.WriteEndElement();
                xmlWriter.WriteEndElement();
                xmlWriter.Close();
            }
            String requestContent = entry.ToString();
            return requestContent;
        }

        public void GetEntity(String tableName, String partitionKey, String rowKey)
        {
            String requestMethod = "GET";

            String urlPath = String.Format("{0}(PartitionKey='{1}',RowKey='{2}')", tableName, partitionKey, rowKey);

            String storageServiceVersion = "2012-02-12";

            String dateInRfc1123Format = DateTime.UtcNow.ToString("R", CultureInfo.InvariantCulture);
            String canonicalizedResource = String.Format("/{0}/{1}", AzureStorageConstants.Account, urlPath);
            String stringToSign = String.Format(
                    "{0}\n\n\n{1}\n{2}",
                    requestMethod,
                    dateInRfc1123Format,
                    canonicalizedResource);
            String authorizationHeader = CreateAuthorizationHeader(stringToSign);

            Uri uri = new Uri(AzureStorageConstants.TableEndPoint + urlPath);
            #pragma warning disable SYSLIB0014 // Type or member is obsolete
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            #pragma warning restore SYSLIB0014 // Type or member is obsolete
            request.Method = requestMethod;
            request.Headers.Add("x-ms-date", dateInRfc1123Format);
            request.Headers.Add("x-ms-version", storageServiceVersion);
            request.Headers.Add("Authorization", authorizationHeader);
            request.Headers.Add("Accept-Charset", "UTF-8");
            request.Accept = "application/atom+xml,application/xml";

            request.Headers.Add("DataServiceVersion", "2.0;NetFx");
            request.Headers.Add("MaxDataServiceVersion", "2.0;NetFx");

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                Stream dataStream = response.GetResponseStream();
                using (StreamReader reader = new StreamReader(dataStream))
                {
                    String responseFromServer = reader.ReadToEnd();
                }
            }
        }
    }
}
