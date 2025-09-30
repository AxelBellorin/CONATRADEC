namespace CONATRADEC.Models
{
    public class LoginResponse
    {
        private int? id;
        private string? username;
        private string? email;
        private string? firstName;
        private string? lastName;
        private string? image;
        private string? accessToken;
        private string? refreshToken;

        public int? Id { get => id; set => id = value; }
        public string? Username { get => username; set => username = value; }
        public string? Email { get => email; set => email = value; }
        public string? FirstName { get => firstName; set => firstName = value; }
        public string? LastName { get => lastName; set => lastName = value; }
        public string? Image { get => image; set => image = value; }
        public string? AccessToken { get => accessToken; set => accessToken = value; }
        public string? RefreshToken { get => refreshToken; set => refreshToken = value; }
    }
}
