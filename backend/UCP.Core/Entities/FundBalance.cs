using System;
using UCP.Core.Entities.Base;

namespace UCP.Core.Entities
{
    public class FundBalance : BaseEntity
    {
        public Guid GroupId { get; set; }
        public int FiscalYear { get; set; }
        public decimal BudgetedBalance { get; set; }
        public decimal OwnFundsBalance { get; set; }
        public decimal CommittedBudgetedBalance { get; set; } 
        public decimal CommittedOwnFundsBalance { get; set; }   
        public Group? Group { get; set; }
    }
}