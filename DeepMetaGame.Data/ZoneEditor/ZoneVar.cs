using DeepCore.EventTrigger;
using DeepCore.IO;
using DeepCore.Reflection;

namespace DeepMetaGame.Data.ZoneEditor
{
    [MessageType(BattleConstants.ZoneVar)]
    [TableClass("Key")]
    public class ZoneVar : IExternalizable, IZoneEnvironmentVar
    {
        public string Key;
        public object Value;
        public bool SyncToClient = false;

        public DescAttribute ValueDesc
        {
            get { return PropertyUtil.GetAttribute<DescAttribute>(Value.GetType()); }
        }

        string IEnvironmentVar.Key { get => Key; set { Key = value; } }
        object IEnvironmentVar.Value { get => Value; set { Value = value; } }
        bool IEnvironmentVar.SyncToClient { get => SyncToClient; set { SyncToClient = value; } }

        sealed public override string ToString()
        {
            return Key;
        }

        public void WriteExternal(IOutputStream output)
        {
            output.PutBool(SyncToClient);
            output.PutUTF(Key);
            output.PutRawData(Value);
        }
        public void ReadExternal(IInputStream input)
        {
            SyncToClient = input.GetBool();
            Key = input.GetUTF();
            Value = input.GetRawData();
        }
    }

}
