using HSB.BE.Dtos;

namespace HSB.BE.Tests
{
	public class ChatRequestDtoTests
	{
		[Fact]
		public void DefaultValues_ShouldBeInitializedCorrectly()
		{
			var dto = new ChatRequestDto();

			Assert.Equal(string.Empty, dto.Role);
			Assert.Equal(string.Empty, dto.Content);
			Assert.Equal(string.Empty, dto.UserId);
			Assert.Equal(string.Empty, dto.ConversationId);
			Assert.NotNull(dto.Settings);
		}

		[Fact]
		public void Role_ExceedingMaxLength_ShouldFailValidation()
		{
			var dto = new ChatRequestDto
			{
				Role = new string('a', 101)
			};

			var results = ValidationHelper.ValidateObject(dto);

			Assert.Contains(results, r => r.MemberNames.Contains(nameof(ChatRequestDto.Role)));
		}

		[Fact]
		public void Content_ExceedingMaxLength_ShouldFailValidation()
		{
			var dto = new ChatRequestDto
			{
				Content = new string('a', 5001)
			};

			var results = ValidationHelper.ValidateObject(dto);

			Assert.Contains(results, r => r.MemberNames.Contains(nameof(ChatRequestDto.Content)));
		}

		[Fact]
		public void UserId_ExceedingMaxLength_ShouldFailValidation()
		{
			var dto = new ChatRequestDto
			{
				UserId = new string('a', 201)
			};

			var results = ValidationHelper.ValidateObject(dto);

			Assert.Contains(results, r => r.MemberNames.Contains(nameof(ChatRequestDto.UserId)));
		}

		[Fact]
		public void ConversationId_ExceedingMaxLength_ShouldFailValidation()
		{
			var dto = new ChatRequestDto
			{
				ConversationId = new string('a', 201)
			};

			var results = ValidationHelper.ValidateObject(dto);

			Assert.Contains(results, r => r.MemberNames.Contains(nameof(ChatRequestDto.ConversationId)));
		}
	}
}
