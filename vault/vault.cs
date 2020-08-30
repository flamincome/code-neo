using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using System;
using System.ComponentModel;
using System.Numerics;

namespace Vault
{
    public class Vault : SmartContract
    {
        private static readonly byte[] TargetToken = "Ae2d6qj91YL3LVUMkza7WQsaTYjzjHm4z1".ToScriptHash();
        private static readonly string SymbolName = "flamCNEO";
        private static readonly string TokenName = "flamincome-CNEO";
        private static readonly byte TokenDecimals = 8;
        [DisplayName("transfer")]
        public static event Action<byte[], byte[], BigInteger> EventTransfer;
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
                if (method == "action")
                {
                    DoAction((string)args[0]);
                    return true;
                }
                if (method == "balanceOf")
                {
                    return GetBalance((byte[])args[0]);
                }
                if (method == "decimals")
                {
                    return TokenDecimals;
                }
                if (method == "deposit")
                {
                    DepositToken((byte[])args[0], (BigInteger)args[1]);
                    return true;
                }
                if (method == "name")
                {
                    return TokenName;
                }
                if (method == "setAction")
                {
                    SetAction((string)args[0], (Action)args[1]);
                    return true;
                }
                if (method == "setGovernance")
                {
                    SetGovernance((byte[])args[0]);
                    return true;
                }
                if (method == "setRefund")
                {
                    SetRefund((string)args[0], (BigInteger)args[1], (Action)args[2]);
                    return true;
                }
                if (method == "setSource")
                {
                    SetSource((Action[])args[0]);
                    return true;
                }
                if (method == "setStrategist")
                {
                    SetStrategist((byte[])args[0]);
                    return true;
                }
                if (method == "supportedStandards")
                {
                    return new string[] { "NEP-5", "NEP-7", "NEP-10" };
                }
                if (method == "symbol")
                {
                    return SymbolName;
                }
                if (method == "totalSupply")
                {
                    return GetTotal();
                }
                if (method == "transfer")
                {
                    TransferToken((byte[])args[0], (byte[])args[1], (BigInteger)args[2]);
                    return true;
                }
                if (method == "withdraw")
                {
                    WithdrawToken((byte[])args[0], (BigInteger)args[1], (string)args[2], (BigInteger)args[3]);
                    return true;
                }
            }
            return false;
        }
#if DEBUG
        [DisplayName("balanceOf")]
        public static BigInteger balanceOf(byte[] account) => 0;
        [DisplayName("decimals")]
        public static byte decimals() => 0;
        [DisplayName("name")]
        public static string name() => "";
        [DisplayName("symbol")]
        public static string symbol() => "";
        [DisplayName("supportedStandards")]
        public static string[] supportedStandards() => new string[] { "NEP-5", "NEP-7", "NEP-10" };
        [DisplayName("totalSupply")]
        public static BigInteger totalSupply() => 0;
        [DisplayName("transfer")]
        public static bool transfer(byte[] from, byte[] to, BigInteger amount) => true;
        [DisplayName("deposit")]
        public static bool deposit(byte[] hash, BigInteger amount) => true;
        [DisplayName("withdraw")]
        public static bool withdraw(byte[] hash, BigInteger amount, string key, BigInteger refund) => true;
        [DisplayName("action")]
        public static bool action(string key) => true;
        [DisplayName("setAction")]
        public static bool setAction(string key, Action action) => true;
        [DisplayName("setRefund")]
        public static bool setRefund(string key, BigInteger num, Action action) => true;
        [DisplayName("setSource")]
        public static bool setSource(Action[] actions) => true;
        [DisplayName("setGovernance")]
        public static bool setGovernance(byte[] hash) => true;
        [DisplayName("setStrategist")]
        public static bool setStrategist(byte[] hash) => true;
