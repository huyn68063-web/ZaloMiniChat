using System.Runtime.Serialization;

namespace ZaloMini.Client.Models
{
    [DataContract]
    public class LoginRequest
    {
        [DataMember]
        public string Username { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string Password { get; set; }
    }

    [DataContract]
    public class LoginResult
    {
        [DataMember]
        public bool IsSuccess { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string ErrorMessage { get; set; }

        // Simplified user info. Extend as needed.
        [DataMember(EmitDefaultValue = false)]
        public UserInfo UserInfo { get; set; }
    }

    [DataContract]
    public class UserInfo
    {
        [DataMember]
        public string Id { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string DisplayName { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string AvatarUrl { get; set; }
    }
}