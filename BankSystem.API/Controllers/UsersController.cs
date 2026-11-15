using BankDataAccessLayer;
using BankDataAccessLayer.DTOs;
using BankSystem.Business.BusinessObjects;
using BankSystem.DataAccess.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BankSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly BankSystemDbContext _context;

        public UsersController(BankSystemDbContext context)
        {
            _context = context;
        }

        [HttpGet("All")]
        [Authorize(Roles = "Manager,Employee")]
        public async Task<ActionResult<IEnumerable<UserDTO>>> GetAllUsers()
        {
            var users = await UserBusiness.GetAllUsersAsync(_context);
            return Ok(users);
        }

        [HttpGet("{id:guid}")]
        [Authorize(Roles = "Manager,Employee")]
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
        [AllowAnonymous]
        public async Task<ActionResult<UserDTO>> RegisterUser([FromBody] UserDTO userDto)
        {
            if (userDto == null) return BadRequest("Invalid user data.");

            bool isExist = await UserBusiness.IsUserExist(_context, userDto.Email);
            if (isExist) return Conflict("Email is already registered.");

            userDto.Role = "Customer";

            var userBusiness = new UserBusiness(_context, userDto, UserBusiness.enMode.AddNew);
            bool isSaved = await userBusiness.SaveAsync();

            if (!isSaved) return StatusCode(500, "A problem occurred while handling your request.");

            return CreatedAtAction(nameof(GetUserById), new { id = userBusiness.Id }, userBusiness.ToDTO);
        }

        [HttpPut("deactivate/{id:guid}")]
        [Authorize(Roles = "Manager")]
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
        [Authorize(Roles = "Manager,Employee")]
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

        [HttpPut("profile/{id:guid}")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile(Guid id, [FromBody] UserProfileUpdateDTO profileDto)
        {
            if (profileDto == null) return BadRequest("Invalid data.");
            if (id != profileDto.Id) return BadRequest("User ID mismatch.");

            var userBusiness = await UserBusiness.FindAsync(_context, id);
            if (userBusiness == null) return NotFound($"User with ID {id} not found.");

            if (!string.IsNullOrWhiteSpace(profileDto.FullName))
                userBusiness.FullName = profileDto.FullName;

            if (!string.IsNullOrWhiteSpace(profileDto.Email) && profileDto.Email != userBusiness.Email)
            {
                string emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
                if (!System.Text.RegularExpressions.Regex.IsMatch(profileDto.Email, emailPattern))
                    return BadRequest("Invalid email format.");

                if (await UserBusiness.IsUserExist(_context, profileDto.Email))
                    return Conflict("This email is already taken.");

                userBusiness.Email = profileDto.Email;
            }

            if (!string.IsNullOrWhiteSpace(profileDto.Password))
            {
                string passwordPattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)[A-Za-z\d]{8,}$";
                if (!System.Text.RegularExpressions.Regex.IsMatch(profileDto.Password, passwordPattern))
                    return BadRequest("Password is too weak.");

                userBusiness.PasswordHash = profileDto.Password;
            }

            userBusiness.Mode = UserBusiness.enMode.Update;
            if (!await userBusiness.SaveAsync())
                return StatusCode(500, "A problem occurred while updating the profile.");

            return NoContent();
        }

        [HttpPut("{id}/role")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> UpdateUserRole(Guid id, [FromBody] string newRole)
        {
            if (newRole != "Manager" && newRole != "Employee" && newRole != "Customer")
                return BadRequest("Invalid role. Allowed roles: Manager, Employee, Customer.");

            var user = await UserBusiness.FindAsync(_context, id);

            if (user == null)
                return NotFound("User not found.");

            if (user.Role == newRole)
                return BadRequest($"User already has the {newRole} role.");

            user.Role = newRole;
            user.Mode = UserBusiness.enMode.Update;
            bool isUpdated = await user.SaveAsync();

            if (!isUpdated)
                return StatusCode(500, "Failed to update user role.");

            return Ok(new { message = $"User role updated to {newRole} successfully." });
        }

        [HttpPut("{id:guid}/unlock")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> UnlockUser(Guid id)
        {
            var userBusiness = await UserBusiness.FindAsync(_context, id);
            if (userBusiness == null) return NotFound($"User with ID {id} not found.");

            userBusiness.FailedLoginAttempts = 0;
            userBusiness.LockoutEnd = null;
            userBusiness.Mode = UserBusiness.enMode.Update;

            if (!await userBusiness.SaveAsync())
                return StatusCode(500, "Failed to unlock user account.");

            return Ok(new { message = "User account unlocked successfully." });
        }

        [HttpPut("{id:guid}/activate")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> ActivateUser(Guid id)
        {
            var userBusiness = await UserBusiness.FindAsync(_context, id);
            if (userBusiness == null) return NotFound($"User with ID {id} not found.");

            if (userBusiness.IsActive)
                return BadRequest("User account is already active.");

            userBusiness.IsActive = true;
            userBusiness.Mode = UserBusiness.enMode.Update;

            if (!await userBusiness.SaveAsync())
                return StatusCode(500, "Failed to activate user account.");

            return Ok(new { message = "User account activated successfully." });
        }

    }
}