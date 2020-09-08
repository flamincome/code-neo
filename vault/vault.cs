using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using System;
using System.ComponentModel;
using System.Numerics;

// WTF: IT IS CODING IN ASSEMBLY (EVEN WORSE) RATHER THAN CODING IN C#

namespace Vault
{
    public class Vault : SmartContract
    {
        // constants
        // `110e493ab5703f2fb8d1b0570397f8357e153318` will be replace by the script hash of target token
        private static readonly object TargetToken = "110e493ab5703f2fb8d1b0570397f8357e153318".HexToBytes();
        private static readonly object PREFIX = "flam";
        private static readonly object PREFIXNAME = "flamincomed ";
        // WTF: PUSH2 + PACK + PUSH['action'] + APPCALL
        private static readonly object CMD_ACTION = "52c106616374696f6e67".HexToBytes();
        // WTF: PUSH2 + PACK + PUSH['withdraw'] + APPCALL
        private static readonly object CMD_WITHDRAW = "52c108776974686472617767".HexToBytes();
        // predefs
        [DisplayName("transfer")]
        // NEP-5 transfer event
        public static event Action<object, object, object> EventTransfer;
        // dynamic call
        delegate object CallContract(string method, object[] args);
#if DEBUG // ONLY FOR ABI
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
        public static bool action(string key, byte[] args) => true;
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
            };
            class balance : Map<byte[], BigInteger> { };
        };
