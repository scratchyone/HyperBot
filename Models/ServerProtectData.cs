using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
namespace HyperBot.Models
{
    public class ServerProtectUnsafeFile
    {
        [Key]
        public string Hash { get; set; }
        public string Description { get; set; }

    }
    public class IPGrabberUrl
    {
        [Key]
        public string Domain { get; set; }
    }
}
