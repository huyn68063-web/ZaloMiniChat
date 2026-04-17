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

// sample method inside FormChat class
private void OnNetworkPacketReceived(ZaloMini.Client.Models.Packet packet)
{
    if (packet == null) return;

    if (packet.Type == ZaloMini.Client.Models.PacketType.Message)
    {
        // deserialize payload to MessageDTO
        var ser = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(ZaloMini.Client.Models.MessageDTO));
        ZaloMini.Client.Models.MessageDTO msg;
        using (var ms = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(packet.Payload)))
        {
            msg = ser.ReadObject(ms) as ZaloMini.Client.Models.MessageDTO;
        }

        // UI update must run on UI thread
        this.Invoke((Action)(() =>
        {
            // Example: ChatMessageItemControl is a hypothetical control you use to display a message.
            // Replace with your actual control creation code.
            var item = new ChatMessageItemControl(); // ensure this control exists in project
            item.SetMessage(msg.SenderId, msg.Content, msg.Timestamp, isOutgoing: msg.SenderId == currentUserId);

            // assume flowPanelMessages is a FlowLayoutPanel hosting messages
            flowPanelMessages.Controls.Add(item);
            flowPanelMessages.ScrollControlIntoView(item);
        }));
    }
}