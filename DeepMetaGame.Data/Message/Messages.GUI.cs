using DeepCore;
using DeepCore.Geometry;
using DeepCore.GUI;
using DeepCore.IO;
using DeepCore.Reflection;
using DeepMetaGame.Data.ZoneEditor;
using System;
using System.Collections.Generic;
using System.Text;
using DeepCore.GUI.Input;

namespace DeepMetaGame.Data.Message.UI
{
    public interface IMessageGUI: IBattleMessage
    {

    }


    //-----------------------------------------------------------------------------------------
    #region GUIForm

    [MessageType(BattleConstants.ShowFormEvent)]
    public class ShowFormEvent : ZoneNotify, IMessageGUI
    {
        public string GUID;
        public int GUITemplateID;
        public bool IsDialog;
        public bool CloseOnClick;
        protected override void OnDisposing()
        {
            GUID = null;
            GUITemplateID = 0;
            IsDialog = false;
            CloseOnClick = false;
        }
        public override void ReadExternal(IInputStream input)
        {
            this.GUID = input.GetUTF();
            this.GUITemplateID = input.GetS32();
            this.IsDialog = input.GetBool();
            this.CloseOnClick = input.GetBool();
        }
        public override void WriteExternal(IOutputStream output)
        {
            output.PutUTF(GUID);
            output.PutS32(GUITemplateID);
            output.PutBool(IsDialog);
            output.PutBool(CloseOnClick);
        }
    }
    [MessageType(BattleConstants.CloseFormEvent)]
    public class CloseFormEvent : ZoneNotify, IMessageGUI
    {
        public string GUID;
        protected override void OnDisposing()
        {
            GUID = null;
        }
        public override void ReadExternal(IInputStream input)
        {
            this.GUID = input.GetUTF();
        }
        public override void WriteExternal(IOutputStream output)
        {
            output.PutUTF(GUID);
        }
    }

