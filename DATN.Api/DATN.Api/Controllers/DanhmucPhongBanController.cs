using DATN.Application.Interfaces;
using DATN.Application.Utils;
using DATN.Domain.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DATN.Api.Controllers
{
    [Route("api/danh-muc-phong-ban")]
    [ApiController]
    public class DanhmucPhongBanController : ControllerBase
    {
        private readonly IDanhMucPhongBan _danhMucService;
        public DanhmucPhongBanController(IDanhMucPhongBan danhMucService)
        {
            _danhMucService = danhMucService;
        }

        // GET: api/DanhMucCapDo/get-all
        [HttpGet("")]
        [Authorize]
        public async Task<ActionResult<PaginatedList<danh_muc_dto>>> Get([FromQuery]string? keySearch)
        {
            try
            {
                var result = await _danhMucService.Get(keySearch);
                return Ok(result);
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }

        // GET: api/DanhMucCapDo/get-all
        [HttpGet("get-all")]
        [Authorize]
        public async Task<ActionResult<PaginatedList<danh_muc_dto>>> GetAll([FromQuery] danh_muc_dto request)
        {
            try
            {
                var result = await _danhMucService.GetAll(request);
                return Ok(result);
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }

        // GET: api/DanhMucCapDo/{id}
        [HttpGet("get-by-id/{id}")]
        [Authorize]
        public async Task<ActionResult<danh_muc_dto>> GetById(Guid id)
        {
            try
            {
                var result = await _danhMucService.GetById(id);
                if (result == null) return NotFound("Không tìm thấy bản ghi!");
                return Ok(result);
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }

        // POST: api/DanhMucCapDo
        [HttpPost("create")]
        [Authorize]
        public async Task<ActionResult<danh_muc_dto>> Create([FromBody] danh_muc_dto request)
        {
            try
            {
                var result = await _danhMucService.Create(request);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }

        // PUT: api/DanhMucCapDo/{id}
        [HttpPut("update/{id}")]
        [Authorize]
        public async Task<ActionResult<danh_muc_dto>> Update(Guid id, [FromBody] danh_muc_dto request)
        {
            if (id != request.Id) return BadRequest("Id không khớp!");

            try
            {
                var result = await _danhMucService.Update(request);
                if (result == null) return NotFound("Không tìm thấy bản ghi để cập nhật!");
                return Ok(result);
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }

        // DELETE: api/DanhMucCapDo/{id}
        [HttpDelete("delete/{id}")]
        [Authorize]
        public async Task<ActionResult> Delete(Guid id)
        {
            try
            {
                var success = await _danhMucService.Delete(id);
                if (!success) return NotFound("Không tìm thấy bản ghi để xóa!");
                return NoContent();
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }

        // DELETE: api/DanhMucCapDo/{id}
        [HttpDelete("delete-any")]
        [Authorize]
        public async Task<ActionResult> DeleteAny(DeleteRequestPhongBan data)
        {
            try
            {
                var success = await _danhMucService.DeleteAny(data.Ids);
                if (!success) return NotFound("Không tìm thấy bản ghi để xóa!");
                return NoContent();
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }

        public class DeleteRequestPhongBan
        {
            public List<Guid> Ids { get; set; }
        }
    }
}
