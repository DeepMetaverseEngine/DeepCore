
using DeepCore;
using DeepCore.IO;
using DeepCore.ORM;
using DeepCore.SQL;
using DeepCore.Xml;
using Gate.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Gate.Data
{
    //----------------------------------------------------------------------------------------------------------

    /// <summary>
    /// 邮件
    /// </summary>
    [MessageType(Constants.MAIL_DATA_START + 5)]
    [PersistType]
    [SQLTable]
    public class MailData : ISerializable, IStructMapping
    {
        [SQLField(Length = 128, PrimaryKey = true)]
        public string uuid;
        [SQLField(Length = 128)]
        public string from;
        [SQLField(Length = 128)]
        public string to;
        [SQLField(Length = 128)]
        public string title;
        [SQLField()]
        public string text;

        [SQLField(Length = 128)]
        public string alias;
        [SQLField(Length = 128)]
        public string type;
        [SQLField()]
        public DateTime utcTime;
        [SQLField()]
        public bool readed;
        [SQLField()]
        public bool opened;
        [SQLField(SQLValueType.JsonObject)]
        public ISerializable content;


        public override string ToString()
        {
            return $"from:{from} to:{to} title:{title}";
        }

        public MailSnap ToSnap()
        {
            return new MailSnap()
            {
                uuid = this.uuid,
                from = this.from,
                to = this.to,
                title = this.title,
                type = this.type,
                utcTime = this.utcTime,
                readed = this.readed,
                opened = this.opened,
            };
        }
    }

    [MessageType(Constants.MAIL_DATA_START + 6)]
    [PersistType]
    public class MailSnap : ISerializable, IStructMapping
    {
        public string uuid;
        public string from;
        public string to;
        public string title;
        public string type;
        public DateTime utcTime;
        public bool readed;
        public bool opened;
    }
    //----------------------------------------------------------------------------------------------------------
}
