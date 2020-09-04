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
        // constants
        // `110e493ab5703f2fb8d1b0570397f8357e153318` will be replace by the script hash of target token
        private static readonly byte[] TargetToken = "110e493ab5703f2fb8d1b0570397f8357e153318".HexToBytes();
        // `TARGET` will be replaced by the symbol of target token
        private static readonly string SymbolName = "flamTARGET";
        // `TARGET` will be replaced by the name of target token
        private static readonly string TokenName = "flamincome TARGET";
        // `8` will be replaced by the decimal of target token
        private static readonly byte TokenDecimals = 8;
        // predefs
        [DisplayName("transfer")]
        public static event Action<byte[], byte[], BigInteger> EventTransfer;
        delegate object CallContract(string method, object[] args);
#if DEBUG
        // NEP-5
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
        // custom
        [DisplayName("deposit")]
        public static bool deposit(byte[] hash, BigInteger amount) => true;
        [DisplayName("withdraw")]
        public static bool withdraw(byte[] hash, BigInteger amount) => true;
        [DisplayName("action")]
        public static bool action(string key, object[] args) => true;
        [DisplayName("setAction")]
        public static bool setAction(Map<string, byte[]> action) => true;
        [DisplayName("setGovernance")]
        public static bool setGovernance(byte[] hash) => true;
        [DisplayName("setStrategist")]
        public static bool setStrategist(byte[] hash) => true;
        class ContractStorage
        {
            class contract
            {
                BigInteger total;
                Map<string, byte[]> actions;
                byte[] governance;
                byte[] strategist;
                byte[] WTFCALLER;
                byte[] WTFBYPASS;
            };
            class balance : Map<byte[], BigInteger> { };
        };
