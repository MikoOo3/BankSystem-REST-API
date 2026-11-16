using BankAPIBusinessLayer.Security;
using BankDataAccessLayer;
using BankSystem.DataAccess.DTOs;
using BankSystem.DataAccess.DTOs.Auth;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BankSystem.Business.BusinessObjects
{
    public class UserBusiness
    {
        public enum enMode { AddNew = 0, Update = 1 };
        public enMode Mode = enMode.AddNew;

        private readonly BankSystemDbContext _context;

        public UserDTO ToDTO
        {
            get { return new UserDTO(this.Id, this.FullName, this.Email, this.PasswordHash, this.Role, this.FailedLoginAttempts, this.LockoutEnd, this.IsActive, this.CreatedAt); }
        }

        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int FailedLoginAttempts { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }


        public UserBusiness(BankSystemDbContext context, UserDTO uDTO, enMode cMode = enMode.AddNew)
        {
            _context = context;
            this.Id = uDTO.Id;
            this.FullName = uDTO.FullName;
            this.Email = uDTO.Email;
            this.PasswordHash = uDTO.PasswordHash;
            this.Role = uDTO.Role;
            this.FailedLoginAttempts = uDTO.FailedLoginAttempts;
            this.LockoutEnd = uDTO.LockoutEnd;
            this.IsActive = uDTO.IsActive;
            this.CreatedAt = uDTO.CreatedAt;
            Mode = cMode;
        }

        private async Task<bool> _AddNewUserAsync()
        {
            this.PasswordHash = PasswordHasher.HashPassword(this.PasswordHash);

            var entity = new User
            {
                Id = Guid.NewGuid(),
                FullName = this.FullName,
                Email = this.Email,
                PasswordHash = this.PasswordHash,
                Role = this.Role,
                FailedLoginAttempts = 0,
                LockoutEnd = null,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Users.AddAsync(entity);
            int rows = await _context.SaveChangesAsync();

            if (rows > 0)
            {
                this.Id = entity.Id;
                this.CreatedAt = entity.CreatedAt;
                return true;
            }

            return false;
        }

        private async Task<bool> _UpdateUserAsync()
        {
            var entity = await _context.Users.FindAsync(this.Id);
            if (entity == null) return false;

            if (this.PasswordHash != entity.PasswordHash && !string.IsNullOrWhiteSpace(this.PasswordHash))
            {
                this.PasswordHash = PasswordHasher.HashPassword(this.PasswordHash);
            }
            else
            {
                this.PasswordHash = entity.PasswordHash;
            }

            entity.FullName = this.FullName;
            entity.Email = this.Email;
            entity.PasswordHash = this.PasswordHash;
            entity.Role = this.Role;
            entity.FailedLoginAttempts = this.FailedLoginAttempts;
            entity.LockoutEnd = this.LockoutEnd;
            entity.IsActive = this.IsActive;

            _context.Users.Update(entity);
            int rows = await _context.SaveChangesAsync();

            return rows > 0;
        }

        public async Task<bool> SaveAsync()
        {
            switch (Mode)
            {
                case enMode.AddNew:
                    if (await _AddNewUserAsync())
                    {
                        Mode = enMode.Update;
                        return true;
                    }
                    return false;

                case enMode.Update:
                    return await _UpdateUserAsync();
            }
            return false;
        }

        public static async Task<UserBusiness?> FindAsync(BankSystemDbContext context, Guid id)
        {
            var entity = await context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
            if (entity == null) return null;

            var dto = new UserDTO(entity.Id, entity.FullName, entity.Email, entity.PasswordHash, entity.Role, entity.FailedLoginAttempts, entity.LockoutEnd, entity.IsActive, entity.CreatedAt);
            return new UserBusiness(context, dto, enMode.Update);
        }

        public static async Task<UserBusiness?> FindByEmailAsync(BankSystemDbContext context, string email)
        {
            var entity = await context.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == email);

            if (entity == null)
                return null;

            var dto = new UserDTO(
                entity.Id,
                entity.FullName,
                entity.Email,
                entity.PasswordHash,
                entity.Role,
                entity.FailedLoginAttempts,
                entity.LockoutEnd,
                entity.IsActive,
                entity.CreatedAt
            );

            return new UserBusiness(context, dto, enMode.Update);
        }

        public static async Task<PagedResult<UserDTO>> GetAllUsersAsync(BankSystemDbContext context,int pageNumber = 1,int pageSize = 10)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 50) pageSize = 50;

            var query = context.Users
                .AsNoTracking()
                .OrderByDescending(u => u.CreatedAt);

            int totalCount = await query.CountAsync();

            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UserDTO(
                    u.Id,
                    u.FullName,
                    u.Email,
                    u.PasswordHash,
                    u.Role,
                    u.FailedLoginAttempts,
                    u.LockoutEnd,
                    u.IsActive,
                    u.CreatedAt))
                .ToListAsync();

            return new PagedResult<UserDTO>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public async Task<bool> DeactivateUserAsync()
        {
            var entity = await _context.Users.FindAsync(this.Id);
            if (entity == null) return false;

            entity.IsActive = false;
            this.IsActive = false;

            _context.Users.Update(entity);
            int rows = await _context.SaveChangesAsync();

            return rows > 0;
        }

        public static async Task<bool> IsUserExist(BankSystemDbContext context, string email)
        {
            return await context.Users.AsNoTracking().AnyAsync(u => u.Email == email);
        }

        public static async Task<(UserBusiness? User, string ErrorMessage)> AuthenticateAsync(BankSystemDbContext context, LoginDTO loginDto)
        {
            var entity = await context.Users.FirstOrDefaultAsync(u => u.Email == loginDto.Email);

            if (entity == null)
                return (null, "Invalid email or password.");

            if (!entity.IsActive)
                return (null, "This account is deactivated. Please contact bank administration.");

            if (entity.LockoutEnd.HasValue && entity.LockoutEnd.Value > DateTimeOffset.UtcNow)
            {
                var remainingMinutes = Math.Ceiling((entity.LockoutEnd.Value - DateTimeOffset.UtcNow).TotalMinutes);
                return (null, $"Account is temporarily locked due to multiple failed attempts. Try again in {remainingMinutes} minutes.");
            }

            bool isPasswordValid = PasswordHasher.VerifyPassword(loginDto.Password, entity.PasswordHash);

            if (!isPasswordValid)
            {
                entity.FailedLoginAttempts++;

                if (entity.FailedLoginAttempts >= 5)
                {
                    entity.LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(15);
                }

                await context.SaveChangesAsync();
                return (null, "Invalid email or password.");
            }

            if (entity.FailedLoginAttempts > 0 || entity.LockoutEnd != null)
            {
                entity.FailedLoginAttempts = 0;
                entity.LockoutEnd = null;
                await context.SaveChangesAsync();
            }

            var dto = new UserDTO(entity.Id, entity.FullName, entity.Email, entity.PasswordHash, entity.Role,
                entity.FailedLoginAttempts, entity.LockoutEnd, entity.IsActive, entity.CreatedAt);

            return (new UserBusiness(context, dto, enMode.Update), string.Empty);
        }

    }
}