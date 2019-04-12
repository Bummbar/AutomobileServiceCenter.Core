using System;
using System.Collections.Generic;
using System.Text;
using ASC.Models.BaseTypes;

namespace ASC.Models.Models
{
    public class OnlineUser : BaseEntity
    {
        public OnlineUser() { }
        public OnlineUser(string name)
        {
            this.RowKey = Guid.NewGuid().ToString();
            this.PartitionKey = name;
        }
    }
}
