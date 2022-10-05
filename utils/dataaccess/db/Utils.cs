using es.dmoreno.utils.dataaccess.filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using FilterUtils = es.dmoreno.utils.dataaccess.filters.Utils;

namespace es.dmoreno.utils.dataaccess.db
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void void_setter(object reg, object value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate object object_getter(object reg);

    internal class DBRegistryLinkData
    {
        public void_setter SetterCustom = null;
        public object_getter GetterCustom = null;
        public FieldAttribute FieldAttributes = null;
    }

    public class Utils
    {
        static private List<ClassDBProperties> PropertiesList { get; set; } = new List<ClassDBProperties>();

        static public string buildInString(string[] array, bool add_quotes = false)
        {
            string result;

            result = "";

            for (int i = 0; i < array.Length; i++)
            {
                if (add_quotes)
                {
                    result += "'";
                }
                result += array[i];
                if (add_quotes)
                {
                    result += "'";
                }

                if (i < array.Length - 1)
                {
                    result += ", ";
                }
            }

            return result;
        }

        static public string buildInString(int[] array)
        {
            string result;

            result = "";

            for (int i = 0; i < array.Length; i++)
            {
                result += array[i].ToString();

                if (i < array.Length - 1)
                {
                    result += ", ";
                }
            }

            return result;
        }

        static internal string buildInString(SQLData data, string field)
        {
            List<string> elements;
            string result;

            elements = new List<string>();
            result = "[";

            while (data.next())
            {
                elements.Add(data.getString(field));
            }

            result += buildInString(elements.ToArray());

            result += "]";

            return result;
        }

        static public List<PropertyInfo> getPropertyInfos<T>(T reg, bool with_fieldattribute = false) where T : class, new()
        {
            List<PropertyInfo> result;

            var properties = reg.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            result = new List<PropertyInfo>(properties.Length);

            foreach (var item in properties)
            {
                if (with_fieldattribute)
                {
                    var att = item.GetCustomAttribute<FieldAttribute>();

                    if (att != null)
                    {
                        result.Add(item);
                    }
                }
                else
                {
                    result.Add(item);
                }
            }

            return result;
        }

        /// <summary>
        /// Obtiene los PropertyInfo de la clase T que contenga atributos del tipo I
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="I"></typeparam>
        /// <param name="reg"></param>
        /// <returns></returns>
        static public List<PropertyInfo> getPropertiesInfos<T,I>(T reg) where T : class, new() where I : Attribute
        {
            var properties = reg.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var result = new List<PropertyInfo>(properties.Length);
            foreach (var item in properties)
            {
                var attributelist = item.GetCustomAttributes<I>();
                if (attributelist.Count() > 0)
                {
                    result.Add(item);
                }                
            }

            return result;
        }

        static public List<FieldAttribute> getFieldAttributes(List<PropertyInfo> p)
        {
            var result = new List<FieldAttribute>(p.Count);

            foreach (var item in p)
            {
                var att = item.GetCustomAttribute<FieldAttribute>();

                if (att != null)
                {
                    result.Add(att);
                }
            }

            return result;
        }

        static public List<IndexAttribute> getIndexAttributes(List<PropertyInfo> p)
        {
            var result = new List<IndexAttribute>(p.Count);

            foreach (var item in p)
            {
                var att = item.GetCustomAttribute<IndexAttribute>();

                if (att != null)
                {
                    result.Add(att);
                }
            }

            return result;
        }

        static public string buildSQLStatement(DBMSType dbmstype, string select, string where, string orderby = null, int limit_to = 0, int limit_length = 0)
        {
            string sql;

            sql = select;

            if (!string.IsNullOrWhiteSpace(where))
            {
                if (!where.Trim().ToUpper().StartsWith("AND"))
                {
                    sql += " AND ";
                }

                sql += where;
            }

            if (!string.IsNullOrWhiteSpace(orderby))
            {
                if (!orderby.Trim().ToUpper().StartsWith("ORDER BY"))
                {
                    sql += " ORDER BY ";
                }

                sql += orderby;
            }

            if (limit_to > 0)
            {
                sql += " LIMIT " + limit_to.ToString();

                if (limit_length > 0)
                {
                    if (dbmstype == DBMSType.SQLite)
                    {

                        sql += " OFFSET " + limit_length.ToString();
                    }
                    else if (dbmstype == DBMSType.MySQL)
                    {
                        sql += "," + limit_length.ToString();
                    }
                }
            }

            return sql;
        }

        //static internal ClassDBProperties getProperties(object registry, List<ClassDBProperties> PropertiesList)
        //{
        //    foreach (var item in PropertiesList)
        //    {
        //        if (item.Type == registry.GetType())
        //        {
        //            return item;
        //        }
        //    }

        //    var table_att = registry.GetType().GetTypeInfo().GetCustomAttribute<TableAttribute>();

        //    var p = new ClassDBProperties
        //    {
        //        Properties = Utils.getPropertyInfos(registry, true),
        //        Type = registry.GetType(),
        //        TableName = table_att.Name
        //    };

        //    p.DBSortableAttributes = new List<SortableAttribute>();
        //    p.DBFieldAttributes = new List<FieldAttribute>(p.Properties.Count);
        //    foreach (var item in p.Properties)
        //    {
        //        p.DBFieldAttributes.Add(item.GetCustomAttribute<FieldAttribute>());
        //        var sort_attrib = item.GetCustomAttribute<SortableAttribute>();
        //        if (sort_attrib != null)
        //        {
        //            p.DBSortableAttributes.Add(sort_attrib);
        //        }
        //    }

        //    p.DBFiltersAttributes = FilterUtils.getFiltersSchema(registry);

        //    PropertiesList.Add(p);

        //    return p;
        //}

        //static internal ClassDBProperties getProperties<T>(List<ClassDBProperties> PropertiesList) where T : class, new()
        //{
        //    foreach (var item in PropertiesList)
        //    {
        //        if (item.Type == typeof(T))
        //        {
        //            return item;
        //        }
        //    }

        //    var t = new T();

        //    var table_att = t.GetType().GetTypeInfo().GetCustomAttribute<TableAttribute>();

        //    var p = new ClassDBProperties
        //    {
        //        Properties = Utils.getPropertyInfos<T>(t, true),
        //        Type = typeof(T),
        //        TableName = table_att.Name
        //    };

        //    p.DBSortableAttributes = new List<SortableAttribute>();
        //    p.DBFieldAttributes = new List<FieldAttribute>(p.Properties.Count);
        //    foreach (var item in p.Properties)
        //    {
        //        p.DBFieldAttributes.Add(item.GetCustomAttribute<FieldAttribute>());
        //        var sort_attrib = item.GetCustomAttribute<SortableAttribute>();
        //        if (sort_attrib != null)
        //        {
        //            p.DBSortableAttributes.Add(sort_attrib);
        //        }
        //    }

        //    p.DBFiltersAttributes = FilterUtils.getFiltersSchema<T>();

        //    PropertiesList.Add(p);

        //    return p;
        //}

        static internal List<DBRegistryLinkData> getSetters<T>(bool only_from_pks = false) where T : class, new()
        {
            T t = new T();
            var props = getPropertyInfos<T>(t);
            var result = new List<DBRegistryLinkData>(props.Count);

            foreach (var item in props)
            {
                var att = item.GetCustomAttribute<FieldAttribute>();

                if (att != null)
                {
                    if (only_from_pks)
                    {
                        if (!att.IsPrimaryKey)
                        {
                            att = null;
                        }
                    }

                    if (att != null)
                    {
                        void_setter dcustom = null;
                        switch (att.Type)
                        {
                            case ParamType.Boolean:
                                dcustom = item.SetValue;
                                break;
                            case ParamType.Int16:
                                dcustom = item.SetValue;
                                break;
                            case ParamType.Int32:
                                dcustom = item.SetValue;
                                break;
                            case ParamType.Int64:
                                dcustom = item.SetValue;
                                break;
                            case ParamType.String:
                            case ParamType.LongString:
                                dcustom = item.SetValue;
                                break;
                            case ParamType.DateTime:
                                dcustom = item.SetValue;
                                break;
                            case ParamType.Decimal:
                                dcustom = item.SetValue;
                                break;
                            case ParamType.ByteArray:
                                throw new Exception("Type ByteArray is not supported");
                            default:
                                throw new Exception("Type is not supported");
                        }

                        result.Add(new DBRegistryLinkData
                        {
                            SetterCustom = dcustom,
                            FieldAttributes = att
                        });
                    }
                }
            }

            return result;
        }

        static internal List<DBRegistryLinkData> getGetters<T>(bool only_from_pks = false) where T : class, new()
        {
            T t = new T();
            var props = getPropertyInfos<T>(t);
            var result = new List<DBRegistryLinkData>(props.Count);

            foreach (var item in props)
            {
                var att = item.GetCustomAttribute<FieldAttribute>();

                if (att != null)
                {
                    if (only_from_pks)
                    {
                        if (!att.IsPrimaryKey)
                        {
                            att = null;
                        }
                    }

                    if (att != null)
                    {
                        object_getter dcustom;
                        switch (att.Type)
                        {
                            case ParamType.Boolean:
                                dcustom = item.GetValue;
                                break;
                            case ParamType.Int16:
                                dcustom = item.GetValue;
                                break;
                            case ParamType.Int32:
                                dcustom = item.GetValue;
                                break;
                            case ParamType.Int64:
                                dcustom = item.GetValue;
                                break;
                            case ParamType.String:
                            case ParamType.LongString:
                                dcustom = item.GetValue;
                                break;
                            case ParamType.DateTime:
                                dcustom = item.GetValue;
                                break;
                            case ParamType.Decimal:
                                dcustom = item.GetValue;
                                break;
                            case ParamType.ByteArray:
                                throw new Exception("Type ByteArray is not supported");
                            default:
                                throw new Exception("Type no supported");
                        }

                        result.Add(new DBRegistryLinkData
                        {
                            FieldAttributes = att,
                            GetterCustom = dcustom
                        });
                    }
                }
            }

            return result;
        }

        static internal ClassDBProperties getProperties(object registry)
        {
            foreach (var item in PropertiesList)
            {
                if (item.Type == registry.GetType())
                {
                    return item;
                }
            }

            var table_att = registry.GetType().GetTypeInfo().GetCustomAttribute<TableAttribute>();

            var p = new ClassDBProperties
            {
                Properties = Utils.getPropertyInfos(registry, true),
                Type = registry.GetType(),
                TableName = table_att.Name
            };

            p.DBSortableAttributes = new List<SortableAttribute>();
            p.DBFieldAttributes = new List<FieldAttribute>(p.Properties.Count);
            foreach (var item in p.Properties)
            {
                var field_attrib = item.GetCustomAttribute<FieldAttribute>();
                p.DBFieldAttributes.Add(field_attrib);
                var sort_attrib = item.GetCustomAttribute<SortableAttribute>();
                if (sort_attrib != null)
                {
                    if (string.IsNullOrWhiteSpace(sort_attrib.FieldName))
                    {
                        sort_attrib.FieldName = field_attrib.FieldName;
                    }
                    p.DBSortableAttributes.Add(sort_attrib);
                }
            }

            p.DBFiltersAttributes = getFiltersSchema(registry);

            PropertiesList.Add(p);

            return p;
        }

        static internal ClassDBProperties getProperties<T>() where T : class, new()
        {
            foreach (var item in PropertiesList)
            {
                if (item.Type == typeof(T))
                {
                    return item;
                }
            }

            var t = new T();

            var table_att = t.GetType().GetTypeInfo().GetCustomAttribute<TableAttribute>();

            var p = new ClassDBProperties
            {
                Properties = Utils.getPropertyInfos<T>(t, true),
                Type = typeof(T),
                TableName = table_att.Name
            };

            p.DBSortableAttributes = new List<SortableAttribute>();
            p.DBFieldAttributes = new List<FieldAttribute>(p.Properties.Count);
            foreach (var item in p.Properties)
            {
                var field_attrib = item.GetCustomAttribute<FieldAttribute>();
                p.DBFieldAttributes.Add(field_attrib);
                var sort_attrib = item.GetCustomAttribute<SortableAttribute>();
                if (sort_attrib != null)
                {
                    if (string.IsNullOrWhiteSpace(sort_attrib.FieldName))
                    {
                        sort_attrib.FieldName = field_attrib.FieldName;
                    }
                    p.DBSortableAttributes.Add(sort_attrib);
                }
            }

            p.DBFiltersAttributes = getFiltersSchema<T>();

            PropertiesList.Add(p);

            return p;
        }

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
