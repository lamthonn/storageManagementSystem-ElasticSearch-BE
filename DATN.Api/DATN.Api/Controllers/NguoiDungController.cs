using backend_v3.Dto.Common;
using DATN.Application.Interfaces;
using DATN.Domain.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DATN.Api.Controllers
{
    [Route("api/nguoi-dung")]
    [ApiController]
    public class NguoiDungController : ControllerBase
    {
        private readonly INguoiDung _nguoiDungService;
        public NguoiDungController(INguoiDung nguoiDungService)
        {
            _nguoiDungService = nguoiDungService;
        }

        [HttpGet("get-all")]
        [Authorize]
        public async Task<IActionResult> GetAllNguoiDung([FromQuery] string? keySearch)
        {
            try
            {
                var result = await _nguoiDungService.GetAllNguoiDung(keySearch);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        
        [HttpGet("get-pagination")]
        [Authorize]
        public async Task<IActionResult> GetPaginNguoiDung([FromQuery] nguoiDungPaginParams request)
        {
            try
            {
                var result = await _nguoiDungService.GetPaginNguoiDung(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("get-by-id")]
        [Authorize]
        public async Task<IActionResult> GetNguoiDungById([FromQuery] Guid id)
        {
            try
            {
                var result = await _nguoiDungService.GetNguoiDungyId(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        
        [HttpPost("get-by-ids")]
        [Authorize]
        public async Task<IActionResult> GetNguoiDungByIds([FromBody]List<Guid> ids)
        {
            try
            {
                var result = await _nguoiDungService.GetNguoiDungyIds(ids);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        #region API thêm mới người dùng
        /// <summary>
        /// Thêm mới người dùng
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("")]
        [Authorize]
        public async Task<IActionResult> AddNguoiDung([FromBody]nguoi_dung_dto request)
        {
            try
            {
                var result = await _nguoiDungService.AddNguoiDung(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        #endregion

        #region API sửa thông tin người dùng
        /// <summary>
        /// Thêm mới người dùng
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPut("")]
        [Authorize]
        public async Task<IActionResult> UpdateNguoiDung(Guid id, nguoi_dung_dto request)
        {
            try
            {
                var result = await _nguoiDungService.UpdateNguoiDung(id, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        #endregion

        #region API xóa người dùng
        /// <summary>
        /// Thêm mới người dùng
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpDelete("")]
        [Authorize]
        public async Task<IActionResult> DeleteNguoiDung(Guid id)
        {
            try
            {
                var result = await _nguoiDungService.DeleteNguoiDung(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        #endregion

        #region API xóa nhiều người dùng
        /// <summary>
        /// Thêm mới người dùng
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpDelete("delete-many")]
        [Authorize]
        public async Task<IActionResult> DeleteManyNguoiDung(List<Guid> ids)
        {
            try
            {
                var result = await _nguoiDungService.DeleteManyNguoiDung(ids);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        #endregion


        #region API get menu theo id người dùng
        /// <summary>
        /// Thêm mới người dùng
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpGet("get-menu-by-nd")]
        [Authorize]
        public async Task<IActionResult> GetMenuByIdnguoiDung(Guid id_nguoi_dung)
        {
            try
            {
                var result = await _nguoiDungService.GetMenuByNguoiDung(id_nguoi_dung);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        #endregion

        #region API get perm theo id người dùng vaf menu
        /// <summary>
        /// Thêm mới người dùng
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpGet("get-pers")]
        [Authorize]
        public async Task<IActionResult> GetPersByIdnguoiDung(Guid id_nguoi_dung, string ma)
        {
            try
            {
                var result = await _nguoiDungService.GetPermByNguoiDung(id_nguoi_dung, ma);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        #endregion
        
        
        #region API get ds người dùng phòng ban
        /// <summary>
        /// Thêm mới người dùng
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpGet("get-colleague")]
        [Authorize]
        public async Task<IActionResult> GetAllNguoiDungByPhongBan(Guid nguoi_dung_id, Guid tai_lieu_id)
        {
            try
            {
                var result = await _nguoiDungService.GetAllNguoiDungByPhongBan(nguoi_dung_id, tai_lieu_id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        #endregion
        
        
        #region API get ds người dùng đã được chia sẻ tài liệu
        /// <summary>
        /// Thêm mới người dùng
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpGet("get-user-by-doc")]
        [Authorize]
        public async Task<IActionResult> GetDsNguoiDungInDocs(Guid tai_lieu_id)
        {
            try
            {
                var result = await _nguoiDungService.GetDsNguoiDungInDocs(tai_lieu_id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        #endregion
    }
}
