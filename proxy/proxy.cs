using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using System;
using System.ComponentModel;
using System.Numerics;

namespace Proxy
{
    public class Proxy : SmartContract
    {
        delegate object CallContract(string method, object[] args);
        public static object Main(string method, object[] args)
        {
            if (Runtime.Trigger == TriggerType.Verification)
            {
                CheckGovernance();
                return true;
            }
            else if (Runtime.Trigger == TriggerType.Application)
            {
                if (method == "call")
                {
                    return CallTarget((string)args[0], (string)args[1], (object[])args[2]);
                }
                if (method == "set")
                {
                    SetTarget((string)args[0], (byte[])args[1]);
                    return true;
                }
                if (method == "setGovernance")
                {
                    SetGovernance((byte[])args[0]);
                    return true;
                }
            }
            return false;
        }
#if DEBUG
        [DisplayName("set")]
        public static BigInteger set(string key, byte[] hash) => 0;
        [DisplayName("setGovernance")]
        public static bool setGovernance(byte[] hash) => true;
        [DisplayName("call")]
        public static byte call(string key, string method, object[] args) => 0;
#endif
        // user
        private static object CallTarget(string key, string method, object[] args)
        {
            StorageMap mapping = Storage.CurrentContext.CreateMap(nameof(mapping));
            byte[] hash = mapping.Get(key);
            CallContract call = (CallContract)hash.ToDelegate();
            return call(method, args);
        }
        // governance
        private static void SetTarget(string key, byte[] hash)
        {
            CheckGovernance();
            CheckHash(hash);
            StorageMap mapping = Storage.CurrentContext.CreateMap(nameof(mapping));
            mapping.Put(key, hash);
        }
        private static void SetGovernance(byte[] hash)
        {
            CheckGovernance();
            CheckHash(hash);
            StorageMap contract = Storage.CurrentContext.CreateMap(nameof(contract));
            contract.Put("governance", hash);
        }
        // check
        private static void CheckGovernance()
        {
            StorageMap contract = Storage.CurrentContext.CreateMap(nameof(contract));
            byte[] hash = contract.Get("governance");
            if (hash.Length != 20)
            {
                return;
            }
            CheckWitness(hash);
        }
        private static void CheckStrategist()
        {
            StorageMap contract = Storage.CurrentContext.CreateMap(nameof(contract));
            byte[] hash = contract.Get("strategist");
            if (hash.Length != 20)
            {
                return;
            }
            if (Runtime.CheckWitness(hash))
            {
                return;
            }
            throw new InvalidOperationException(nameof(CheckStrategist));
        }
        private static void CheckHash(byte[] hash)
        {
            if (hash.Length == 20)
            {
                return;
            }
            throw new InvalidOperationException(nameof(CheckHash));
        }
        private static void CheckKey(string key)
        {
            if (key.Length == 0x10)
            {
                return;
            }
            throw new InvalidOperationException(nameof(CheckHash));
        }
        private static void CheckPositive(BigInteger num)
        {
            if (num > 0)
            {
                return;
            }
            throw new InvalidOperationException(nameof(CheckPositive));
        }
        private static void CheckNonNegative(BigInteger num)
        {
            if (num >= 0)
            {
                return;
            }
            throw new InvalidOperationException(nameof(CheckNonNegative));
        }
        private static void CheckWitness(byte[] hash)
        {
            if (Runtime.CheckWitness(hash))
            {
                return;
            }
            if (hash.AsBigInteger() == ExecutionEngine.CallingScriptHash.AsBigInteger())
            {
                return;
            }
            throw new InvalidOperationException(nameof(CheckWitness));
        }
        // struct
        public struct Action
        {
            public byte[] hash;
            public string method;
            public object[] args;
        }
    }
}