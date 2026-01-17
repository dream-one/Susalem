using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace susalem.EasyDemo.Entities
{
    /// <summary>
    /// 当前机柜信息
    /// </summary>
    [Table("CabinetInfo")]
    public class CabinetInfoModel: ValidateModelBase
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // 声明为自增
        [Column(name: "CabinetId")]
        public int CabinetId {  get; set; }

        /// <summary>
        /// 格子名称
        /// </summary>
        [Column(name: "ChamName")]
        [Required(ErrorMessage = "必须填写格口名称")]
        public string ChamName { get; set; }

        /// <summary>
        /// 工匠品料号
        /// </summary>
        [Column(name: "PNCode")]
        public string? PNCode { get; set; }

        /// <summary>
        /// 机台码
        /// </summary>
        [Column(name: "MachineId")]
        public string? MachineId { get; set; }

        /// <summary>
        /// 是否回温状态
        /// </summary>
        [Column("isTemperaturing")]
        public bool IsTemperaturing {  get; set; }

        /// <summary>
        /// 是否闲置状态
        /// </summary>
        [Column("IsNull")]
        public bool IsNull { get; set; }

        /// <summary>
        /// 回温开始时间
        /// </summary>
        [Column("TemperatureStartTime")]
        public DateTime? TemperatureStartTime { get; set; }

        /// <summary>
        /// 回温结束时间
        /// </summary>
        [Column("TemperatureEndTime")]
        public DateTime? TemperatureEndTime { get; set; }

        /// <summary>
        /// 保质期
        /// </summary>
        [Column("ExpirationDate")]
        public DateTime? ExpirationDate { get; set; }


        /// <summary>
        /// 锁信号地址
        /// </summary>
        [Column("LockAddress")]
        [Required(ErrorMessage = "锁地址不能为空")]
        public string? LockAddress { get; set; }

        /// <summary>
        /// 绿灯地址
        /// </summary>
        [Column("GreenLightAddress")]
        [Required(ErrorMessage = "绿灯地址不能为空")]
        public string? GreenLightAddress { get; set; }

        /// <summary>
        /// 红灯地址
        /// </summary>
        [Column("RedLightAddress")]
        [Required(ErrorMessage = "红灯地址不能为空")]
        public string? RedLightAddress { get; set; }

        /// <summary>
        /// 门反馈地址
        /// </summary>
        [Column("DoorAddress")]
        [Required(ErrorMessage = "门反馈地址不能为空")]
        public string? DoorAddress { get; set; }
    }
}