#endif
        // user
        private static void DepositToken(byte[] hash, BigInteger amount)
        {
            CheckHash(hash);
            CheckPositive(amount);
            BigInteger pool = GetPool();
            BigInteger ex = GetExtern();
            BigInteger all = pool + ex;
            CheckNonNegative(all);
            RecvTarget(hash, amount);
            if (all > 0)
            {
                BigInteger total = GetTotal();
                amount = amount * total / all;
            }

            CheckPositive(amount);
            AddTotal(amount);
            AddBalance(hash, amount);
        }
        private static void WithdrawToken(byte[] hash, BigInteger amount, string key, BigInteger refund)
        {
            CheckHash(hash);
            CheckWitness(hash);
            CheckPositive(amount);
            CheckKey(key);
            CheckPositive(refund);
            BigInteger pool = GetPool();
            BigInteger ex = GetExtern();
            BigInteger all = pool + ex;
            BigInteger total = GetTotal();
            CheckNonNegative(pool);
            CheckNonNegative(ex);
            CheckNonNegative(all);
            CheckPositive(total);
            BigInteger num = amount * all / total;
            if (pool < num)
            {
                BigInteger need = num - pool;
                BigInteger delta = need - refund;
                CheckPositive(need);
                CheckPositive(delta);
                RefundToken(key, refund);
                num -= delta;
            }
            CheckPositive(num);
            SubTotal(amount);
            SubBalance(hash, amount);
            SendTarget(hash, num);
        }
        private static void TransferToken(byte[] from, byte[] to, BigInteger amount)
        {
            CheckHash(from);
            CheckHash(to);
            CheckPositive(amount);
            CheckWitness(from);
            SubBalance(from, amount);
            AddBalance(to, amount);
            EventTransfer(from, to, amount);
        }
        // strategist
        private static object DoAction(string key)
        {
            CheckStrategist();
            CheckKey(key);
            StorageMap whitelist = Storage.CurrentContext.CreateMap(nameof(whitelist));
            byte[] value = whitelist.Get(key);
            Action action = (Action)value.Deserialize();
            CallContract call = (CallContract)action.hash.ToDelegate();
            object ret = call(action.method, action.args);
            return ret;
        }
        // governance
        private static void SetAction(string key, Action action)
        {
            CheckGovernance();
            CheckKey(key);
            StorageMap whitelist = Storage.CurrentContext.CreateMap(nameof(whitelist));
            whitelist.Put(key, action.Serialize());
        }
        private static void SetRefund(string key, BigInteger num, Action action)
        {
            CheckGovernance();
            CheckKey(key);
            CheckPositive(num);
            StorageMap refund = Storage.CurrentContext.CreateMap(nameof(refund));
            refund.Put(key.AsByteArray().Concat(num.ToByteArray()), action.Serialize());
        }
        private static void SetSource(Action[] actions)
        {
            CheckGovernance();
            StorageMap contract = Storage.CurrentContext.CreateMap(nameof(contract));
            contract.Put("source", actions.Serialize());
        }
        private static void SetGovernance(byte[] hash)
        {
            CheckGovernance();
            CheckHash(hash);
            StorageMap contract = Storage.CurrentContext.CreateMap(nameof(contract));
            contract.Put("governance", hash);
        }
        private static void SetStrategist(byte[] hash)
        {
            CheckGovernance();
            CheckHash(hash);
            StorageMap contract = Storage.CurrentContext.CreateMap(nameof(contract));
            contract.Put("strategist", hash);
        }
        // readonly
        private static Action[] GetSource()
        {
            StorageMap contract = Storage.CurrentContext.CreateMap(nameof(contract));
            return (Action[])contract.Get("source").Deserialize();
        }
        private static BigInteger GetExtern()
        {
            BigInteger num = 0;
            Action[] actions = GetSource();
            for (var i = 0; i < actions.Length; i++)
            {
                Action action = actions[i];
                CallContract call = (CallContract)action.hash.ToDelegate();
                num += (BigInteger)call(action.method, action.args);
            }
            return num;
        }
        private static BigInteger GetPool()
        {
            CallContract call = (CallContract)TargetToken.ToDelegate();
            object[] args = new object[] { ExecutionEngine.ExecutingScriptHash };
            return (BigInteger)call("balanceOf", args);
        }
        private static BigInteger GetTotal()
        {
            StorageMap contract = Storage.CurrentContext.CreateMap(nameof(contract));
            return contract.Get("total").AsBigInteger();
        }
        private static BigInteger GetBalance(byte[] hash)
        {
            CheckHash(hash);
            StorageMap balance = Storage.CurrentContext.CreateMap(nameof(balance));
            return balance.Get(hash).AsBigInteger();
        }
        // util
        private static void RefundToken(string key, BigInteger num)
        {
            StorageMap refund = Storage.CurrentContext.CreateMap(nameof(refund));
            byte[] value = refund.Get(key.AsByteArray().Concat(num.ToByteArray()));
            Action action = (Action)value.Deserialize();
            CallContract call = (CallContract)action.hash.ToDelegate();
            call(action.method, action.args);
        }
        private static void RecvTarget(byte[] hash, BigInteger amount)
        {
            CallContract call = (CallContract)TargetToken.ToDelegate();
            object[] args = new object[] { hash, ExecutionEngine.ExecutingScriptHash, amount };
            if ((bool)call("transfer", args))
            {
                return;
            }
            throw new InvalidOperationException(nameof(RecvTarget));
        }
        private static void SendTarget(byte[] hash, BigInteger amount)
        {
            CallContract call = (CallContract)TargetToken.ToDelegate();
            object[] args = new object[] { ExecutionEngine.ExecutingScriptHash, hash, amount };
            if ((bool)call("transfer", args))
            {
                return;
            }
            throw new InvalidOperationException(nameof(SendTarget));
        }
        private static void SetBalance(byte[] hash, BigInteger amount)
        {
            CheckNonNegative(amount);
            StorageMap balance = Storage.CurrentContext.CreateMap(nameof(balance));
            balance.Put(hash, amount);
        }
        private static void AddBalance(byte[] hash, BigInteger amount)
        {
            CheckPositive(amount);
            BigInteger num = GetBalance(hash);
            num += amount;
            SetBalance(hash, num);
        }
        private static void SubBalance(byte[] hash, BigInteger amount)
        {
            CheckPositive(amount);
            BigInteger num = GetBalance(hash);
            num -= amount;
            SetBalance(hash, num);
        }
        private static void AddTotal(BigInteger amount)
        {
            StorageMap contract = Storage.CurrentContext.CreateMap(nameof(contract));
            BigInteger total = contract.Get("total").AsBigInteger();
            CheckPositive(amount);
            total += amount;
            CheckNonNegative(total);
            contract.Put("total", total);
        }
        private static void SubTotal(BigInteger amount)
        {
            StorageMap contract = Storage.CurrentContext.CreateMap(nameof(contract));
            BigInteger total = contract.Get("total").AsBigInteger();
            CheckPositive(amount);
            total -= amount;
            CheckNonNegative(total);
            contract.Put("total", total);
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
            if (Runtime.CheckWitness(hash))
            {
                return;
            }
            throw new InvalidOperationException(nameof(CheckGovernance));
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