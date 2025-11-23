using HSB.BE.Models;
using HSB.BE.Repository;
using System.Threading.Tasks;

namespace HSB.BE.Services
{
	public interface IChatTokenService
	{
		public Task<bool> TryConsumeTokens(int userId, int tokensNeeded);
		public int EstimateTokenCount(string text);
	}
	public class ChatTokenService(UserRepository userRepository) : IChatTokenService
	{
		private readonly UserRepository _userRepository = userRepository;

		public int EstimateTokenCount(string text)
		{
			// Rough estimate: ~4 characters per token
			return (int)Math.Ceiling(text.Length / 4.0);
		}

		public async Task<bool> TryConsumeTokens(int userId, int tokensNeeded)
		{
			User? user = await _userRepository.GetByIdAsync(userId);
			if (user == null) return false;

			bool wasReset = ResetTokensIfNeeded(user);
			if (wasReset) await _userRepository.UpdateAsync(user);

			if (user.ChatTokens >= tokensNeeded)
			{
				user.ChatTokens -= tokensNeeded;
				await _userRepository.UpdateAsync(user);
				return true;
			}
			return false;
		}
		private bool ResetTokensIfNeeded(User user)
		{
			if (DateTime.UtcNow.Date > user.LastTokenResetUtc.Date)
			{
				user.ChatTokens = 12000;
				user.LastTokenResetUtc = DateTime.UtcNow;
				return true;
			}
			return false;
		}
	}
}
