using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace TestFusion.Core.Models
{
    public class StoredJsonModel
    {
        [Key]
        public string Id { get; set; } = string.Empty;

        public string Json { get; set; } = string.Empty;
    }
}
