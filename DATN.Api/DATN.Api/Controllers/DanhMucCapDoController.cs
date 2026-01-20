using DATN.Api.Utils.Cache_service;
using DATN.Application.Interfaces;
using DATN.Application.Utils;
using DATN.Domain.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DATN.Api.Controllers
{
    [Route("api/danh-muc-cap-do")]
    [ApiController]
    public class DanhMucCapDoController : ControllerBase
    {
        private readonly IDanhMucCapDo _danhMucCapDoService;
        private readonly IRedisCacheService _redisService;

        public DanhMucCapDoController(IDanhMucCapDo danhMucCapDoService, IRedisCacheService redisService)
        {
            _danhMucCapDoService = danhMucCapDoService;
            _redisService = redisService;
        }

        // GET: api/DanhMucCapDo/get-all
        [HttpGet("get-all")]
        [Authorize]
        public async Task<ActionResult<PaginatedList<danh_muc_dto>>> GetAll([FromQuery] danh_muc_dto request)
        {
            try
            {
                var cachedData = await _redisService.GetAsync<PaginatedList<danh_muc_dto>>("danh_muc_cap_do." + (request != null ? request : ""), (request != null ? request : ""));
                if (cachedData != null)
                {
                    return cachedData;
                }

                var result = await _danhMucCapDoService.GetAll(request);

                await _redisService.SetAsync("danh_muc_cap_do." + request.ToString(), (request != null ? request : ""), result);

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
                var result = await _danhMucCapDoService.GetById(id);
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
                var result = await _danhMucCapDoService.Create(request);
                await _redisService.ClearByModuleAsync("danh_muc_cap_do.");

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
                var result = await _danhMucCapDoService.Update(request);
                if (result == null) return NotFound("Không tìm thấy bản ghi để cập nhật!");

                await _redisService.ClearByModuleAsync("danh_muc_cap_do.");
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
                var success = await _danhMucCapDoService.Delete(id);
                if (!success) return NotFound("Không tìm thấy bản ghi để xóa!");

                await _redisService.ClearByModuleAsync("danh_muc_cap_do.");
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
        public async Task<ActionResult> DeleteAny(DeleteRequestCapDo data)
        {
            try
            {
                var success = await _danhMucCapDoService.DeleteAny(data.Ids);
                if (!success) return NotFound("Không tìm thấy bản ghi để xóa!");

                await _redisService.ClearByModuleAsync("danh_muc_cap_do.");
                return NoContent();
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }

        public class DeleteRequestCapDo
        {
            public List<Guid> Ids { get; set; }
        }
    }
}
