using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObjects.Common;
using System.ComponentModel.DataAnnotations;
using BusinessObjects.CustomDesigns;

namespace DTOs.CustomDesigns
{
    public class UpdateCustomDesignRequest
    {
        public string? DesignName { get; set; }
        public string? PromptText { get; set; }
        public int? GarmentType { get; set; }
        public int? BaseColor { get; set; }
        public int? Size { get; set; }
        public string? SpecialRequirements { get; set; }
        public int? Quantity { get; set; }
        public decimal? TotalPrice { get; set; }
        public int? Status { get; set; }
        public string? DesignImageUrl { get; set; }
    }

}
