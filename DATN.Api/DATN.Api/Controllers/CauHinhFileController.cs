using DATN.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DATN.Api.Controllers
{
    [Route("api/cau-hinh-file")]
    [ApiController]
    public class CauHinhFileController : ControllerBase
    {
        private readonly ICauHinhFile _cauHinhFileService;
        public CauHinhFileController(ICauHinhFile cauHinhFileService)
        {
            _cauHinhFileService = cauHinhFileService;
        }

        [HttpGet("get-all-config")]
        public async Task<IActionResult> GetAllConfig()
        {
            try
            {
                var result = await _cauHinhFileService.getAllConfig();
                return Ok(result);
            }
            catch (Exception ex) {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPut("edit-config")]
        public async Task<IActionResult> EditConfig([FromBody] List<Domain.DTO.cau_hinh_file_dto> cau_Hinh_File_Dto)
        {
            try
            {
                var result = await _cauHinhFileService.editConfig(cau_Hinh_File_Dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
