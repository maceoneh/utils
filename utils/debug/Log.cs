using es.dmoreno.utils.dataaccess.db;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace es.dmoreno.utils.debug
{
    static public class Log
    {
        static public string DBHost { get; set; }

        static public int DBPort { get; set; }

        static public string DBUser { get; set; }

        static public string DBPassword { get; set; }

        static public string DBName { get; set; }

        static public DBMSType DBType { get; set; } = DBMSType.None;

        static public IConnector DBConnector { get; set; } = null;

        static private SemaphoreSlim SemaphoreLogs { get; set; } = new SemaphoreSlim(1);

        static private bool RotationLocked { get; set; } = false;

        static private DateTime LastRotation { get; set; } = new DateTime();

        static public ETypeLog[] LogLevel { get; set; } = new ETypeLog[] { ETypeLog.Error, ETypeLog.Warning };

        static private DTOLogHeader getHeader(ETypeLog t, string Tag)
        {
            var d = DateTime.Now;
            var result = new DTOLogHeader();

            string month = d.Month.ToString();
            string day = d.Day.ToString();
            string hour = d.Hour.ToString();
            string minute = d.Minute.ToString();
            string second = d.Second.ToString();

            if (d.Month < 10)
            {
                month = "0" + month;
            }

            if (d.Day < 10)
            {
                day = "0" + day;
            }

            if (d.Hour < 10)
            {
                hour = "0" + hour;
            }

            if (d.Minute < 10)
            {
                minute = "0" + minute;
            }

            if (d.Second < 10)
            {
                second = "0" + second;
            }

            result.TextHeader = "[" + day + "-" + month + "-" + d.Year.ToString() + " " + hour + ":" + minute + ":" + second + "] ";
            result.Time = DateTime.Now;

            result.Tag = Tag;
            if (!string.IsNullOrWhiteSpace(Tag))
            {
                result.TextHeader += Tag + ": ";
            }

            switch (t)
            {
                case ETypeLog.Error: result.TextHeader += "Error: "; break;
                case ETypeLog.Verbose: result.TextHeader += "Verbose: "; break;
                case ETypeLog.Debug: result.TextHeader += "Debug: "; break;
                case ETypeLog.Warning: result.TextHeader += "Warning: "; break;
            }
            result.Type = t;

            return result;
        }

        static public void Write(ETypeLog t, string Tag, string text)
        {
            if (!isEnabledLogLevel(t)) { return; }

            var header = getHeader(t, Tag);

            var lines = text.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var item in lines)
            {
                if (t == ETypeLog.Error)
                {
                    Console.Error.Write(header.TextHeader + item + Environment.NewLine);
                }
                else
                {
                    Console.Write(header.TextHeader + item + Environment.NewLine);
                }
            }

            insertInDB(new DTOLogDaily
            {
                Description = text,
                Tag = header.Tag,
                Time = header.Time,
                Type = header.Type
            });

            doLogRotationTablesInDB();
        }

        static public async Task WriteAsync(ETypeLog t, string Tag, string text)
        {
            if (!isEnabledLogLevel(t)) { return; }

            var header = getHeader(t, Tag);

            var lines = text.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var item in lines)
            {
                if (t == ETypeLog.Error)
                {
                    await Console.Error.WriteAsync(header.TextHeader + item + Environment.NewLine);
                }
                else
                {
                    Console.Write(header.TextHeader + item + Environment.NewLine);
                }
            }

            insertInDB(new DTOLogDaily
            {
                Description = text,
                Tag = header.Tag,
                Time = header.Time,
                Type = header.Type
            });

            doLogRotationTablesInDB();
        }

        static public bool isEnabledLogLevel(ETypeLog t)
        {
            for (int i = 0; i < LogLevel.Length; i++)
            {
                if (LogLevel[i] == t)
                {
                    return true;
                }
            }

            return false;
        }

        static public async Task<bool> createTables(DBMSType type)
        {
            ConnectionParameters p = new ConnectionParameters { Type = Log.DBType };
            if (Log.DBType == DBMSType.SQLite)
            {
                p.File = Log.DBName;
            }
            else
            {
                p.Host = Log.DBHost;
                p.Port = Log.DBPort;
                p.Database = Log.DBName;
                p.User = Log.DBUser;
                p.Password = Log.DBPassword;
            }
            using (var db = new DataBaseLogic(p))
            {
                if ((await db.Management.createAlterTableAsync<DTOLogDaily>()) &&
                       (await db.Management.createAlterTableAsync<DTOLogMonthly>()) &&
                       (await db.Management.createAlterTableAsync<DTOLogYearly>()) &&
                       (await db.Management.createAlterTableAsync<DTOLogStored>()))
                {
                    Log.DBType = type;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        static private void insertInDB(DTOLog l)
        {
            if (Log.DBType != DBMSType.None)
            {
                if (LogLevel == null)
                {
                    return;
                }
                else
                {
                    if (!LogLevel.Contains(l.Type))
                    {
                        return;
                    }
                }

                Task.Factory.StartNew(async (log) =>
                {
                    ConnectionParameters p = new ConnectionParameters { Type = Log.DBType };
                    if (Log.DBType == DBMSType.SQLite)
                    {
                        p.File = Log.DBName;
                    }
                    else
                    {
                        p.Host = Log.DBHost;
                        p.Port = Log.DBPort;
                        p.Database = Log.DBName;
                        p.User = Log.DBUser;
                        p.Password = Log.DBPassword;
                    }
                    var li = log as DTOLog;
                    using (var db = new DataBaseLogic(p))
                    {
                        await db.Statement.insertAsync(li);
                    }
                }, l);
            }
        }

        static private void doLogRotationTablesInDB()
        {
            if (Log.DBType != DBMSType.None)
            {
                if (LastRotation.AddHours(24) < DateTime.Now)
                {
                    var dorotation = false;
                    SemaphoreLogs.Wait();

                    if (!RotationLocked)
                    {
                        dorotation = true;
                        RotationLocked = true;
                    }

                    SemaphoreLogs.Release();

                    if (dorotation)
                    {
                        Task.Factory.StartNew(async () =>
                        {
                            try
                            {
                                ConnectionParameters p = new ConnectionParameters { Type = Log.DBType };
                                if (Log.DBType == DBMSType.SQLite)
                                {
                                    p.File = Log.DBName;
                                }
                                else
                                {
                                    p.Host = Log.DBHost;
                                    p.Port = Log.DBPort;
                                    p.Database = Log.DBName;
                                    p.User = Log.DBUser;
                                    p.Password = Log.DBPassword;
                                }

                                using (var db = new DataBaseLogic(p))
                                {
                                    var now = DateTime.Now;
                                    var notdaily = await db.Statement.selectAsync<DTOLogDaily>("log_time < @date", null, new List<StatementParameter> {
                                    new StatementParameter("@date", ParamType.DateTime, new DateTime(now.Year, now.Month, now.Day))
                                    });

                                    foreach (var item in notdaily)
                                    {
                                        await db.Statement.insertAsync(new DTOLogMonthly
                                        {
                                            Description = item.Description,
                                            Tag = item.Tag,
                                            Time = item.Time,
                                            Type = item.Type
                                        });
                                        await db.Statement.deleteAsync(item);
                                    }

                                    var notmonthly = await db.Statement.selectAsync<DTOLogMonthly>("log_time < @date", null, new List<StatementParameter> {
                                    new StatementParameter("@date", ParamType.DateTime, new DateTime(now.Year, now.Month, 1))
                                    });

                                    foreach (var item in notmonthly)
                                    {
                                        await db.Statement.insertAsync(new DTOLogYearly
                                        {
                                            Description = item.Description,
                                            Tag = item.Tag,
                                            Time = item.Time,
                                            Type = item.Type
                                        });
                                        await db.Statement.deleteAsync(item);
                                    }
                                }

                                LastRotation = DateTime.Now;
                            }
                            finally
                            {
                                RotationLocked = false;
                            }
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Registramos logs sin que guarde en la base de datos
        /// Es algo más visual de cara a verlo en la consola durante el DEBUG
        /// sobretodo viene bien para los mensajes de FIREBASE
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="information"></param>
        public static void ConsoleWrite(string methodName, object information)
        {
            Console.WriteLine($"\nMethod name: {methodName}\nInformation: {information}");
        }
    }
}
