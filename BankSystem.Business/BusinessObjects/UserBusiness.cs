using BankAPIBusinessLayer.Security;
using BankDataAccessLayer;
using BankSystem.DataAccess.DTOs;
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

            using var dbTransaction = await _context.Database.BeginTransactionAsync();

            try
            {
                await _context.Users.AddAsync(entity);
                int rows = await _context.SaveChangesAsync();
                if (rows <= 0) throw new Exception("Insertion failed.");

                this.Id = entity.Id;
                this.CreatedAt = entity.CreatedAt;

                string newUserJson = System.Text.Json.JsonSerializer.Serialize(this.ToDTO);

                var auditLog = new AuditLogBusiness(_context, new AuditLogDTO(0, this.Id, "User Registration", null,
                    newUserJson, DateTime.UtcNow));

                await auditLog.SaveAsync();

                await dbTransaction.CommitAsync();
                return true;
            }
            catch (Exception)
            {
                await dbTransaction.RollbackAsync();
                return false;
            }
        }

        private async Task<bool> _UpdateUserAsync()
        {
            var entity = await _context.Users.FindAsync(this.Id);
            if (entity == null) return false;

            var oldDto = new UserDTO(entity.Id, entity.FullName, entity.Email, entity.PasswordHash, entity.Role,
                entity.FailedLoginAttempts, entity.LockoutEnd, entity.IsActive, entity.CreatedAt);

            string oldUserJson = System.Text.Json.JsonSerializer.Serialize(oldDto);

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

            using var dbTransaction = await _context.Database.BeginTransactionAsync();

            try
            {
                _context.Users.Update(entity);
                int rows = await _context.SaveChangesAsync();
                if (rows <= 0) throw new Exception("Update failed.");

                string newUserJson = System.Text.Json.JsonSerializer.Serialize(this.ToDTO);

                var auditLog = new AuditLogBusiness(_context, new AuditLogDTO(0, this.Id,
                    "User Profile Updated", oldUserJson, newUserJson, DateTime.UtcNow));

                await auditLog.SaveAsync();

                await dbTransaction.CommitAsync();
                return true;
            }
            catch (Exception)
            {
                await dbTransaction.RollbackAsync();
                return false;
            }
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

        public static async Task<List<UserDTO>> GetAllUsersAsync(BankSystemDbContext context)
        {
            return await context.Users
                .Select(u => new UserDTO(u.Id, u.FullName, u.Email, u.PasswordHash, u.Role, u.FailedLoginAttempts, u.LockoutEnd, u.IsActive, u.CreatedAt))
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<bool> DeactivateUserAsync()
        {
            var entity = await _context.Users.FindAsync(this.Id);
            if (entity == null) return false;

            var oldDto = new UserDTO(entity.Id, entity.FullName, entity.Email, entity.PasswordHash,
                entity.Role, entity.FailedLoginAttempts, entity.LockoutEnd, entity.IsActive, entity.CreatedAt);

            string oldUserJson = System.Text.Json.JsonSerializer.Serialize(oldDto);

            entity.IsActive = false;
            this.IsActive = false;

            using var dbTransaction = await _context.Database.BeginTransactionAsync();

            try
            {
                _context.Users.Update(entity);
                int rows = await _context.SaveChangesAsync();
                if (rows <= 0) throw new Exception("Deactivation failed.");

                string newUserJson = System.Text.Json.JsonSerializer.Serialize(this.ToDTO);

                var auditLog = new AuditLogBusiness(_context, new AuditLogDTO(
                    0,
                    this.Id,
                    "User Deactivated (Soft Delete)",
                    oldUserJson,
                    newUserJson,
                    DateTime.UtcNow
                ));
                await auditLog.SaveAsync();

                await dbTransaction.CommitAsync();
                return true;
            }
            catch (Exception)
            {
                await dbTransaction.RollbackAsync();
                return false;
            }
        }

        public static async Task<bool> IsUserExist(BankSystemDbContext context, string email)
        {
            return await context.Users.AsNoTracking().AnyAsync(u => u.Email == email);
        }
    }
}