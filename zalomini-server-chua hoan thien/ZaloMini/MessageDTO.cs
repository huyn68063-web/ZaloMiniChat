csharp ZaloMini.Client\Models\MessageDTO.cs
using System;
using System.Runtime.Serialization;

namespace ZaloMini.Client.Models
{
    [DataContract]
    public class MessageDTO
    {
        [DataMember]
        public string SenderId { get; set; }

        [DataMember]
        public string ReceiverId { get; set; }

        [DataMember]
        public string Content { get; set; }

        [DataMember]
        public DateTime Timestamp { get; set; }
    }
}