using DATN.Application.Interfaces;
using DATN.Application.Utils;
using DATN.Domain.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DATN.Api.Controllers
{
    [Route("api/thu-muc")]
    [ApiController]
    public class ThuMucController : ControllerBase
    {
        private readonly IThuMucService _service;
        public ThuMucController(IThuMucService service)
        {
            _service = service;
        }

        [HttpPost("")]
        [Authorize]
        public async Task<ActionResult<thu_muc_dto>> AddThuMuc(thu_muc_dto request)
        {
            try
            {
                var result = await _service.AddThuMuc(request);
                return Ok(result);
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }

        [HttpGet("")]
        [Authorize]
        public async Task<ActionResult<List<thu_muc_dto>>> GetAll([FromQuery]Guid nguoi_dung_id)
        {
            try
            {
                var result = await _service.GetAll(nguoi_dung_id);
                return Ok(result);
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }

        [HttpPut("")]
        [Authorize]
        public async Task<ActionResult<thu_muc_dto>> UpdateThuMuc(Guid id, thu_muc_dto request)
        {
            try
            {
                var result = await _service.UpdateThuMuc(id, request);
                return Ok(result);
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }

        [HttpDelete("")]
        [Authorize]
        public async Task<ActionResult<string>> DeleteThuMuc([FromQuery] Guid id)
        {
            try
            {
                var result = await _service.DeleteThuMuc(id);
                return Ok(result);
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }
    }
}
