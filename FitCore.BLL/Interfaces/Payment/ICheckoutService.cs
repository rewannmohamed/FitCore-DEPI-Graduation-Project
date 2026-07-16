using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitCore.BLL.Interfaces.Payment
{
    public interface ICheckoutService
    {
        public Task<int?> ProcessCheckoutAsync(int userId);
    }
}
