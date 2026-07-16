using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitCore.DAL.Interfaces
{
    public interface ISoftDelete
    {
        public bool IsDeleted { get; set; } 
        public DateTime? DeletedAt { get; set; } 
    }
}
