using System;
using System.Text.Json.Serialization;
namespace HyperBot.Models
{
    public class ServerProtectData
    {
        [JsonPropertyName("unsafeFiles")]
        public ServerProtectUnsafeFile[] UnsafeFiles { get; set; }
        [JsonPropertyName("ipGrabberUrls")]
        public String[] IPGrabberURLs { get; set; }
    }
    public class ServerProtectUnsafeFile
    {
        [JsonPropertyName("description")]
        public string Description;

        [JsonPropertyName("hash")]
        public string Hash;
    }
}
