using es.dmoreno.utils.dataaccess.db;
using es.dmoreno.utils.dataaccess.filters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace es.dmoreno.utils.permissions
{
    public class Permissions : IDisposable
    {
        private bool disposedValue;

        private DataBaseLogic DBLogic { get; set; } = null;

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
            var db_file = Path.Combine(permissions_directory, "permissions.db");
            this.DBLogic = new DataBaseLogic(new ConnectionParameters { 
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
        }

        public async Task<DTOEntity> AddEntityAsync(string name)
        {
            name = name.ToUpper().Trim();
            var db_entity = await this.DBLogic.ProxyStatement<DTOEntity>();
            var entity_in_db = await db_entity.FirstIfExistsAsync<DTOEntity>(new StatementOptions { 
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
            var permision_in_db = await db_permisions.FirstIfExistsAsync<DTOPermission>(new StatementOptions { 
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

        public async Task<DTOSubjectHasPermission> AddSubjectToPermission(DTOPermission p, string remote_uuid)
        {
            var db_subject_has_permission = await this.DBLogic.ProxyStatement<DTOSubjectHasPermission>();
            var subject_permission = await db_subject_has_permission.FirstIfExistsAsync<DTOSubjectHasPermission>(new StatementOptions { 
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

        public async Task<bool> AddSubjectToGroup(string remote_uuid, DTOGroup g)
        {
            var db_subject_pertain_group = await this.DBLogic.ProxyStatement<DTOSubjectPertainGroup>();
            var subject_pertain_group = await db_subject_pertain_group.FirstIfExistsAsync<DTOSubjectPertainGroup>(new StatementOptions { 
                Filters = new List<Filter> { 
                    new Filter { Name = DTOSubjectPertainGroup.FilterRefGroup, ObjectValue = g.ID,  Type = FilterType.Equal },
                    new Filter { Name = DTOSubjectPertainGroup.FilterRemoteUUID, ObjectValue = remote_uuid,  Type = FilterType.Equal }
                }
            });
            if (subject_pertain_group == null)
            {
                return await db_subject_pertain_group.insertAsync(new DTOSubjectPertainGroup { 
                    RefGroup = g.ID,
                    RemoteUUID = remote_uuid
                });
            }

            return true;
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
