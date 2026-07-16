using FitCore.DAL.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitCore.DAL.Data.Models
{
    public class Supplier : IAuditable,  ISoftDelete
    {
        public int SupplierID { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string SupplierPhone { get; set; } = string.Empty;
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}