using BillingSystem.Core.DTOs.Auth;
using BillingSystem.Core.Entities;
using BillingSystem.Core.Enums;
using BillingSystem.Core.Interfaces;
using BillingSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BillingSystem.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IJwtService _jwtService;

        public AuthService(ApplicationDbContext context, IJwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                throw new InvalidOperationException("Email already registered.");

            var user = new User
            {
                FullName = request.FullName,
                Email = request.Email.ToLower().Trim(),
                PhoneNumber = request.PhoneNumber,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = request.Role,
                Status = request.Role == UserRole.Admin ? UserStatus.Approved : UserStatus.Pending,
                ApprovedAt = request.Role == UserRole.Admin ? DateTime.UtcNow : null
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var token = _jwtService.GenerateToken(user);
            return new AuthResponse
            {
                Token = token,
                RefreshToken = Guid.NewGuid().ToString(),
                User = MapToDto(user)
            };
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email.ToLower().Trim());

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                throw new UnauthorizedAccessException("Invalid email or password.");

            if (user.Status == UserStatus.Pending)
                throw new UnauthorizedAccessException("Your account is pending admin approval.");

            if (user.Status == UserStatus.Rejected)
                throw new UnauthorizedAccessException("Your account has been rejected. Contact admin.");

            if (user.Status == UserStatus.Blocked)
                throw new UnauthorizedAccessException("Your account has been blocked. Contact admin.");

            var token = _jwtService.GenerateToken(user);
            return new AuthResponse
            {
                Token = token,
                RefreshToken = Guid.NewGuid().ToString(),
                User = MapToDto(user)
            };
        }

        public async Task<List<UserDto>> GetAllUsersAsync()
        {
            var users = await _context.Users
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();
            return users.Select(MapToDto).ToList();
        }

        public async Task<UserDto> GetUserByIdAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId)
                ?? throw new KeyNotFoundException("User not found.");
            return MapToDto(user);
        }

        public async Task UpdateUserStatusAsync(UpdateUserStatusRequest request, int adminId)
        {
            var user = await _context.Users.FindAsync(request.UserId)
                ?? throw new KeyNotFoundException("User not found.");

            user.Status = request.Status;
            if (request.Status == UserStatus.Approved)
            {
                user.ApprovedAt = DateTime.UtcNow;
                user.ApprovedByAdminId = adminId;
            }
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task ChangePasswordAsync(int userId, ChangePasswordRequest request)
        {
            var user = await _context.Users.FindAsync(userId)
                ?? throw new KeyNotFoundException("User not found.");

            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
                throw new UnauthorizedAccessException("Current password is incorrect.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        private static UserDto MapToDto(User u) => new()
        {
            Id = u.Id,
            FullName = u.FullName,
            Email = u.Email,
            PhoneNumber = u.PhoneNumber,
            Role = u.Role.ToString(),
            Status = u.Status.ToString(),
            CreatedAt = u.CreatedAt,
            ApprovedAt = u.ApprovedAt
        };
    }
}
