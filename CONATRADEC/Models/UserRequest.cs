using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CONATRADEC.Models;

namespace CONATRADEC.Models
{
    public class UserRequest
    {
        private int? id;
        private string? firstName;
        private string? lastName;
        private int? age;
        private string? email;
        private string? image;

        public int? Id { get => id; set => id = value; }
        public string? FirstName { get => firstName; set => firstName = value; }
        public string? LastName { get => lastName; set => lastName = value; }
        public int? Age { get => age; set => age = value; }
        public string? Email { get => email; set => email = value; }
        public string? Image { get => image; set => image = value; }

        public UserRequest(UserRP user)
        {
            Id = user.Id;
            FirstName = user.FirstName;
            LastName = user.LastName;
            Age = user.Age;
            Email = user.Email;
            Image = user.Email;
        }
    }
}
