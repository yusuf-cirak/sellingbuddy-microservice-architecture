using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventBus.Base
{
    public class SubscriptionInfo
    {
        public Type HandleType { get;}

        public SubscriptionInfo(Type handleType)
        {
            HandleType = handleType;
        }

        public static SubscriptionInfo Typed(Type handleType) => new(handleType);
    }
}
