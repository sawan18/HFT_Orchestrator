using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockManager.Model
{
    public class DecisionResult
    {
        public DecisionResult(string rule, Decision decision, double tolerance)
        {
            this.RuleDecision = decision;
            this.Rule = rule;
            this.Tolerance = tolerance;
        }
            
        public Decision RuleDecision { get; set; }
        public string Rule { get; set; }

        public double Tolerance { get; set; }
    }
}
