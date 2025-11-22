using HSB.BE.Data;
using HSB.BE.Models;
using Microsoft.EntityFrameworkCore;

namespace HSB.BE.Repository
{
	public interface IUserRepository
	{
		Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
		Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);
		Task<User> AddAsync(User user, CancellationToken ct = default);
		Task<User?> GetByIdAsync(int id, CancellationToken ct = default);
	}

	public class UserRepository : IUserRepository
	{
		private readonly AppDbContext _db;
		public UserRepository(AppDbContext db) => _db = db;

		public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
			_db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

		public Task<bool> EmailExistsAsync(string email, CancellationToken ct = default) =>
			_db.Users.AnyAsync(u => u.Email == email, ct);

		public async Task<User> AddAsync(User user, CancellationToken ct = default)
		{
			_db.Users.Add(user);
			await _db.SaveChangesAsync(ct);
			return user;
		}

		public Task<User?> GetByIdAsync(int id, CancellationToken ct = default)
		{
			try
			{
				var user = _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
				return user;
			}
			catch (Exception)
			{

				throw;
			}
		}
	}
}
