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
        /// <summary>
        /// Clase que tiene como mision transportar datos de las tablas entre funciones
        /// </summary>
        private class TableInfo
        {
            public string Name { get; set; }
            public string FileName { get; set; }
        }

        /// <summary>
        /// Clase que tiene como mision enviar listados a TASKs independientes
        /// </summary>
        /// <typeparam name="T"></typeparam>
        private class AuxList<T>
        {
            public List<T> List { get; set; } = null;
            public List<T> SortedList { get; set; } = null;
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

        /// <summary>
        /// Agrega una entidad (tabla normalmente) al sistema de permisos a la que se le vincularan acciones
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Agrega una acción que se vinculará con una entidad
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Da de alta el UUID de un grupo
        /// </summary>
        /// <param name="name"></param>
        /// <param name="uuid"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Vincula una entidad con una acción creando un permiso
        /// </summary>
        /// <param name="e"></param>
        /// <param name="a"></param>
        /// <param name="description"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Vincula el UUID de un usuario con permiso
        /// </summary>
        /// <param name="p"></param>
        /// <param name="remote_uuid"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Agrega un UUID de usuario a un grupo
        /// </summary>
        /// <param name="remote_uuid"></param>
        /// <param name="g"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Obtiene el listado de permisos de un usuario
        /// </summary>
        /// <param name="uuid"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Genera la tabla donde se guardará la autoria de los registros de una entidad
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="PermissionException"></exception>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<bool> CreateTableDataPermissions<T>() where T : class, new()
        {
            var t = new T();
            if (!(t is IDataPermission))
            {
                throw new PermissionException(t.GetType().Name + " must follow the IDataPermission interface");
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

        /// <summary>
        /// Agrega el permiso indicado a un registro del tipo T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="registry"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        /// <exception cref="PermissionException"></exception>
        public async Task AddDataPermissionAsync<T>(T registry, DTORecordPermission p) where T : class, new()
        {
            if (!(registry is IDataPermission))
            {
                throw new PermissionException(registry.GetType().Name + " must follow the IDataPermission interface");
            }
            //Se obtiene le nombre de la tabla para comprobar si tiene o no tabla de permisos
            var table_att = registry.GetType().GetTypeInfo().GetCustomAttribute<TableAttribute>();
            //Se comprueba si la entidad tiene que trabajar con permisos
            var db_permission_table = await this.DBLogic.ProxyStatement<DTOTableWithPermission>();
            var table_info = await this.GetFileInfoFromTablePermission(table_att.Name);
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

        /// <summary>
        /// Devuelve el nombre del fichero donde se guardan los permisos de los registros de una entidad
        /// </summary>
        /// <param name="tablename"></param>
        /// <returns></returns>
        private async Task<DTOTableWithPermission> GetFileInfoFromTablePermission(string tablename)
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
        /// <returns></returns>
        public async Task<List<T>> FilterByDataPermission<T>(List<T> list, string UUID) where T : class, new()
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
            if (!(first_reg is IDataPermission))
            {
                throw new Exception(first_reg.GetType().Name + " must follow the IDataPermission interface");
            }
            var table_att = first_reg.GetType().GetTypeInfo().GetCustomAttribute<TableAttribute>();
            //Se lanza una tarea para que vaya ordenador la lista
            var aux_list = new AuxList<T> { List = list };
            var task_order_list = Task.Factory.StartNew((o) =>
            {
                ((AuxList<T>)o).SortedList = ((AuxList<T>)o).List.OrderBy(reg => ((IDataPermission)reg).IDRecord).ToList();
            }, aux_list);
            //Se comprueba si existe tabla con permisos,si no existe se devuelve toda la lista
            var table_record_permissions = await this.GetFileInfoFromTablePermission(table_att.Name);
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
            var owners = list_permissions.Where(reg => reg.UUIDOwner == UUID).ToList();
            //Se obtienen los permisos que lo referencian con permiso de lectura
            var can_read_permission = list_permissions.Where(reg => reg.UUIDRecordPermissions.Where(record => record.UUID == UUID && record.CanRead == true).Count() > 0).ToList();
            //Se obtienen los permisos de los grupos a los que pertenece            
            var db_groups = await this.DBLogic.ProxyStatement<DTOGroup>();
            var list_groups = await db_groups.selectAsync<DTOGroup>();
            var db_subject_pertain_group = await this.DBLogic.ProxyStatement<DTOSubjectPertainGroup>();
            var list_subject_pertain_group = await db_subject_pertain_group.selectAsync<DTOSubjectPertainGroup>(new StatementOptions
            {
                Filters = new List<Filter>() {
                    new Filter { Name = DTOSubjectPertainGroup.FilterRemoteUUID, ObjectValue = UUID, Type = FilterType.Equal }
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
                list_data_permissions = await db.Statement.selectAsync<DTODataPermission>(new StatementOptions
                {
                    Filters = new List<Filter> {
                        new Filter { Name = DTODataPermission.FilterRefPermission, ObjectValue = ref_permissions, Type = FilterType.In }
                    },
                    Orders = new List<Order> {
                        new Order { Name = DTODataPermission.SortIdentityRecord, OrderType = EOrderType.Asc }
                    }

                });
            }
            //Se espera hasta que la lista original este ordenada
            task_order_list.Wait();
            //Se comparan la lista de permisos con la lista de registros para eliminar de la ultima los que el usuario no debe ver
            var count = list_data_permissions.Count > aux_list.SortedList.Count ? aux_list.SortedList.Count : list_data_permissions.Count;
            var list_count = 0;
            var new_list = new List<T>(count);
            for (int i = 0; i < count; i++)
            {
                var item_permission = list_data_permissions[i];
                while (list_count < aux_list.SortedList.Count)
                {
                    var item_data = aux_list.SortedList[list_count];
                    if (item_permission.IdentityRecord == ((IDataPermission)item_data).IDRecord)
                    {
                        new_list.Add(item_data);
                        list_count++;
                        break;
                    }
                    list_count++;
                }
                //si antes de terminar el bucle se ha llegado al final de list se sale
                if (list_count == aux_list.SortedList.Count)
                {
                    break;
                }
            }

            return new_list;
        }

        /// <summary>
        /// Dado un registro obtiene si el usuario asociado al uuid indicado tiene permisos, devuelve siempre el caso mas favorable
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="registry"></param>
        /// <param name="UUID"></param>
        /// <returns></returns>
        /// <exception cref="PermissionException"></exception>
        public async Task<DTORecordPermission> GetDataPermissionAsync(object registry, string UUID)
        {
            if (registry == null)
            {
                return null;
            }
            if (!(registry is IDataPermission))
            {
                throw new PermissionException(registry.GetType().Name + " must follow the IDataPermission interface");
            }
            //Se obtiene información sobre la tabla de permisos
            var table_att = registry.GetType().GetTypeInfo().GetCustomAttribute<TableAttribute>();
            var table_record_permissions = await this.GetFileInfoFromTablePermission(table_att.Name);
            //Si no existe esa informacion se devuelve al UUID como creador ya que en este contexto todo es de todos
            if (table_record_permissions == null)
            {
                return new DTORecordPermission { Entity = table_att.Name, UUIDOwner = UUID };
            }
            //Se busca la referencia al permiso del registro
            var ref_permission = int.MinValue;
            using (var db = new DataBaseLogic(new ConnectionParameters { Type = DBMSType.SQLite, File = Path.Combine(this._Path, table_record_permissions.File) }))
            {
                var record_permission = await db.Statement.FirstIfExistsAsync<DTODataPermission>(new StatementOptions
                {
                    Filters = new List<Filter> {
                        new Filter { Name = DTODataPermission.FilterIdentityRecord, ObjectValue = (registry as IDataPermission).IDRecord, Type = FilterType.Equal }
                    }
                });
                if (record_permission != null)
                {
                    ref_permission = record_permission.RefPermission;
                }
                else
                {
                    //Si no encuentra registro devuelve que es el OWNER
                    return new DTORecordPermission { Entity = table_att.Name, UUIDOwner = UUID };
                }
            }
            //Se busca el permiso
            var db_record_permission = await this.DBLogic.ProxyStatement<DTORecordPermission>();
            var permission = await db_record_permission.FirstIfExistsAsync<DTORecordPermission>(new StatementOptions
            {
                Filters = new List<Filter> {
                    new Filter { Name = DTORecordPermission.FilterID, ObjectValue = ref_permission, Type = FilterType.Equal }
                }
            });
            //No deberia de pasar
            if (permission == null)
            {
                throw new PermissionException("Registry of entity " + registry.GetType().Name + " cant resolve permission with id = " + ref_permission.ToString());
            }
            //Se obtiene el resultado mas favorable para el usuario
            if (permission.UUIDOwner == UUID)
            {
                return new DTORecordPermission { Entity = permission.Entity, UUIDOwner = UUID };
            }
            else
            {
                var uuid_permission = new DTOUUIDRecordPermision { UUID = UUID, CanDelete = false, CanRead = false, CanWrite = false };
                if (permission.UUIDRecordPermissions != null)
                {
                    //Se obtienen los permisos de los grupos a los que pertenece            
                    var db_groups = await this.DBLogic.ProxyStatement<DTOGroup>();
                    var list_groups = await db_groups.selectAsync<DTOGroup>();
                    var db_subject_pertain_group = await this.DBLogic.ProxyStatement<DTOSubjectPertainGroup>();
                    var list_subject_pertain_group = await db_subject_pertain_group.selectAsync<DTOSubjectPertainGroup>(new StatementOptions
                    {
                        Filters = new List<Filter>() {
                            new Filter { Name = DTOSubjectPertainGroup.FilterRemoteUUID, ObjectValue = UUID, Type = FilterType.Equal }
                        }
                    });

                    foreach (var item in permission.UUIDRecordPermissions)
                    {
                        //Existe una referencia al propio uuid que se consulta
                        if (item.UUID == UUID)
                        {
                            if (item.CanRead)
                            {
                                uuid_permission.CanRead = true;
                            }
                            if (item.CanDelete)
                            {
                                uuid_permission.CanDelete = true;
                            }
                            if (item.CanWrite)
                            {
                                uuid_permission.CanWrite = true;
                            }
                        }
                        else
                        {
                            //Se comprueba si pertenece a algun grupo asociado al permiso
                            foreach (var item_group in list_subject_pertain_group)
                            {
                                var group = list_groups.Where(reg => reg.ID == item_group.RefGroup).FirstOrDefault();
                                if (item.UUID == group.RemoteUUID)
                                {
                                    if (item.CanRead)
                                    {
                                        uuid_permission.CanRead = true;
                                    }
                                    if (item.CanDelete)
                                    {
                                        uuid_permission.CanDelete = true;
                                    }
                                    if (item.CanWrite)
                                    {
                                        uuid_permission.CanWrite = true;
                                    }
                                }
                            }
                        }
                    }
                }
                return new DTORecordPermission { Entity = permission.Entity, UUIDOwner = permission.UUIDOwner, UUIDRecordPermissions = new DTOUUIDRecordPermision[] { uuid_permission } };
            }
        }

        /// <summary>
        /// Indica si el registro puede ser leido por el usuario uuid
        /// </summary>
        /// <param name="registry"></param>
        /// <param name="UUID"></param>
        /// <returns></returns>
        public async Task<bool> CheckCanReadPermissionAsync(object registry, string UUID, bool throw_exception = true)
        {
            var permission = await GetDataPermissionAsync(registry, UUID);
            var read = permission.UUIDOwner == UUID ? true : (permission.UUIDRecordPermissions[0].CanRead);
            if (throw_exception && !read)
            {
                throw new PermissionException("Access denied for user with UUID " + UUID);
            }
            return read;
        }

        /// <summary>
        /// Indica si el registro puede ser modificado por el usuario uuid
        /// </summary>
        /// <param name="registry"></param>
        /// <param name="UUID"></param>
        /// <returns></returns>
        public async Task<bool> CheckCanWritePermissionAsync(object registry, string UUID, bool throw_exception = true)
        {
            var permission = await GetDataPermissionAsync(registry, UUID);
            var write = permission.UUIDOwner == UUID ? true : (permission.UUIDRecordPermissions[0].CanWrite);
            if (throw_exception && !write)
            {
                throw new PermissionException("Permission denied for user with UUID " + UUID);
            }
            return write;
        }

        /// <summary>
        /// Indica si el registro puede ser borrado por el usuario uuid
        /// </summary>
        /// <param name="registry"></param>
        /// <param name="UUID"></param>
        /// <returns></returns>
        public async Task<bool> CheckCanDeletePermissionAsync(object registry, string UUID, bool throw_exception = true)
        {
            var permission = await GetDataPermissionAsync(registry, UUID);
            var delete = permission.UUIDOwner == UUID ? true : (permission.UUIDRecordPermissions[0].CanDelete);
            if (throw_exception && !delete)
            {
                throw new PermissionException("Permission denied for user with UUID " + UUID);
            }
            return delete;
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

