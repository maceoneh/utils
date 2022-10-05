using es.dmoreno.utils.dataaccess.db;
using es.dmoreno.utils.dataaccess.filters;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UtilsDB = es.dmoreno.utils.dataaccess.db.Utils;

namespace es.dmoreno.utils.permissions
{
    public class Permissions : IDisposable
    {
        private class TableInfo
        {
            public string Name { get; set; }
            public string FileName { get; set; }
        }

        private bool disposedValue;

        private DataBaseLogic DBLogic { get; set; } = null;

        private string _Path { get; set; }

        public Permissions(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                path = "." + Path.PathSeparator;
            }
            var permissions_directory = Path.Combine(path, "permissions");
            if (!Directory.Exists(permissions_directory))
            {
                Directory.CreateDirectory(permissions_directory);
            }
            this._Path = permissions_directory;
            var db_file = Path.Combine(permissions_directory, "permissions.db");
            this.DBLogic = new DataBaseLogic(new ConnectionParameters
            {
                Type = DBMSType.SQLite,
                File = db_file
            });
        }

        public async Task BuildAsync()
        {
            await this.DBLogic.Management.createAlterTableAsync<DTOEntity>();
            await this.DBLogic.Management.createAlterTableAsync<DTOAction>();
            await this.DBLogic.Management.createAlterTableAsync<DTOPermission>();
            await this.DBLogic.Management.createAlterTableAsync<DTOSubjectHasPermission>();
            await this.DBLogic.Management.createAlterTableAsync<DTOGroup>();
            await this.DBLogic.Management.createAlterTableAsync<DTOSubjectPertainGroup>();
            await this.DBLogic.Management.createAlterTableAsync<DTOTableWithPermission>();
            await this.DBLogic.Management.createAlterTableAsync<DTORecordPermission>();
        }

        public async Task<DTOEntity> AddEntityAsync(string name)
        {
            name = name.ToUpper().Trim();
            var db_entity = await this.DBLogic.ProxyStatement<DTOEntity>();
            var entity_in_db = await db_entity.FirstIfExistsAsync<DTOEntity>(new StatementOptions
            {
                Filters = new List<Filter> {
                    new Filter { Name = DTOEntity.FilterName, ObjectValue = name, Type = FilterType.Equal }
                }
            });
            if (entity_in_db == null)
            {
                entity_in_db = new DTOEntity { Name = name };
                await db_entity.insertAsync(entity_in_db);
            }
            return entity_in_db;
        }

        public async Task<DTOAction> AddActionAsync(string name)
        {
            name = name.ToUpper().Trim();
            var db_action = await this.DBLogic.ProxyStatement<DTOAction>();
            var action_in_db = await db_action.FirstIfExistsAsync<DTOAction>(new StatementOptions
            {
                Filters = new List<Filter> {
                    new Filter { Name = DTOAction.FilterName, ObjectValue = name, Type = FilterType.Equal }
                }
            });
            if (action_in_db == null)
            {
                action_in_db = new DTOAction { Name = name };
                await db_action.insertAsync(action_in_db);
            }
            return action_in_db;
        }

        public async Task<DTOGroup> AddGroupAsync(string name, string uuid)
        {
            name = name.ToUpper().Trim();
            var db_group = await this.DBLogic.ProxyStatement<DTOGroup>();
            var action_in_db = await db_group.FirstIfExistsAsync<DTOGroup>(new StatementOptions
            {
                Filters = new List<Filter> {
                    new Filter { Name = DTOGroup.FilterRemoteUUID, ObjectValue = uuid, Type = FilterType.Equal }
                }
            });
            if (action_in_db == null)
            {
                action_in_db = new DTOGroup { Name = name, RemoteUUID = uuid };
                await db_group.insertAsync(action_in_db);
            }
            return action_in_db;
        }

