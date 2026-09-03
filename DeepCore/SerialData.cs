using DeepCore.Concurrent;
using DeepCore.IO;
using DeepCore.Reflection;
using DeepCore.Xml;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCore
{

    public abstract class SerialData : ISerializable, IBeforeExternalizable
    {
        [XmlSerializable(XmlProperty.IgnoreClone)]
        private uint mSN;
        public uint SerialNumber
        {
            get { return mSN; }
        }
        void IBeforeExternalizable.BeforeWrite(IOutputStream output)
        {
            output.PutU32(mSN);
        }
        void IBeforeExternalizable.BeforeRead(IInputStream input)
        {
            this.mSN = input.GetU32();
        }
        public static void GenAllSerialNumber(object alldata, object currentData = null)
        {
            var savedID = new HashMap<uint, SerialData>();
            var datas = new List<SerialData>();
            var re_gens = new List<SerialData>();
            while (true)
            {
                uint nid = 1;
                savedID.Clear();
                datas.Clear();
                re_gens.Clear();
                {
                    var exists = new HashSet<SerialData>();
                    PropertyUtil.CollectFieldTypeValues<SerialData>(alldata, datas);
                    foreach (var sn in datas)
                    {
                        if (!exists.Contains(sn))
                        {
                            exists.Add(sn);
                            if (sn.SerialNumber == 0)
                            {
                                if (!re_gens.Contains(sn))
                                {
                                    re_gens.Add(sn);
                                }
                            }
                            else if (savedID.ContainsKey(sn.SerialNumber))
                            {
                                //nid = Math.Max(nid, sn.SerialNumber);
                                if (!re_gens.Contains(sn))
                                {
                                    re_gens.Add(sn);
                                }
                            }
                            else
                            {
                                //nid = Math.Max(nid, sn.SerialNumber);
                                savedID.Add(sn.SerialNumber, sn);
                            }
                        }
                        else
                        {

                        }                    
                    }
                }             
                if (re_gens.Count > 0)
                {
                    foreach (var sn in re_gens)
                    {
                        for (; nid < uint.MaxValue; nid++)
                        {
                            if (!savedID.ContainsKey(nid))
                            {
                                sn.mSN = (nid);
                                savedID.Add(sn.SerialNumber, sn);
                                break;
                            }
                        }
                    }
                }
                else
                {
                    break;
                }
            }
        }
    }
}
