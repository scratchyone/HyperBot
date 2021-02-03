using System;
using System.ComponentModel.DataAnnotations;

namespace HyperBot.Models
{
    public class ServerProtectGuild
    {
        [Key]
        public ulong Guild { get; set; }
    }
}
