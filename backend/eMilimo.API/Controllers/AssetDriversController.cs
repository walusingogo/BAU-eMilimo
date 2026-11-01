using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using eMilimo.API.Authorization;
using eMilimo.Core.Entities;
using eMilimo.Infrastructure.Data;

namespace eMilimo.API.Controllers;

[ApiController, Route("api/asset-drivers"), Authorize]
public class AssetDriversController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool activeOnly=false)=>Ok(await db.AssetDrivers.AsNoTracking().Where(x=>!activeOnly||x.IsActive).OrderBy(x=>x.Name).ToListAsync());

    [HttpPost, RequirePermission("settings.manage")]
    public async Task<IActionResult> Create(DriverRequest request){if(string.IsNullOrWhiteSpace(request.Name)||string.IsNullOrWhiteSpace(request.Email)||string.IsNullOrWhiteSpace(request.PhoneNumber))return BadRequest(new{message="Driver name, email and phone are required."});var item=new AssetDriver{Id=Guid.NewGuid(),Name=request.Name.Trim(),Email=request.Email.Trim(),PhoneNumber=request.PhoneNumber.Trim()};db.AssetDrivers.Add(item);await db.SaveChangesAsync();return Ok(item);}

    [HttpPut("{id:guid}"), RequirePermission("settings.manage")]
    public async Task<IActionResult> Update(Guid id,DriverRequest request){var item=await db.AssetDrivers.FindAsync(id);if(item==null)return NotFound();item.Name=request.Name.Trim();item.Email=request.Email.Trim();item.PhoneNumber=request.PhoneNumber.Trim();item.IsActive=request.IsActive;item.UpdatedDate=DateTime.UtcNow;item.Version++;await db.SaveChangesAsync();return Ok(item);}

    public record DriverRequest(string Name,string Email,string PhoneNumber,bool IsActive=true);
}
