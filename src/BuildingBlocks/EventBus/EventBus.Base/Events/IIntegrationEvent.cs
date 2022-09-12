using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace EventBus.Base.Events
{
    public class IntegrationEvent
    {
        [JsonProperty]
        public Guid Guid { get; private set; }

        [JsonProperty]
        public DateTime CreatedDate { get; private set; }

        public IntegrationEvent()
        {
            Guid= Guid.NewGuid();
            CreatedDate=DateTime.Now;
        }

        [JsonConstructor]
        public IntegrationEvent(Guid guid, DateTime createdDate)
        {
            Guid = guid;
            CreatedDate = createdDate;
        }
    }
}
