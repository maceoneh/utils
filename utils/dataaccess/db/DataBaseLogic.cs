using es.dmoreno.utils.dataaccess.filters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace es.dmoreno.utils.dataaccess.db
{
    public class DataBaseLogic : IDisposable
    {
        static internal string createStringConnection(DBMSType type, string host, string database, string user, string password, int port)
        {
            string string_connection;

            if (type == DBMSType.MySQL)
            {
                string_connection = string.Format("Server={0};Database={1};Uid={2};Pwd={3};Port={4};Pooling=true;Encrypt=false;", host, database, user, password, port.ToString());
            }
            else if (type == DBMSType.SQLite)
            {
                string_connection = string.Format("Data Source={0};", host);
            }
            else
            {
                string_connection = null;
            }

            return string_connection;
        }

        private List<FilePerTable> FilePerTables { get; } = new List<FilePerTable>();

        private ConnectionParameters _parameters;
        private SQLStatement _connection;
        private Management _management;
        private IConnector _connector;
        private DBMSType _type;
        private string _string_connection;
        private bool _create_with_begin_transaction;

        public SQLStatement Statement
        {
            get
            {
                if (this._connection == null)
                {
                    this._connection = new SQLStatement(this._string_connection, this._type, this._connector);

                    if (this._create_with_begin_transaction)
                    {
                        this._connection.beginTransaction();
                    }
                }

                return this._connection;
            }
        }

        public Management Management
        {
            get
            {
                if (this._management == null)
                {
                    if (this._type == DBMSType.SQLite)
                    {
                        this._management = new SQLiteManagement(this.Statement);
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }

                return this._management;
            }
        }

        public DataBaseLogic(ConnectionParameters p)
        {
            this.initilize(p);
        }

        private void initilize(ConnectionParameters p)
        {
            this._connection = null;
            this._connector = p.Connector; ;
            this._type = p.Type;
            this._string_connection = DataBaseLogic.createStringConnection(p.Type, p.Type == DBMSType.SQLite ? p.File : p.Host, p.Database, p.User, p.Password, p.Port);
            this._parameters = p;
            this._create_with_begin_transaction = p.BeginTransaction;
        }

        public DataBaseLogic duplicate()
        {
            return new DataBaseLogic(this._parameters);
        }

        public async Task<SQLStatement> ProxyStatement<T>() where T : class, new()
        {
            FilePerTable info_table = null;
            //Se comprueba si la tabla va por fichero
            var table_att = (new T()).GetType().GetTypeInfo().GetCustomAttribute<TableAttribute>();
            if (table_att.FilePerTable && this._parameters.Type == DBMSType.SQLite)
            {
                //Se busca el nombre de la tabla en el listado de tablas en fichero
                foreach (var item in FilePerTables)
                {
                    if (item.Table == table_att.Name)
                    {
                        info_table = item;
                        break;
                    }
                }
                var name_file = Path.GetDirectoryName(this._parameters.File) + Path.DirectorySeparatorChar;
                if (info_table == null)
                {
                    //Si no existe referencia puede ser porque nunca se haya usado, se busca en la tabla file_per_table
                    var select_table = await this.Statement.selectAsync<FilePerTable>(new StatementOptions
                    {
                        Filters = new List<filters.Filter> {
                        new Filter { Name = FilePerTable.FilterNameFile, ObjectValue = table_att.Name, Type = FilterType.Equal }
                    }
                    });
                    if (select_table.Count > 0)
                    {
                        //Si existe se genera una nueva entrada en el listado y se devuelve un statement al la tabla
                        info_table = select_table[0];
                        FilePerTables.Add(info_table);
                    }
                    else
                    {
                        //Si no existe se devuelve un error
                        throw new FilePerTableException(FilePerTableException.FILE_NOT_EXIST, "The file referenced for table " + info_table.Table + " named " + info_table.NameFile + " does not exist");
                    }
                }
                name_file += info_table.NameFile;
                //Si todavia no ha sido accedido se genera el statemtent a esa tabla
                if (info_table.StatementToFile == null)
                {
                    info_table.StatementToFile = new SQLStatement(createStringConnection(DBMSType.SQLite, name_file, "", "", "", 0), DBMSType.SQLite);
                }
                return info_table.StatementToFile;
            }
            else
            {
                return this.Statement;
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // Para detectar llamadas redundantes

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: elimine el estado administrado (objetos administrados).
                    if (this._connection != null)
                    {
                        this._connection.Dispose();
                    }
                    //Se desconectan los statements file_per_table
                    foreach (var item in this.FilePerTables)
                    {
                        if (item.StatementToFile != null)
                        {
                            item.StatementToFile.Dispose();
                            item.StatementToFile = null;
                        }
                    }
                }

                // TODO: libere los recursos no administrados (objetos no administrados) y reemplace el siguiente finalizador.
                // TODO: configure los campos grandes en nulos.

                disposedValue = true;
            }
        }

        // TODO: reemplace un finalizador solo si el anterior Dispose(bool disposing) tiene código para liberar los recursos no administrados.
        // ~DataBaseLogic() {
        //   // No cambie este código. Coloque el código de limpieza en el anterior Dispose(colocación de bool).
        //   Dispose(false);
        // }

        // Este código se agrega para implementar correctamente el patrón descartable.
        public void Dispose()
        {
            // No cambie este código. Coloque el código de limpieza en el anterior Dispose(colocación de bool).
            Dispose(true);
            // TODO: quite la marca de comentario de la siguiente línea si el finalizador se ha reemplazado antes.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
