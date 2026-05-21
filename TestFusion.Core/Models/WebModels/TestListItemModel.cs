using System.ComponentModel.DataAnnotations;

namespace TestFusion.Core.Models.WebModels
{
    public class TestListItemModel
    {
        [Key]
        public string Id { get; set; } = default!;

        public string PartNumber { get; set; } = default!;

        public string PartBrand { get; set; } = default!;

        public string PartType { get; set; } = default!;

        public DateTimeOffset  DateTime { get; set; }
    }
}