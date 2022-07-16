using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace es.dmoreno.utils.dataaccess.db
{
    internal class SQLiteManagement : Management
    {
        internal SQLiteManagement(SQLStatement s) : base(s) { }

        internal bool IgnoreFilePerTable { get; set; } = false;

        private string ExtractNameFileFromStringConnection(string stringconnection)
        {
            var regex = new Regex("Data Source=([^;]+);");
            var match = regex.Match(stringconnection);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            else
            {
                throw new Exception("Can't extract filename");
            }
        }

        public override async Task<bool> createAlterTableAsync<T>()
        {
            T t;
            TableAttribute table_att;
            bool result;
            bool new_table;
            string sql;
            var indexes = new List<List<string>>();

            this.checkSchemaSQLite<T>();

            result = true;

            t = new T();

            //Check if table exists
            table_att = t.GetType().GetTypeInfo().GetCustomAttribute<TableAttribute>();

            //Check FilePerTable
            if (table_att.FilePerTable && !this.IgnoreFilePerTable)
            {
                //Se comprueba si existe la tabla en la conexion central
                await this.createAlterTableAsync<FilePerTable>();
                //Se extrae la ruta del fichero central
                var path_to_file = this.ExtractNameFileFromStringConnection(this.Statement.StringConnection);
                var directory = Path.GetDirectoryName(path_to_file);
                var namefile = Path.GetFileName(path_to_file);
                var prename = Path.GetFileNameWithoutExtension(namefile);
                var ext = Path.GetExtension(namefile);
                var new_name = prename + "_" + table_att.Name  + ext;
                var new_path = directory + Path.DirectorySeparatorChar   + new_name;
                //Se crea la tebla en el nuevo fichero
                var create = false;
                using (var fptstatement = new SQLStatement(DataBaseLogic.createStringConnection(DBMSType.SQLite, new_path, "", "", "", 0), DBMSType.SQLite))
                {
                    var mgt = new SQLiteManagement(fptstatement);
                    mgt.IgnoreFilePerTable = true;
                    create = await mgt.createAlterTableAsync<T>();
                }
                if (create)
                {
                    //Si la tabla se crea se agrega su info al fichero FilePerTable
                    var infotable = await this.Statement.selectAsync<FilePerTable>("tablename = '" + table_att.Name + "'");
                    if (infotable.Count == 0)
                    {
                        await this.Statement.insertAsync(new FilePerTable
                        {
                            NameFile = new_name,
                            Table = table_att.Name
                        });
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                try
                {
                    sql = "SELECT * FROM " + table_att.Name + " LIMIT 1";
                    await this.Statement.executeAsync(sql);
                    result = true;
                    new_table = false;
                }
                catch
                {
                    result = false;
                    new_table = true;
                }
            }

            if (new_table)
            {
                var pks = Utils.getFieldAttributes(Utils.getPropertyInfos<T>(t, true)).Where(a => a.IsPrimaryKey).ToList();

                sql = "CREATE TABLE " + table_att.Name + " (";
                if (pks.Count == 0)
                {
                    sql += " _auto_created INTEGER DEFAULT NULL";
                }
                else if (pks.Count == 1)
                {
                    sql += " " + this.getCreateFieldSQLite(pks[0], true);
                }
                else
                {
                    for (int i = 0; i < pks.Count; i++)
                    {
                        sql += this.getCreateFieldSQLite(pks[i], false) + ", ";
                    }

                    sql += " PRIMARY KEY (";
                    for (int i = 0; i < pks.Count; i++)
                    {
                        sql += pks[i].FieldName;

                        if (i < pks.Count - 1)
                        {
                            sql += ", ";
                        }
                    }
                    sql += ")";
                }

                //sql += await this.createUpdateConstraint<T>();

                sql += ")";

                await this.Statement.executeNonQueryAsync(sql);
            }

            //Check if fields exists
            foreach (var item in Utils.getFieldAttributes(Utils.getPropertyInfos<T>(t, true)).Where(a => !a.IsPrimaryKey))
            {
                //Check if exists
                sql = "SELECT " + item.FieldName + " FROM " + table_att.Name + " LIMIT 1";
                try
                {
                    await this.Statement.executeAsync(sql);
                    result = true;
                }
                catch
                {
                    result = false;
                }

                //Create field
                if (!result)
                {
                    sql = "ALTER TABLE " + table_att.Name + " ADD COLUMN " + this.getCreateFieldSQLite(item, false);
                    await this.Statement.executeNonQueryAsync(sql);
                    result = true;
                }   
            }

            //Se obtiene los indices de los campos de la clase
            foreach (var item in Utils.getPropertiesInfos<T, IndexAttribute>(t))
            {
                //Se obtienen los indices
                var lindex_att = item.GetCustomAttributes<IndexAttribute>();
                //Se obtiene información sobre el campo
                var field_att = item.GetCustomAttribute<FieldAttribute>();
                //carga toda la información de indices disponible
                foreach (var a_item in lindex_att)
                {
                    List<string> index_schema = null;
                    string index_name;
                    if (a_item.Unique)
                    {
                        index_name = "unq_" + a_item.Name;
                    }
                    else
                    {
                        index_name = "idx_" + a_item.Name;
                    }
                    foreach (var i_item in indexes)
                    {
                        if (i_item[0].Equals(index_name))
                        {
                            index_schema = i_item;
                            break;
                        }
                    }
                    if (index_schema == null)
                    {
                        index_schema = new List<string>();
                        index_schema.Add(index_name);
                        indexes.Add(index_schema);
                    }
                    index_schema.Add(field_att.FieldName);
                }
            }
            //Se cargan los indices encontrados
            foreach (var i_item in indexes)
            {
                if (!await this.existIndexInSQLiteAsync(i_item[0]))
                {
                    sql = "CREATE ";
                    if (i_item[0].StartsWith("unq_"))
                    {
                        sql += "UNIQUE ";
                    }
                    sql += "INDEX " + i_item[0] +
                          " ON " + table_att.Name + " (";
                    for (int i = 1; i < i_item.Count; i++)
                    {
                        sql += i_item[i];
                        if (i < i_item.Count - 1)
                        {
                            sql += ", ";
                        }
                    }
                    sql += ")";
                    await this.Statement.executeNonQueryAsync(sql);
                }
            }

            return result;
        }

        private async Task<bool> existIndexInSQLiteAsync(string index)
        {
            //SELECT count(*) AS num FROM sqlite_master WHERE type='index' and name=?;"
            var sql = "SELECT count(*) AS num FROM sqlite_master WHERE type='index' AND name = '" + index + "'";
            var rs = await this.Statement.executeAsync(sql);
            if (rs.next())
            {
                do
                {
                    if (rs.getInt32("num") > 0)
                    {
                        return true;
                    }
                }
                while (rs.next());
            }
            return false;
        }

        public override Task<List<DescRow>> getDescAsync<T>()
        {
            throw new NotImplementedException();
        }

        internal string getCreateFieldSQLite(FieldAttribute field_info, bool include_pk = false)
        {
            string result;

            result = field_info.FieldName + " " + this.Statement.getTypeSQLiteString(field_info.Type);

            if (include_pk)
            {
                if (field_info.IsPrimaryKey)
                {
                    result += " PRIMARY KEY";
                }
            }

            if (field_info.IsAutoincrement && field_info.isNumeric)
            {
                result += " AUTOINCREMENT";
            }

            if (!field_info.IsPrimaryKey || (field_info.IsPrimaryKey && !include_pk))
            {
                if (!field_info.AllowNull)
                {
                    result += " NOT NULL";
                }
            }

            if (field_info.DefaultValue != null)
            {
                if (field_info.Type == ParamType.Boolean)
                {
                    if ((bool)field_info.DefaultValue)
                    {
                        result += " DEFAULT 1";
                    }
                    else
                    {
                        result += " DEFAULT 0";
                    }
                }
                else if (field_info.Type == ParamType.Int16 || field_info.Type == ParamType.Int32 || field_info.Type == ParamType.Int64)
                {
                    result += " DEFAULT " + Convert.ToInt32(field_info.DefaultValue).ToString();
                }
                else if (field_info.Type == ParamType.DateTime)
                {
                    if ((string)field_info.DefaultValue == "0")
                    {
                        result += " DEFAULT " + DateTime.MinValue.Ticks.ToString();
                    }
                    else
                    {
                        result += " DEFAULT " + DateTime.Parse((string)field_info.DefaultValue).Ticks.ToString();
                    }
                }
                else if (field_info.Type == ParamType.String)
                {
                    result += " DEFAULT '" + (string)field_info.DefaultValue + "'";
                }
                else if (field_info.Type == ParamType.Decimal)
                {
                    result += " DEFAULT " + Convert.ToDecimal(field_info.DefaultValue).ToString();
                }
                else
                {
                    throw new Exception("Datatype not supported");
                }
            }

            return result;
        }

        private void checkSchemaSQLite<T>() where T : class, new()
        {
            var attributes = Utils.getFieldAttributes(Utils.getPropertyInfos<T>(new T(), true));

            var pks = 0;
            var ai = 0;
            var pks_ai = 0;

            foreach (var item in attributes)
            {
                if (item.IsAutoincrement)
                {
                    ai++;

                    if (!item.isNumeric)
                    {
                        throw new Exception("Field " + item.FieldName + " is has AUTOINCREMENT attribute but not is numeric");
                    }
                }

                if (item.IsPrimaryKey)
                {
                    pks++;
                }

                if (item.IsAutoincrement && item.IsPrimaryKey)
                {
                    pks_ai++;
                }
            }

            if (ai > 1)
            {
                throw new Exception("The use of the AUTOINCREMENT attribute in more than one field is not allowed");
            }

            if ((pks_ai == 1) && (pks > 1))
            {
                throw new Exception("The use of the AUTOINCREMENT attribute in primary key field when exists primary key combined is not allowed");
            }
        }

        //private async Task<string> createUpdateConstraint<T>() where T : class, new()
        //{
        //    var reg = new T();
        //    var table_att = reg.GetType().GetTypeInfo().GetCustomAttribute<TableAttribute>();

        //    //first get all constraints
        //    var properties = Utils.getPropertyInfos<T>(reg, false);
        //    var cons = Utils.getFieldConstraints(properties).ToList();

        //    //agroup constraints by name
        //    var cons_availables = cons.GroupBy(c => c.Name).ToList();

        //    var sql = "";
        //    foreach (var item in cons_availables)
        //    {
        //        sql += ", ";
        //        if (item.ElementAt(0).Type == EConstraintType.ForeignKey)
        //        {
        //            sql += " FOREIGN KEY (";
        //        }
        //        else
        //        {
        //            sql += " UNIQUE KEY (";
        //        }

        //        var references = "";
        //        for (int i = 0; i < item.Count(); i++)
        //        {
        //            sql += item.ElementAt(i).FieldName;
        //            references += item.ElementAt(i).ReferencedField;

        //            if (i < (item.Count() - 1))
        //            {
        //                sql += ", ";
        //                references += ", ";
        //            }
        //        }

        //        sql += ") REFERENCES " + item.ElementAt(0).ReferencedTable + " (" + references + ")";
        //    }

        //    return sql;
        //}
    }
}
