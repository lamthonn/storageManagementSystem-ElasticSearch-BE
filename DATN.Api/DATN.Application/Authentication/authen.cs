using DATN.Application.Interfaces;
using DATN.Domain.Entities;
using DATN.Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using DATN.Domain.DTO;
using Microsoft.EntityFrameworkCore;
using DATN.Application.Utils;

namespace DATN.Application.Authentication
{
    public record loginParam
    {
        public string? tai_khoan { get; set; }
        public string? mat_khau { get; set; }
    }
    public class authen : IAuthen
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly Helper _hepper;
        private readonly INhatKyHeThong _logger;
        public authen(AppDbContext context, IConfiguration configuration, Helper hepper, INhatKyHeThong logger)
        {
            _context = context;
            _configuration = configuration;
            _hepper = hepper;
            _logger = logger;
        }

        async public Task<nguoi_dung_dto> RefreshToken(RefreshTokenRequest refreshToken)
        {
            var user = _context.nguoi_dung.FirstOrDefault(x => x.RefreshToken == refreshToken.RefreshToken);

            if (user == null)
            {
                return new nguoi_dung_dto { errrorMessage = "Refresh Token không hợp lệ" };
            }

            if (user.RefreshTokenExpiryTime < DateTime.UtcNow)
            {
                return new nguoi_dung_dto { errrorMessage = "Refresh Token đã hết hạn" };
            }

            var newAccessToken = _hepper.GenerateJwtToken(user);
            //var newRefreshToken = GenerateRefreshToken();

            //user.RefreshToken = newRefreshToken;
            //user.RefreshTokenExpiryTime = DateTime.UtcNow.AddMinutes(1);

            //_context.Update(user);
            //await _context.SaveChangesAsync();
            return new nguoi_dung_dto { token = newAccessToken, RefreshToken = user.RefreshToken };
        }

        public Task<string> Register(nguoi_dung_dto request)
        {
            try
            {
                var userDuplicate = _context.nguoi_dung.FirstOrDefault(x => x.tai_khoan == request.tai_khoan);
                if (userDuplicate != null)
                {
                    throw new Exception("Tài khoản đã tồn tại");
                }

                // Tạo Salt ngẫu nhiên cho mật khẩu
                byte[] salt = _hepper.GenerateSalt();

                // Mã hóa mật khẩu sử dụng PBKDF2 và salt
                string hashPassword = _hepper.GetPBKDF2(request.mat_khau, salt);

                var newAccount = new nguoi_dung
                {
                    Id = Guid.NewGuid(),
                    tai_khoan = request.tai_khoan,
                    mat_khau = hashPassword,
                    salt_code = Convert.ToBase64String(salt), // Lưu Salt vào CSDL
                    ten = request.ten,
                    ngay_sinh = request.ngay_sinh,
                    gioi_tinh = request.gioi_tinh ?? true,
                    email = request.email,
                    trang_thai = request.trang_thai ?? true,
                    so_dien_thoai = request.so_dien_thoai,
                    ngay_tao = DateTime.Now,
                    nguoi_tao = request.tai_khoan,
                };
                _context.Add(newAccount);
                _context.SaveChanges();

                return Task.FromResult("Đăng ký thành công!");
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }
        public async Task<nguoi_dung_dto> login(loginParam request)
        {
            try
            {
                if (!string.IsNullOrEmpty(request.tai_khoan) || !string.IsNullOrEmpty(request.mat_khau))
                {
                    var user = _context.nguoi_dung.FirstOrDefault(x => x.tai_khoan == request.tai_khoan);

                    if (user != null)
                    {
                        // Tạo lại hash mật khẩu từ mật khẩu người dùng nhập vào và salt trong DB
                        var salt = Convert.FromBase64String(user.salt_code!); // Salt đã lưu trong DB
                        var hashedPassword = _hepper.GetPBKDF2(request.mat_khau, salt);
                        if (hashedPassword == user.mat_khau)
                        {
                            var jwtToken = _hepper.GenerateJwtToken(user);
                            var refreshToken = _hepper.GenerateRefreshToken();

                            // Lưu Refresh Token vào DB
                            user.RefreshToken = refreshToken;
                            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddHours(3);
                            _context.Update(user);
                            await _context.SaveChangesAsync();

                            await _logger.AddLog(new nhat_ky_he_thong_dto
                            {
                                loai = 1,
                                detail = "Truy cập hệ thống"
                            });
                            return new nguoi_dung_dto { token = jwtToken, RefreshToken = refreshToken };
                        }
                        else throw new Exception("Mật khẩu không đúng");
                    }
                    else throw new Exception("Không tìm thấy tài khoản");
                }
                else throw new Exception("Tài khoản và mật khẩu không được để trống");
            }
            catch (Exception e)
            {
                await _logger.AddLog(new nhat_ky_he_thong_dto
                {
                    loai = 2,
                    detail = "Lỗi truy cập hệ thống"
                });
                throw new Exception(e.Message);
            }
        }

    }
}
