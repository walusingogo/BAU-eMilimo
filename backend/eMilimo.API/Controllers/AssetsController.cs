using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using eMilimo.API.Authorization;
using eMilimo.API.Services;
using eMilimo.Core.Entities;
using eMilimo.Core.Entities.Companions;
using eMilimo.Core.Services;
using eMilimo.Infrastructure.Data;
using eMilimo.API.Platform;

namespace eMilimo.API.Controllers;

[ApiController,Route("api/assets"),Authorize]
public class AssetsController : ControllerBase
{
    private readonly AppDbContext _db; private readonly ICurrentUserService _current; private readonly IEmailService _email; private readonly ILockService _locks; private readonly IFileStorage _storage; private readonly IRolePermissionService _permissions; private readonly IApprovalNotificationService _approvalNotifications; private readonly IGroupAccessService _groupAccess; private readonly IReferenceNumberService _referenceNumbers;
    public AssetsController(AppDbContext db,ICurrentUserService current,IEmailService email,ILockService locks,IFileStorage storage,IRolePermissionService permissions,IApprovalNotificationService approvalNotifications,IGroupAccessService groupAccess,IReferenceNumberService referenceNumbers)=>(_db,_current,_email,_locks,_storage,_permissions,_approvalNotifications,_groupAccess,_referenceNumbers)=(db,current,email,locks,storage,permissions,approvalNotifications,groupAccess,referenceNumbers);
    private Guid UserId()=>_current.GetUserId()??throw new UnauthorizedAccessException();

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var query=_db.Assets.AsNoTracking().AsQueryable();
        if(!IsAdministrator()) query=query.Where(a=>_db.AssetCheckouts.Any(c=>c.AssetId==a.Id&&c.RequestedById==UserId()));
        return Ok(await ProjectAssets(query).ToListAsync());
    }

    [HttpGet("catalog"),RequirePermission("assets.request")]
    public async Task<IActionResult> Catalog()=>Ok(await ProjectAssets(_db.Assets.AsNoTracking().Where(x=>x.IsActive&&x.QuantityAvailable>0)).ToListAsync());

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var query=_db.Assets.AsNoTracking().Where(x=>x.Id==id);
        if(!IsAdministrator()) query=query.Where(a=>_db.AssetCheckouts.Any(c=>c.AssetId==a.Id&&c.RequestedById==UserId()));
        var asset=await ProjectAssets(query).FirstOrDefaultAsync();
        return asset==null?NotFound(new{message="Asset was not found or is not available to this user."}):Ok(asset);
    }

    [HttpPost,RequirePermission("assets.manage")]
    public async Task<IActionResult> Create(CreateAssetRequest r)
    {
        if(!IsAdministrator())return Forbid();
        if(r.QuantityTotal<1)return BadRequest(new{message="Quantity must be at least one."});
        var category=await _db.AssetCategories.FirstOrDefaultAsync(x=>x.Id==r.AssetCategoryId&&x.IsActive);if(category==null)return BadRequest(new{message="Select an active asset category."});
        var asset=new Asset{Id=Guid.NewGuid(),AssetNumber=await _referenceNumbers.NextAsync("ASSET"),Name=r.Name.Trim(),Category=category.Code,AssetCategoryId=category.Id,Description=r.Description,QuantityTotal=r.QuantityTotal,QuantityAvailable=r.QuantityTotal,RegistrationNumber=r.RegistrationNumber,CreatedById=UserId()};
        _db.Assets.Add(asset);await _db.SaveChangesAsync();return Ok(asset);
    }

    [HttpPost("{id:guid}/lock"),RequirePermission("assets.manage")]
    public async Task<IActionResult> Lock(Guid id){var acquired=await _locks.AcquireLockAsync<AssetLck>(id,UserId(),300,"Editing asset");if(!acquired)return Conflict(new{message="This asset is currently being edited by another user."});_db.AssetAudits.Add(new AssetAud{Id=Guid.NewGuid(),AssetId=id,Action="LOCK_ACQUIRED",ChangesJson="{}",PerformedById=UserId(),PerformedAtUtc=DateTime.UtcNow});await _db.SaveChangesAsync();return Ok(new{message="Asset locked for editing."});}
    [HttpDelete("{id:guid}/lock"),RequirePermission("assets.manage")]
    public async Task<IActionResult> Unlock(Guid id){await _locks.ReleaseLockAsync<AssetLck>(id,UserId());_db.AssetAudits.Add(new AssetAud{Id=Guid.NewGuid(),AssetId=id,Action="LOCK_RELEASED",ChangesJson="{}",PerformedById=UserId(),PerformedAtUtc=DateTime.UtcNow});await _db.SaveChangesAsync();return Ok();}

    [HttpPut("{id:guid}"),RequirePermission("assets.manage")]
    public async Task<IActionResult> Update(Guid id,UpdateAssetRequest r)
    {
        if(!IsAdministrator())return Forbid();var owner=await _locks.GetLockOwnerAsync<AssetLck>(id);if(owner!=UserId())return Conflict(new{message="Acquire the asset edit lock before saving."});
        var asset=await _db.Assets.FindAsync(id);if(asset==null)return NotFound();var category=await _db.AssetCategories.FirstOrDefaultAsync(x=>x.Id==r.AssetCategoryId&&x.IsActive);if(category==null)return BadRequest(new{message="Select an active category."});
        var checkedOut=asset.QuantityTotal-asset.QuantityAvailable;if(r.QuantityTotal<checkedOut)return BadRequest(new{message=$"Total quantity cannot be below the {checkedOut} units currently checked out."});
        asset.Name=r.Name.Trim();asset.AssetCategoryId=category.Id;asset.Category=category.Code;asset.Description=r.Description;asset.RegistrationNumber=r.RegistrationNumber;asset.QuantityTotal=r.QuantityTotal;asset.QuantityAvailable=r.QuantityTotal-checkedOut;asset.UpdatedById=UserId();asset.UpdatedDate=DateTime.UtcNow;asset.Version++;
        await _db.SaveChangesAsync();await _locks.ReleaseLockAsync<AssetLck>(id,UserId());return Ok(asset);
    }

    [HttpPost("{id:guid}/status"),RequirePermission("assets.manage")]
    public async Task<IActionResult> SetStatus(Guid id,AssetStatusRequest request){if(!IsAdministrator())return Forbid();var asset=await _db.Assets.FindAsync(id);if(asset==null)return NotFound();asset.IsActive=request.IsActive;asset.UpdatedById=UserId();asset.UpdatedDate=DateTime.UtcNow;asset.Version++;await _db.SaveChangesAsync();return Ok(new{asset.Id,asset.IsActive,message=asset.IsActive?"Asset marked in stock.":"Asset marked out of stock."});}

    [HttpPost("{id:guid}/attachments"),RequirePermission("assets.manage")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadAssetAttachment(Guid id,IFormFile file,[FromForm]string attachmentType="ASSET_IMAGE")
    {if(!IsAdministrator())return Forbid();if(!await _db.Assets.AnyAsync(x=>x.Id==id))return NotFound();if(attachmentType.Equals("ASSET_IMAGE",StringComparison.OrdinalIgnoreCase)&&await _db.AssetAtms.CountAsync(x=>x.EntityId==id&&x.AttachmentType=="ASSET_IMAGE")>=6)return BadRequest(new{message="An asset can have a maximum of 6 images."});var attachment=await SaveAssetAttachment(id,file,attachmentType,UserId());return Ok(attachment);}

    [HttpGet("{id:guid}/attachments")]
    public async Task<IActionResult> Attachments(Guid id){if(!IsAdministrator()&&!await _db.AssetCheckouts.AnyAsync(x=>x.AssetId==id&&x.RequestedById==UserId()))return Forbid();var files=await _db.AssetAtms.AsNoTracking().Where(x=>x.EntityId==id).Select(x=>new{x.Id,x.FileName,x.MimeType,x.Size,x.AttachmentType,AssetCheckoutId=(Guid?)null,x.UploadedAt}).ToListAsync();var checkoutQuery=_db.AssetCheckoutAtms.AsNoTracking().Where(x=>x.AssetCheckout!.AssetId==id);if(!IsAdministrator()&&!CanApproveCheckout()){var latestId=await _db.AssetCheckouts.Where(x=>x.AssetId==id&&x.RequestedById==UserId()).OrderByDescending(x=>x.CreatedDate).Select(x=>(Guid?)x.Id).FirstOrDefaultAsync();checkoutQuery=checkoutQuery.Where(x=>x.EntityId==latestId);}files.AddRange(await checkoutQuery.Select(x=>new{x.Id,x.FileName,x.MimeType,x.Size,x.AttachmentType,AssetCheckoutId=(Guid?)x.EntityId,x.UploadedAt}).ToListAsync());return Ok(files.OrderByDescending(x=>x.UploadedAt));}
    [HttpGet("{id:guid}/attachments/{attachmentId:guid}/file")]
    public async Task<IActionResult> AttachmentFile(Guid id,Guid attachmentId){var a=await _db.AssetAtms.AsNoTracking().FirstOrDefaultAsync(x=>x.Id==attachmentId&&x.EntityId==id);if(a!=null){var path=_storage.GetPhysicalPath(a.FilePath);return System.IO.File.Exists(path)?PhysicalFile(path,a.MimeType,a.FileName):NotFound();}var checkoutAttachment=await _db.AssetCheckoutAtms.AsNoTracking().Include(x=>x.AssetCheckout).FirstOrDefaultAsync(x=>x.Id==attachmentId&&x.AssetCheckout!.AssetId==id);if(checkoutAttachment==null)return NotFound();if(!IsAdministrator()&&!CanApproveCheckout()&&checkoutAttachment.AssetCheckout!.RequestedById!=UserId())return Forbid();var checkoutPath=_storage.GetPhysicalPath(checkoutAttachment.FilePath);return System.IO.File.Exists(checkoutPath)?PhysicalFile(checkoutPath,checkoutAttachment.MimeType,checkoutAttachment.FileName):NotFound();}
    [HttpDelete("{id:guid}/attachments/{attachmentId:guid}"),RequirePermission("assets.manage")]
    public async Task<IActionResult> DeleteAttachment(Guid id,Guid attachmentId){if(!IsAdministrator())return Forbid();var a=await _db.AssetAtms.FirstOrDefaultAsync(x=>x.Id==attachmentId&&x.EntityId==id);if(a==null)return NotFound();await _storage.DeleteAsync(a.FilePath);_db.AssetAtms.Remove(a);_db.AssetAudits.Add(new AssetAud{Id=Guid.NewGuid(),AssetId=id,RecordType="ASSET",Action="ATTACHMENT_REMOVED",ChangesJson=System.Text.Json.JsonSerializer.Serialize(new{a.FileName,a.AttachmentType}),PerformedById=UserId(),PerformedAtUtc=DateTime.UtcNow});await _db.SaveChangesAsync();return NoContent();}

    [HttpGet("checkouts")]
    public async Task<IActionResult> Checkouts([FromQuery]bool organization=false,[FromQuery]bool review=false,[FromQuery]Guid? userId=null,[FromQuery]Guid? assetId=null,[FromQuery]DateTime? fromDate=null,[FromQuery]DateTime? toDate=null,[FromQuery]string? status=null,[FromQuery]Guid? groupId=null,[FromQuery]Guid? sectionId=null,[FromQuery]Guid? committeeId=null){var query=_db.AssetCheckouts.AsNoTracking().Include(x=>x.Asset).AsQueryable();var canReview=review&&CanApproveCheckout();var canReportAll=organization&&IsAdministrator();if(!canReview&&!canReportAll)query=query.Where(x=>x.RequestedById==UserId());else if(canReportAll&&userId.HasValue)query=query.Where(x=>x.RequestedById==userId);if(assetId.HasValue)query=query.Where(x=>x.AssetId==assetId);if(fromDate.HasValue)query=query.Where(x=>x.CreatedDate>=Utc(fromDate.Value));if(toDate.HasValue)query=query.Where(x=>x.CreatedDate<Utc(toDate.Value).AddDays(1));if(!string.IsNullOrWhiteSpace(status))query=query.Where(x=>x.Status==status);if(groupId.HasValue)query=query.Where(x=>x.GroupId==groupId);if(sectionId.HasValue)query=query.Where(x=>x.SectionId==sectionId);if(committeeId.HasValue)query=query.Where(x=>x.CommitteeId==committeeId);return Ok(await query.OrderByDescending(x=>x.CreatedDate).Select(x=>new{x.Id,x.RequestNumber,x.AssetId,AssetName=x.Asset!.Name,Category=x.Asset.Category,x.Quantity,x.Status,x.Purpose,x.RequestedById,RequestedBy=_db.Users.Where(u=>u.Id==x.RequestedById).Select(u=>new{u.Id,Name=u.FirstName+" "+u.LastName}).FirstOrDefault(),x.GroupId,x.SectionId,x.CommitteeId,RequestingEntity=x.GroupId.HasValue?_db.Groups.Where(g=>g.Id==x.GroupId).Select(g=>g.GroupName).FirstOrDefault():x.SectionId.HasValue?_db.Sections.Where(s=>s.Id==x.SectionId).Select(s=>s.SectionName).FirstOrDefault():x.CommitteeId.HasValue?_db.Committees.Where(c=>c.Id==x.CommitteeId).Select(c=>c.CommitteeName).FirstOrDefault():"Personal",x.DriverId,x.CreatedDate,x.ExpectedOutAtUtc,x.ExpectedReturnAtUtc,x.CheckedOutAtUtc,x.ReturnedAtUtc,x.ApprovalNote,x.PaymentRequired,x.PaymentAmount,x.PaymentHonored,x.PaymentHonoredAtUtc,x.PaymentVerifiedById,PaymentProofAttachmentId=_db.AssetCheckoutAtms.Where(a=>a.EntityId==x.Id&&a.AttachmentType=="PAYMENT_PROOF").Select(a=>(Guid?)a.Id).FirstOrDefault()}).ToListAsync());}

    [HttpDelete("checkouts/{id:guid}")]
    public async Task<IActionResult> Recall(Guid id){var checkout=await _db.AssetCheckouts.Include(x=>x.Asset).FirstOrDefaultAsync(x=>x.Id==id);if(checkout==null)return NotFound();if(checkout.RequestedById!=UserId())return Forbid();if(checkout.Status!="PENDING")return BadRequest(new{message="Only pending checkout requests can be recalled."});var attachments=await _db.AssetCheckoutAtms.Where(x=>x.EntityId==id).ToListAsync();foreach(var attachment in attachments)await _storage.DeleteAsync(attachment.FilePath);_db.AssetCheckoutAtms.RemoveRange(attachments);_db.AssetAudits.Add(new AssetAud{Id=Guid.NewGuid(),AssetId=checkout.AssetId,AssetCheckoutId=checkout.Id,RecordType="CHECKOUT",Action="RECALLED",ChangesJson=System.Text.Json.JsonSerializer.Serialize(new{checkout.Quantity,checkout.Purpose}),PerformedById=UserId(),PerformedAtUtc=DateTime.UtcNow});_db.AssetCheckouts.Remove(checkout);await _db.SaveChangesAsync();await NotifyRequester(checkout,"RECALLED",null);return Ok(new{message="Asset checkout request recalled and removed."});}

    [HttpGet("checkouts/{id:guid}")]
    public async Task<IActionResult> Checkout(Guid id){var c=await _db.AssetCheckouts.AsNoTracking().Include(x=>x.Asset).FirstOrDefaultAsync(x=>x.Id==id);if(c==null)return NotFound();if(c.RequestedById!=UserId()&&!CanApproveCheckout()&&!IsAdministrator())return Forbid();var requester=await _db.Users.AsNoTracking().Where(x=>x.Id==c.RequestedById).Select(x=>new{x.Id,x.FirstName,x.LastName,x.MembershipId,x.Role,x.SignatureData}).FirstOrDefaultAsync();var approver=await _db.Users.AsNoTracking().Where(x=>x.Id==c.ApprovedById).Select(x=>new{x.Id,x.FirstName,x.LastName,x.MembershipId,x.Role,x.SignatureData}).FirstOrDefaultAsync();var paymentProof=await _db.AssetCheckoutAtms.AsNoTracking().Where(a=>a.EntityId==c.Id&&a.AttachmentType=="PAYMENT_PROOF").Select(a=>new{a.Id,a.FileName,a.MimeType}).FirstOrDefaultAsync();var revenueContribution=await _db.Contributions.AsNoTracking().Where(x=>x.AssetCheckoutId==c.Id).Select(x=>new{x.Id,x.ContributionNumber,x.Amount,x.Status,x.FinalizedFinancialRecordBookId}).FirstOrDefaultAsync();return Ok(new{c.Id,c.RequestNumber,c.AssetId,AssetName=c.Asset!.Name,c.Asset.AssetNumber,c.Quantity,c.Purpose,c.Status,c.CreatedDate,c.ExpectedOutAtUtc,c.ExpectedReturnAtUtc,c.CheckedOutAtUtc,c.ReturnedAtUtc,c.ApprovalNote,c.PaymentRequired,c.PaymentAmount,c.PaymentHonored,PaymentProof=paymentProof,RevenueContribution=revenueContribution,Requester=requester,Approver=approver});}

    [HttpGet("drivers"),RequirePermission("assets.request")]
    public async Task<IActionResult> Drivers()=>Ok(await _db.AssetDrivers.AsNoTracking().Where(x=>x.IsActive).OrderBy(x=>x.Name).Select(x=>new{x.Id,FirstName=x.Name,LastName="",x.Name,x.Email,x.PhoneNumber}).ToListAsync());

    [HttpPost("{assetId:guid}/checkout"),RequirePermission("assets.request")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> RequestCheckout(Guid assetId,[FromForm]CheckoutRequest r)
    {
        var asset=await _db.Assets.FindAsync(assetId);if(asset==null||!asset.IsActive)return NotFound();
        if(await _db.AssetCheckouts.AnyAsync(x=>x.AssetId==assetId&&x.RequestedById==UserId()&&(x.Status=="PENDING"||x.Status=="CHECKED_OUT")))return Conflict(new{message="You already have a pending or active checkout for this asset."});
        if(r.Quantity<1||r.Quantity>asset.QuantityAvailable)return BadRequest(new{message="Requested quantity is not currently available."});
        if(asset.Category=="VEHICLE"&&!r.DriverId.HasValue)return BadRequest(new{message="A driver is required for vehicle checkout."});
        if(r.PaymentRequired&&(!r.PaymentAmount.HasValue||r.PaymentAmount<=0))return BadRequest(new{message="Enter the required payment amount."});
        var entityCount=new[]{r.GroupId,r.SectionId,r.CommitteeId}.Count(x=>x.HasValue);if(entityCount>1)return BadRequest(new{message="Select only one requesting entity."});
        if(entityCount==1&&!await _groupAccess.CanAccessEntityAsync(r.GroupId,r.SectionId,r.CommitteeId))return Forbid();
        var checkoutId=Guid.NewGuid();var checkout=new AssetCheckout{Id=checkoutId,RequestNumber=$"ARQ-{DateTime.UtcNow:yyyyMMdd}-{checkoutId.ToString("N")[..8].ToUpperInvariant()}",AssetId=assetId,RequestedById=UserId(),GroupId=r.GroupId,SectionId=r.SectionId,CommitteeId=r.CommitteeId,DriverId=r.DriverId,Quantity=r.Quantity,Purpose=r.Purpose.Trim(),ExpectedOutAtUtc=Utc(r.ExpectedOutAtUtc),ExpectedReturnAtUtc=Utc(r.ExpectedReturnAtUtc),PaymentRequired=r.PaymentRequired,PaymentAmount=r.PaymentRequired?r.PaymentAmount:null,CreatedById=UserId()};
        _db.AssetCheckouts.Add(checkout);await _db.SaveChangesAsync();if(r.PaymentProof!=null)await SaveCheckoutAttachment(checkout,r.PaymentProof,"PAYMENT_PROOF",UserId());await NotifyRequester(checkout,"PENDING",null);await _approvalNotifications.NotifyAsync("assets.approve","Asset request",checkout.RequestNumber,$"{checkout.Quantity} × {asset.Name} requires review.",$"assets/checkouts/{checkout.Id}",checkout.RequestedById,["Approve","Reject with comment","Return with comment (after checkout)"],ccUserId:checkout.RequestedById);return Ok(checkout);
    }

    [HttpPost("checkouts/{id:guid}/approve"),RequirePermission("assets.approve")]
    public async Task<IActionResult> Approve(Guid id,ActionRequest r)
    {
        await using var tx=await _db.Database.BeginTransactionAsync();
        var c=await _db.AssetCheckouts.Include(x=>x.Asset).FirstOrDefaultAsync(x=>x.Id==id);if(c==null)return NotFound();
        if(c.RequestedById==UserId())return BadRequest(new{message="A checkout request must be approved by another authorized user."});
        if(c.Status!="PENDING")return BadRequest(new{message="Only pending requests can be approved."});
        if(c.PaymentRequired&&r.PaymentHonored!=true)return BadRequest(new{message="Confirm that the required payment was honored before approving checkout."});
        if(c.Asset!.QuantityAvailable<c.Quantity)return BadRequest(new{message="Insufficient available quantity."});
        c.Asset.QuantityAvailable-=c.Quantity;c.Asset.Version++;c.Status="CHECKED_OUT";c.ApprovedById=UserId();c.CheckedOutAtUtc=DateTime.UtcNow;c.ApprovalNote=r.Note;c.PaymentHonored=c.PaymentRequired?r.PaymentHonored:null;c.PaymentHonoredAtUtc=c.PaymentHonored==true?DateTime.UtcNow:null;c.PaymentVerifiedById=c.PaymentHonored==true?UserId():null;c.UpdatedById=UserId();c.Version++;
        _db.AssetMovements.Add(new AssetMovement{Id=Guid.NewGuid(),AssetId=c.AssetId,CheckoutId=c.Id,QuantityChange=-c.Quantity,BalanceAfter=c.Asset.QuantityAvailable,Action="CHECKED_OUT",PerformedById=UserId(),PerformedAtUtc=DateTime.UtcNow,Notes=r.Note});
        if(c.PaymentHonored==true&&c.PaymentAmount>0&&!await _db.Contributions.AnyAsync(x=>x.AssetCheckoutId==c.Id))
        {
            var type=await _db.ContributionTypes.FirstOrDefaultAsync(x=>x.Code=="ASSET_HIRE"&&x.IsActive);
            var source=await _db.ContributionSources.FirstOrDefaultAsync(x=>x.Name=="Asset Hire"&&x.IsActive);
            if(type==null||source==null)return BadRequest(new{message="Asset Hire contribution setup is missing or inactive. Activate the Asset Hire contribution type and source before approving this paid checkout."});
            var requester=await _db.Users.AsNoTracking().FirstAsync(x=>x.Id==c.RequestedById);
            var member=string.IsNullOrWhiteSpace(requester.MembershipId)?null:await _db.Members.AsNoTracking().FirstOrDefaultAsync(x=>x.MembershipNumber==requester.MembershipId&&!x.IsArchived);
            var contribution=new Contribution{Id=Guid.NewGuid(),ContributionNumber=await _referenceNumbers.NextAsync("CONTRIBUTION"),ContributionTypeId=type.Id,SourceId=source.Id,
                Amount=c.PaymentAmount.Value,ContributionDate=c.PaymentHonoredAtUtc??DateTime.UtcNow,Description=$"Asset hire receipt: {c.Asset.Name} ({c.RequestNumber})",ReferenceNumber=c.RequestNumber,
                PaymentMethod="ASSET_HIRE",Status="CONFIRMED",ReceivedBy=c.RequestedById,ConfirmedBy=UserId(),ConfirmedAt=DateTime.UtcNow,CreatedById=UserId(),UpdatedById=UserId(),
                OriginType="ASSET_HIRE",AssetCheckoutId=c.Id,MemberId=member?.Id,MembershipNumberSnapshot=requester.MembershipId,ContributorNameSnapshot=$"{requester.FirstName} {requester.LastName}".Trim(),
                CurrencyCode="ZMW",OriginalCurrencyAmount=c.PaymentAmount.Value,ExchangeRateSnapshot=1m};
            _db.Contributions.Add(contribution);
            if(c.SectionId.HasValue||c.GroupId.HasValue||c.CommitteeId.HasValue)
                _db.ContributionAllocations.Add(new ContributionAllocation{Id=Guid.NewGuid(),ContributionId=contribution.Id,ContributionTypeId=type.Id,
                    SectionId=c.SectionId,GroupId=c.GroupId,CommitteeId=c.CommitteeId,AllocatedAmount=contribution.Amount,PercentageAllocation=100m,IsPrimary=true,
                    Notes=$"Generated from paid asset hire {c.RequestNumber}",CreatedById=UserId(),UpdatedById=UserId()});
            _db.AssetAudits.Add(new AssetAud{Id=Guid.NewGuid(),AssetId=c.AssetId,AssetCheckoutId=c.Id,RecordType="CHECKOUT",Action="HIRE_REVENUE_RECORDED",
                ChangesJson=System.Text.Json.JsonSerializer.Serialize(new{ContributionId=contribution.Id,contribution.ContributionNumber,contribution.Amount}),PerformedById=UserId(),PerformedAtUtc=DateTime.UtcNow});
        }
        await _db.SaveChangesAsync();await tx.CommitAsync();
        await NotifyRequester(c,"APPROVED",r.Note);if(c.DriverId.HasValue){var driver=await _db.AssetDrivers.AsNoTracking().FirstOrDefaultAsync(x=>x.Id==c.DriverId);if(driver!=null&&!string.IsNullOrWhiteSpace(driver.Email))await _email.SendAsync(driver.Email,"Vehicle assignment",$"<p>You have been assigned to drive <strong>{c.Asset.Name}</strong>.</p><p>Purpose: {System.Net.WebUtility.HtmlEncode(c.Purpose)}<br>Expected out: {c.ExpectedOutAtUtc:u}<br>Expected return: {c.ExpectedReturnAtUtc:u}</p>");}
        return Ok(c);
    }

    [HttpPost("checkouts/{id:guid}/reject"),RequirePermission("assets.approve")]
    public async Task<IActionResult> Reject(Guid id,ActionRequest r){if(!CanApproveCheckout())return Forbid();if(string.IsNullOrWhiteSpace(r.Note))return BadRequest(new{message="A rejection comment is required."});var c=await _db.AssetCheckouts.Include(x=>x.Asset).FirstOrDefaultAsync(x=>x.Id==id);if(c==null)return NotFound();if(c.RequestedById==UserId())return BadRequest(new{message="A checkout request must be reviewed by another authorized user."});if(c.Status!="PENDING")return BadRequest();c.Status="REJECTED";c.ApprovedById=UserId();c.ApprovalNote=r.Note;c.UpdatedById=UserId();c.Version++;await _db.SaveChangesAsync();await NotifyRequester(c,"REJECTED",r.Note);return Ok(c);}

    [HttpPost("checkouts/{id:guid}/return")]
    public async Task<IActionResult> Return(Guid id,ActionRequest r){await using var tx=await _db.Database.BeginTransactionAsync();var c=await _db.AssetCheckouts.Include(x=>x.Asset).FirstOrDefaultAsync(x=>x.Id==id);if(c==null)return NotFound();if(c.RequestedById!=UserId()&&!IsAdministrator())return Forbid();if(c.Status!="CHECKED_OUT")return BadRequest(new{message="Only checked-out assets can be returned."});c.Asset!.QuantityAvailable=Math.Min(c.Asset.QuantityTotal,c.Asset.QuantityAvailable+c.Quantity);c.Asset.Version++;c.Status="RETURNED";c.ReturnedAtUtc=DateTime.UtcNow;c.UpdatedById=UserId();c.Version++;_db.AssetMovements.Add(new AssetMovement{Id=Guid.NewGuid(),AssetId=c.AssetId,CheckoutId=c.Id,QuantityChange=c.Quantity,BalanceAfter=c.Asset.QuantityAvailable,Action="RETURNED",PerformedById=UserId(),PerformedAtUtc=DateTime.UtcNow,Notes=r.Note});await _db.SaveChangesAsync();await tx.CommitAsync();await NotifyRequester(c,"RETURNED",r.Note);return Ok(c);}

    [HttpGet("{assetId:guid}/history"),RequirePermission("assets.view")]
    public async Task<IActionResult> History(Guid assetId)=>Ok(await _db.AssetMovements.AsNoTracking().Where(x=>x.AssetId==assetId).OrderByDescending(x=>x.PerformedAtUtc).ToListAsync());
    [HttpGet("{assetId:guid}/audit")]
    public async Task<IActionResult> Audit(Guid assetId){var query=_db.AssetAudits.AsNoTracking().Where(x=>x.AssetId==assetId);if(!IsAdministrator()){var checkoutId=await _db.AssetCheckouts.AsNoTracking().Where(x=>x.AssetId==assetId&&x.RequestedById==UserId()).OrderByDescending(x=>x.CreatedDate).Select(x=>(Guid?)x.Id).FirstOrDefaultAsync();if(!checkoutId.HasValue)return Forbid();query=query.Where(x=>x.AssetCheckoutId==checkoutId.Value);}return Ok(await query.OrderByDescending(x=>x.PerformedAtUtc).Select(x=>new{x.Id,x.AssetId,x.AssetCheckoutId,x.RecordType,x.Action,x.ChangesJson,x.PerformedById,PerformedBy=_db.Users.Where(u=>u.Id==x.PerformedById).Select(u=>u.FirstName+" "+u.LastName).FirstOrDefault()??"System",x.PerformedAtUtc}).ToListAsync());}

    private async Task<AssetAtm> SaveAssetAttachment(Guid assetId,IFormFile file,string type,Guid userId)
    {ValidateAttachment(file);var ext=Path.GetExtension(file.FileName).ToLowerInvariant();var stored=await StoreAttachment(file,assetId,ext);var a=new AssetAtm{Id=Guid.NewGuid(),EntityId=assetId,AttachmentType=type.ToUpperInvariant(),FileName=file.FileName,FilePath=stored,MimeType=file.ContentType,Size=file.Length,UploadedById=userId,UploadedAt=DateTime.UtcNow,CreatedById=userId};_db.AssetAtms.Add(a);_db.AssetAudits.Add(new AssetAud{Id=Guid.NewGuid(),AssetId=assetId,RecordType="ASSET",Action="ATTACHMENT_ADDED",ChangesJson=System.Text.Json.JsonSerializer.Serialize(new{a.FileName,a.AttachmentType}),PerformedById=userId,PerformedAtUtc=DateTime.UtcNow});await _db.SaveChangesAsync();return a;}
    private async Task<AssetCheckoutAtm> SaveCheckoutAttachment(AssetCheckout checkout,IFormFile file,string type,Guid userId)
    {ValidateAttachment(file);var ext=Path.GetExtension(file.FileName).ToLowerInvariant();var stored=await StoreAttachment(file,checkout.AssetId,ext);var a=new AssetCheckoutAtm{Id=Guid.NewGuid(),EntityId=checkout.Id,AttachmentType=type.ToUpperInvariant(),FileName=file.FileName,FilePath=stored,MimeType=file.ContentType,Size=file.Length,UploadedById=userId,UploadedAt=DateTime.UtcNow,CreatedById=userId};_db.AssetCheckoutAtms.Add(a);_db.AssetAudits.Add(new AssetAud{Id=Guid.NewGuid(),AssetId=checkout.AssetId,AssetCheckoutId=checkout.Id,RecordType="CHECKOUT",Action="ATTACHMENT_ADDED",ChangesJson=System.Text.Json.JsonSerializer.Serialize(new{a.FileName,a.AttachmentType}),PerformedById=userId,PerformedAtUtc=DateTime.UtcNow});await _db.SaveChangesAsync();return a;}
    private static void ValidateAttachment(IFormFile file){if(file.Length==0||file.Length>10*1024*1024)throw new InvalidOperationException("Attachment must be between 1 byte and 10MB.");var ext=Path.GetExtension(file.FileName).ToLowerInvariant();if(!new[]{".pdf",".jpg",".jpeg",".png"}.Contains(ext))throw new InvalidOperationException("Only PDF, JPG, JPEG and PNG files are allowed.");}
    private async Task<string> StoreAttachment(IFormFile file,Guid assetId,string ext){var key=$"uploads/assets/{assetId}/{Guid.NewGuid()}{ext}";await using var stream=file.OpenReadStream();await _storage.SaveAsync(key,stream,file.ContentType,HttpContext.RequestAborted);return key;}
    private IQueryable<AssetListItem> ProjectAssets(IQueryable<Asset> query)=>query.OrderBy(x=>x.Name).Select(x=>new AssetListItem{Id=x.Id,AssetNumber=x.AssetNumber,Name=x.Name,Category=x.Category,AssetCategoryId=x.AssetCategoryId,AssetCategoryName=x.AssetCategory==null?null:x.AssetCategory.Name,Description=x.Description,QuantityTotal=x.QuantityTotal,QuantityAvailable=x.QuantityAvailable,RegistrationNumber=x.RegistrationNumber,IsActive=x.IsActive,Version=x.Version,ImageAttachmentId=_db.AssetAtms.Where(a=>a.EntityId==x.Id&&a.AttachmentType=="ASSET_IMAGE").OrderByDescending(a=>a.UploadedAt).Select(a=>(Guid?)a.Id).FirstOrDefault(),ImageAttachmentIds=_db.AssetAtms.Where(a=>a.EntityId==x.Id&&a.AttachmentType=="ASSET_IMAGE").OrderBy(a=>a.UploadedAt).Select(a=>a.Id).ToList()});
    private async Task NotifyRequester(AssetCheckout checkout,string status,string? note){var user=await _db.Users.AsNoTracking().FirstOrDefaultAsync(x=>x.Id==checkout.RequestedById);if(user==null||string.IsNullOrWhiteSpace(user.Email))return;var assetName=checkout.Asset?.Name??await _db.Assets.Where(x=>x.Id==checkout.AssetId).Select(x=>x.Name).FirstAsync();await _email.SendAssetRequestStatusChangedAsync(user.Email,$"{user.FirstName} {user.LastName}".Trim(),checkout.RequestNumber,assetName,checkout.Quantity,status,checkout.ExpectedOutAtUtc,checkout.ExpectedReturnAtUtc,checkout.PaymentRequired,checkout.PaymentAmount,note);}
    private static DateTime Utc(DateTime d)=>d.Kind==DateTimeKind.Utc?d:d.Kind==DateTimeKind.Local?d.ToUniversalTime():DateTime.SpecifyKind(d,DateTimeKind.Utc);
    private bool IsAdministrator()=>_current.GetUserRole() is "Admin" or "SuperAdmin";
    private bool CanApproveCheckout()=>_permissions.HasAsync(_current.GetUserRole(),"assets.approve").GetAwaiter().GetResult();
    public record CreateAssetRequest(string Name,Guid AssetCategoryId,string? Description,int QuantityTotal,string? RegistrationNumber);
    public record UpdateAssetRequest(string Name,Guid AssetCategoryId,string? Description,int QuantityTotal,string? RegistrationNumber);
    public class CheckoutRequest{public int Quantity{get;set;}public string Purpose{get;set;}="";public Guid? GroupId{get;set;}public Guid? SectionId{get;set;}public Guid? CommitteeId{get;set;}public Guid? DriverId{get;set;}public DateTime ExpectedOutAtUtc{get;set;}public DateTime ExpectedReturnAtUtc{get;set;}public bool PaymentRequired{get;set;}public decimal? PaymentAmount{get;set;}public IFormFile? PaymentProof{get;set;}}
    public record ActionRequest(string? Note,bool? PaymentHonored=null);
    public record AssetStatusRequest(bool IsActive);
    public class AssetListItem{public Guid Id{get;set;}public string AssetNumber{get;set;}="";public string Name{get;set;}="";public string Category{get;set;}="";public Guid? AssetCategoryId{get;set;}public string? AssetCategoryName{get;set;}public string? Description{get;set;}public int QuantityTotal{get;set;}public int QuantityAvailable{get;set;}public string? RegistrationNumber{get;set;}public bool IsActive{get;set;}public int Version{get;set;}public Guid? ImageAttachmentId{get;set;}public List<Guid> ImageAttachmentIds{get;set;}=[];public object? AssetCategory=>AssetCategoryId.HasValue?new{Id=AssetCategoryId,Name=AssetCategoryName}:null;}
}
