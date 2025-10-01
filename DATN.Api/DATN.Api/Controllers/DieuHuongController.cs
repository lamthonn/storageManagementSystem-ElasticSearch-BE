using DATN.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DATN.Api.Controllers
{
    [Route("api/dieu-huong")]
    [ApiController]
    public class DieuHuongController : ControllerBase
    {
        private readonly IDieuHuong _dieuHuongService;
        private readonly INhomNguoiDung _nhomNguoiDungService;

        public DieuHuongController(IDieuHuong dieuHuongService, INhomNguoiDung nhomNguoiDungService)
        {
            _dieuHuongService = dieuHuongService;
            _nhomNguoiDungService = nhomNguoiDungService;
        }

        [HttpGet("GetMenu")]
        public async Task<IActionResult> GetMenu()
        {
            try
            {
                var result = await _dieuHuongService.GetMenu();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("get-phan-quyen")]
        public async Task<IActionResult> GetPhanQuyen([FromQuery]Guid nhom_nguoi_dung_id)
        {
            try
            {
                var result = await _nhomNguoiDungService.GetPhanQuyen(nhom_nguoi_dung_id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
