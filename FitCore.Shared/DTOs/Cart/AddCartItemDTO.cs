using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitCore.Shared.DTOs.Cart
{
    public class AddCartItemDTO
    {
        public int ProductID { get; set; }
        public int Quantity { get; set; }
    }
}
