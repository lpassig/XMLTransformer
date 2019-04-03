using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace XMLTransformer
{
    public static class XMLTransform
    {
        [FunctionName("XMLTransform")]
        public static async Task RunAsync([QueueTrigger("tobetransformed", Connection = "storageconnectionstring")]string myQueueItem, ILogger log)
        {
            log.LogInformation($"C# Queue trigger function processed: {myQueueItem}");

            CloudStorageAccount storageAccount;

            //Get Connectionstring from app settings
            string storageConnectionString = Environment.GetEnvironmentVariable("storageconnectionstring");

            try
            {
                MessageStack message = await LoadAndSerializeXmlFile(myQueueItem);

                var origin = message.Item as OriginType;
                Console.WriteLine($"Crane ID: {origin.Machine.Liuid.ToString()}");

                /*
                Geoposition of Crane
                Console.WriteLine(message.Messages[0].AdditionalInfos.AdditionalInfo[0].Identity);
                Console.WriteLine($" GeoPositionLongitude: {message.Messages[0].AdditionalInfos.AdditionalInfo[0].Value}");

                Console.WriteLine(message.Messages[0].AdditionalInfos.AdditionalInfo[1].Identity);
                Console.WriteLine($" GeoPositionLatitude: {message.Messages[0].AdditionalInfos.AdditionalInfo[1].Value}");
                */

                //Transform GPS Data
                String GeoPositionLongitudeString = message.Messages[0].AdditionalInfos.AdditionalInfo[0].Value.ToString();
                Decimal GeoPositionLongitude = decimal.Parse(GeoPositionLongitudeString);
                Decimal GeoPositionLongitudeAccurate = GeoPositionLongitude / 1000000;

                String GeoPositionLatitudeString = message.Messages[0].AdditionalInfos.AdditionalInfo[1].Value.ToString();
                Decimal GeoPositionLatitude = decimal.Parse(GeoPositionLatitudeString);
                Decimal GeoPositionLatitudeAccurate = GeoPositionLatitude / 1000000;

                CraneData GeoInformation = new CraneData
                {
                    CraneID = origin.Machine.Liuid.ToString(),
                    MachineSerialNumber = origin.Machine.MachineSerialNumber.ToString(),
                    GeoPositionLongitude = GeoPositionLongitudeAccurate.ToString().Replace(",", "."),
                    GeoPositionLatitude = GeoPositionLatitudeAccurate.ToString().Replace(",",".")
                };

                string json = JsonConvert.SerializeObject(GeoInformation, Formatting.None);
                
                //How to DeserializeObject:
                //CraneData crane2 = JsonConvert.DeserializeObject<CraneData>(json);

                //Update Queue with transformed XML 
                CloudStorageAccount.TryParse(storageConnectionString, out storageAccount);
                UpdateQueue(json, storageAccount);
                UpdateBlob(json, storageAccount, $"{origin.Machine.Liuid.ToString()}{DateTime.Now.ToString("yyyyMMddss")}.json");
                //UpdateTable(json, storageAccount);
            }
            catch(Exception)
            {

            }
        }

        private static async void UpdateQueue(string response, CloudStorageAccount storageAccount)
        {
            try
            {
                CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
                CloudQueue queue = queueClient.GetQueueReference("transformed");
                CloudQueueMessage message = new CloudQueueMessage(response);

                await queue.AddMessageAsync(message);

            }
            catch (Exception)
            {
            }

        }
        
        private static async void UpdateBlob(string response, CloudStorageAccount storageAccount, string filename)
        {
            try
            {
                CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference("transformed");
                var blockBlob = cloudBlobContainer.GetBlockBlobReference(filename);

                await blockBlob.UploadTextAsync(response);

            }
            catch (Exception)
            {
            }

        }
        private static async Task<MessageStack> LoadAndSerializeXmlFile(string myQueueItem)
        {
            //Initialize Serializer 
            var serializer = new XmlSerializer(typeof(XMLTransformer.MessageStack));

            //Get Data/Transform
            byte[] data = System.Text.Encoding.UTF8.GetBytes(myQueueItem);

            MemoryStream memory = new MemoryStream(data);

            MessageStack message = (MessageStack)serializer.Deserialize(memory);

            return message;

        }

    }

    public class CraneData
    {
        public string CraneID { get; set; }
        public string GeoPositionLongitude { get; set; }
        public string GeoPositionLatitude { get; set; }
        public string MachineSerialNumber { get; set; }
    }

    
}
