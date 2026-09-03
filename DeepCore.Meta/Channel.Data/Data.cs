using DeepCore.IO;

namespace DeepCore.Meta.Channel.Data
{

//     [MessageType(Constants.DATA_START + 0x01)]
//     public class UnitInfo : ISerializable
//     {
//         public string UUID;
//         public string Name;
//         public ISerializable Data;
//     }


    [MessageType(Constants.DATA_START + 0x02)]
    public class ChannelInfo : ISerializable
    {
        public string UUID;
        public string Name;
        public ISerializable Data;
    }

}
