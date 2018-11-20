using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace ActivitySpike
{

    public class SubsetActivity
    {
        public string ActivityId { get; set; }
        public string ParentId { get; set; }
        public string RootId { get; set; }
    }
    public class Context
    {
        public string ActivityId { get; set; }
        public string ParentId { get; set; }

        public bool Completed { get; set; }

        public List<SubsetActivity> Stack { get; set; }

        public Context()
        {

        }

    }
}
