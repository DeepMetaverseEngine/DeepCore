using DeepCore.IO;
using DeepCore.Protocol;

namespace Gate.Data.Protocol
{

    [MessageType(Constants.MAIL_PROTOCOL_START + 0x01)] public class QueryTotalMailsRequest : Request { }
    [MessageType(Constants.MAIL_PROTOCOL_START + 0x02)] public class QueryTotalMailsResponse : Response { public long total; public long unread_total; }


    [MessageType(Constants.MAIL_PROTOCOL_START + 0x05)] public class QueryMailRequest : Request { public string uuid; }
    [MessageType(Constants.MAIL_PROTOCOL_START + 0x06)] public class QueryMailResponse : Response { public MailData mail; }


    [MessageType(Constants.MAIL_PROTOCOL_START + 0x07)] public class QueryMailsRequest : Request {  }
    [MessageType(Constants.MAIL_PROTOCOL_START + 0x08)] public class QueryMailsResponse : Response { public MailSnap[] mails; }


    [MessageType(Constants.MAIL_PROTOCOL_START + 0x21)] public class DeleteMailRequest : Request { public string[] uuid; }
    [MessageType(Constants.MAIL_PROTOCOL_START + 0x22)] public class DeleteMailResponse : Response { public int count; }


    [MessageType(Constants.MAIL_PROTOCOL_START + 0x23)] public class ReadMailRequest : Request { public string uuid; }
    [MessageType(Constants.MAIL_PROTOCOL_START + 0x24)] public class ReadMailResponse : Response { public MailData mail; }


    [MessageType(Constants.MAIL_PROTOCOL_START + 0x25)] public class OpenMailContentRequest : Request { public string uuid; }
    [MessageType(Constants.MAIL_PROTOCOL_START + 0x26)] public class OpenMailContentResponse : Response { public MailData mail; }


    [MessageType(Constants.MAIL_PROTOCOL_START + 0x27)] public class SendMailRequest : Request { public MailData mail; }
    [MessageType(Constants.MAIL_PROTOCOL_START + 0x28)] public class SendMailResponse : Response { public string uuid; }


    [MessageType(Constants.MAIL_PROTOCOL_START + 0x29)] public class ClaimAttachmentRequest : Request { public string uuid; }
    [MessageType(Constants.MAIL_PROTOCOL_START + 0x30)] public class ClaimAttachmentResponse : Response { public string uuid; }


    [MessageType(Constants.MAIL_PROTOCOL_START + 0x41)] public class IncomingMailNotify : Notify { public string uuid; }



}
