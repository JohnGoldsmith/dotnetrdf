﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace VDS.RDF.Query.Engine.Joins.Workers
{
    public class LoopJoinWorker
        : ReusableJoinWorker
    {
        public LoopJoinWorker(IList<String> joinVars, IEnumerable<ISolution> rhs)
        {
            if (joinVars == null) throw new ArgumentNullException("joinVars");
            if (rhs == null) throw new ArgumentNullException("rhs");
            this.JoinVariables = joinVars is ReadOnlyCollection<String> ? joinVars : new List<string>(joinVars).AsReadOnly();
            this.Rhs = rhs;
        }

        public IEnumerable<ISolution> Rhs { get; private set; }

        public IList<String> JoinVariables { get; private set; } 

        public override IEnumerable<ISolution> Find(ISolution lhs, IExecutionContext context)
        {
            return this.Rhs.Where(s => lhs.IsCompatibleWith(s, this.JoinVariables));
        }
    }
}
