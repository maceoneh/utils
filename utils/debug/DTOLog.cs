using es.dmoreno.utils.dataaccess.db;
using System;
using System.Collections.Generic;
using System.Text;

namespace es.dmoreno.utils.debug
{
    public abstract class DTOLog
    {
        [Field(FieldName = "id", IsPrimaryKey = true, IsAutoincrement = true, Type = ParamType.Int32)]
        public int ID { get; set; }

        [Field(FieldName = "log_time", Type = ParamType.DateTime)]
        public DateTime Time { get; set; }

        [Field(FieldName = "log_type", Type = ParamType.Int32)]
        public ETypeLog Type { get; set; }

        [Field(FieldName = "tag", Type = ParamType.String, AllowNull = true, DefaultValue = "")]
        public string Tag { get; set; }

        [Field(FieldName = "description", Type = ParamType.String)]
        public string Description { get; set; }
    }

    [Table(Name = "logs_daily")]
    public class DTOLogDaily : DTOLog { }

    [Table(Name = "logs_monthly")]
    public class DTOLogMonthly : DTOLog { }

    [Table(Name = "logs_yearly")]
    public class DTOLogYearly : DTOLog { }

    [Table(Name = "logs_stored")]
    public class DTOLogStored : DTOLog { }
}
