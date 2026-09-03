using DeepCore;
using DeepCore.Reflection;
using Nethereum.Contracts;
using Nethereum.RPC.Eth.DTOs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace DeepFrozen.ETH.ABI
{
    public class ContractInfo
    {
        public MemberInfo[] members;


        public static ContractInfo LoadFromABIText(string text)
        {
            var funcs = JsonConvert.DeserializeObject<MemberInfo[]>(text);
            Array.Sort(funcs, new MemberComparer());
            return new ContractInfo() { members = funcs };
        }

    }

    public class MemberInfo
    {
        public string type;
        public string name;
        public string stateMutability;
        [Expandable]
        public ParamInfo[] inputs;
        [Expandable]
        public ParamInfo[] outputs;

        public bool IsEvent { get => type == "event"; }
        public bool IsFunction { get => type == "function"; }
        public bool IsConstructor { get => type == "constructor"; }
        public bool IsFallback { get => type == "fallback"; }
        public bool IsReceive { get => type == "receive"; }

        public bool IsView { get => stateMutability == "view"; }
        public bool IsPayable { get => stateMutability == "payable"; }
        public bool IsNonpayable { get => stateMutability == "nonpayable"; }



        public override string ToString()
        {
            if (outputs != null && outputs.Length > 0)
            {
                return $"{type} {name}({CUtils.ArrayToString(inputs)}) {stateMutability} returns ({CUtils.ArrayToString(outputs)})";
            }
            else
            {
                return $"{type} {name}({CUtils.ArrayToString(inputs)}) {stateMutability}";
            }
        }

  
    }
    public class ParamInfo
    {
        public string type;
        public string name;
        public bool IsArray { get => type.EndsWith("[]"); }
        public Type ElementType
        {
            get
            {
                var et = IsArray ? type.Substring(0, type.Length - 2) : type;
                if (et == "string") return typeof(string);
                if (et == "address") return typeof(string);
                if (et == "bool") return typeof(bool);
                if (et == "byte") return typeof(BigInteger);
                if (et == "bytes") return typeof(byte[]);
                if (et.StartsWith("bytes")) return typeof(string);
                if (et == "int") return typeof(BigInteger);
                if (et.StartsWith("int")) return typeof(BigInteger);
                if (et == "uint") return typeof(BigInteger);
                if (et.StartsWith("uint")) return typeof(BigInteger);
                return typeof(string);
            }
        }
        public Type ParamType
        {
            get
            {
                var et = ElementType;
                if (IsArray)
                {
                    return et.MakeArrayType();
                }
                return et;
            }
        }
        public override string ToString()
        {
            return $"{type} {name}";
        }
        public object Parse(string value)
        {
            var etype = ElementType;
            if (IsArray)
            {
                var arrayValue = value.Split(',');
                var ret = Array.CreateInstance(etype, arrayValue.Length);
                for(int i = 0; i < arrayValue.Length; i++)
                {
                    var ev = arrayValue[i];
                    if (etype == typeof(BigInteger))
                    {
                        ret.SetValue(BigInteger.Parse(ev),i);
                    }
                    else if (etype == typeof(byte[]))
                    {
                        ret.SetValue(CUtils.HexToBin(ev), i);
                    }
                    else if (etype == typeof(bool))
                    {
                        ret.SetValue(bool.Parse(ev), i);
                    }
                    else
                    {
                        ret.SetValue(ev, i);
                    }
                }
                return ret;
            }
            else if (etype == typeof(BigInteger)) 
            {
                return BigInteger.Parse(value);
            }
            else if (etype == typeof(byte[]))
            {
                return CUtils.HexToBin(value);
            }
            else if (etype == typeof(bool))
            {
                return bool.Parse(value);
            }
            return value;
        }
    }

    public class MemberComparer : IComparer
    {
        public virtual int Compare(object x, object y)
        {
            if (x is MemberInfo a && y is MemberInfo b)
            {
                if (a.type != b.type)
                {
                    if (a.IsEvent) return -1;
                    if (b.IsEvent) return 1;
                    if (a.IsConstructor) return -1;
                    if (b.IsConstructor) return 1;
                    if (a.IsReceive) return -1;
                    if (b.IsReceive) return 1;
                    if (a.IsFallback) return -1;
                    if (b.IsFallback) return 1;
                }
                if (a.name == null) return -1;
                if (b.name == null) return 1;
                return a.name.CompareTo(b.name);
            }
            return 0;
        }
    }
    /*
            "inputs": [
            {
            "internalType": "bytes32",
            "name": "keyName",
            "type": "bytes32"
            }
            ],
            "name": "getSingle",
            "outputs": [
            {
            "internalType": "address",
            "name": "",
            "type": "address"
            }
            ],
            "stateMutability": "view",
            "type": "function"
             */
}
