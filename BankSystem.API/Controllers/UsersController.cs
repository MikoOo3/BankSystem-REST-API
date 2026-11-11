using BankDataAccessLayer;
using BankSystem.Business.BusinessObjects;
using BankSystem.DataAccess.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BankSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly BankSystemDbContext _context;

        public UsersController(BankSystemDbContext context)
        {
            _context = context;
        }

        [HttpGet("All")]
        public async Task<ActionResult<IEnumerable<UserDTO>>> GetAllUsers()
        {
            var users = await UserBusiness.GetAllUsersAsync(_context);
            return Ok(users);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<UserDTO>> GetUserById(Guid id)
        {
            if (id == Guid.Empty)
            {
                return BadRequest("Invalid User ID format.");
            }

            var userBusiness = await UserBusiness.FindAsync(_context, id);

            if (userBusiness == null)
            {
                return NotFound($"User with ID {id} not found.");
            }

            return Ok(userBusiness.ToDTO);
        }

        [HttpPost("register")]
        public async Task<ActionResult<UserDTO>> RegisterUser([FromBody] UserDTO userDto)
        {
            if (userDto == null) return BadRequest("Invalid user data.");

            bool isExist = await UserBusiness.IsUserExist(_context, userDto.Email);
            if (isExist) return Conflict("Email is already registered.");

            var userBusiness = new UserBusiness(_context, userDto, UserBusiness.enMode.AddNew);
            bool isSaved = await userBusiness.SaveAsync();

            if (!isSaved) return StatusCode(500, "A problem occurred while handling your request.");

            return CreatedAtAction(nameof(GetUserById), new { id = userBusiness.Id }, userBusiness.ToDTO);
        }

        [HttpPut("deactivate/{id:guid}")]
        public async Task<IActionResult> DeactivateUser(Guid id)
        {
            if (id == Guid.Empty)
            {
                return BadRequest("Invalid User ID format.");
            }

            var userBusiness = await UserBusiness.FindAsync(_context, id);
            if (userBusiness == null)
            {
                return NotFound($"User with ID {id} not found.");
            }

            bool isDeactivated = await userBusiness.DeactivateUserAsync();

            if (!isDeactivated)
            {
                return StatusCode(500, "A problem occurred while deactivating the user.");
            }

            return Ok($"User with ID {id} has been successfully deactivated.");
        }


        [HttpGet("{email}")]
        public async Task<ActionResult<UserDTO>> GetUserByEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest("Email cannot be empty.");
            }

            string emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";

            if (!System.Text.RegularExpressions.Regex.IsMatch(email, emailPattern))
            {
                return BadRequest("Invalid email format. Expected format: example@domain.com");
            }

            var userBusiness = await UserBusiness.FindByEmailAsync(_context, email);

            if (userBusiness == null)
            {
                return NotFound($"User with email {email} not found.");
            }

            return Ok(userBusiness.ToDTO);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UserUpdateDTO userUpdateDto)
        {
            if (userUpdateDto == null) return BadRequest("Invalid user data.");
            if (id != userUpdateDto.Id) return BadRequest("User ID mismatch.");

            var userBusiness = await UserBusiness.FindAsync(_context, id);
            if (userBusiness == null) return NotFound($"User with ID {id} not found.");

            if (!string.IsNullOrWhiteSpace(userUpdateDto.Email) && userUpdateDto.Email != userBusiness.Email)
            {
                string emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
                if (!System.Text.RegularExpressions.Regex.IsMatch(userUpdateDto.Email, emailPattern))
                {
                    return BadRequest("Invalid email format. Must be complete (e.g., example@domain.com).");
                }

                bool isExist = await UserBusiness.IsUserExist(_context, userUpdateDto.Email);
                if (isExist) return Conflict("This email is already taken by another user.");

                userBusiness.Email = userUpdateDto.Email;
            }

            if (!string.IsNullOrWhiteSpace(userUpdateDto.PasswordHash) && userUpdateDto.PasswordHash != userBusiness.PasswordHash)
            {
                string passwordPattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)[A-Za-z\d]{8,}$";
                if (!System.Text.RegularExpressions.Regex.IsMatch(userUpdateDto.PasswordHash, passwordPattern))
                {
                    return BadRequest("Password is too weak. It must contain at least one uppercase letter, one lowercase letter, and one number.");
                }

                userBusiness.PasswordHash = userUpdateDto.PasswordHash; 
            }

            if (!string.IsNullOrWhiteSpace(userUpdateDto.FullName) && userUpdateDto.FullName != userBusiness.FullName)
            {
                userBusiness.FullName = userUpdateDto.FullName;
            }

            if (!string.IsNullOrWhiteSpace(userUpdateDto.Role) && userUpdateDto.Role != userBusiness.Role)
            {
                if (userUpdateDto.Role == "Admin" || userUpdateDto.Role == "Manager" || userUpdateDto.Role == "Employee" || userUpdateDto.Role == "Customer")
                {
                    userBusiness.Role = userUpdateDto.Role;
                }
                else
                {
                    return BadRequest("Invalid Role specified.");
                }
            }

            if (userUpdateDto.IsActive.HasValue && userUpdateDto.IsActive.Value != userBusiness.IsActive)
            {
                userBusiness.IsActive = userUpdateDto.IsActive.Value;
            }

            if (userUpdateDto.FailedLoginAttempts.HasValue && userUpdateDto.FailedLoginAttempts.Value != userBusiness.FailedLoginAttempts)
            {
                if (userUpdateDto.FailedLoginAttempts.Value < 0)
                {
                    return BadRequest("Failed login attempts cannot be a negative number.");
                }
                userBusiness.FailedLoginAttempts = userUpdateDto.FailedLoginAttempts.Value;
            }

            if (userUpdateDto.LockoutEnd != userBusiness.LockoutEnd)
            {
                userBusiness.LockoutEnd = userUpdateDto.LockoutEnd;
            }

            userBusiness.Mode = UserBusiness.enMode.Update;
            bool isSaved = await userBusiness.SaveAsync();

            if (!isSaved) return StatusCode(500, "A problem occurred while updating the user.");

            return NoContent();
        }



    }
}