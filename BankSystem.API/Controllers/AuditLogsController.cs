using BankDataAccessLayer;
using BankSystem.Business.BusinessObjects;
using BankSystem.DataAccess.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BankSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "BankManager")]
    [EnableRateLimiting("GeneralApiLimiter")]
    public class AuditLogsController : ControllerBase
    {
        private readonly BankSystemDbContext _context;

        public AuditLogsController(BankSystemDbContext context)
        {
            _context = context;
        }

        [HttpGet("All")]
        public async Task<ActionResult<PagedResult<AuditLogDTO>>> GetAllLogs([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var pagedLogs = await AuditLogBusiness.GetAllLogsAsync(_context, pageNumber, pageSize);
            return Ok(pagedLogs);
        }

        [HttpGet("{id:long}")]
        public async Task<ActionResult<AuditLogDTO>> GetLogById(long id)
        {
            if (id <= 0) return BadRequest("Invalid Audit Log ID.");

            var logBusiness = await AuditLogBusiness.FindAsync(_context, id);
            if (logBusiness == null)
            {
                return NotFound($"Audit log with ID {id} not found.");
            }

            return Ok(logBusiness.ToDTO);
        }

        [HttpGet("user/{userId:guid}")]
        public async Task<ActionResult<PagedResult<AuditLogDTO>>> GetLogsByUserId(Guid userId,[FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            if (userId == Guid.Empty)
            {
                return BadRequest("Invalid User ID format.");
            }

            var pagedLogs = await AuditLogBusiness.GetLogsByUserIdAsync(_context, userId, pageNumber, pageSize);
            return Ok(pagedLogs);
        }
    }
}