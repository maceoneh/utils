using es.dmoreno.utils.dataaccess.db;
using es.dmoreno.utils.serialize;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace es.dmoreno.utils.apps.masterrecords
{
    public class MasterRecordsTable<T> where T : MasterRecord, new()
    {
        private ConnectionParameters _params;

        protected MasterRecordsTable(ConnectionParameters param)
        {
            this._params = param;
        }

        public void createDBSchema()
        {
            using (DataBaseLogic db = new DataBaseLogic(this._params))
            {
                db.Management.createAlterTableAsync<T>();

                if (this._params.BeginTransaction)
                {
                    db.Statement.acceptTransaction();
                }
            }
        }

        public async Task<T> getAsync(string key, bool initialize_if_no_exists = false)
        {
            T v;

            using (DataBaseLogic db = new DataBaseLogic(this._params))
            {
                v = await db.Statement.FirstIfExistsAsync<T>(" AND key = @k", null, new List<StatementParameter>() {
                    new StatementParameter("@k", ParamType.String, key)
                });
                if (v == null)
                {
                    if (initialize_if_no_exists)
                    {
                        var p = new T() { Key = key, Value = "" };
                        await db.Statement.insertAsync(p);
                        return p;
                    }
                }
            }

            return v;
        }

        public async Task<T> InitializeAndGetAsync(string key)
        {
            T v;

            using (DataBaseLogic db = new DataBaseLogic(this._params))
            {
                v = await db.Statement.FirstIfExistsAsync<T>(" AND key = @k", null, new List<StatementParameter>() {
                    new StatementParameter("@k", ParamType.String, key)
                });
                if (v == null)
                {
                    v = new T { Key = key, Value = "" };
                    await db.Statement.insertAsync(v);
                }
            }

            return v;
        }

        public async Task<T> getAsync(int id)
        {
            T v;

            using (DataBaseLogic db = new DataBaseLogic(this._params))
            {
                v = await db.Statement.FirstIfExistsAsync<T>(" AND id = " + id.ToString());
            }

            return v;
        }

        public async Task<List<T>> getList()
        {
            using (DataBaseLogic db = new DataBaseLogic(this._params))
            {
                return await db.Statement.selectAsync<T>();
            }
        }

        public async Task setAsync(T record)
        {
            using (DataBaseLogic db = new DataBaseLogic(this._params))
            {
                var register = await db.Statement.FirstIfExistsAsync<T>(" AND id = @id", null, new List<StatementParameter>() {
                    new StatementParameter("@id", ParamType.Int32, record.ID)
                });

                if (register == null)
                {
                    await db.Statement.insertAsync(record);
                    record.ID = db.Statement.lastID;
                }
                else
                {
                    await db.Statement.updateAsync(record);
                }

                if (this._params.BeginTransaction)
                {
                    db.Statement.acceptTransaction();
                }
            }
        }
    }
}