        public async Task<DTOPermission> AddPermissionAsync(DTOEntity e, DTOAction a, string description)
        {
            description = description.ToUpper().Trim();
            var db_permisions = await this.DBLogic.ProxyStatement<DTOPermission>();
            var permision_in_db = await db_permisions.FirstIfExistsAsync<DTOPermission>(new StatementOptions
            {
                Filters = new List<Filter> {
                    new Filter { Name =  DTOPermission.FilterRefEntity, ObjectValue = e.ID, Type = FilterType.Equal },
                    new Filter { Name =  DTOPermission.FilterRefAction, ObjectValue = a.ID, Type = FilterType.Equal }
                }
            });
            if (permision_in_db == null)
            {
                permision_in_db = new DTOPermission
                {
                    Action = a.CopyTo(new DTOAction()),
                    RefAction = a.ID,
                    Entity = e.CopyTo(new DTOEntity()),
                    RefEntity = e.ID,
                    Description = description
                };
                await db_permisions.insertAsync(permision_in_db);
            }
            else
            {
                permision_in_db.Action = a.CopyTo(new DTOAction());
                permision_in_db.Entity = e.CopyTo(new DTOEntity());
            }
            return permision_in_db;
        }

        public async Task<DTOSubjectHasPermission> AddSubjectToPermissionAsync(DTOPermission p, string remote_uuid)
        {
            var db_subject_has_permission = await this.DBLogic.ProxyStatement<DTOSubjectHasPermission>();
            var subject_permission = await db_subject_has_permission.FirstIfExistsAsync<DTOSubjectHasPermission>(new StatementOptions
            {
                Filters = new List<Filter> {
                    new Filter { Name = DTOSubjectHasPermission.FilterRemoteUUID, ObjectValue = remote_uuid, Type = FilterType.Equal },
                    new Filter { Name = DTOSubjectHasPermission.FilterRefEntity, ObjectValue = p.RefEntity, Type = FilterType.Equal },
                    new Filter { Name = DTOSubjectHasPermission.FilterRefAction, ObjectValue = p.RefAction, Type = FilterType.Equal }
                }
            });
            if (subject_permission == null)
            {
                subject_permission = new DTOSubjectHasPermission
                {
                    Permission = p.CopyTo(new DTOPermission()),
                    RefAction = p.RefAction,
                    RefEntity = p.RefEntity,
                    RemoteUUID = remote_uuid
                };
                await db_subject_has_permission.insertAsync(subject_permission);
            }
            else
            {
                subject_permission.Permission = p.CopyTo(new DTOPermission());
            }
            return subject_permission;
        }

        public async Task<bool> AddSubjectToGroupAsync(string remote_uuid, DTOGroup g)
        {
            var db_subject_pertain_group = await this.DBLogic.ProxyStatement<DTOSubjectPertainGroup>();
            var subject_pertain_group = await db_subject_pertain_group.FirstIfExistsAsync<DTOSubjectPertainGroup>(new StatementOptions
            {
                Filters = new List<Filter> {
                    new Filter { Name = DTOSubjectPertainGroup.FilterRefGroup, ObjectValue = g.ID,  Type = FilterType.Equal },
                    new Filter { Name = DTOSubjectPertainGroup.FilterRemoteUUID, ObjectValue = remote_uuid,  Type = FilterType.Equal }
                }
            });
            if (subject_pertain_group == null)
            {
                return await db_subject_pertain_group.insertAsync(new DTOSubjectPertainGroup
                {
                    RefGroup = g.ID,
                    RemoteUUID = remote_uuid
                });
            }

            return true;
        }

