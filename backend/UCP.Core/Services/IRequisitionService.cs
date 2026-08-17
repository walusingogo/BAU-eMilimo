using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UCP.Core.DTOs;
using UCP.Core.Entities;
using UCP.Core.Enums;

namespace UCP.Core.Services
{
    public interface IRequisitionService
    {
        Task<Requisition> CreateRequisitionAsync(CreateRequisitionDto dto, Guid userId);
        Task<Requisition> GetRequisitionAsync(Guid id, Guid userId);
        Task<RequisitionStatsDto> GetStatsAsync(Guid userId);
        Task<IEnumerable<Requisition>> GetRequisitionsAsync(RequisitionFilterDto filter, Guid userId);
        Task<Requisition> UpdateRequisitionAsync(Guid id, UpdateRequisitionDto dto, Guid userId);
        Task<Requisition> SubmitRequisitionAsync(Guid id, Guid userId);
        Task<Requisition> ApproveRequisitionAsync(Guid id, ApproveRequisitionDto dto, Guid userId);
        Task<Requisition> RejectRequisitionAsync(Guid id, RejectRequisitionDto dto, Guid userId);
        Task<Requisition> ReturnRequisitionAsync(Guid id, RejectRequisitionDto dto, Guid userId);
        Task<Requisition> MarkMoneyReadyAsync(Guid id, Guid userId);
        Task<Requisition> CollectRequisitionAsync(Guid id, CollectRequisitionDto dto, Guid userId);
        Task ArchiveRequisitionAsync(Guid id, ArchiveRequisitionDto dto, Guid userId);
        Task<bool> CanUserApproveAsync(Guid requisitionId, Guid userId);
        Task<RequisitionStatus> GetNextStatusAsync(Guid requisitionId);
    }
}
