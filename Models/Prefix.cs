using System;
using System.ComponentModel.DataAnnotations;

namespace HyperBot.Models
{
    public class Prefix
    {
        [Required]
        public string PrefixText { get; set; }

        [Required]
        public ulong Guild { get; set; }
    }
}
