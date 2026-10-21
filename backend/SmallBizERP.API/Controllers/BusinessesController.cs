using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmallBizERP.Core.Entities;
using SmallBizERP.Infrastructure.Data;

namespace SmallBizERP.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BusinessesController : ControllerBase
{
    private readonly AppDbContext _context;

    public BusinessesController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/businesses
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Business>>> GetBusinesses()
    {
        var businesses = await _context.Businesses
            .AsNoTracking()
            .ToListAsync();

        return Ok(businesses);
    }

    // POST: api/businesses
    [HttpPost]
    public async Task<ActionResult<Business>> CreateBusiness(Business business)
    {
        business.Id = Guid.NewGuid();

        _context.Businesses.Add(business);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetBusinesses),
            new { id = business.Id },
            business);
    }
}