using DATN.Application.DanhMucCapDo;
using DATN.Application.Interfaces;
using DATN.Application.Utils;
using DATN.Domain.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DATN.Api.Controllers
{
    [Route("api/nhom-nguoi-dung")]
    [ApiController]
    public class NhomNguoiDungController : ControllerBase
    {
        private readonly INhomNguoiDung _service;
        public NhomNguoiDungController(INhomNguoiDung service)
        {
            _service = service;
        }

        // GET: api/DanhMucCapDo/get-all
        [HttpGet("get-all")]
        [Authorize]
        public async Task<ActionResult<PaginatedList<nhom_nguoi_dung_dto>>> GetAll([FromQuery] nhom_nguoi_dung_dto request)
        {
            try
            {
                var result = await _service.GetPaginNhomNguoiDung(request);
                return Ok(result);
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }

        [HttpPost("phan-quyen")]
        [Authorize]
        public async Task<ActionResult<string>> PhanQuyen([FromBody] PhanQuyenDto request)
        {
            try
            {
                var result = await _service.PhanQuyen(request);
                return Ok(result);
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }
        
        [HttpPost("them-nhom-nguoi-dung")]
        [Authorize]
        public async Task<ActionResult<string>> AddNhomNguoiDung([FromBody] nhom_nguoi_dung_dto request)
        {
            try
            {
                var result = await _service.AddNhomnguoiDung(request);
                return Ok(result);
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }

        [HttpGet("get-by-id")]
        [Authorize]
        public async Task<ActionResult<nhom_nguoi_dung_dto>> GetNhomNguoiDungById([FromQuery] Guid id)
        {
            try
            {
                var result = await _service.GetNhomNguoiDungById(id);
                return Ok(result);
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }

        [HttpPut("")]
        [Authorize]
        public async Task<ActionResult<string>> SuaNhomNguoiDung(Guid id, nhom_nguoi_dung_dto request)
        {
            try
            {
                var result = await _service.EditNhomnguoiDung(id, request);
                return Ok(result);
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }

        [HttpDelete("")]
        [Authorize]
        public async Task<ActionResult<string>> XoaNhomNguoiDung([FromQuery] Guid id)
        {
            try
            {
                var result = await _service.DeleteNhomnguoiDung(id);
                return Ok(result);
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }

        [HttpDelete("xoa-nhieu")]
        [Authorize]
        public async Task<ActionResult<string>> XoaNhieuNhomNguoiDung(List<Guid> ids)
        {
            try
            {
                var result = await _service.DeleteManyNhomnguoiDung(ids);
                return Ok(result);
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }
    }
}
