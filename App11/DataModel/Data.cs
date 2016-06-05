using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace App11.DataModel
{
    class Data
    {
        
        public string Id { get; set; }

        [JsonProperty(PropertyName = "LONGITUDE")]
        public double Longitude { get; set; }

        [JsonProperty(PropertyName = "LATITUDE")]
        public double Latitude { get; set; }

        [JsonProperty(PropertyName = "SPEED")]
        public double Speed { get; set; }

        [JsonProperty(PropertyName = "DeviceId")]
        public string DeviceId { get; set; }

        [JsonProperty(PropertyName = "TIMESTAMP")]
        public string Timestamp { get; set; }

    }
}
