/*
* 29/07/2018: Resolve error when use SQLite database and her state is locked (https://github.com/aspnet/Microsoft.Data.Sqlite/issues/474)
*/
#define DELETE_ROW_COMPARED_BEHAVIOR_WHEN_SYNC
using es.dmoreno.utils.dataaccess.filters;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace es.dmoreno.utils.dataaccess.db
{
    public class SQLStatement : IDisposable
    {
        private class ConfigStatement
        {
            public bool isFirstPKAutoIncrementInt { get; set; } = false;
            public string SQL { get; set; }
            public string SELECT { get; set; }
            public string WHERE { get; set; }
            public string ORDERBY { get; set; }
            public List<StatementParameter> Params { get; set; }
            public List<StatementParameter> FilterParams { get; set; }
        }

        private const int kMaxTimeWaitUnlock = 30000;

        public string LastError { get; internal set; }

        internal IConnector Conector { get { return this._connector; } }
        internal string StringConnection { get => this._string_connection; }

        private SemaphoreSlim Semaphore { get; } = new SemaphoreSlim(1);
        private DbConnection _connection = null;
        private IConnector _connector = null;
        private DbCommand _command = null;
        private DbDataReader _datareader = null;
        private DbTransaction _transaction = null;
        private List<DbParameter> _parameters = new List<DbParameter>();
        private string _string_connection;
        private DBMSType sgbd;
        private bool disposed = false;
        private SQLData _result_data = null;
        private object _transaction_secure_object = null;
        private bool _transaction_secure_active = false;

        public DBMSType Type
        {
            get { return sgbd; }
        }

        public SQLStatement(string string_connection, DBMSType dbms)
        {
            this.initialize(string_connection, dbms, null);
        }

        public SQLStatement(string string_connection, DBMSType dbms, IConnector connector)
        {
            this.initialize(string_connection, dbms, connector);
        }

        private void initialize(string string_connection, DBMSType dbms, IConnector connector)
        {
            this._string_connection = string_connection;
            this.sgbd = dbms;
            this._connector = connector;

            if (dbmsTypeRequireConnector(dbms) && connector == null)
            {
                throw new Exception("DBMS selected require extern connector");
            }

            this.open();
        }

        static private bool dbmsTypeRequireConnector(DBMSType type)
        {
            switch (type)
            {
                case DBMSType.MySQL: return true;
                default: return false;
            }
        }

        private void createCommand()
        {
            this._command = this._connection.CreateCommand();
            this.loadParameters();
        }

        #region Transacciones seguras
        /// <summary>
        /// Genera una transacción de forma segura de forma que permite el uso de transacciones anidadas
        /// </summary>
        /// <param name="o"></param>
        public void beginTransaction(object o)
        {
            if (o == null)
                throw new Exception("No se ha indicado un objeto para iniciar una transacción segura");

            if (this._transaction_secure_object == null)
            {
                this._transaction_secure_object = o;

                if (this.transactionInProgress())
                {
                    this._transaction_secure_active = false;
                    return;
                }
                else
                {
                    this._transaction_secure_active = true;
                    this.beginTransaction();
                }
            }
        }

        /// <summary>
        /// Acepta una transacción segura
        /// </summary>
        /// <param name="o"></param>
        public void acceptTransaction(object o)
        {
            if (o == null)
                throw new Exception("No se ha indicado un objeto para terminar una transacción segura");

            if (this._transaction_secure_object == o)
            {
                if (this._transaction_secure_active)
                    this.acceptTransaction();
                this._transaction_secure_object = null;
            }
        }

        /// <summary>
        /// Rechaza una transacción segura
        /// </summary>
        /// <param name="o"></param>
        public void refuseTransaction(object o)
        {
            if (o == null)
                throw new Exception("No se ha indicado un objeto para terminar una transacción segura");

            if (this._transaction_secure_object == o)
            {
                if (this._transaction_secure_active)
                    this.refuseTransaction();
                this._transaction_secure_object = null;
            }
        }

        /// <summary>
        /// Muestra si la transacción seguro esta activa
        /// </summary>
        public bool isActiveSecureTransaction
        {
            get { return this._transaction_secure_active; }
        }
        #endregion

        #region *************Transacciones****************

        public bool transactionInProgress()
        {
            return this._transaction != null;
        }

        public void beginTransaction()
        {
            if (this._transaction == null)
            {
                if (this._connection != null)
                {
                    this.finalizeSqlData(); //finalizamos los objetos data que estan abiertos
                    this._transaction = this._connection.BeginTransaction();
                }
                else
                    throw new Exception("No existe ninguna conexión activa");
            }
            else
                throw new Exception("Existe una transacción en curso");
        }

        public void acceptTransaction()
        {
            if (this._transaction != null)
            {
                this.finalizeSqlData();
                this.empty();
                this._transaction.Commit();
                this._transaction = null;
            }
            else
                throw new Exception("No existe transacción en curso");
        }

        public void refuseTransaction()
        {
            if (this._transaction != null)
            {
                this.finalizeSqlData();
                this.empty();
                this._transaction.Rollback();
                this._transaction = null;
            }
            else
                throw new Exception("No existe transacción en curso");
        }

        #endregion

        static private bool isBusySQLite(int code)
        {
            return (code == SQLitePCL.raw.SQLITE_BUSY) || (code == SQLitePCL.raw.SQLITE_LOCKED) || (code == SQLitePCL.raw.SQLITE_LOCKED_SHAREDCACHE);
        }

        public SQLData execute(string sql)
        {
            bool completed = false;
            this.finalizeSqlData();
            this.createCommand();
            this._command.CommandText = sql;

            var stopwatch = Stopwatch.StartNew();
            while (!completed)
            {
                try
                {
                    this._datareader = this._command.ExecuteReader();
                    completed = true;
                }
                catch (SqliteException ex)
                {
                    if (!isBusySQLite(ex.SqliteErrorCode) || stopwatch.ElapsedMilliseconds > kMaxTimeWaitUnlock)
                    {
                        this.LastError = ex.Message;
                        throw ex;
                    }
                }
                catch (Exception ex)
                {
                    this.LastError = ex.Message;
                    throw ex;
                }
            }
            SQLData d = new SQLData(this._datareader, this._command, this);
            this._result_data = d;
            this.empty();
            return d;
        }

        public async Task<SQLData> executeAsync(string sql)
        {
            bool completed = false;
            this.finalizeSqlData();
            this.createCommand();
            this._command.CommandText = sql;

            var stopwatch = Stopwatch.StartNew();
            while (!completed)
            {
                try
                {
                    this._datareader = await this._command.ExecuteReaderAsync();
                    completed = true;
                }
                catch (SqliteException ex)
                {
                    if (!isBusySQLite(ex.SqliteErrorCode) || stopwatch.ElapsedMilliseconds > kMaxTimeWaitUnlock)
                    {
                        this.LastError = ex.Message;
                        throw ex;
                    }
                }
                catch (Exception ex)
                {
                    this.LastError = ex.Message;
                    throw ex;
                }
            }

            SQLData d = new SQLData(this._datareader, this._command, this);
            this._result_data = d;
            this.empty();
            return d;
        }

        public int executeNonQuery(string sql)
        {
            bool completed = false;
            int resultado = 0;

            this.finalizeSqlData();
            this.createCommand();
            this._command.CommandText = sql;

            var stopwatch = Stopwatch.StartNew();
            while (!completed)
            {
                try
                {
                    resultado = this._command.ExecuteNonQuery();
                    completed = true;
                }
                catch (SqliteException ex)
                {
                    if (!isBusySQLite(ex.SqliteErrorCode) || stopwatch.ElapsedMilliseconds > kMaxTimeWaitUnlock)
                    {
                        this.LastError = ex.Message;
                        throw ex;
                    }
                }
                catch (Exception ex)
                {
                    this.LastError = ex.Message;
                    throw ex;
                }
            }

            this.empty();
            return resultado;
        }

        public async Task<int> executeNonQueryAsync(string sql)
        {
            bool completed = false;
            int resultado = 0;

            this.finalizeSqlData();
            this.createCommand();
            this._command.CommandText = sql;

            var stopwatch = Stopwatch.StartNew();
            while (!completed)
            {
                try
                {
                    resultado = await this._command.ExecuteNonQueryAsync();
                    completed = true;
                }
                catch (SqliteException ex)
                {
                    if (!isBusySQLite(ex.SqliteErrorCode) || stopwatch.ElapsedMilliseconds > kMaxTimeWaitUnlock)
                    {
                        this.LastError = ex.Message;
                        throw ex;
                    }
                }
                catch (Exception ex)
                {
                    this.LastError = ex.Message;
                    throw ex;
                }
            }

            this.empty();
            return resultado;
        }

        private void loadParameters()
        {
            foreach (var item in _parameters)
            {
                this._command.Parameters.Add(item);
            }
            this._parameters.Clear();
        }

        public void open()
        {
            if (string.IsNullOrEmpty(this._string_connection))
            {
                throw new Exception("String connection is empty");
            }

            this._connection = this.getConnection();
        }

        public void close()
        {
            this.finalizeSqlData();
            if (this._connection != null)
            {
                this._connection.Close();
                this._connection.Dispose();
                this._connection = null;
            }
        }

        private void finalizeSqlData()
        {
            if (this._result_data != null)
            {
                this._result_data.Dispose();
                this._result_data = null;
            }
        }

        private void empty()
        {
            this._command = null;
            this._datareader = null;
        }

        private DbConnection getConnection()
        {
            DbConnection con;

            switch (this.sgbd)
            {
                case DBMSType.MySQL:
                    con = this._connector.getConnection(this._string_connection);
                    break;
                case DBMSType.SQLite:
                    con = this.getConnectionSQLite();
                    break;
                default:
                    throw new Exception("Select a valid DBMS");
            }

            if (con.State == System.Data.ConnectionState.Closed)
            {
                con.Open();
            }

            return con;
        }

        private SqliteConnection getConnectionSQLite()
        {
            SqliteConnection con;

            con = new SqliteConnection(this._string_connection);
            con.Open();

            return con;
        }

        public int lastID
        {
            get
            {
                int id;

                if (this.sgbd == DBMSType.MySQL)
                {
                    SQLData d = this.execute("SELECT LAST_INSERT_ID() AS id;");
                    if (d.isEmpty())
                    {
                        id = 0;
                    }
                    else
                    {
                        d.next();
                        id = d.getInt32("id");
                    }
                }
                else if (this.sgbd == DBMSType.SQLite)
                {
                    using (SQLData d = this.execute("SELECT last_insert_rowid() AS id;"))
                    {
                        if (d.isEmpty())
                        {
                            id = 0;
                        }
                        else
                        {
                            d.next();
                            id = d.getInt32("id");
                        }
                    }
                }
                else
                {
                    throw new Exception("Can't get LastID with DBMS selected");
                }

                return id;
            }
        }

        #region ***************Manipulación de parámetros***************

        public void clearParameters()
        {
            this._parameters.Clear();
        }

        internal SqliteType getTypeSQLite(ParamType type)
        {
            SqliteType t;
            switch (type)
            {
                case ParamType.Decimal:
                    t = SqliteType.Real;
                    break;
                case ParamType.String:
                case ParamType.LongString:
                    t = SqliteType.Text;
                    break;
                case ParamType.Int16:
                    t = SqliteType.Integer;
                    break;
                case ParamType.Int32:
                    t = SqliteType.Integer;
                    break;
                case ParamType.Int64:
                    t = SqliteType.Integer;
                    break;
                case ParamType.Boolean:
                    t = SqliteType.Integer;
                    break;
                case ParamType.DateTime:
                    t = SqliteType.Integer;
                    break;
                default:
                    throw new Exception("Param type is not supported");
            }

            return t;
        }

        internal string getTypeSQLiteString(ParamType type)
        {
            SqliteType t;
            string result;

            t = this.getTypeSQLite(type);

            if (t == SqliteType.Integer)
            {
                result = "INTEGER";
            }
            else if (t == SqliteType.Real)
            {
                result = "REAL";
            }
            else if (t == SqliteType.Text)
            {
                result = "TEXT";
            }
            else
            {
                result = "BLOB";
            }

            return result;
        }

        private void addParameterSQLite(string name, ParamType type, object value)
        {
            SqliteType t;
            switch (type)
            {
                case ParamType.Decimal:
                    t = SqliteType.Real;
                    break;
                case ParamType.String:
                case ParamType.LongString:
                    t = SqliteType.Text;
                    break;
                case ParamType.Int16:
                    t = SqliteType.Integer;
                    break;
                case ParamType.Int32:
                    t = SqliteType.Integer;
                    break;
                case ParamType.Int64:
                    t = SqliteType.Integer;
                    break;
                case ParamType.Boolean:
                    t = SqliteType.Integer;

                    if ((bool)value == true)
                    {
                        value = 1;
                    }
                    else
                    {
                        value = 0;
                    }

                    break;
                case ParamType.DateTime:
                    t = SqliteType.Integer;
                    value = ((DateTime)value).Ticks;
                    break;
                default:
                    throw new Exception("Param type is not supported");
            }

            SqliteParameter p = new SqliteParameter(name, t);            
            p.Value = value;
            this._parameters.Add(p);
        }

        public SQLStatement addParameter(string name, ParamType type, object value)
        {
            if (this.sgbd == DBMSType.MySQL)
            {
                //this.addParameterMySQL(name, type, value);
                this._connector.addParameter(this._parameters, name, type, value);
            }
            else if (this.sgbd == DBMSType.SQLite)
            {
                this.addParameterSQLite(name, type, value);
            }

            return this;
        }

        public SQLStatement addParameter(StatementParameter param)
        {
            this.addParameter(param.Nombre, param.Tipo, param.Valor);
            return this;
        }

        public SQLStatement addParameters(IList<StatementParameter> parameters)
        {
            foreach (StatementParameter item in parameters)
            {
                this.addParameter(item);
            }

            return this;
        }

        #endregion

        #region Miembros de IDisposable

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    if (this._transaction != null) this.refuseTransaction();
                    this.close();
                }
            }
            this.disposed = true;
        }

        ~SQLStatement()
        {
            this.Dispose(false);
        }

        #endregion

        /// <summary>
        /// Método que devuelve la hora del servidor
        /// </summary>
        /// <returns></returns>
        public DateTime getNow()
        {
            DateTime now;
            string sql;

            this.Semaphore.Wait();
            try
            {
                this.clearParameters();

                if (this.Type == DBMSType.MySQL)
                {
                    sql = "SELECT NOW() AS ahora;";
                }
                else
                {
                    throw new Exception("El sistema gestor de base de datos no pede proporcionar la hora del servidor");
                }

                SQLData d = this.execute(sql);
                try
                {
                    d.next();
                    now = d.getDateTime("ahora");
                }
                finally
                {
                    d.Dispose();
                }
            }
            finally
            {
                this.Semaphore.Release();
            }

            return now;
        }

        private ConfigStatement getConfigDelete(object registry)
        {
            ConfigStatement c;
            List<string> fields;
            TableAttribute table_att;
            FieldAttribute att;
            object value;
            string aux;

            c = new ConfigStatement() { SQL = "", Params = new List<StatementParameter>() };
            fields = new List<string>();
            //Get table attribute from class
            table_att = registry.GetType().GetTypeInfo().GetCustomAttribute<TableAttribute>();
            //Get fields attributes from properties
            foreach (PropertyInfo item in registry.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                //Create SQL parameters
                att = item.GetCustomAttribute<FieldAttribute>();
                if (att != null)
                {
                    if (att.IsPrimaryKey)
                    {
                        value = item.GetValue(registry);
                        if (att.AllowNull)
                        {
                            if (this.isNull(att, value))
                            {
                                value = DBNull.Value;
                            }
                        }
                        c.Params.Add(new StatementParameter("@arg" + fields.Count, att.Type, value));
                        fields.Add(table_att.Name + "." + att.FieldName);
                        if (!att.AllowNull)
                        {
                            if (value == null || value == DBNull.Value)
                            {
                                throw new Exception("Field '" + att.FieldName + "' not support NULL value");
                            }
                        }
                    }
                }
            }
            //Build a INSERT statement
            c.SQL = "DELETE FROM {0} WHERE 1 = 1 {1}";
            aux = "";
            for (int i = 0; i < fields.Count; i++)
            {
                aux += " AND " + fields[i] + " = " + c.Params[i].Nombre;
            }
            if (string.IsNullOrEmpty(aux))
            {
                throw new Exception("This query can not be performed without primary keys");
            }
            c.SQL = string.Format(c.SQL, table_att.Name, aux);
            return c;
        }

        private ConfigStatement getConfigInsert(object registry)
        {
            ConfigStatement c;
            List<string> fields;
            FieldAttribute att;
            string aux;
            string aux2;
            bool autoincrement_detected;
            object value;

            autoincrement_detected = false;

            c = new ConfigStatement() { SQL = "", Params = new List<StatementParameter>() };
            fields = new List<string>();

            //Get table attribute from class
            var props = Utils.getProperties(registry);

            //Get fields attributes from properties
            var item_count = 0;
            foreach (var item in props.Properties)
            {
                //Create SQL parameters
                att = props.DBFieldAttributes[item_count];
                item_count++;
                if (att != null)
                {
                    value = item.GetValue(registry);

                    if (value == null && !att.AllowNull)
                    {
                        value = att.DefaultValue;
                    }

                    if (!att.IsAutoincrement)
                    {
                        //Check if value por int represent NULL value
                        if (att.AllowNull)
                        {
                            if (this.isNull(att, value))
                            {
                                value = DBNull.Value;
                            }
                        }

                        //Set value
                        c.Params.Add(new StatementParameter("@arg" + fields.Count, att.Type, value));
                        if (this.Type != DBMSType.SQLite)
                        {
                            fields.Add(props.TableName + "." + att.FieldName);
                        }
                        else
                        {
                            fields.Add(att.FieldName);
                        }
                    }
                    else
                    {
                        if (!autoincrement_detected)
                        {
                            if (att.isInteger)
                            {
                                c.isFirstPKAutoIncrementInt = true;
                                autoincrement_detected = true;
                            }
                        }
                    }

                    if (!att.AllowNull)
                    {
                        if (value == null || value == DBNull.Value)
                        {
                            throw new Exception("Field '" + att.FieldName + "' not support NULL value");
                        }
                    }
                }
            }

            //Build a INSERT statement
            c.SQL = "INSERT INTO {0}({1}) VALUES ({2})";

            aux = "";
            for (int i = 0; i < fields.Count; i++)
            {
                aux += fields[i];

                if (i < fields.Count - 1)
                {
                    aux += ", ";
                }
            }

            aux2 = "";
            for (int i = 0; i < c.Params.Count; i++)
            {
                aux2 += c.Params[i].Nombre;

                if (i < c.Params.Count - 1)
                {
                    aux2 += ", ";
                }
            }

            c.SQL = string.Format(c.SQL, props.TableName, aux, aux2);

            return c;
        }

        private ConfigStatement getConfigCount(object registry)
        {
            ConfigStatement c;
            TableAttribute table_att;

            c = new ConfigStatement() { SQL = "", Params = new List<StatementParameter>() };

            //Get table attribute from class
            table_att = registry.GetType().GetTypeInfo().GetCustomAttribute<TableAttribute>();

            //Build a INSERT statement
            c.SQL = "SELECT count(*) AS records FROM {0} WHERE 1 = 1 ";


            c.SELECT = string.Format(c.SQL, table_att.Name);
            c.SQL = string.Format(c.SQL, table_att.Name);

            return c;
        }

        private ConfigStatement getConfigSelect(object registry, bool withoutwhere = false, List<Filter> filters = null, List<Order> orders = null)
        {
            ConfigStatement c;
            List<string> fields;
            List<string> fields_pk;
            TableAttribute table_att;
            FieldAttribute att;
            object value;
            string aux;
            string aux2;
            string aux3;

            c = new ConfigStatement() { SQL = "", Params = new List<StatementParameter>() };
            fields = new List<string>();
            fields_pk = new List<string>();

            var properties = Utils.getProperties(registry);

            //Get table attribute from class
            table_att = registry.GetType().GetTypeInfo().GetCustomAttribute<TableAttribute>();

            //Get fields attributes from properties
            foreach (PropertyInfo item in registry.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                //Create SQL parameters
                att = item.GetCustomAttribute<FieldAttribute>();

                if (att != null)
                {
                    if (!withoutwhere)
                    {
                        if (att.IsPrimaryKey)
                        {
                            value = item.GetValue(registry);

                            if (att.AllowNull)
                            {
                                if (this.isNull(att, value))
                                {
                                    value = DBNull.Value;
                                }
                            }

                            //c.Params.Add(new StatementParameter("@pk" + fields.Count, att.Type, item.GetValue(registry)));
                            c.Params.Add(new StatementParameter("@pk" + fields.Count, att.Type, value));
                            fields_pk.Add(table_att.Name + "." + att.FieldName);

                            if (!att.AllowNull)
                            {
                                if (value == null || value == DBNull.Value)
                                {
                                    throw new Exception("Field '" + att.FieldName + "' not support NULL value");
                                }
                            }
                        }
                    }

                    fields.Add(table_att.Name + "." + att.FieldName);
                }
            }

            aux3 = "";
            var props = Utils.getProperties(registry);
            if (filters != null)
            {
                c.FilterParams = new List<StatementParameter>();
                for (int i = 0; i < filters.Count; i++)
                {
                    var item = filters[i];
                    var dbfilter = props.DBFiltersAttributes.Where(reg => reg.FilterName == item.Name).FirstOrDefault();
                    if (dbfilter != null)
                    {
                        if (item.ObjectValue == null)
                        {
                            item.castStringValue(dbfilter.FieldType);
                        }
                        if (item.ObjectValue == null)
                        {
                            item.Type = FilterType.IsNULL;
                        }
                        if (item.Type != FilterType.In && item.Type != FilterType.IsNULL)
                        {
                            c.FilterParams.Add(new StatementParameter("@ft" + c.FilterParams.Count, dbfilter.FieldType, item.ObjectValue));
                        }
                        aux3 += " AND " + dbfilter.TableName + "." + dbfilter.FieldName;
                        switch (item.Type)
                        {
                            case FilterType.Equal:
                                aux3 += " = "; break;
                            case FilterType.NotEqual:
                                aux3 += " <> ";
                                break;
                            case FilterType.Greater:
                                aux3 += " > "; break;
                            case FilterType.GreaterOrEqual:
                                aux3 += " >= "; break;
                            case FilterType.Less:
                                aux3 += " < "; break;
                            case FilterType.LessOrEqual:
                                aux3 += " >= "; break;
                            case FilterType.Like:
                                aux3 += " LIKE "; break;
                            case FilterType.IsNULL:
                                aux3 += " IS NULL "; break;
                            case FilterType.Between:
                                aux3 += " BETWEEN @ft" + (c.FilterParams.Count - 1).ToString(); ;
                                c.FilterParams.Add(new StatementParameter("@ft" + c.FilterParams.Count, dbfilter.FieldType, item.ObjectValue2));
                                aux3 += " AND ";
                                break;
                            case FilterType.In:
                                aux3 += " IN (";
                                break;
                            default:
                                throw new NotImplementedException();
                        }

                        if (item.Type == FilterType.In)
                        {
                            if (dbfilter.FieldType == ParamType.Int32)
                            {
                                var list = item.ObjectValue as List<int>;
                                aux3 += Utils.buildInString(list.ToArray()) + ")";
                            }
                            else if (dbfilter.FieldType == ParamType.String)
                            {
                                var list = item.ObjectValue as List<string>;
                                aux3 += Utils.buildInString(list.ToArray(), true) + ")";
                            }
                            else
                            {
                                throw new Exception("Filter on '" + dbfilter.TableName + "." + dbfilter.FieldName + "' not support type " + item.Type + " value");
                            }
                        }
                        else if (item.Type != FilterType.IsNULL)
                        {
                            aux3 += "@ft" + (c.FilterParams.Count - 1).ToString();
                        }

                        if (!dbfilter.AllowNull)
                        {
                            if (item.ObjectValue == null || item.ObjectValue == DBNull.Value)
                            {
                                throw new Exception("Filter on '" + dbfilter.TableName + "." + dbfilter.FieldName + "' not support NULL value");
                            }
                        }
                    }
                }

                foreach (var item in c.FilterParams)
                {
                    c.Params.Add(item);
                }
            }

            //Build a INSERT statement
            c.SQL = "SELECT {1} FROM {0} WHERE 1 = 1 {2} {3}";

            aux = "";
            for (int i = 0; i < fields.Count; i++)
            {
                aux += fields[i];

                if (i < fields.Count - 1)
                {
                    aux += ", ";
                }
            }

            aux2 = "";
            for (int i = 0; i < fields_pk.Count; i++)
            {
                aux2 += " AND " + fields_pk[i] + " = " + c.Params[i].Nombre;
            }

            if (!withoutwhere)
            {
                if (string.IsNullOrEmpty(aux2))
                {
                    throw new Exception("This query can not be performed without primary keys");
                }
            }

            c.SELECT = string.Format(c.SQL, table_att.Name, aux, "", "");
            c.SQL = string.Format(c.SQL, table_att.Name, aux, aux2, aux3);
            c.WHERE = aux3;
            c.ORDERBY = "";
            if (orders != null)
            {                
                for (int i = 0; i < orders.Count; i++)
                {
                    var order_attrib = properties.DBSortableAttributes.Where(reg => reg.Name == orders[i].Name).FirstOrDefault();
                    c.ORDERBY += order_attrib.FieldName;
                    if (orders[i].OrderType == EOrderType.Asc)
                    {
                        c.ORDERBY += " ASC";
                    }
                    else
                    {
                        c.ORDERBY += " DESC";
                    }
                    if (i < orders.Count - 1)
                    {
                        c.ORDERBY += ",";
                    }
                }
            }

            return c;
        }

        private bool isNull(FieldAttribute att, object o)
        {
            var result = false;

            if (att.isNumeric)
            {
                switch (att.Type)
                {
                    case ParamType.Int16:
                        result = att.NullValueForInt == (Int16)o;
                        break;
                    case ParamType.Int32:
                        result = att.NullValueForInt == (Int32)o;
                        break;
                    case ParamType.Int64:
                        result = att.NullValueForInt == (Int64)o;
                        break;
                    case ParamType.Decimal:
                        result = att.NullValueForDecimal == (Decimal)o;
                        break;
                }
            }
            else
            {
                result = (o == null);
            }

            return result;
        }

        private ConfigStatement getConfigUpdate(object registry, bool syncAllFields = true)
        {
            ConfigStatement c;
            List<StatementParameter> parameters;
            List<StatementParameter> parameters_pk;
            List<string> fields;
            List<string> fields_pk;
            TableAttribute table_att;
            FieldAttribute att;
            string aux;
            string aux2;
            object value;

            c = new ConfigStatement() { SQL = "", Params = new List<StatementParameter>() };
            fields = new List<string>();
            fields_pk = new List<string>();
            parameters = new List<StatementParameter>();
            parameters_pk = new List<StatementParameter>();

            //Get table attribute from class            
            var props = Utils.getProperties(registry);
            //Get fields in SET section from properties            
            var item_count = 0;
            foreach (var item in props.Properties)
            {
                //Create SQL parameters                
                att = props.DBFieldAttributes[item_count];
                item_count++;
                if (att != null)
                {
                    value = item.GetValue(registry);

                    ////Check if value por int represent NULL value
                    if (att.AllowNull)
                    {
                        if (this.isNull(att, value))
                        {
                            value = DBNull.Value;
                        }
                    }

                    if (!att.IsPrimaryKey)
                    {
                        if (syncAllFields || (!syncAllFields && att.IsSyncrhonizable))
                        {
                            parameters.Add(new StatementParameter("@arg" + fields.Count, att.Type, value));
                            if (this.sgbd != DBMSType.SQLite)
                            {
                                fields.Add(props.TableName + "." + att.FieldName);
                            }
                            else
                            {
                                fields.Add(att.FieldName);
                            }
                        }
                    }

                    if (!att.AllowNull)
                    {
                        if (syncAllFields || (!syncAllFields && att.IsSyncrhonizable))
                        {
                            if (value == null || value == DBNull.Value)
                            {
                                throw new Exception("Field '" + att.FieldName + "' not support NULL value");
                            }
                        }
                    }
                }
            }

            //Get fields in WHERE section from properties
            item_count = 0;
            foreach (var item in props.Properties)
            {
                //Create SQL parameters
                att = props.DBFieldAttributes[item_count];
                item_count++;
                if (att != null)
                {
                    if (att.IsPrimaryKey)
                    {
                        value = item.GetValue(registry);

                        //Check if value por int represent NULL value
                        if (att.AllowNull)
                        {
                            if (this.isNull(att, value))
                            {
                                value = DBNull.Value;
                            }
                        }

                        parameters_pk.Add(new StatementParameter("@pk" + fields_pk.Count, att.Type, value));
                        if (this.sgbd != DBMSType.SQLite)
                        {
                            fields_pk.Add(props.TableName + "." + att.FieldName);
                        }
                        else
                        {
                            fields_pk.Add(att.FieldName);
                        }

                        if (!att.AllowNull)
                        {
                            if (value == null || value == DBNull.Value)
                            {
                                throw new Exception("Field '" + att.FieldName + "' not support NULL value");
                            }
                        }
                    }
                }
            }

            c.SQL = "UPDATE {0} SET {1} WHERE 1 = 1 {2}";

            //Build SET section in UPDATE statement
            aux = "";
            for (int i = 0; i < fields.Count; i++)
            {
                aux += fields[i] + " = " + parameters[i].Nombre;

                if (i < fields.Count - 1)
                {
                    aux += ", ";
                }

                c.Params.Add(parameters[i]);
            }

            //Build WHERE section in UPDATE statement
            aux2 = "";
            for (int i = 0; i < fields_pk.Count; i++)
            {
                aux2 += " AND " + fields_pk[i] + " = " + parameters_pk[i].Nombre;

                c.Params.Add(parameters_pk[i]);
            }

            if (string.IsNullOrEmpty(aux2))
            {
                throw new Exception("This query can not be performed without primary keys");
            }

            c.SQL = string.Format(c.SQL, props.TableName, aux, aux2);

            return c;
        }

        private ConfigStatement getConfigUpdateFields(object registry, params FieldValue[] values)
        {
            ConfigStatement c;
            List<StatementParameter> parameters;
            List<StatementParameter> parameters_pk;
            List<string> fields;
            List<string> fields_pk;
            TableAttribute table_att;
            FieldAttribute att;
            string aux;
            string aux2;
            object value;

            c = new ConfigStatement() { SQL = "", Params = new List<StatementParameter>() };
            fields = new List<string>();
            fields_pk = new List<string>();
            parameters = new List<StatementParameter>();
            parameters_pk = new List<StatementParameter>();

            //Get table attribute from class
            var props = Utils.getProperties(registry);

            //Get fields in SET section from properties
            var item_count = 0;
            foreach (var item in props.Properties)
            {
                //Create SQL parameters
                att = props.DBFieldAttributes[item_count];
                item_count++;
                if (att != null)
                {
                    FieldValue f;
                    if (!string.IsNullOrWhiteSpace((f = values.Where(reg => reg.PropertyName.Equals(item.Name)).FirstOrDefault()).PropertyName))
                    {
                        value = f.Value;

                        ////Check if value por int represent NULL value
                        if (att.AllowNull)
                        {
                            if (this.isNull(att, value))
                            {
                                value = DBNull.Value;
                            }
                        }

                        if (!att.IsPrimaryKey)
                        {
                            parameters.Add(new StatementParameter("@arg" + fields.Count, att.Type, value));
                            if (this.sgbd != DBMSType.SQLite)
                            {
                                fields.Add(props.TableName + "." + att.FieldName);
                            }
                            else
                            {
                                fields.Add(att.FieldName);
                            }
                        }

                        if (!att.AllowNull)
                        {
                            if (value == null || value == DBNull.Value)
                            {
                                throw new Exception("Field '" + att.FieldName + "' not support NULL value");
                            }
                        }
                    }
                }
            }

            if (fields.Count == 0)
            {
                throw new Exception("Not exists values for finish this operation");
            }

            //Get fields in WHERE section from properties
            item_count = 0;
            foreach (var item in props.Properties)
            {
                //Create SQL parameters
                att = props.DBFieldAttributes[item_count];
                item_count++;
                if (att != null)
                {
                    if (att.IsPrimaryKey)
                    {
                        value = item.GetValue(registry);

                        //Check if value por int represent NULL value
                        if (att.AllowNull)
                        {
                            if (this.isNull(att, value))
                            {
                                value = DBNull.Value;
                            }
                        }

                        parameters_pk.Add(new StatementParameter("@pk" + fields_pk.Count, att.Type, value));
                        if (this.sgbd != DBMSType.SQLite)
                        {
                            fields_pk.Add(props.TableName + "." + att.FieldName);
                        }
                        else
                        {
                            fields_pk.Add(att.FieldName);
                        }

                        if (!att.AllowNull)
                        {
                            if (value == null || value == DBNull.Value)
                            {
                                throw new Exception("Field '" + att.FieldName + "' not support NULL value");
                            }
                        }
                    }
                }
            }

            c.SQL = "UPDATE {0} SET {1} WHERE 1 = 1 {2}";

            //Build SET section in UPDATE statement
            aux = "";
            for (int i = 0; i < fields.Count; i++)
            {
                aux += fields[i] + " = " + parameters[i].Nombre;

                if (i < fields.Count - 1)
                {
                    aux += ", ";
                }

                c.Params.Add(parameters[i]);
            }

            //Build WHERE section in UPDATE statement
            aux2 = "";
            for (int i = 0; i < fields_pk.Count; i++)
            {
                aux2 += " AND " + fields_pk[i] + " = " + parameters_pk[i].Nombre;

                c.Params.Add(parameters_pk[i]);
            }

            if (string.IsNullOrEmpty(aux2))
            {
                throw new Exception("This query can not be performed without primary keys");
            }

            c.SQL = string.Format(c.SQL, props.TableName, aux, aux2);

            return c;
        }

        private void updateFirstAutoincrement(object registry, int value)
        {
            FieldAttribute att;

            foreach (PropertyInfo item in registry.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                att = item.GetCustomAttribute<FieldAttribute>();

                if (att != null)
                {
                    if (att.IsAutoincrement && (att.Type == ParamType.Int16 || att.Type == ParamType.Int32 || att.Type == ParamType.Int64))
                    {
                        item.SetValue(registry, value);

                        break;
                    }
                }
            }
        }

        public async Task<bool> IsEmptyAsync<T>() where T : class, new()
        {
            T t;
            TableAttribute table_att;
            string sql;

            t = new T();

            //Get table attribute from class
            table_att = t.GetType().GetTypeInfo().GetCustomAttribute<TableAttribute>();
            sql = "SELECT count(*) AS num FROM {0}";
            sql = string.Format(sql, table_att.Name);

            await this.Semaphore.WaitAsync();
            try
            {
                var data = await this.executeAsync(sql);
                if (data.next())
                {
                    return data.getInt32("num") == 0;
                }
                throw new DataBaseLogicException(DataBaseLogicException.E_GENERIC, "The operaration has no returned data: " + sql);
            }
            finally
            {
                this.Semaphore.Release();
            }
        }

        public async Task<bool> emptyTableAsync<T>() where T : class, new()
        {
            T t;
            TableAttribute table_att;
            string sql;
            bool result;

            t = new T();

            //Get table attribute from class
            table_att = t.GetType().GetTypeInfo().GetCustomAttribute<TableAttribute>();
            sql = "DELETE FROM {0} WHERE 1 = 1";
            sql = string.Format(sql, table_att.Name);

            await this.Semaphore.WaitAsync();
            try
            {
                result = await this.executeNonQueryAsync(sql) > 0;
            }
            finally
            {
                this.Semaphore.Release();
            }

            return result;
        }

        public bool emptyTable<T>() where T : class, new()
        {
            return this.emptyTableAsync<T>().Result;
        }

        public bool load<T>(T registry) where T : class, new()
        {
            return this.loadAsync<T>(registry).Result;
        }

        public async Task<bool> loadAsync<T>(T registry) where T : class, new()
        {
            ConfigStatement c;
            bool result;

            await this.Semaphore.WaitAsync();
            try
            {
                c = this.getConfigSelect(registry);

                this.addParameters(c.Params);

                using (SQLData d = await this.executeAsync(c.SQL))
                {
                    if (d.next())
                    {
                        d.fill<T>(registry);
                        result = true;
                    }
                    else
                    {
                        result = false;
                    }
                }
            }
            finally
            {
                this.Semaphore.Release();
            }

            return result;
        }

        private string addAND(string sql)
        {
            if (!string.IsNullOrWhiteSpace(sql))
            {
                if (!sql.TrimStart().StartsWith("AND"))
                {
                    return " AND " + sql;
                }
            }

            return sql;
        }

        public List<T> select<T>(string sql = "", string orderby = "", List<StatementParameter> parameters = null) where T : class, new()
        {
            return this.selectAsync<T>(sql, orderby, parameters).Result;
        }

        public async Task<List<T>> selectAsync<T>(string sql = "", string orderby = "", List<StatementParameter> parameters = null, int limit_to = 0, int limit_length = 0) where T : class, new()
        {
            return await this.selectAsync<T>(new StatementOptions
            {
                SQL = sql,
                OrderBy = orderby,
                Parameters = parameters,
                LimitLength = limit_length,
                LimitTo = limit_to
            });
        }

        public async Task<List<T>> selectAsync<T>(StatementOptions options) where T : class, new()
        {
            ConfigStatement c;
            List<T> result;

            if (options.SQL == null)
            {
                options.SQL = "";
            }

            await this.Semaphore.WaitAsync();
            try
            {
                c = this.getConfigSelect(new T(), true, options.Filters, options.Orders);

                if (options.Parameters != null)
                {
                    this.addParameters(options.Parameters);
                }

                if (c.Params != null)
                {
                    this.addParameters(c.Params);
                }

                using (SQLData d = await this.executeAsync(Utils.buildSQLStatement(this.sgbd, c.SELECT, options.Filters == null ? options.SQL : c.WHERE, options.Orders == null ? options.OrderBy : c.ORDERBY, options.LimitTo, options.LimitLength)))
                {
                    result = d.fillToList<T>();
                }
            }
            finally
            {
                this.Semaphore.Release();
            }

            return result;
        }

        public async Task<T> FirstIfExistsAsync<T>(string sql = "", string orderby = "", List<StatementParameter> parameters = null) where T : class, new()
        {
            return await this.FirstIfExistsAsync<T>(new StatementOptions
            {
                Parameters = parameters,
                SQL = sql,
                OrderBy = orderby
            });
        }

        public async Task<T> FirstIfExistsAsync<T>(StatementOptions options) where T : class, new()
        {
            ConfigStatement c;
            T result;

            Utils.getProperties<T>();

            if (options.SQL == null)
            {
                options.SQL = "";
            }
            await this.Semaphore.WaitAsync();
            try
            {
                c = this.getConfigSelect(new T(), true, options.Filters, options.Orders);

                if (options.Parameters != null)
                {
                    this.addParameters(options.Parameters);
                }

                if (c.Params != null)
                {
                    this.addParameters(c.Params);
                }

                using (SQLData d = await this.executeAsync(Utils.buildSQLStatement(this.sgbd, c.SELECT, options.Filters == null ? options.SQL : c.WHERE, options.Orders == null ? options.OrderBy : c.ORDERBY)))
                {

                    if (d.next())
                    {
                        result = new T();
                        result = d.fill<T>(result);
                    }
                    else
                    {
                        result = null;
                    }
                }
            }
            finally
            {
                this.Semaphore.Release();
            }

            return result;
        }

        //public async Task<T> firstAsync<T>(string sql = "", string orderby = "", List<StatementParameter> parameters = null) where T : class, new()
        //{
        //    ConfigStatement c;
        //    T result;

        //    if (sql == null)
        //    {
        //        sql = "";
        //    }

        //    await this.Semaphore.WaitAsync();
        //    try
        //    {
        //        c = this.getConfigSelect(new T(), true);

        //        if (parameters != null)
        //        {
        //            this.addParameters(parameters);
        //        }

        //        using (SQLData d = await this.executeAsync(c.SELECT + " " + this.addAND(sql) + " LIMIT 1"))
        //        {
        //            if (d.next())
        //            {
        //                result = d.fill<T>();
        //            }
        //            else
        //            {
        //                result = null;
        //            }
        //        }
        //    }
        //    finally
        //    {
        //        this.Semaphore.Release();
        //    }

        //    return result;
        //}

        public async Task<int> countAsync<T>(string sql = "", List<StatementParameter> parameters = null) where T : class, new()
        {
            ConfigStatement c;

            if (sql == null)
            {
                sql = "";
            }

            await this.Semaphore.WaitAsync();
            try
            {
                c = this.getConfigCount(new T());

                if (parameters != null)
                {
                    this.addParameters(parameters);
                }

                using (SQLData d = await this.executeAsync(c.SELECT + " " + this.addAND(sql) + " LIMIT 1"))
                {
                    d.next();
                    return d.getInt32("records");
                }
            }
            finally
            {
                this.Semaphore.Release();
            }
        }

        public bool insert(object registry)
        {
            return this.insertAsync(registry).Result;
        }

        public async Task<bool> insertAsync(object registry)
        {
            ConfigStatement c;
            bool result;

            await this.Semaphore.WaitAsync();
            try
            {
                c = this.getConfigInsert(registry);

                this.addParameters(c.Params);
                result = await this.executeNonQueryAsync(c.SQL) > 0;

                if (result)
                {
                    this.updateFirstAutoincrement(registry, this.lastID);
                }
            }
            finally
            {
                this.Semaphore.Release();
            }

            return result;
        }

        public bool update(object registry)
        {
            return this.updateAsync(registry).Result;
        }

        public async Task<bool> updateAsync(object registry, bool syncAllFields = true)
        {
            ConfigStatement c;
            bool result;
            int records;

            await this.Semaphore.WaitAsync();
            try
            {
                c = this.getConfigUpdate(registry, syncAllFields);

                this.addParameters(c.Params);
                records = await this.executeNonQueryAsync(c.SQL);
            }
            finally
            {
                this.Semaphore.Release();
            }

            result = records > 0;

            return result;
        }

        public bool delete(object registry)
        {
            return this.deleteAsync(registry).Result;
        }

        public async Task<bool> deleteAsync(object registry)
        {
            ConfigStatement c;
            bool result;

            await this.Semaphore.WaitAsync();
            try
            {
                c = this.getConfigDelete(registry);

                this.addParameters(c.Params);
                result = await this.executeNonQueryAsync(c.SQL) > 0;
            }
            finally
            {
                this.Semaphore.Release();
            }

            return result;
        }

        private bool isEqual(ParamType t, object obj1, object obj2)
        {
            bool result = false;

            if (obj1 != null && obj2 != null)
            {
                if (t == ParamType.Boolean)
                {
                    result = (bool)obj1 == (bool)obj2;
                }
                else if (t == ParamType.DateTime)
                {
                    result = (DateTime)obj1 == (DateTime)obj2;
                }
                else if (t == ParamType.Decimal)
                {
                    result = (decimal)obj1 == (decimal)obj2;
                }
                else if (t == ParamType.Int16)
                {
                    result = (Int16)obj1 == (Int16)obj2;
                }
                else if (t == ParamType.Int32)
                {
                    result = (Int32)obj1 == (Int32)obj2;
                }
                else if (t == ParamType.Int64)
                {
                    result = (Int64)obj1 == (Int64)obj2;
                }
                else if (t == ParamType.String)
                {
                    result = (string)obj1 == (string)obj2;
                }
                else if (t == ParamType.ByteArray)
                {
                    result = ((Byte)obj1).Equals((Byte)obj2);
                }
                else
                {
                    throw new Exception("Comparision value is not valid");
                }
            }
            else
            {
                result = obj1 == null && obj2 == null;
            }

            return result;
        }

        private bool checkIfSameForeignKey<T>(List<PropertyInfo> fks, List<T> l)
        {
            bool result;
            object[] fk_in_zero_pos = new object[fks.Count];
            var fk_attribs = new FieldAttribute[fks.Count];

            result = true;

            //load foreign key in firest row
            for (int i = 0; i < fks.Count; i++)
            {
                fk_in_zero_pos[i] = fks[i].GetValue(l[0]);
                fk_attribs[i] = fks[i].GetCustomAttribute<FieldAttribute>();
            }

            //check row at row y fk are the same
            foreach (T item in l)
            {
                for (int i = 0; i < fks.Count; i++)
                {
                    if (!(result = this.isEqual(fk_attribs[i].Type, fk_in_zero_pos[i], fks[i].GetValue(item))))
                    {
                        break;
                    }
                }

                if (!result)
                {
                    break;
                }
            }

            return result;
        }

        private bool isSameRowV2<T>(T t1, T t2, List<DBRegistryLinkData> getters)
        {
            var res = true;

            for (int i = 0; i < getters.Count; i++)
            {
                switch (getters[i].FieldAttributes.Type)
                {
                    case ParamType.Boolean:
                        var fbool = getters[i].GetterCustom;
                        res = res && ((bool)fbool(t1) == (bool)fbool(t2));
                        break;

                    case ParamType.DateTime:
                        var fdatetime = getters[i].GetterCustom;
                        res = res && ((DateTime)fdatetime(t1) == (DateTime)fdatetime(t2));
                        break;

                    case ParamType.Decimal:
                        var fdecimal = getters[i].GetterCustom;
                        res = res && ((decimal)fdecimal(t1) == (decimal)fdecimal(t2));
                        break;

                    case ParamType.Int16:
                        var fint16 = getters[i].GetterCustom;
                        res = res && ((Int16)fint16(t1) == (Int16)fint16(t2));
                        break;

                    case ParamType.Int32:
                        var fint32 = getters[i].GetterCustom;
                        res = res && ((Int32)fint32(t1) == (Int32)fint32(t2));
                        break;

                    case ParamType.Int64:
                        var fint64 = getters[i].GetterCustom;
                        res = res && ((Int64)fint64(t1) == (Int64)fint64(t2));
                        break;

                    case ParamType.String:
                        var fstring = getters[i].GetterCustom;
                        res = res && (string.Equals((string)fstring(t1), (string)fstring(t2)));
                        break;

                    default:
                        throw new Exception();
                }
            }

            return res;
        }

        public async Task<int> synchronizeListAsync<T>(SynchronizeListOptions<T> options) where T : class, ISynchronizableRow, new()
        {
            //D -> For delete
            //I or null -> For insert
            //U -> For modify
            //N -> None to do

            List<StatementParameter> parameters;
            List<T> original_rows;
            string sql;
            int count = 0;

            if (options.List.Count == 0)
            {
                return count;
            }

            //initialize updatestate by the flies
            foreach (T item in options.List)
            {
                item.UpdateState = null;
            }

            //Build statement for get original rows            
            var fks = Utils.getPropertiesInfos<T, ConstraintAttribute>(options.List[0]);

            if (!this.checkIfSameForeignKey<T>(fks, options.List))
            {
                throw new Exception("Foreign key are not the same in all rows from list");
            }

            if (fks.Count > 0)
            {
                sql = "";
                parameters = new List<StatementParameter>(fks.Count);

                foreach (PropertyInfo item in fks)
                {
                    var field = item.GetCustomAttribute<FieldAttribute>();

                    if (field != null)
                    {
                        sql += " AND " + field.FieldName + " = @var" + count.ToString();
                        parameters.Add(new StatementParameter("@var" + count.ToString(), field.Type, item.GetValue(options.List[0])));
                        count++;
                    }
                }

                //Get stored rows
                original_rows = await this.selectAsync<T>(sql, null, parameters);

                //Find in original rows register for deleting
                //var tmp = new List<T>(l);
                var tmp = new List<T>(options.List);
                var getters = Utils.getGetters<T>(true);

                foreach (ISynchronizableRow item in original_rows)
                {
                    var row_in_l = tmp.Where(
                        r => this.isSameRowV2<T>(r, (T)item, getters)
                    );

                    if (row_in_l.Count() == 0)
                    {
                        //if (delete_if_not_exists)
                        if (options.DeleteIfNotExists)
                        {
                            if (options.OnBeforeDeleteRecord != null)
                            {
                                await options.OnBeforeDeleteRecord(item as T);
                            }
                            await this.deleteAsync(item);
                            if (options.OnAfterDeleteRecord != null)
                            {
                                await options.OnAfterDeleteRecord(item as T);
                            }
                        }
                    }
                    else
                    {
                        var row = row_in_l.ElementAt(0);

                        if (row.Equals(item))
                        {
                            row.UpdateState = "N";
                        }
                        else
                        {
                            row.UpdateState = "U";
                        }

#if DELETE_ROW_COMPARED_BEHAVIOR_WHEN_SYNC
                        tmp.Remove(row);
#endif
                    }
                }

                //Insert or update rows in l
                count = 0;

                //foreach (T item in l)
                foreach (T item in options.List)
                {
                    var update_state = ((ISynchronizableRow)item).UpdateState;

                    if (update_state == "U")
                    {
                        if (options.OnBeforeUpdateRecord != null)
                        {
                            await options.OnBeforeUpdateRecord(item);
                        }
                        await this.updateAsync(item, false);
                        count++;
                        if (options.OnAfterUpdateRecord != null)
                        {
                            await options.OnAfterUpdateRecord(item);
                        }
                    }
                    else if (update_state == "I" || update_state == null)
                    {
                        if (options.OnBeforeInsertRecord != null)
                        {
                            await options.OnBeforeInsertRecord(item);
                        }
                        await this.insertAsync(item);
                        count++;
                        if (options.OnAfterInsertRecord != null)
                        {
                            await options.OnAfterInsertRecord(item);
                        }
                    }
                }
            }

            return count;
        }
    }
}
