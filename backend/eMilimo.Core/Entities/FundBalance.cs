using System;
using eMilimo.Core.Entities.Base;

namespace eMilimo.Core.Entities
{
    public class FundBalance : BaseEntity
    {
        public Guid? GroupId { get; set; }
        public Guid? SectionId { get; set; }
        public Guid? CommitteeId { get; set; }
        public int FiscalYear { get; set; }
        public decimal BudgetedBalance { get; set; }
        public decimal OwnFundsBalance { get; set; }
        public decimal CommittedBudgetedBalance { get; set; } 
        public decimal CommittedOwnFundsBalance { get; set; }   
        public Group? Group { get; set; }
        public Section? Section { get; set; }
        public Committee? Committee { get; set; }
    }
}
