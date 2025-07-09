using EasyAuth;
using EasyAuth.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace App.Models
{
    public class User: IAuthUser
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Username { get; set; }
        [Required]
        public string Password { get; set; }
        [Required]
        public string Email { get; set; }
        public bool IsVerified { get; set; }
        public string RefreshToken { get; set; }
        public DateTime RefreshTokenExpirationUTC { get; set; }
        //public string AssignedRoles { get; set; }
    }
}