#endif
        public static object Main(string method, object[] args)
        {
            byte[] WTFCALLER = ExecutionEngine.CallingScriptHash;
            StorageMap contract = Storage.CurrentContext.CreateMap(nameof(contract));
            contract.Put("WTFCALLER", WTFCALLER);

            if (Runtime.Trigger == TriggerType.Verification)
            {
                byte[] flag = contract.Get("WTFBYPASS");
                if (flag.Length > 0)
                {
                    return true;
                }
                CheckGovernance();
                return true;
            }
            else if (Runtime.Trigger == TriggerType.Application)
            {
                if (method == "action")
                {
                    DoAction((string)args[0], (object[])args[1]);
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
                    SetAction((Map<string, byte[]>)args[0]);
                    return true;
                }
                if (method == "setGovernance")
                {
                    SetGovernance((byte[])args[0]);
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
                    return GetTotalSupply();
                }
                if (method == "transfer")
                {
                    return TransferToken((byte[])args[0], (byte[])args[1], (BigInteger)args[2]);
                }
                if (method == "withdraw")
                {
                    WithdrawToken((byte[])args[0], (BigInteger)args[1]);
                    return true;
                }
            }
            return false;
        }
        // user
        private static void DepositToken(byte[] hash, BigInteger amount)
        {
            CheckHash(hash);
            CheckPositive(amount);
            BigInteger inside = GetVaultBalance();
            BigInteger outside = GetExternBalance();
            BigInteger all = inside + outside;
            CheckNonNegative(all);
            RecvTarget(hash, amount);
            if (all > 0)
            {
                BigInteger total = GetTotalSupply();
                amount = amount * total / all;
            }
            CheckPositive(amount);
            AddTotal(amount);
            AddBalance(hash, amount);
        }
        private static void WithdrawToken(byte[] hash, BigInteger amount)
        {
            CheckHash(hash);
            CheckWitness(hash);
            CheckPositive(amount);
            BigInteger inside = GetVaultBalance();
            BigInteger outside = GetExternBalance();
            BigInteger all = inside + outside;
            BigInteger total = GetTotalSupply();
            CheckNonNegative(inside);
            CheckNonNegative(outside);
            CheckNonNegative(all);
            CheckPositive(total);
            BigInteger num = amount * all / total;
            CheckPositive(num);
            if (inside < num)
            {
                BigInteger need = num - inside;
                CheckPositive(need);
                RefundToken(need);
            }
            SubTotal(amount);
            SubBalance(hash, amount);
            SendTarget(hash, num);
        }
        private static bool TransferToken(byte[] from, byte[] to, BigInteger amount)
        {
            CheckHash(from);
            CheckWitness(from);
            CheckHash(to);
            CheckNonNegative(amount);
            BigInteger balance = GetBalance(from);
            if (balance < amount)
            {
                return false;
            }
            // NOTE: THE NEP-5 ISPAYABLE CHECKING CONSTRAINT IS NOT OBEYED
            if (amount > 0)
            {
                SubBalance(from, amount);
                AddBalance(to, amount);
            }
            EventTransfer(from, to, amount);
            return true;
        }
        // strategist
        private static void DoAction(string key, object[] args)
        {
            CheckStrategist();
            StorageMap contract = Storage.CurrentContext.CreateMap(nameof(contract));
            contract.Put("WTFBYPASS", 1);
            Map<string, byte[]> map = (Map<string, byte[]>)contract.Get("actions").Deserialize();
            byte[] hash = map[key];
            CallContract call = (CallContract)hash.ToDelegate();
            call("do", args);
            contract.Delete("WTFBYPASS");
        }
        // governance
        private static void SetAction(Map<string, byte[]> map)
        {
            CheckGovernance();
            byte[] data = map.Serialize();
            StorageMap contract = Storage.CurrentContext.CreateMap(nameof(contract));
            contract.Put("actions", data);
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
        private static BigInteger GetExternBalance()
        {
            BigInteger num = 0;
            StorageMap contract = Storage.CurrentContext.CreateMap(nameof(contract));
            Map<string, byte[]> map = (Map<string, byte[]>)contract.Get("actions").Deserialize();
            foreach (byte[] hash in map.Values)
            {
                object[] args = new object[] { };
                CallContract call = (CallContract)hash.ToDelegate();
                num += (BigInteger)call("balance", args);
            }
            return num;
        }
        private static BigInteger GetVaultBalance()
        {
            object[] args = new object[] { ExecutionEngine.ExecutingScriptHash };
            CallContract call = (CallContract)TargetToken.ToDelegate();
            return (BigInteger)call("balanceOf", args);
        }
        private static BigInteger GetTotalSupply()
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
        private static void RefundToken(BigInteger num)
        {
            StorageMap contract = Storage.CurrentContext.CreateMap(nameof(contract));
            Map<string, byte[]> map = (Map<string, byte[]>)contract.Get("actions").Deserialize();
            foreach (byte[] hash in map.Values)
            {
                if (num <= 0)
                {
                    return;
                }
                BigInteger amount = num;
                {
                    object[] args = new object[] { };
                    CallContract call = (CallContract)hash.ToDelegate();
                    BigInteger balance = (BigInteger)call("balance", args);
                    if (balance <= amount)
                    {
                        amount = balance;
                    }
                }
                {
                    object[] args = new object[] { amount };
                    CallContract call = (CallContract)hash.ToDelegate();
                    call("refund", args);
                }
                num -= amount;
            }
        }
        private static void RecvTarget(byte[] hash, BigInteger amount)
        {
            object[] args = new object[] { hash, ExecutionEngine.ExecutingScriptHash, amount };
            CallContract call = (CallContract)TargetToken.ToDelegate();
            bool ret = (bool)call("transfer", args);
            if (ret)
            {
                return;
            }
            throw new InvalidOperationException(nameof(RecvTarget));
        }
        private static void SendTarget(byte[] hash, BigInteger amount)
        {
            object[] args = new object[] { ExecutionEngine.ExecutingScriptHash, hash, amount };
            CallContract call = (CallContract)TargetToken.ToDelegate();
            bool ret = (bool)call("transfer", args);
            if (ret)
            {
                return;
            }
            throw new InvalidOperationException(nameof(SendTarget));
        }
        private static void AddBalance(byte[] hash, BigInteger amount)
        {
            StorageMap balance = Storage.CurrentContext.CreateMap(nameof(balance));
            BigInteger num = balance.Get(hash).AsBigInteger();
            num += amount;
            CheckNonNegative(num);
            balance.Put(hash, num);
        }
        private static void SubBalance(byte[] hash, BigInteger amount)
        {
            StorageMap balance = Storage.CurrentContext.CreateMap(nameof(balance));
            BigInteger num = balance.Get(hash).AsBigInteger();
            num -= amount;
            CheckNonNegative(num);
            balance.Put(hash, num);
        }
        private static void AddTotal(BigInteger amount)
        {
            StorageMap contract = Storage.CurrentContext.CreateMap(nameof(contract));
            BigInteger total = contract.Get("total").AsBigInteger();
            total += amount;
            CheckNonNegative(total);
            contract.Put("total", total);
        }
        private static void SubTotal(BigInteger amount)
        {
            StorageMap contract = Storage.CurrentContext.CreateMap(nameof(contract));
            BigInteger total = contract.Get("total").AsBigInteger();
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
            CheckWitness(hash);
        }
        private static void CheckHash(byte[] hash)
        {
            if (hash.Length == 20)
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
            StorageMap contract = Storage.CurrentContext.CreateMap(nameof(contract));
            byte[] WTFCALLER = contract.Get("WTFCALLER");
            if (hash.AsBigInteger() == WTFCALLER.AsBigInteger())
            {
                return;
            }
            throw new InvalidOperationException(nameof(CheckWitness));
        }
    }
}