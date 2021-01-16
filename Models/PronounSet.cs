using System;
using System.ComponentModel.DataAnnotations;

namespace HyperBot.Models
{
    public class PronounSet
    {
        [Required]
        [Key]
        public string Set { get; set; }
    }
}
