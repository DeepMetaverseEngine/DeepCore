using DeepCore.IO;

namespace DeepCore.Event.EventSystem.Message
{
    public class NamedEventMessage : EventMessage
    {
        public string Name;
        public UnionValue Content;

        public override void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutUTF(Name);
            UnionValueSerializer.WriteToStream(output, Content);
        }

        public override void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            Name = input.GetUTF();
            Content = UnionValueSerializer.ReadFromStream(input);
        }
        public override string ToString()
        {
            return base.ToString() + " " + Name;
        }
    }
}