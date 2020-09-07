using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using System;
using System.ComponentModel;
using System.Numerics;

namespace Action
{
    public class None : SmartContract
    {
        delegate object CallContract(string method, object[] args);
        public static object Main(string method, object[] args)
        {
            if (Runtime.Trigger == TriggerType.Verification)
            {
                return true;
            }
            else if (Runtime.Trigger == TriggerType.Application)
            {
                if (method == "do")
                {
                    return true;
                }
                if (method == "refund")
                {
                    return true;
                }
                if (method == "balance")
                {
                    return 0;
                }
            }
            return false;
        }
#if DEBUG
        [DisplayName("do")]
        public static bool do(byte[] bytes) => true;
        [DisplayName("refund")]
        public static bool refund(BigInteger amount) => true;
        [DisplayName("balance")]
        public static BigInteger balance() => 0;
#endif
    }
}