using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading.Tasks;

namespace API.Furnistore.Shared.DTOs
{
    public class UserRegistrationRequestDto
    {
        [Required]
        public required string Name { get; set; }

        [Required]
        public required string EmailAddress { get; set; }

        [Required]
        public required string Password { get; set; }
    }
}