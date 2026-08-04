using Microsoft.AspNetCore.Authorization;
using UCP.API.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UCP.Core.Entities;
using UCP.Core.Enums;
using UCP.Infrastructure.Data;
using UCP.Core.Services;

namespace UCP.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class GroupsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IGroupAccessService _groupAccess;
        private readonly ICurrentUserService _currentUser;

        public GroupsController(AppDbContext context, IGroupAccessService groupAccess, ICurrentUserService currentUser)
        {
            _context = context;
            _groupAccess = groupAccess;
            _currentUser = currentUser;
        }

        // ==========================================
        // GET: api/groups
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? type = null, [FromQuery] bool includeDisabled = false)
        {
            var query = _context.Groups.AsQueryable();
            query = query.Where(g => g.GroupType == "ChurchGroup");

            if (!_groupAccess.HasGlobalAccess())
            {
                var accessibleIds = await _groupAccess.GetAccessibleGroupIdsAsync();
                query = query.Where(g => accessibleIds.Contains(g.Id));
            }

            if (!includeDisabled)
                query = query.Where(g => g.IsActive);

            if (!string.IsNullOrEmpty(type))
                query = query.Where(g => g.GroupType == type);

            var groups = await query
                .OrderBy(g => g.GroupName)
                .Select(g => new
                {
                    g.Id,
                    g.GroupName,
                    g.GroupType,
                    g.Description,
                    g.LeaderId,
                    g.ParentGroupId,
                    g.IsActive,
                    g.CreatedDate,
                    g.UpdatedDate
                })
                .ToListAsync();

            return Ok(groups);
        }

        // ==========================================
        // GET: api/groups/active
        // ==========================================
        [HttpGet("active")]
        public async Task<IActionResult> GetActiveGroups()
        {
            var query = _context.Groups.AsQueryable();
            query = query.Where(g => g.GroupType == "ChurchGroup");
            if (!_groupAccess.HasGlobalAccess())
            {
                var accessibleIds = await _groupAccess.GetAccessibleGroupIdsAsync();
                query = query.Where(g => accessibleIds.Contains(g.Id));
            }

            var groups = await query
                .Where(g => g.IsActive)
                .OrderBy(g => g.GroupName)
                .Select(g => new
                {
                    g.Id,
                    g.GroupName,
                    g.GroupType
                })
                .ToListAsync();

            return Ok(groups);
        }

        // ==========================================
        // GET: api/groups/{id}
        // ==========================================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            if (!await _groupAccess.CanAccessGroupAsync(id))
                return Forbid();

            var group = await _context.Groups
                .Where(g => g.Id == id && g.GroupType == "ChurchGroup")
                .Select(g => new
                {
                    g.Id,
                    g.GroupName,
                    g.GroupType,
                    g.Description,
                    g.LeaderId,
                    g.ParentGroupId,
                    g.IsActive,
                    g.CreatedDate,
                    g.UpdatedDate
                })
                .FirstOrDefaultAsync();

            if (group == null)
                return NotFound(new { Message = "Group not found" });

            return Ok(group);
        }

        // ==========================================
        // GET: api/groups/with-balances
        // ==========================================
        [HttpGet("with-balances")]
        public async Task<IActionResult> GetGroupsWithBalances([FromQuery] bool includeDisabled = false)
        {
            var fiscalYear = DateTime.UtcNow.Year;
            var query = _context.Groups.AsQueryable();
            query = query.Where(g => g.GroupType == "ChurchGroup");
            if (!_groupAccess.HasGlobalAccess())
            {
                var accessibleIds = await _groupAccess.GetAccessibleGroupIdsAsync();
                query = query.Where(g => accessibleIds.Contains(g.Id));
            }
            if (!includeDisabled)
                query = query.Where(g => g.IsActive);

            var groups = await query
                .Select(g => new
                {
                    g.Id,
                    g.GroupName,
                    g.GroupType,
                    g.Description,
                    g.IsActive,
                    Balance = _context.FundBalances
                        .Where(fb => fb.GroupId == g.Id && fb.FiscalYear == fiscalYear)
                        .Select(fb => new
                        {
                            fb.BudgetedBalance,
                            fb.OwnFundsBalance,
                            fb.CommittedBudgetedBalance,
                            fb.CommittedOwnFundsBalance,
                            AvailableBudgeted = fb.BudgetedBalance - fb.CommittedBudgetedBalance,
                            AvailableOwnFunds = fb.OwnFundsBalance - fb.CommittedOwnFundsBalance
                        })
                        .FirstOrDefault()
                })
                .ToListAsync();

            return Ok(groups);
        }

        // ==========================================
        // POST: api/groups
        // ==========================================
        [HttpPost]
        [Authorize]
        [RequirePermission("groups.manage")]
        public async Task<IActionResult> Create([FromBody] CreateGroupDto dto)
        {
            if (dto.GroupType != "ChurchGroup")
                return BadRequest(new { Message = "Groups may only use the ChurchGroup type. Use the Sections or Committees API for those records." });

            var exists = await _context.Groups
                .AnyAsync(g => g.GroupName.ToLower() == dto.GroupName.ToLower() && g.IsActive);
            if (exists)
                return Conflict(new { Message = "A group with this name already exists." });

            var group = new Group
            {
                Id = Guid.NewGuid(),
                GroupName = dto.GroupName,
                Description = dto.Description,
                GroupType = dto.GroupType,
                IsActive = true,
                LeaderId = dto.LeaderId,
                ParentGroupId = dto.ParentGroupId,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow,
                LastProcessedTimeStamp = DateTime.UtcNow,
                Version = 1
            };

            _context.Groups.Add(group);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = group.Id }, new
            {
                group.Id,
                group.GroupName,
                group.GroupType,
                group.Description,
                group.IsActive
            });
        }

        // ==========================================
        // POST: api/groups/{id}/add-funds (UPDATED)
        // ==========================================
        [HttpPost("{id}/add-funds")]
        [Authorize]
        [RequirePermission("groups.manage")]
        public async Task<IActionResult> AddFunds(Guid id, [FromBody] AddFundsDto dto)
        {
            if (dto.BudgetedAmount == 0 && dto.OwnFundsAmount == 0)
                return BadRequest(new { Message = "Enter a budgeted or own-fund amount." });
            if (string.IsNullOrWhiteSpace(dto.Reason))
                return BadRequest(new { Message = "A reason is required for every fund entry." });
            if (!await _context.Groups.AnyAsync(g => g.Id == id)) return NotFound();

            var fiscalYear = DateTime.UtcNow.Year;
            
            var balance = await _context.FundBalances
                .FirstOrDefaultAsync(fb => fb.GroupId == id && fb.FiscalYear == fiscalYear);

            if (balance == null)
            {
                balance = new FundBalance
                {
                    Id = Guid.NewGuid(),
                    GroupId = id,
                    FiscalYear = fiscalYear,
                    BudgetedBalance = 0,
                    OwnFundsBalance = 0,
                    CommittedBudgetedBalance = 0,
                    CommittedOwnFundsBalance = 0,
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow,
                    LastProcessedTimeStamp = DateTime.UtcNow,
                    Version = 1
                };
                _context.FundBalances.Add(balance);
            }

            // Prevent negative balances
            if (balance.BudgetedBalance + dto.BudgetedAmount < 0)
                return BadRequest(new { Message = "Budgeted balance cannot go negative." });

            if (balance.OwnFundsBalance + dto.OwnFundsAmount < 0)
                return BadRequest(new { Message = "Own funds balance cannot go negative." });

            // Apply adjustment (supports both positive and negative values)
            balance.BudgetedBalance += dto.BudgetedAmount;
            balance.OwnFundsBalance += dto.OwnFundsAmount;
            balance.UpdatedDate = DateTime.UtcNow;
            balance.UpdatedById = _currentUser.GetUserId();
            balance.Version++;

            AddFundAuditEntries(id, balance, dto);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                balance.BudgetedBalance,
                balance.OwnFundsBalance,
                balance.CommittedBudgetedBalance,
                balance.CommittedOwnFundsBalance,
                AvailableBudgeted = balance.BudgetedBalance - balance.CommittedBudgetedBalance,
                AvailableOwnFunds = balance.OwnFundsBalance - balance.CommittedOwnFundsBalance,
                Message = "Funds updated successfully.",
                Adjustment = new
                {
                    BudgetedAmount = dto.BudgetedAmount,
                    OwnFundsAmount = dto.OwnFundsAmount
                }
            });
        }

        private void AddFundAuditEntries(Guid groupId, FundBalance balance, AddFundsDto dto)
        {
            var userId = _currentUser.GetUserId();
            if (dto.BudgetedAmount != 0)
                _context.FundTransactions.Add(CreateFundEntry(groupId, "BUDGETED", dto.BudgetedAmount, balance.BudgetedBalance - dto.BudgetedAmount, balance.BudgetedBalance, dto, userId));
            if (dto.OwnFundsAmount != 0)
                _context.FundTransactions.Add(CreateFundEntry(groupId, "OWN_FUNDS", dto.OwnFundsAmount, balance.OwnFundsBalance - dto.OwnFundsAmount, balance.OwnFundsBalance, dto, userId));
        }

        private static FundTransaction CreateFundEntry(Guid groupId, string fundType, decimal amount, decimal previous, decimal current, AddFundsDto dto, Guid? userId) => new()
        {
            Id = Guid.NewGuid(), GroupId = groupId, TransactionType = "MANUAL_FUND_ENTRY", FundType = fundType,
            Amount = amount, PreviousBalance = previous, NewBalance = current, Description = dto.Reason.Trim(),
            Source = string.IsNullOrWhiteSpace(dto.Source) ? "Manual fund entry" : dto.Source.Trim(),
            ReferenceType = "MANUAL", CreatedById = userId, CreatedDate = DateTime.UtcNow, UpdatedDate = DateTime.UtcNow,
            Metadata = dto.AttachmentData == null ? null : System.Text.Json.JsonSerializer.Serialize(new { dto.AttachmentName, dto.AttachmentContentType, dto.AttachmentData })
        };

        // ==========================================
        // POST: api/groups/{id}/disable
        // ==========================================
        [HttpPost("{id}/disable")]
        [Authorize]
        [RequirePermission("groups.manage")]
        public async Task<IActionResult> Disable(Guid id)
        {
            var group = await _context.Groups.FindAsync(id);
            if (group == null)
                return NotFound(new { Message = "Group not found" });

            if (!group.IsActive)
                return BadRequest(new { Message = "Group is already disabled." });

            var hasActiveRequisitions = await _context.Requisitions
                .AnyAsync(r => r.GroupId == id && r.IsArchived == false);
            if (hasActiveRequisitions)
                return Conflict(new { Message = "Cannot disable group that has active requisitions." });

            group.IsActive = false;
            group.UpdatedDate = DateTime.UtcNow;
            group.LastProcessedTimeStamp = DateTime.UtcNow;
            group.Version++;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Group disabled successfully.",
                GroupId = group.Id,
                GroupName = group.GroupName
            });
        }

        // ==========================================
        // POST: api/groups/{id}/reactivate
        // ==========================================
        [HttpPost("{id}/reactivate")]
        [Authorize]
        [RequirePermission("groups.manage")]
        public async Task<IActionResult> Reactivate(Guid id)
        {
            var group = await _context.Groups.FindAsync(id);
            if (group == null)
                return NotFound(new { Message = "Group not found" });

            if (group.IsActive)
                return BadRequest(new { Message = "Group is already active." });

            group.IsActive = true;
            group.UpdatedDate = DateTime.UtcNow;
            group.LastProcessedTimeStamp = DateTime.UtcNow;
            group.Version++;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Group reactivated successfully.",
                GroupId = group.Id,
                GroupName = group.GroupName
            });
        }

        // ==========================================
        // PUT: api/groups/{id}
        // ==========================================
        [HttpPut("{id}")]
        [Authorize]
        [RequirePermission("groups.manage")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateGroupDto dto)
        {
            var group = await _context.Groups.FindAsync(id);
            if (group == null)
                return NotFound(new { Message = "Group not found" });

            if (!string.IsNullOrEmpty(dto.GroupType) && dto.GroupType != "ChurchGroup")
                return BadRequest(new { Message = "Groups may only use the ChurchGroup type." });

            if (!string.IsNullOrEmpty(dto.GroupName) && dto.GroupName != group.GroupName)
            {
                var exists = await _context.Groups
                    .AnyAsync(g => g.GroupName.ToLower() == dto.GroupName.ToLower() && g.Id != id && g.IsActive);
                if (exists)
                    return Conflict(new { Message = "A group with this name already exists." });
            }

            if (!string.IsNullOrEmpty(dto.GroupName))
                group.GroupName = dto.GroupName;

            if (dto.Description != null)
                group.Description = dto.Description;

            if (!string.IsNullOrEmpty(dto.GroupType))
                group.GroupType = dto.GroupType;

            if (dto.LeaderId.HasValue)
                group.LeaderId = dto.LeaderId;

            if (dto.ParentGroupId.HasValue)
                group.ParentGroupId = dto.ParentGroupId;

            group.UpdatedDate = DateTime.UtcNow;
            group.LastProcessedTimeStamp = DateTime.UtcNow;
            group.Version++;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                group.Id,
                group.GroupName,
                group.GroupType,
                group.Description,
                group.IsActive
            });
        }

        // ==========================================
        // GET: api/groups/types
        // ==========================================
        [HttpGet("types")]
        public IActionResult GetTypes()
        {
            var types = new[] { "ChurchGroup" };
            return Ok(types);
        }

        // ==========================================
        // GET: api/groups/search
        // ==========================================
        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return Ok(new List<object>());

            var query = _context.Groups.AsQueryable();
            query = query.Where(g => g.GroupType == "ChurchGroup");
            if (!_groupAccess.HasGlobalAccess())
            {
                var accessibleIds = await _groupAccess.GetAccessibleGroupIdsAsync();
                query = query.Where(g => accessibleIds.Contains(g.Id));
            }

            var groups = await query
                .Where(g => g.IsActive && g.GroupName.Contains(q))
                .OrderBy(g => g.GroupName)
                .Select(g => new
                {
                    g.Id,
                    g.GroupName,
                    g.GroupType
                })
                .ToListAsync();

            return Ok(groups);
        }
    }

    // ==========================================
    // DTOs
    // ==========================================

    public class CreateGroupDto
    {
        public string GroupName { get; set; } = null!;
        public string? Description { get; set; }
        public string GroupType { get; set; } = "ChurchGroup";
        public Guid? LeaderId { get; set; }
        public Guid? ParentGroupId { get; set; }
    }

    public class UpdateGroupDto
    {
        public string? GroupName { get; set; }
        public string? Description { get; set; }
        public string? GroupType { get; set; }
        public Guid? LeaderId { get; set; }
        public Guid? ParentGroupId { get; set; }
    }

    public class AddFundsDto
    {
        public decimal BudgetedAmount { get; set; }
        public decimal OwnFundsAmount { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? Source { get; set; }
        public string? AttachmentName { get; set; }
        public string? AttachmentContentType { get; set; }
        public string? AttachmentData { get; set; }
    }
}
