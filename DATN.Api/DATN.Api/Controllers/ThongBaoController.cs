using DATN.Application.Interfaces;
using DATN.Application.NguoiDung;
using DATN.Domain.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DATN.Api.Controllers
{
    [Route("api/thong-bao")]
    [ApiController]
    public class ThongBaoController : ControllerBase
    {
        private readonly IThongBaoService _service;
        public ThongBaoController(IThongBaoService service)
        {
            _service = service;
        }

        [HttpGet("get-all")]
        [Authorize]
        public async Task<IActionResult> GetAll([FromQuery] Guid userId)
        {
            try
            {
                var result = await _service.GetThongBaosByUser(userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("gui-thong-bao")]
        [Authorize]
        public async Task<IActionResult> GuiThongBao([FromBody]thong_bao_request request)
        {
            try
            {
                var result = await _service.GuiThongBao(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("chap-nhan-thong-bao")]
        [Authorize]
        public async Task<IActionResult> ChapNhanThongBao([FromBody] respone_thong_bao_dto request)
        {
            try
            {
                var result = await _service.HandleOkShareFile(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("tu-choi-thong-bao")]
        [Authorize]
        public async Task<IActionResult> TuChoiThongBao([FromBody] respone_thong_bao_dto res)
        {
            try
            {
                var result = await _service.HandleCancelShareFile(res);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
