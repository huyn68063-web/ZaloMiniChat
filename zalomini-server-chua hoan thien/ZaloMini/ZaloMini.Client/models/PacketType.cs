using System.Runtime.Serialization;

namespace ZaloMini.Client.Models
{
    [DataContract]
    public enum PacketType
    {
        Login = 0,
        Message = 1,
        Status = 2,
        Logout = 3
    }

    [DataContract]
    public class Packet
    {
        [DataMember]
        public PacketType Type { get; set; }

        // JSON payload text (depends on Type)
        [DataMember]
        public string Payload { get; set; }

        // Optional correlation id
        [DataMember]
        public string RequestId { get; set; }
    }
}
