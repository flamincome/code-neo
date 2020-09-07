using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using System;
using System.ComponentModel;
using System.Numerics;

namespace Action
{
    public class Vault : SmartContract
    {
        // constants
        // `110e493ab5703f2fb8d1b0570397f8357e153318` will be replace by the script hash of target token
        private static readonly object TargetToken = "110e493ab5703f2fb8d1b0570397f8357e153318".HexToBytes();
        //predefs
        delegate object CallContract(string method, object[] args);
        public static object Main(string METHOD, object[] ARGS)
        {
            if (Runtime.Trigger == TriggerType.Verification)
            {
                return true;
            }
            else if (Runtime.Trigger == TriggerType.Application)
            {
                byte[] WTFCALLER = ExecutionEngine.CallingScriptHash;
                if (METHOD == "do")
                {
                    object bytes = ARGS[0];
                    object args = new object[] { WTFCALLER, ExecutionEngine.ExecutingScriptHash, bytes };
                    object call = ((CallContract)TargetToken)("transfer", ((object[])args));
                    if (((bool)call))
                    {
                        return true;
                    }
                    throw new InvalidOperationException("do");
                }
                if (METHOD == "refund")
                {
                    BigInteger amount = (BigInteger)ARGS[0];
                    object args = new object[] { ExecutionEngine.ExecutingScriptHash, WTFCALLER, amount };
                    object call = ((CallContract)TargetToken)("transfer", ((object[])args));
                    if (((bool)call))
                    {
                        return true;
                    }
                    throw new InvalidOperationException("refund");
                }
                if (METHOD == "balance")
                {
                    object args = new object[] { ExecutionEngine.ExecutingScriptHash };
                    return ((CallContract)TargetToken)("balanceOf", ((object[])args));
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