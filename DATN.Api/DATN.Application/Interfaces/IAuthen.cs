using DATN.Application.Authentication;
using DATN.Domain.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DATN.Application.Interfaces
{
    public interface IAuthen
    {
        public Task<nguoi_dung_dto> login(loginParam dto);
        public Task<string> Register(nguoi_dung_dto request);
        public Task<nguoi_dung_dto> RefreshToken(RefreshTokenRequest request);
    }
}
