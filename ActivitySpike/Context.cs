using System;
using System.Collections.Generic;
using System.Text;

namespace ActivitySpike
{
    public class Context
    {
        public string ActivityId { get; set; }
        public string ParentId { get; set; }

        public bool Completed { get; set; }

    }
}
