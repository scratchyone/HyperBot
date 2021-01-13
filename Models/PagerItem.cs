using System;
using System.ComponentModel.DataAnnotations;

namespace HyperBot.Models
{
    public class PagerItem
    {
        public int Id { get; set; }
        [Required]
        public ulong Author { get; set; }
        [Required]
        public string Text { get; set; }
        public ulong? Guild { get; set; }
    }
}
