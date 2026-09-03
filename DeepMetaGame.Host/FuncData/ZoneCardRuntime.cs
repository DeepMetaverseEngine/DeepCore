using DeepCore.Game3D.Host.Instance;
using DeepCore.IO;
using DeepCore.Log;
using DeepMetaGame.Data;
using DeepMetaGame.Data.FuncData;
using DeepMetaGame.Data.Template;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCore.Game3D.Host.FuncData
{
    public struct ZoneCardRuntime : ICardRuntime
    {
        public InstanceZone Zone { get; }
        public Logger Log => Zone.Log;
        public IExternalizableFactory Codec => ZoneDataFactory.Factory.PersistCodec;
        public IReadOnlyCollection<CardTemplate> AllOriginCards => Zone.Templates.AllCards;
        public ZoneCardRuntime(InstanceZone zone)
        {
            this.Zone = zone;
        }
        public bool TryGetOriginCard(int tableName, out CardTemplate card)
        {
            card = Zone.Templates.GetCard(tableName);
            return card != null;
        }
        public bool TryGetOriginTemplate(Type templateType, int templateID, out TemplateData temp)
        {
            return Zone.Templates.TryGetTemplate(templateType, templateID, out temp);
        }

    }
}
