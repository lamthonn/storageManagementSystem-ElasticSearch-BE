using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;

namespace DATN.Domain.Entities
{
    public class nguoi_dung_2_nhom_nguoi_dung
    {
        [Key]
        public Guid id { get; set; }
        public Guid nguoi_dung_id { get; set; }
        public Guid nhom_nguoi_dung_id { get; set; }

        /// <summary>
        /// Mặc định (1-Mặc đinh; 0-Không)
        /// </summary>
        [DefaultValue(false)]
        public bool mac_dinh { get; set; }
        [ForeignKey(nameof(nguoi_dung_id))]
        public virtual nguoi_dung nguoi_dung { get; set; }
        [ForeignKey(nameof(nhom_nguoi_dung_id))]
        public virtual nhom_nguoi_dung nhom_nguoi_dung { get; set; }
    }
}
