using Microsoft.EntityFrameworkCore;
using ZoraVault.Data;
using ZoraVault.Models.DTOs;
using ZoraVault.Models.Entities;
using ZoraVault.Helpers;

namespace ZoraVault.Services
{
    public class AuthService
    {
        private readonly ApplicationDbContext _db;
        private readonly string _serverSecret;

        public AuthService(ApplicationDbContext db, string serverSecret)
        {
            _db = db;
            _serverSecret = serverSecret;
        }

        public async Task<Response> RegisterUserAsync(UserRegistrationReq req)
        {
            if (await _db.Users.AnyAsync(u => u.Username == req.Username))
                return new Response("A user with the same username already exists", 409);

            if (await _db.Users.AnyAsync(u => u.Email == req.Email))
                return new Response("A user with the same email already exists", 409);

            // The request's PasswordHash is the client-side hashed password (using argon2id)
            // Now, add another layer of security by hashing it again with PBKDF2 + server-side secret (pepper)
            string serverSalt = SecurityHelpers.GenerateSalt();
            string serverPasswdHash = SecurityHelpers.HashPassword(
                _serverSecret, 
                req.PasswordHash,
                serverSalt, 
                req.KdfParams.Iterations,
                req.KdfParams.KeyLength
            );

            User user = new()
            {
                Id = Guid.NewGuid(),
                Username = req.Username,
                Email = req.Email,
                ServerSalt = serverSalt,
                ServerPasswordHash = serverPasswdHash,
                KdfParams = req.KdfParams,
                CreatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return new Response("User created successfully", 201, new PublicUser(user));
        }

        public async Task<Response> LoginUserAsync(UserLoginReq req)
        {
            User? user = await _db.Users.FirstOrDefaultAsync(u => 
                u.Username == req.UsernameOrEmail || 
                u.Email == req.UsernameOrEmail
            );

            if (user == null)
                return new Response("User doesn't exist", 404);

            // Server-side hash
            int serverIterations = user.KdfParams.Iterations; 
            int keyLength = user.KdfParams.KeyLength;

            // Generate the server-side hash from the "unverified" client-provided hash
            string serverComputedHash = SecurityHelpers.HashPassword(
                _serverSecret, 
                req.PasswordHash, 
                user.ServerSalt, 
                serverIterations, 
                keyLength
            );

            // Compare the computed hash with the stored hash
            if (serverComputedHash != user.ServerPasswordHash)
                return new Response("The provided credentials are incorrect", 401);

            user.LastLogin = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return new Response();
        }
    }
}