#endif
        public static object Main(string METHOD, object[] ARGS)
        {
            object WTFCALLER = ExecutionEngine.CallingScriptHash;
            StorageMap contract = Storage.CurrentContext.CreateMap(nameof(contract));
            if (Runtime.Trigger == TriggerType.Verification)
            {
                if (METHOD == "governance")
                {
                    object hash = ((StorageMap)contract).Get("governance");
                    if (((byte[])hash).Length == 20)
                    {
                        CheckWitness(hash, WTFCALLER);
                    }
                    return true;
                }
                if (METHOD == "strategist")
                {
                    object hash = ((StorageMap)contract).Get("strategist");
                    if (((byte[])hash).Length == 20)
                    {
                        CheckWitness(hash, WTFCALLER);
                    }
                    CheckWTF(ARGS[0], CMD_ACTION);
                    return true;
                }
                if (METHOD == "user")
                {
                    CheckWTF(ARGS[0], CMD_WITHDRAW);
                    return true;
                }
                return false;
            }
            else if (Runtime.Trigger == TriggerType.Application)
            {
                if (METHOD == "action")
                {
                    object hash = ((StorageMap)contract).Get("strategist");
                    if (((byte[])hash).Length == 20)
                    {
                        CheckWitness(hash, WTFCALLER);
                    }
                    DoAction(ARGS[0], ARGS[1]);
                    return true;
                }
                if (METHOD == "balanceOf")
                {
                    return GetBalance(ARGS[0]);
                }
                if (METHOD == "decimals")
                {
                    object args = new object[] { };
                    return ((CallContract)TargetToken)("decimals", ((object[])args));
                }
                if (METHOD == "deposit")
                {
                    if (ARGS[0].Equals(ExecutionEngine.ExecutingScriptHash))
                    {
                        return false;
                    }
                    DepositToken(ARGS[0], ARGS[1]);
                    return true;
                }
                if (METHOD == "name")
                {
                    object args = new object[] { };
                    object call = ((CallContract)TargetToken)("name", ((object[])args));
                    return ((byte[])PREFIXNAME).Concat(((byte[])call));
                }
                if (METHOD == "setAction")
                {
                    object hash = ((StorageMap)contract).Get("governance");
                    if (((byte[])hash).Length == 20)
                    {
                        CheckWitness(hash, WTFCALLER);
                    }
                    SetAction(ARGS[0]);
                    return true;
                }
                if (METHOD == "setGovernance")
                {
                    object hash = ((StorageMap)contract).Get("governance");
                    if (((byte[])hash).Length == 20)
                    {
                        CheckWitness(hash, WTFCALLER);
                    }
                    SetGovernance(ARGS[0]);
                    return true;
                }
                if (METHOD == "setStrategist")
                {
                    object hash = ((StorageMap)contract).Get("governance");
                    if (((byte[])hash).Length == 20)
                    {
                        CheckWitness(hash, WTFCALLER);
                    }
                    SetStrategist(ARGS[0]);
                    return true;
                }
                if (METHOD == "supportedStandards")
                {
                    return new string[] { "NEP-5", "NEP-7", "NEP-10" };
                }
                if (METHOD == "symbol")
                {
                    object args = new object[] { };
                    object call = ((CallContract)TargetToken)("symbol", ((object[])args));
                    return ((byte[])PREFIX).Concat(((byte[])call));
                }
                if (METHOD == "totalSupply")
                {
                    return GetTotalSupply();
                }
                if (METHOD == "transfer")
                {
                    CheckWitness(ARGS[0], WTFCALLER);
                    return TransferToken(ARGS[0], ARGS[1], ARGS[2]);
                }
                if (METHOD == "withdraw")
                {
                    CheckWitness(ARGS[0], WTFCALLER);
                    WithdrawToken(ARGS[0], ARGS[1]);
                    return true;
                }
            }
            return false;
        }
        // user
        private static void DepositToken(object hash, object amount)
        {
            CheckHash(hash);
            CheckPositive(amount);
            object inside = GetVaultBalance();
            object outside = GetExternBalance();
            object all = ((BigInteger)inside) + ((BigInteger)outside);
            CheckNonNegative(all);
            RecvTarget(hash, amount);
            if (((BigInteger)all) > 0)
            {
                object total = GetTotalSupply();
                amount = ((BigInteger)amount) * ((BigInteger)total) / ((BigInteger)all);
            }
            CheckPositive(amount);
            AddTotal(amount);
            AddBalance(hash, amount);
        }
        private static void WithdrawToken(object hash, object amount)
        {
            CheckHash(hash);
            CheckPositive(amount);
            object inside = GetVaultBalance();
            object outside = GetExternBalance();
            object all = ((BigInteger)inside) + ((BigInteger)outside);
            object total = GetTotalSupply();
            CheckNonNegative(inside);
            CheckNonNegative(outside);
            CheckNonNegative(all);
            CheckPositive(total);
            object num = ((BigInteger)amount) * ((BigInteger)all) / ((BigInteger)total);
            CheckPositive(num);
            if (((BigInteger)inside) < ((BigInteger)num))
            {
                object need = ((BigInteger)num) - ((BigInteger)inside);
                CheckPositive(need);
                RefundToken(need);
            }
            SubTotal(amount);
            SubBalance(hash, amount);
            SendTarget(hash, num);
        }
        private static bool TransferToken(object from, object to, object amount)
        {
            CheckHash(from);
            CheckHash(to);
            CheckNonNegative(amount);
            object balance = GetBalance(from);
            if (((BigInteger)balance) < ((BigInteger)amount))
            {
                return false;
            }
            // NOTE: THE NEP-5 ISPAYABLE CHECKING CONSTRAINT IS NOT OBEYED
            if (((BigInteger)amount) > 0)
            {
                SubBalance(from, amount);
                AddBalance(to, amount);
            }
            EventTransfer(from, to, amount);
            return true;
        }
        // strategist
        private static void DoAction(object key, object bytes)
        {
            object contract = Storage.CurrentContext.CreateMap(nameof(contract));
            object map = ((StorageMap)contract).Get("actions").Deserialize();
            object hash = ((Map<object, object>)map)[key];
            object args = new object[] { bytes };
            ((CallContract)hash)("do", ((object[])args));
        }
        // governance
        private static void SetAction(object map)
        {
            object contract = Storage.CurrentContext.CreateMap(nameof(contract));
            ((StorageMap)contract).Put("actions", map.Serialize());
        }
        private static void SetGovernance(object hash)
        {
            CheckHash(hash);
            object contract = Storage.CurrentContext.CreateMap(nameof(contract));
            ((StorageMap)contract).Put("governance", ((byte[])hash));
        }
        private static void SetStrategist(object hash)
        {
            CheckHash(hash);
            object contract = Storage.CurrentContext.CreateMap(nameof(contract));
            ((StorageMap)contract).Put("strategist", ((byte[])hash));
        }
        // readonly
        private static object GetExternBalance()
        {
            object num = 0;
            object contract = Storage.CurrentContext.CreateMap(nameof(contract));
            object map = ((StorageMap)contract).Get("actions").Deserialize();
            foreach (object hash in ((Map<string, byte[]>)map).Values)
            {
                object args = new object[] { };
                object item = ((CallContract)hash)("balance", ((object[])args));
                num = ((BigInteger)num) + ((BigInteger)item);
            }
            return num;
        }
        private static object GetVaultBalance()
        {
            object args = new object[] { ExecutionEngine.ExecutingScriptHash };
            return ((CallContract)TargetToken)("balanceOf", ((object[])args));
        }
        private static object GetTotalSupply()
        {
            object contract = Storage.CurrentContext.CreateMap(nameof(contract));
            return ((StorageMap)contract).Get("total");
        }
        private static object GetBalance(object hash)
        {
            CheckHash(hash);
            object balance = Storage.CurrentContext.CreateMap(nameof(balance));
            return ((StorageMap)balance).Get((byte[])hash);
        }
        // util
        private static void RefundToken(object num)
        {
            object contract = Storage.CurrentContext.CreateMap(nameof(contract));
            object map = ((StorageMap)contract).Get("actions").Deserialize();
            foreach (object hash in ((Map<object, object>)map).Values)
            {
                if (((BigInteger)num) <= 0)
                {
                    return;
                }
                object amount = num;
                {
                    object args = new object[] { };
                    object balance = ((CallContract)hash)("balance", ((object[])args));
                    if (((BigInteger)balance) <= ((BigInteger)amount))
                    {
                        amount = ((BigInteger)balance);
                    }
                }
                {
                    object args = new object[] { amount };
                    ((CallContract)hash)("refund", ((object[])args));
                }
                num = ((BigInteger)num) - ((BigInteger)amount);
            }
        }
        private static void RecvTarget(object hash, object amount)
        {
            object args = new object[] { hash, ExecutionEngine.ExecutingScriptHash, amount };
            object call = ((CallContract)TargetToken)("transfer", ((object[])args));
            if (((bool)call))
            {
                return;
            }
            throw new InvalidOperationException(nameof(RecvTarget));
        }
        private static void SendTarget(object hash, object amount)
        {
            object args = new object[] { ExecutionEngine.ExecutingScriptHash, hash, amount };
            object call = ((CallContract)TargetToken)("transfer", ((object[])args));
            if (((bool)call))
            {
                return;
            }
            throw new InvalidOperationException(nameof(SendTarget));
        }
        private static void AddBalance(object hash, object amount)
        {
            object balance = Storage.CurrentContext.CreateMap(nameof(balance));
            object num = ((StorageMap)balance).Get(((byte[])hash));
            num = ((BigInteger)num) + ((BigInteger)amount);
            CheckNonNegative(num);
            ((StorageMap)balance).Put(((byte[])hash), ((byte[])num));
        }
        private static void SubBalance(object hash, object amount)
        {
            object balance = Storage.CurrentContext.CreateMap(nameof(balance));
            object num = ((StorageMap)balance).Get(((byte[])hash));
            num = ((BigInteger)num) - ((BigInteger)amount);
            CheckNonNegative(num);
            ((StorageMap)balance).Put(((byte[])hash), ((byte[])num));
        }
        private static void AddTotal(object amount)
        {
            object contract = Storage.CurrentContext.CreateMap(nameof(contract));
            object total = ((StorageMap)contract).Get("total");
            total = ((BigInteger)total) + ((BigInteger)amount);
            CheckNonNegative(total);
            ((StorageMap)contract).Put("total", ((byte[])total));
        }
        private static void SubTotal(object amount)
        {
            object contract = Storage.CurrentContext.CreateMap(nameof(contract));
            object total = ((StorageMap)contract).Get("total");
            total = ((BigInteger)total) - ((BigInteger)amount);
            CheckNonNegative(total);
            ((StorageMap)contract).Put("total", ((byte[])total));
        }
        // check
        private static void CheckHash(object hash)
        {
            if (((byte[])hash).Length == 20)
            {
                return;
            }
            throw new InvalidOperationException(nameof(CheckHash));
        }
        private static void CheckPositive(object num)
        {
            if (((BigInteger)num) > 0)
            {
                return;
            }
            throw new InvalidOperationException(nameof(CheckPositive));
        }
        private static void CheckNonNegative(object num)
        {
            if (((BigInteger)num) >= 0)
            {
                return;
            }
            throw new InvalidOperationException(nameof(CheckNonNegative));
        }
        private static void CheckWitness(object hash, object caller)
        {
            if (Runtime.CheckWitness((byte[])hash))
            {
                return;
            }
            if (hash.Equals(caller))
            {
                return;
            }
            throw new InvalidOperationException(nameof(CheckWitness));
        }
        private static void CheckWTF(object obj, object cmd)
        {
            object length = 0;
            object flag = ((byte[])obj).Range(2, 1);
            if (flag.Equals(new byte[] { 0xFD }))
            {
                length = ((byte[])obj).Range(3, 2).Concat(new byte[] { 0x00 });
            }
            else if (flag.Equals(new byte[] { 0xFE }))
            {
                length = ((byte[])obj).Range(3, 4).Concat(new byte[] { 0x00 });
            }
            else if (flag.Equals(new byte[] { 0xFF }))
            {
                length = ((byte[])obj).Range(3, 8).Concat(new byte[] { 0x00 });
            }
            else
            {
                length = ((byte[])obj).Range(2, 1).Concat(new byte[] { 0x00 });
            }
            object script = ((byte[])obj).Range(3, ((int)length));
            flag = ((byte[])script).Range(0, 1);
            if (((BigInteger)flag) < 0)
            {
                throw new InvalidOperationException(nameof(CheckWTF));
            }
            else if (((BigInteger)flag) > 0x4E)
            {
                throw new InvalidOperationException(nameof(CheckWTF));
            }
            else if (((byte[])flag).Equals(new byte[] { 0x4E }))
            {
                flag = ((byte[])script).Range(1, 4).Concat(new byte[] { 0x00 });
                script = ((byte[])script).Range(((int)flag) + 5, ((byte[])script).Length - 5 - ((int)flag));
            }
            else if (((byte[])flag).Equals(new byte[] { 0x4D }))
            {
                flag = ((byte[])script).Range(1, 2).Concat(new byte[] { 0x00 });
                script = ((byte[])script).Range(((int)flag) + 3, ((byte[])script).Length - 3 - ((int)flag));
            }
            else if (((byte[])flag).Equals(new byte[] { 0x4C }))
            {
                flag = ((byte[])script).Range(1, 1).Concat(new byte[] { 0x00 });
                script = ((byte[])script).Range(((int)flag) + 2, ((byte[])script).Length - 2 - ((int)flag));
            }
            else
            {
                script = ((byte[])script).Range(((int)flag) + 1, ((byte[])script).Length - 1 - ((int)flag));
            }
            flag = ((byte[])script).Range(0, 1);
            if (((BigInteger)flag) < 0)
            {
                throw new InvalidOperationException(nameof(CheckWTF));
            }
            else if (((BigInteger)flag) > 0x40)
            {
                throw new InvalidOperationException(nameof(CheckWTF));
            }
            else
            {
                script = ((byte[])script).Range(((int)flag) + 1, ((byte[])script).Length - 1 - ((int)flag));
            }
            flag = ((byte[])script).Range(0, ((byte[])cmd).Length);
            if (flag.Equals(cmd))
            {
                script = ((byte[])script).Range(((byte[])cmd).Length, ((byte[])script).Length - ((byte[])cmd).Length);
            }
            else
            {
                throw new InvalidOperationException(nameof(CheckWTF));
            }
            if (!((byte[])script).Equals(ExecutionEngine.ExecutingScriptHash))
            {
                throw new InvalidOperationException(nameof(CheckWTF));
            }
            object thathash = Hash256((byte[])obj);
            object tx = ExecutionEngine.ScriptContainer;
            object thishash = ((Transaction)tx).Hash;
            if (((BigInteger)thishash) != ((BigInteger)thathash))
            {
                throw new InvalidOperationException(nameof(CheckWTF));
            }
        }
    }
}