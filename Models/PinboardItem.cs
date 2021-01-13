using System;
using System.ComponentModel.DataAnnotations;

namespace HyperBot.Models
{
    public class PinboardItem
    {
        public int Id { get; set; }
        [Required]
        public ulong Author { get; set; }
        [Required]
        public string Text { get; set; }

        [DataType(DataType.DateTime)]
        [Required]
        public DateTime Timestamp { get; set; }
    }
}