        public async Task<List<DTOPermission>> GetPermisssionsAsync(string uuid)
        {
            var permissions = new List<DTOPermission>();
            //Se obtienen maestros
            var db_actions = await this.DBLogic.ProxyStatement<DTOAction>();
            var actions = await db_actions.selectAsync<DTOAction>();
            var db_entities = await this.DBLogic.ProxyStatement<DTOEntity>();
            var entities = await db_entities.selectAsync<DTOEntity>();
            var db_permission = await this.DBLogic.ProxyStatement<DTOPermission>();
            var permission = await db_permission.selectAsync<DTOPermission>();
            var db_groups = await this.DBLogic.ProxyStatement<DTOGroup>();
            var groups = await db_groups.selectAsync<DTOGroup>();
            //Se obtienen los grupos a los que pertenece
            var db_subject_pertain_group = await this.DBLogic.ProxyStatement<DTOSubjectPertainGroup>();
            var subject_pertain_groups = await db_subject_pertain_group.selectAsync<DTOSubjectPertainGroup>(new StatementOptions
            {
                Filters = new List<Filter> {
                    new Filter { Name = DTOSubjectPertainGroup.FilterRemoteUUID, ObjectValue = uuid, Type = FilterType.Equal }
                }
            });
            //Se obtiene el uuid de los grupos
            var uuids = new List<string>();
            foreach (var item in subject_pertain_groups)
            {
                uuids.Add(groups.Where(reg => reg.ID == item.RefGroup).FirstOrDefault().RemoteUUID);
            }
            uuids.Add(uuid);
            //Se obtienen los permisos a los que pertenece
            var db_permisions = await this.DBLogic.ProxyStatement<DTOSubjectHasPermission>();
            var group_permissions = await db_permisions.selectAsync<DTOSubjectHasPermission>(new StatementOptions
            {
                Filters = new List<Filter> {
                    new Filter { Name = DTOSubjectHasPermission.FilterRemoteUUID, ObjectValue = uuids, Type = FilterType.In }
                }
            });
            //Se agregan a la respuesta los permisos admitidos
            foreach (var item in group_permissions)
            {
                if (permissions.Where(reg => reg.RefAction == item.RefAction && reg.RefEntity == item.RefEntity).Count() == 0)
                {
                    var p = new DTOPermission
                    {
                        RefAction = item.RefAction,
                        RefEntity = item.RefEntity
                    };
                    p.Description = permission.Where(reg => reg.RefEntity == item.RefEntity && reg.RefAction == item.RefAction).FirstOrDefault().Description;
                    p.Action = actions.Where(reg => reg.ID == item.RefAction).FirstOrDefault().CopyTo(new DTOAction());
                    p.Entity = entities.Where(reg => reg.ID == item.RefEntity).FirstOrDefault().CopyTo(new DTOEntity());
                    permissions.Add(p);
                }
            }

            return permissions;
        }

        public async Task<bool> CreateTablePermissions<T>() where T : class, new()
        {
            var t = new T();
            if (!(t is IDataPermission))
            {
                throw new Exception(t.GetType().Name + " must follow the IDataPermission interface");
            }
            TableInfo tableInfo = new TableInfo();
            var table_att = t.GetType().GetTypeInfo().GetCustomAttribute<TableAttribute>();
            tableInfo.FileName = "data_permission_" + table_att.Name + ".db";
            tableInfo.Name = table_att.Name;
            var create = false;
            if (this.DBLogic.Statement.Type == DBMSType.SQLite)
            {
                //create = await this.SQLiteCreateTablePermissions<T>(tableInfo);
                using (var aux_db = new DataBaseLogic(new ConnectionParameters { Type = DBMSType.SQLite, File = Path.Combine(this._Path, tableInfo.FileName) }))
                {
                    create = await aux_db.Management.createAlterTableAsync<DTODataPermission>();
                }
            }
            else
            {
                throw new NotImplementedException("Only support SQLite engine");
            }

            if (create)
            {
                var db_table_permisions = await this.DBLogic.ProxyStatement<DTOTableWithPermission>();
                var table_permission = await db_table_permisions.FirstIfExistsAsync<DTOTableWithPermission>(new StatementOptions
                {
                    Filters = new List<Filter> {
                        new Filter { Name = DTOTableWithPermission.FilterName, ObjectValue = tableInfo.Name, Type = FilterType.Equal }
                    }
                });
                if (table_permission != null)
                {
                    table_permission.File = tableInfo.FileName;
                    await db_table_permisions.updateAsync(table_permission);
                }
                else
                {
                    table_permission = new DTOTableWithPermission { File = tableInfo.FileName, Name = tableInfo.Name };
                    await db_table_permisions.insertAsync(table_permission);
                }
            }
            return create;
        }

