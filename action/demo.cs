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
        // constants
        private static readonly byte[] TargetToken = "110e493ab5703f2fb8d1b0570397f8357e153318".HexToBytes();
        //predefs
        delegate object CallContract(string method, object[] args);
        public static object Main(string method, object[] args)
        {
            byte[] FUCKNEOCALLER = ExecutionEngine.CallingScriptHash;
            if (Runtime.Trigger == TriggerType.Verification)
            {
                return true;
            }
            else if (Runtime.Trigger == TriggerType.Application)
            {
                if (method == "do")
                {
                    BigInteger amount = (BigInteger)args[0];
                    object[] args = new object[] { FUCKNEOCALLER, ExecutionEngine.ExecutingScriptHash, amount };
                    CallContract call = (CallContract)TargetToken.ToDelegate();
                    bool ret = (bool)call("transfer", args);
                    if (ret)
                    {
                        return true;
                    }
                    throw new InvalidOperationException(nameof(RecvTarget));
                }
                if (method == "refund")
                {
                    BigInteger amount = (BigInteger)args[0];
                    object[] args = new object[] { ExecutionEngine.ExecutingScriptHash, FUCKNEOCALLER, amount };
                    CallContract call = (CallContract)TargetToken.ToDelegate();
                    bool ret = (bool)call("transfer", args);
                    if (ret)
                    {
                        return true;
                    }
                    throw new InvalidOperationException(nameof(RecvTarget));
                }
                if (method == "balance")
                {
                    object[] args = new object[] { ExecutionEngine.ExecutingScriptHash };
                    CallContract call = (CallContract)TargetToken.ToDelegate();
                    bool ret = (bool)call("balanceOf", args);
                }
            }
            return false;
        }
#if DEBUG
        [DisplayName("do")]
        public static bool do() => true;
        [DisplayName("refund")]
        public static bool refund(BigInteger amount) => true;
        [DisplayName("balance")]
        public static BigInteger balance() => 0;
#endif
    }
}