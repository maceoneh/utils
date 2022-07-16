using es.dmoreno.utils.dataaccess.db;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace es.dmoreno.utils.dataaccess.filters
{
    public class Filter
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "type")]
        public FilterType Type { get; set; } = FilterType.Equal;

        public object ObjectValue { get; set; } = null;

        [DataMember(Name = "value")]
        public string StringFormatValue { get; set; }

        public object ObjectValue2 { get; set; } = null;

        [DataMember(Name = "value2")]
        public string StringFormatValue2 { get; set; }

        internal void castStringValue(ParamType t)
        {
            if (this.StringFormatValue == null)
            {
                this.ObjectValue = null;
            }
            else
            {
                switch (t)
                {
                    case ParamType.Boolean:
                        this.ObjectValue = this.StringFormatValue.Equals("true") || this.StringFormatValue.Equals("True"); break;
                    case ParamType.DateTime:
                        this.ObjectValue = new DateTime(Convert.ToInt64(this.StringFormatValue)); break;
                    case ParamType.Decimal:
                        this.ObjectValue = Convert.ToDecimal(this.StringFormatValue); break;
                    case ParamType.Int16:
                        this.ObjectValue = Convert.ToInt16(this.StringFormatValue); break;
                    case ParamType.Int32:
                        this.ObjectValue = Convert.ToInt32(this.StringFormatValue); break;
                    case ParamType.Int64:
                        this.ObjectValue = Convert.ToInt64(this.StringFormatValue); break;
                    case ParamType.String:
                        this.ObjectValue = this.StringFormatValue; break;
                    default:
                        this.ObjectValue = null; break;
                }
            }

            if (this.StringFormatValue2 == null)
            {
                this.ObjectValue2 = null;
            }
            else
            {
                switch (t)
                {
                    case ParamType.Boolean:
                        this.ObjectValue2 = this.StringFormatValue2.Equals("true") || this.StringFormatValue2.Equals("True"); break;
                    case ParamType.DateTime:
                        this.ObjectValue2 = new DateTime(Convert.ToInt64(this.StringFormatValue2)); break;
                    case ParamType.Decimal:
                        this.ObjectValue2 = Convert.ToDecimal(this.StringFormatValue2); break;
                    case ParamType.Int16:
                        this.ObjectValue2 = Convert.ToInt16(this.StringFormatValue2); break;
                    case ParamType.Int32:
                        this.ObjectValue2 = Convert.ToInt32(this.StringFormatValue2); break;
                    case ParamType.Int64:
                        this.ObjectValue2 = Convert.ToInt64(this.StringFormatValue2); break;
                    case ParamType.String:
                        this.ObjectValue2 = this.StringFormatValue2; break;
                    default:
                        this.ObjectValue2 = null; break;
                }
            }
        }
    }
}
