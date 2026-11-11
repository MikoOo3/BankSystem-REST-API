using BankDataAccessLayer;
using BankSystem.Business.BusinessObjects;
using BankSystem.DataAccess.DTOs;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BankSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuditLogsController : ControllerBase
    {
        private readonly BankSystemDbContext _context;

        public AuditLogsController(BankSystemDbContext context)
        {
            _context = context;
        }

        [HttpGet("All")]
        public async Task<ActionResult<IEnumerable<AuditLogDTO>>> GetAllLogs()
        {
            var logs = await AuditLogBusiness.GetAllLogsAsync(_context);
            return Ok(logs);
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
        public async Task<ActionResult<IEnumerable<AuditLogDTO>>> GetLogsByUserId(Guid userId)
        {
            if (userId == Guid.Empty)
            {
                return BadRequest("Invalid User ID format.");
            }

            var logs = await AuditLogBusiness.GetLogsByUserIdAsync(_context, userId);
            return Ok(logs);
        }
    }
}