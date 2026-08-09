using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UCP.Core.Entities;
using UCP.Infrastructure.Data;

namespace UCP.API.Controllers;
[ApiController,Route("api/asset-categories"),Authorize]
public class AssetCategoriesController : ControllerBase
{
    private readonly AppDbContext _db; public AssetCategoriesController(AppDbContext db)=>_db=db;
    [HttpGet] public async Task<IActionResult> List([FromQuery] bool includeInactive=false)=>Ok(await _db.AssetCategories.AsNoTracking().Where(x=>includeInactive||x.IsActive).OrderBy(x=>x.Name).ToListAsync());
    [HttpPost,Authorize(Roles="Admin,SuperAdmin")] public async Task<IActionResult> Create(CategoryRequest r){var code=r.Code.Trim().ToUpperInvariant();if(await _db.AssetCategories.AnyAsync(x=>x.Code==code))return Conflict(new{message="Category code already exists."});var x=new AssetCategory{Id=Guid.NewGuid(),Code=code,Name=r.Name.Trim(),Description=r.Description};_db.AssetCategories.Add(x);await _db.SaveChangesAsync();return Ok(x);}
    [HttpPut("{id:guid}"),Authorize(Roles="Admin,SuperAdmin")] public async Task<IActionResult> Update(Guid id,CategoryRequest r){var x=await _db.AssetCategories.FindAsync(id);if(x==null)return NotFound();var code=r.Code.Trim().ToUpperInvariant();if(await _db.AssetCategories.AnyAsync(y=>y.Code==code&&y.Id!=id))return Conflict(new{message="Category code already exists."});x.Code=code;x.Name=r.Name.Trim();x.Description=r.Description;x.UpdatedDate=DateTime.UtcNow;x.Version++;await _db.SaveChangesAsync();return Ok(x);}
    [HttpPut("{id:guid}/toggle-status"),Authorize(Roles="Admin,SuperAdmin")] public async Task<IActionResult> Toggle(Guid id){var x=await _db.AssetCategories.FindAsync(id);if(x==null)return NotFound();x.IsActive=!x.IsActive;x.UpdatedDate=DateTime.UtcNow;x.Version++;await _db.SaveChangesAsync();return Ok(x);}
    public record CategoryRequest(string Code,string Name,string? Description);
}
