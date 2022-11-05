using EasyAuth.Utils.PasswordUtil;
using System;
using Xunit;

namespace TestAuth
{
    public class UserClass1
    {
        public long Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public bool IsVerified { get; set; } = false;
        public string RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiration { get; set; }
    }

    public class UnitTest1
    {
        public UserClass1 PrepUser()
        {
            var user = new UserClass1() { Id = 1, Username = "test", Password = null, Email = "asd@test.ts", IsVerified = false, RefreshToken = null, RefreshTokenExpiration = null };
            return user;
        }
        
        [Fact]
        public void TestPasswordsDifferent()
        {
            PasswordHasher.SetSalt("teafssrs");
            var user = PrepUser();
            var protectedPassword = PasswordHasher.Hashify(user.Password, user.Username);
            Assert.NotEqual(user.Password, protectedPassword);
        }
    }
}
