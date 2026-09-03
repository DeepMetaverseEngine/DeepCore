using System;

namespace DeepCore.Game3D.Host.ZoneServer
{
    public class PlayerAlreadyExistException : Exception
    {
        public string PlayerUUID { get; private set; }
        public string SceneID { get; private set; }

        public PlayerAlreadyExistException(string uuid, string sid = "", string msg = "")
            : base("Player already exist : " + uuid + " SceneID : " + sid + " : " + msg)
        {
            this.PlayerUUID = uuid;
            this.SceneID = sid;
        }
    }

    public class PlayerNotExistException : Exception
    {
        public string PlayerUUID { get; private set; }
        public string SceneID { get; private set; }

        public PlayerNotExistException(string uuid, string sid = "", string msg = "")
            : base("Player not exist : " + uuid + " SceneID : " + sid + " : " + msg)
        {
            this.PlayerUUID = uuid;
            this.SceneID = sid;
        }
    }

}
