using FitCore.DAL.Interfaces;
using FitCore.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitCore.DAL.Data.Models
{
    public class CartItem : IAuditable , ISoftDelete
    {
        public int CartItemID { get; set; }

        public int CartID { get; set; }
        public Cart Cart { get; set; } = null!;

        public int ProductID { get; set; }
        public Product Product { get; set; } = null!;

        public int Quantity { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

    }
}
