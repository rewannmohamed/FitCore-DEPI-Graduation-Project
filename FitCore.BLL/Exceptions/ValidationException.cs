using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitCore.BLL.Exceptions
{
    public class ValidationException : Exception
    {
        public IEnumerable<string> Errors { get; }

        public ValidationException(string message) : base(message)
        {
            Errors = new List<string>();
        }

        public ValidationException(IEnumerable<string> errors) : base("There are errors at the entered data, please try again.")
        {
            Errors = errors;
        }
    }
}
