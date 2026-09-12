using System.Threading.Tasks;
using OnlineComputerStore.Core.Services;
using OnlineComputerStore.Tests.TestHelpers;
using Xunit;

namespace OnlineComputerStore.Tests
{
    public class UserServiceTests : DatabaseTestBase
    {
        private readonly UserService _sut;

        public UserServiceTests()
        {
            _sut = new UserService(Context);
        }

        [Fact]
        public async Task RegisterAsync_NeverStoresThePasswordInPlainText()
        {
            var user = await _sut.RegisterAsync("Jane Doe", "jane@example.com", "Sup3rSecret!");

            Assert.NotEqual("Sup3rSecret!", user.PasswordHash);
            Assert.False(string.IsNullOrWhiteSpace(user.PasswordSalt));
        }

        [Fact]
        public async Task RegisterAsync_SameSourcePassword_GetsADifferentHashPerUser()
        {
            // Confirms each registration gets its own random salt (PBKDF2 salted per-user),
            // not a shared/static salt that would make hashes comparable across accounts.
            var user1 = await _sut.RegisterAsync("Jane Doe", "jane@example.com", "SamePassword1!");
            var user2 = await _sut.RegisterAsync("John Doe", "john@example.com", "SamePassword1!");

            Assert.NotEqual(user1.PasswordSalt, user2.PasswordSalt);
            Assert.NotEqual(user1.PasswordHash, user2.PasswordHash);
        }

        [Fact]
        public async Task ValidateCredentialsAsync_CorrectPassword_ReturnsTheUser()
        {
            await _sut.RegisterAsync("Jane Doe", "jane@example.com", "Sup3rSecret!");

            var result = await _sut.ValidateCredentialsAsync("jane@example.com", "Sup3rSecret!");

            Assert.NotNull(result);
            Assert.Equal("jane@example.com", result!.Email);
        }

        [Fact]
        public async Task ValidateCredentialsAsync_WrongPassword_ReturnsNull()
        {
            await _sut.RegisterAsync("Jane Doe", "jane@example.com", "Sup3rSecret!");

            var result = await _sut.ValidateCredentialsAsync("jane@example.com", "SomethingElse!");

            Assert.Null(result);
        }

        [Fact]
        public async Task ValidateCredentialsAsync_UnknownEmail_ReturnsNull()
        {
            var result = await _sut.ValidateCredentialsAsync("nobody@example.com", "whatever");
            Assert.Null(result);
        }

        [Fact]
        public async Task ChangePasswordAsync_WrongCurrentPassword_FailsAndLeavesOldPasswordWorking()
        {
            var user = await _sut.RegisterAsync("Jane Doe", "jane@example.com", "Sup3rSecret!");

            var (success, _) = await _sut.ChangePasswordAsync(user.Id, "NotTheRightOne", "NewPass123!");
            var stillLogsInWithOldPassword = await _sut.ValidateCredentialsAsync("jane@example.com", "Sup3rSecret!");

            Assert.False(success);
            Assert.NotNull(stillLogsInWithOldPassword);
        }

        [Fact]
        public async Task ChangePasswordAsync_CorrectCurrentPassword_SwitchesWhichPasswordWorks()
        {
            var user = await _sut.RegisterAsync("Jane Doe", "jane@example.com", "Sup3rSecret!");

            var (success, _) = await _sut.ChangePasswordAsync(user.Id, "Sup3rSecret!", "NewPass123!");
            var oldPasswordLogin = await _sut.ValidateCredentialsAsync("jane@example.com", "Sup3rSecret!");
            var newPasswordLogin = await _sut.ValidateCredentialsAsync("jane@example.com", "NewPass123!");

            Assert.True(success);
            Assert.Null(oldPasswordLogin);
            Assert.NotNull(newPasswordLogin);
        }

        [Fact]
        public async Task UpdateProfileAsync_EmailAlreadyUsedByAnotherAccount_Fails()
        {
            await _sut.RegisterAsync("Jane Doe", "jane@example.com", "pw");
            var other = await _sut.RegisterAsync("John Doe", "john@example.com", "pw");

            var (success, message) = await _sut.UpdateProfileAsync(other.Id, "John Doe", "jane@example.com", "0400000000", "1 St");

            Assert.False(success);
            Assert.Contains("already used", message);
        }

        [Fact]
        public async Task DeleteAccountAsync_RemovesTheUser()
        {
            var user = await _sut.RegisterAsync("Jane Doe", "jane@example.com", "pw");

            var deleted = await _sut.DeleteAccountAsync(user.Id);
            var found = await _sut.FindByIdAsync(user.Id);

            Assert.True(deleted);
            Assert.Null(found);
        }

        [Fact]
        public async Task DeleteAccountAsync_UnknownId_ReturnsFalse()
        {
            var deleted = await _sut.DeleteAccountAsync(999);
            Assert.False(deleted);
        }
    }
}
