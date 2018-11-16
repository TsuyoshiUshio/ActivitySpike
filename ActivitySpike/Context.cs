using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace ActivitySpike
{
    public class Context
    {
        public string ActivityId { get; set; }
        public string ParentId { get; set; }

        public bool Completed { get; set; }

        public Stack<Activity> ActivityStack { get; set; }

        public Context()
        {

        }

    }
}