        //private async Task<bool> SQLiteCreateTablePermissions<T>(TableInfo info) where T : class, new()
        //{

        //var t = new T();
        ////Se obtienen los datos de la tabla
        //var table_att = t.GetType().GetTypeInfo().GetCustomAttribute<TableAttribute>();
        ////Se crea una conexion a la base de datos con el sufijo _permissions
        //var table_file = table_att.Name + "_permissions";
        //var db_path = Path.Combine(this._Path, table_file + ".db");
        //using (var db = new DataBaseLogic(new ConnectionParameters { Type = DBMSType.SQLite, File = db_path }))
        //{
        //    var table_exists = false;
        //    //Se comprueba si existe la tabla
        //    var sql = "SELECT * FROM " + table_att.Name + " LIMIT 1";
        //    try
        //    {
        //        await db.Statement.executeAsync(sql);
        //        table_exists = true;
        //    }
        //    catch (SqliteException ex)
        //    {
        //        //Microsoft.Data.Sqlite.SqliteException: 'SQLite Error 1: 'no such table: messages_permissions'.'
        //        if (ex.SqliteErrorCode == 1)
        //        {
        //            if (!ex.Message.StartsWith("SQLite Error 1: 'no such table: "))
        //            {
        //                throw;
        //            }
        //        }
        //        else
        //        {
        //            throw;
        //        }
        //    }
        //    //Si la tabla no existe se crea con los campos de clave primaria y referencia al permiso
        //    if (!table_exists)
        //    {
        //        sql = "CREATE TABLE " + table_att.Name + " (";
        //        //Se obtienen las PK de la clase
        //        var pks = UtilsDB.getFieldAttributes(UtilsDB.getPropertyInfos<T>(t, true)).Where(a => a.IsPrimaryKey).ToList();
        //        //Se desactiva el autoincrement
        //        foreach (var item in pks)
        //        {
        //            item.IsAutoincrement = false;
        //        }
        //        if (pks.Count == 1)
        //        {
        //            sql += " " + SQLiteManagement.getCreateFieldSQLite(db.Statement, pks[0], true);
        //        }
        //        else
        //        {
        //            for (int i = 0; i < pks.Count; i++)
        //            {
        //                sql += SQLiteManagement.getCreateFieldSQLite(db.Statement, pks[i], false) + ", ";
        //            }

        //            sql += " PRIMARY KEY (";
        //            for (int i = 0; i < pks.Count; i++)
        //            {
        //                sql += pks[i].FieldName;

        //                if (i < pks.Count - 1)
        //                {
        //                    sql += ", ";
        //                }
        //            }
        //            sql += ")";
        //        }
        //        //Se agrega el campo de referencia al permiso
        //        sql += ", ref_permission INTEGER)";
        //        //Se lanza la consulta para crear la table
        //        await db.Statement.executeNonQueryAsync(sql);
        //        info.Name = table_att.Name;
        //        info.FileName = table_file + ".db";
        //        return true;
        //    }
        //    else
        //    {
        //        return false;
        //    }
        //}
        //}

        /// <summary>
        /// Obtiene el ID del permiso que se indica
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        private async Task<int> GetIDPermissionAsync(DTORecordPermission p)
        {
            var db_record_permisions = await this.DBLogic.ProxyStatement<DTORecordPermission>();
            var list = await db_record_permisions.selectAsync<DTORecordPermission>();
            var permission = list.Where(reg => reg.Equals(p)).FirstOrDefault();
            if (permission == null)
            {
                return int.MinValue;
            }
            else
            {
                return permission.ID;
            }
        }



