using DATN.Application.Authentication;
using DATN.Application.Interfaces;
using DATN.Domain.DTO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DATN.Api.Controllers
{
    [Route("api/authen")]
    [ApiController]
    public class AuthenController : ControllerBase
    {
        private readonly IAuthen _authenService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthenController(IAuthen authenService, IHttpContextAccessor httpContextAccessor)
        {
            _authenService = authenService;
            _httpContextAccessor = httpContextAccessor;
        }

        [HttpPost]
        [Route("dang-ky")]
        public Task<string> Register(nguoi_dung_dto request)
        {
            try
            {
                var addedUser = _authenService.Register(request);
                return addedUser;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        [HttpPost]
        [Route("dang-nhap")]
        public Task<nguoi_dung_dto> Login(loginParam request)
        {
            try
            {
                var addedUser = _authenService.login(request);
                return addedUser;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        [HttpPost("refresh-token")]
        public Task<nguoi_dung_dto> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                var addedUser = _authenService.RefreshToken(request);
                return addedUser;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }
    }
}
