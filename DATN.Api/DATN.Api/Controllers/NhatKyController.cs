using DATN.Application.DanhMucCapDo;
using DATN.Application.Interfaces;
using DATN.Application.Utils;
using DATN.Domain.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DATN.Api.Controllers
{
    [Route("api/nhat-ky-he-thong")]
    [ApiController]
    public class NhatKyController : ControllerBase
    {
        private readonly INhatKyHeThong _service;
        public NhatKyController(INhatKyHeThong service)
        {
            _service = service;
        }

        // GET: api/DanhMucCapDo/get-all
        [HttpGet("get-all")]
        //[Authorize]
        public async Task<ActionResult<PaginatedList<nhat_ky_he_thong_dto>>> GetAll([FromQuery] nhat_ky_he_thong_pagin_param request)
        {
            try
            {
                var result = await _service.GetAllLog(request);
                return Ok(result);
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }
    }
}