        public async Task AddPermissionAsync<T>(T registry, DTORecordPermission p) where T : class, new()
        {
            if (!(registry is IDataPermission))
            {
                throw new Exception(registry.GetType().Name + " must follow the IDataPermission interface");
            }
            //Se obtiene le nombre de la tabla para comprobar si tiene o no tabla de permisos
            var table_att = registry.GetType().GetTypeInfo().GetCustomAttribute<TableAttribute>();
            //Se comprueba si la entidad tiene que trabajar con permisos
            var db_permission_table = await this.DBLogic.ProxyStatement<DTOTableWithPermission>();
            var table_info = await this.GetFilenameFromTablePermission(table_att.Name);
            if (table_info != null)
            {
                //Se obtiene el permiso, si no existe se crea
                p.Entity = table_att.Name;
                p.ID = await this.GetIDPermissionAsync(p);
                if (p.ID == int.MinValue)
                {
                    var db_record_permissions = await this.DBLogic.ProxyStatement<DTORecordPermission>();
                    await db_record_permissions.insertAsync(p);
                }
                //Se comprueba si el registro tiene ya asociado un permiso
                using (var db = new DataBaseLogic(new ConnectionParameters { Type = DBMSType.SQLite, File = Path.Combine(this._Path, table_info.File) }))
                {
                    var id_record = (registry as IDataPermission).IDRecord;
                    var data_permission = await db.Statement.FirstIfExistsAsync<DTODataPermission>(new StatementOptions
                    {
                        Filters = new List<Filter> {
                            new Filter { Name = DTODataPermission.FilterIdentityRecord, ObjectValue = id_record, Type = FilterType.Equal }
                        }
                    });
                    if (data_permission == null)
                    {
                        await db.Statement.insertAsync(new DTODataPermission
                        {
                            RefPermission = p.ID,
                            IdentityRecord = id_record
                        });
                    }
                    else
                    {
                        data_permission.RefPermission = p.ID;
                        await db.Statement.updateAsync(data_permission);
                    }
                }
            }
        }

        //internal class PermissionsGroup<T>
        //{
        //    public int RefPermission { get; set; }
        //    public List<T> List { get; set; }
        //};

        //private async Task<List<PermissionsGroup<T>>> GetPermissionsTable<T>() where T : class, new()
        //{
        //    var t = new T();
        //    var pks = UtilsDB.getGetters<T>(true);

        //    return null;
        //}

        /// <summary>
        /// Devuelve el nombre del fichero donde se guardan los permisos de los registros de una entidad
        /// </summary>
        /// <param name="tablename"></param>
        /// <returns></returns>
        private async Task<DTOTableWithPermission> GetFilenameFromTablePermission(string tablename)
        {
            var db_permission_table = await this.DBLogic.ProxyStatement<DTOTableWithPermission>();
            var table_info = await db_permission_table.FirstIfExistsAsync<DTOTableWithPermission>(new StatementOptions
            {
                Filters = new List<Filter> {
                    new Filter { Name = DTOTableWithPermission.FilterName, ObjectValue = tablename }
                }
            });
            if (table_info == null)
            {
                return null;
            }
            else
            {
                return table_info;
            }
        }

