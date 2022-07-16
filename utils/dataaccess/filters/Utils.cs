using es.dmoreno.utils.dataaccess.db;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace es.dmoreno.utils.dataaccess.filters
{
    static internal class Utils
    {
        public static List<FieldFilterSchema> getFiltersSchema<T>() where T : class, new()
        {
            var registry = new T();

            //Get table attribute from class
            var table_att = registry.GetType().GetTypeInfo().GetCustomAttribute<TableAttribute>();
            if (table_att != null)
            {
                var filters = new List<FieldFilterSchema>();
                //Get field attributtes
                foreach (PropertyInfo item in registry.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    var att_dbfilter = item.GetCustomAttribute<FilterAttribute>();
                    if (att_dbfilter != null)
                    {
                        var att_dbfield = item.GetCustomAttribute<FieldAttribute>();
                        if (att_dbfield != null)
                        {
                            filters.Add(new FieldFilterSchema
                            {
                                FieldName = att_dbfield.FieldName,
                                FieldType = att_dbfield.Type,
                                TableName = table_att.Name,
                                FilterName = att_dbfilter.Name
                            });
                        }
                    }
                }

                return filters;
            }

            return null;
        }

        public static List<FieldFilterSchema> getFiltersSchema(object registry)
        {
            //Get table attribute from class
            var table_att = registry.GetType().GetTypeInfo().GetCustomAttribute<TableAttribute>();
            if (table_att != null)
            {
                var filters = new List<FieldFilterSchema>();
                //Get field attributtes
                foreach (PropertyInfo item in registry.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    var att_dbfilter = item.GetCustomAttribute<FilterAttribute>();
                    if (att_dbfilter != null)
                    {
                        var att_dbfield = item.GetCustomAttribute<FieldAttribute>();
                        if (att_dbfield != null)
                        {
                            filters.Add(new FieldFilterSchema
                            {
                                FieldName = att_dbfield.FieldName,
                                FieldType = att_dbfield.Type,
                                TableName = table_att.Name,
                                FilterName = att_dbfilter.Name,
                                AllowNull = att_dbfield.AllowNull
                            });
                        }
                    }
                }

                return filters;
            }

            return null;
        }
    }
}
