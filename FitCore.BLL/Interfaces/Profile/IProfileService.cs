using FitCore.Shared.DTOs.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitCore.BLL.Interfaces.Profile
{
    public interface IProfileService
    {
        public Task<UserDto> GetProfile();
        public Task EditProfile(EditUserDto userDto);
    }
}