    [MessageType(BattleConstants.CloseFormAction)]
    public class CloseFormAction : ObjectAction, IMessageGUI
    {
        public string GUID;
        protected override void OnDisposing(uint objID)
        {
            GUID = null;
        }
        public override void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            this.GUID = input.GetUTF();
        }
        public override void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutUTF(GUID);
        }
    }



    [MessageType(BattleConstants.ShowPlayerFormEvent)]
    public class ShowPlayerFormEvent : PlayerNotify, IMessageGUI
    {
        public string GUID;
        public int GUITemplateID;
        public bool IsDialog;
        public bool CloseOnClick;
        protected override void OnDisposing(uint objID)
        {
            GUID = null;
            GUITemplateID = 0;
            IsDialog = false;
            CloseOnClick = false;
        }
        public override void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            this.GUID = input.GetUTF();
            this.GUITemplateID = input.GetS32();
            this.IsDialog = input.GetBool();
            this.CloseOnClick = input.GetBool();
        }
        public override void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutUTF(GUID);
            output.PutS32(GUITemplateID);
            output.PutBool(IsDialog);
            output.PutBool(CloseOnClick);
        }
    }

    #endregion
    //-----------------------------------------------------------------------------------------

    //-----------------------------------------------------------------------------------------
    #region GUINode
    public struct GUINodeArgs
    {
        public string GUID;
        public string NodeName;
        public string SubNodeURL;
    }
    public abstract class GUINodeEvent : ZoneNotify, IMessageGUI
    {
        public string GUID;
        public string NodeName;
        public string SubNodeURL;
        public GUINodeArgs ToArgs()
        {
            return new GUINodeArgs { GUID = GUID, NodeName = NodeName, SubNodeURL = SubNodeURL };
        }
        sealed protected override void OnDisposing()
        {
            this.OnDisposing(GUID);
            GUID = null;
            NodeName = null;
            SubNodeURL = null;
        }
        protected abstract void OnDisposing(string guid);
        public override void ReadExternal(IInputStream input)
        {
            this.GUID = input.GetUTF();
            this.NodeName = input.GetUTF();
            this.SubNodeURL = input.GetUTF();
        }
        public override void WriteExternal(IOutputStream output)
        {
            output.PutUTF(GUID);
            output.PutUTF(NodeName);
            output.PutUTF(SubNodeURL);
        }
    }
    public abstract class GUINodeAction : BattleAction, IMessageGUI
    {
        public string GUID;
        public string NodeName;
        public string SubNodeURL;
        sealed protected override void OnDisposing()
        {
            this.OnDisposing(GUID);
            GUID = null;
            NodeName = null;
            SubNodeURL = null;
        }
        protected abstract void OnDisposing(string guid);
        public override void ReadExternal(IInputStream input)
        {
            this.GUID = input.GetUTF();
            this.NodeName = input.GetUTF();
            this.SubNodeURL = input.GetUTF();
        }
        public override void WriteExternal(IOutputStream output)
        {
            output.PutUTF(GUID);
            output.PutUTF(NodeName);
            output.PutUTF(SubNodeURL);
        }
    }

    //-----------------------------------------------------------------------------------------

    [MessageType(BattleConstants.GUINodeClickAction)]
    public class GUINodeClickAction : GUINodeAction, IMessageGUI
    {
        public string DialogResult;
        protected override void OnDisposing(string guid)
        {
            DialogResult = null;
        }
        public override void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            this.DialogResult = input.GetUTF();
        }
        public override void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutUTF(DialogResult);
        }
    }


    [MessageType(BattleConstants.GUINodeBindDataEvent)]
    public class GUINodeBindDataEvent : GUINodeEvent, IMessageGUI
    {
        public string Key;
        public object ZoneVar;
        public bool Deep;
        protected override void OnDisposing(string guid)
        {
            Key = null;
            ZoneVar = null;
            Deep = false;
        }
        public override void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            this.Key = input.GetUTF();
            this.ZoneVar = input.GetRawData();
            this.Deep = input.GetBool();
        }
        public override void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutUTF(Key);
            output.PutRawData(ZoneVar);
            output.PutBool(Deep);
        }
    }

    [MessageType(BattleConstants.GUINodeDataChangedAction)]
    public class GUINodeDataChangedAction : GUINodeAction, IMessageGUI
    {
        public string TextValue;
        public bool BooleanValue;
        public double NumberValue;
        protected override void OnDisposing(string guid)
        {
            TextValue = null;
            BooleanValue = false;
            NumberValue = 0;
        }
        public override void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            this.TextValue = input.GetUTF();
            this.BooleanValue = input.GetBool();
            this.NumberValue = input.GetF64();
        }
        public override void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutUTF(this.TextValue);
            output.PutBool(this.BooleanValue);
            output.PutF64(this.NumberValue);
        }
    }

    [MessageType(BattleConstants.GUINodeVisibleEvent)]
    public class GUINodeVisibleEvent : GUINodeEvent, IMessageGUI
    {
        public bool Visible;
        protected override void OnDisposing(string guid)
        {
            Visible = false;
        }
        public override void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            this.Visible = input.GetBool();
        }
        public override void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutBool(Visible);
        }
    }

    [MessageType(BattleConstants.GUINodeControlEvent)]
    public class GUINodeControlEvent : GUINodeEvent, IMessageGUI
    {
        public string Name;
        public int? Index;
        public object ZoneVar;
        protected override void OnDisposing(string guid)
        {
            Name = default;
            Index = default;
            ZoneVar = null;
        }
        public override void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            //             this.Name = input.GetUTF();
            //             this.Index = input.GetNullable<int>(static input => input.GetS32());
            this.ZoneVar = input.GetRawData();
        }
        public override void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            //             output.PutUTF(Name);
            //             output.PutNullable(Index, static (output, v) => output.PutS32(v));
            output.PutRawData(ZoneVar);
        }
    }

    #endregion
    //-----------------------------------------------------------------------------------------

}