        /// <summary>
        /// Elimina los registros de una lista de los que el usuario indicado no tiene permisos de visualizacion
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list">Lista a filtrar</param>
        /// <param name="UUID">UUID del usuario o grupo que hace la consulta</param>
        /// <param name="limit">Establece cada cuantos registros comprueba en la base de datos</param>
        /// <returns></returns>
        public async Task<List<T>> FilterByPermission<T>(List<T> list, string UUID, int limit = 100) where T : class, new()
        {
            //Si la lista viene vacia no se hace nada
            if (list == null)
            {
                return list;
            }
            if (list.Count == 0)
            {
                return list;
            }
            //Se obtienen detalles del tipo de datos del listado
            var first_reg = list[0];
            var table_att = first_reg.GetType().GetTypeInfo().GetCustomAttribute<TableAttribute>();
            //Se comprueba si existe tabla con permisos,si no existe se devuelve toda la lista
            var table_record_permissions = await this.GetFilenameFromTablePermission(table_att.Name);
            if (table_record_permissions == null)
            {
                return list;
            }
            //Se obtienen los permisos relacionados con el UUID
            var db_record_permisions = await this.DBLogic.ProxyStatement<DTORecordPermission>();
            var list_permissions = (await db_record_permisions.selectAsync<DTORecordPermission>()).Where(reg => reg.Entity == table_att.Name).ToList();
            if (list_permissions.Count == 0) //Si no existen permisos establecidos se devuelve toda la lista
            {
                return list;
            }
            //Se obtienen los permisos que lo tienen como owner
            var owners = list_permissions.Where(reg => reg.Entity == UUID).ToList();
            //Se obtienen los permisos que lo referencian con permiso de lectura
            var can_read_permission = list_permissions.Where(reg => reg.UUIDRecordPermissions.Where(record => record.UUID == UUID && record.CanRead == true).Count() > 0).ToList();
            //Se obtienen los permisos de los grupos a los que pertenece            
            var db_groups = await this.DBLogic.ProxyStatement<DTOGroup>();
            var list_groups = await db_groups.selectAsync<DTOGroup>();
            var db_subject_pertain_group = await this.DBLogic.ProxyStatement<DTOSubjectPertainGroup>();
            var list_subject_pertain_group = await db_subject_pertain_group.selectAsync<DTOSubjectPertainGroup>(new StatementOptions
            {
                Filters = new List<Filter>() {
                    new Filter { Name = DTOSubjectPertainGroup.FilterRefGroup, ObjectValue = UUID, Type = FilterType.Equal }
                }
            });
            var groups = new List<string>();
            foreach (var item in list_subject_pertain_group)
            {
                groups.Add(list_groups.Where(reg => reg.ID == item.RefGroup).FirstOrDefault().RemoteUUID);
            }
            var groups_can_read_permission = list_permissions.Where(reg => reg.UUIDRecordPermissions.Where(record => groups.Contains(record.UUID) && record.CanRead == true).Count() > 0).ToList();
            //Se unifican todas las referencias a los permisos que se pueden usar
            var ref_permissions = new List<int>();
            foreach (var item in owners)
            {
                ref_permissions.Add(item.ID);
            }
            foreach (var item in groups_can_read_permission)
            {
                ref_permissions.Add(item.ID);
            }
            //Se buscan los registros en la tabla de permisos a los que el usuario tiene permisos
            List<DTODataPermission> list_data_permissions = null;
            using (var db = new DataBaseLogic(new ConnectionParameters { Type = DBMSType.SQLite, File = Path.Combine(this._Path, table_record_permissions.File) }))
            {
                list_data_permissions = await db.Statement.selectAsync<DTODataPermission>(new StatementOptions { 
                    Filters = new List<Filter> { 
                        new Filter { Name = DTODataPermission.FilterRefPermission, ObjectValue = ref_permissions, Type = FilterType.In }
                    },
                    Orders = new List<Order> { 
                        new Order { Name = DTODataPermission.SortIdentityRecord, OrderType = EOrderType.Asc }
                    }
                    
                });
            }










            int pos = 0;
            var count = 0;
            //Se obtienen las primary keys y los metodos para leer los campos

            while (pos < list.Count)
            {

                if (count == limit)
                {
                    count = 0;
                }
            }

            return null;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: eliminar el estado administrado (objetos administrados)
                    this.DBLogic.Dispose();
                }

                // TODO: liberar los recursos no administrados (objetos no administrados) y reemplazar el finalizador
                // TODO: establecer los campos grandes como NULL
                disposedValue = true;
            }
        }

        // // TODO: reemplazar el finalizador solo si "Dispose(bool disposing)" tiene código para liberar los recursos no administrados
        // ~Permissions()
        // {
        //     // No cambie este código. Coloque el código de limpieza en el método "Dispose(bool disposing)".
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // No cambie este código. Coloque el código de limpieza en el método "Dispose(bool disposing)".
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}

