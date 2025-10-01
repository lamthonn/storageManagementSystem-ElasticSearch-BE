using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DATN.Domain.Entities
{
    public class tai_lieu_2_nguoi_dung
    {
        public Guid Id { get; set; }
        public Guid tai_lieu_id { get; set; }
        public Guid nguoi_dung_id { get; set; }
        [ForeignKey(nameof(nguoi_dung_id))]
        public virtual nguoi_dung nguoi_dung { get; set; }
        [ForeignKey(nameof(tai_lieu_id))]
        public virtual tai_lieu tai_lieu { get; set; }
    }
}
