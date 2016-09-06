using MySql.Data.MySqlClient;
using scada_diplom.entitys;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Drawing;
namespace scada_diplom.managers
{
   public class MainDispatcher
    {
       private static MainDispatcher instance;
       public const int STATUS_SUCCESS = 1;
       public const int STATUS_CHANNEL_BROKEN = 2;
       public const int STATUS_DEVICE_S_BROKEN = 3;
       public const int STATUS_DEVICE_R_BROKEN = 8;
       public const int STATUS_NOT_AVAILABLE = 4;
       public const int STATUS_ERROR_UNKNOWN = 5;
       public const int STATUS_DEV_RECV_NOT_AVAILABLE = 6;
       public const int STATUS_DEV_SEND_NOT_AVAILABLE = 7;
       public const int STATUS_REQUEST_SEND = 8;
       public const int STATUS_CHANNEL_FIXED = 9;
       public const int STATUS_DEVICE_FIXED = 10;
       public const int STATUS_PACK_SENDING = 11;
       public const int STATUS_PACK_RECIVED = 12;
       public object draw_critycal_section = new object();
       public MainDispatcher()
       {
           devs = new Collection<device>();
           chan = new Collection<channel>();
           proc = new Collection<process>();
           bproc = new Collection<binding_proc>();
       }
       public static MainDispatcher Instance
       {
           get
           {
               if (instance == null)
                   instance = new MainDispatcher();
               return instance;
           }
       }
       public int project_pk;
       public string project_name;
       public int f_time_scale;
       public Int64 f_model_time;
       public int f_is_modeling;
       public project current_project;
       public int chan_index_selected = -1;
       public Collection<device> devs;
       public Collection<channel> chan;
       public Collection<process> proc;
       public Collection<binding_proc> bproc;
       public Collection<visualisation> vis;
       public Collection<deviceDispatcher> dev_dispatch;
       public static Random r = new Random();
       public Thread thr;
       public object broke_critycal_section = new object();
       public object load_proj_critycal_section = new object();
       public Graphics graph;
       public Image m;
       public Image p;
       public struct Rect
       {
           public Point p1;
           public Point p2;
       }
       public Collection<Rect> chan_chache;
       public void InitDrawing(Graphics g,Image micr,Image pic)
       {
           graph = g;
           m = micr;
           p = pic;
       }
       public bool veroyatnost(int ver)
       {
           Random gen = new Random();
           int prob = gen.Next(100);
           if (prob <= ver)
               return true;
           else
               return false;
       }
       public void UptdateChannelEvent(int channel_pk)
       {
           Collection<_event> Event = new Collection<_event>();
           DataTable table = dbProvider.Instance.select_query("select * from t_event where fk_target_channel='" + channel_pk + "' order by pk desc limit 0,1");
           int event_pk = 0;
           bool null_select = (table.Rows.Count == 0);
           for (int j = 0; j < table.Rows.Count; j++)
           {
               for (int i = 0; i < table.Columns.Count; i++)
               {
                   //   int local_pk = 0;
                   if (table.Columns[i].Caption.CompareTo("pk") == 0)
                   {
                       event_pk = (int)table.Rows[j].ItemArray[i];
                       break;
                   }
               }
           }
           _event ev = new _event(event_pk);
           ev.load();

           channel ch = new channel(channel_pk);
           ch.load();
           if (ev.f_type == STATUS_CHANNEL_BROKEN && ch.f_not_active_until_time < mod_time_provider.time())
               RegisterEvent(STATUS_CHANNEL_FIXED, 0, mod_time_provider.time(), mod_time_provider.time(), 0, mod_time_provider.time(), channel_pk, 0, 0, 0, 0, 0);
           if ((ev.f_type == STATUS_CHANNEL_FIXED || null_select) && ch.f_not_active_until_time > mod_time_provider.time())
           {
               RegisterEvent(STATUS_CHANNEL_BROKEN, 0, mod_time_provider.time(), mod_time_provider.time(), ch.f_not_active_until_time, mod_time_provider.time(), channel_pk, 0, 0, 0, 0, 0);
           }
       }
       public void UptdateDeviceEvent(int dev_pk)
       {
           Collection<_event> Event = new Collection<_event>();
           DataTable table = dbProvider.Instance.select_query("select * from t_event where fk_device='" + dev_pk + "' order by pk desc limit 0,1");
           int event_pk = 0;
           bool null_select = (table.Rows.Count == 0);
           for (int j = 0; j < table.Rows.Count; j++)
           {
               for (int i = 0; i < table.Columns.Count; i++)
               {
                   //   int local_pk = 0;
                   if (table.Columns[i].Caption.CompareTo("pk") == 0)
                   {
                       event_pk = (int)table.Rows[j].ItemArray[i];
                       break;
                   }
               }
           }
           _event ev = new _event(event_pk);
           ev.load();

           device dv = new device(dev_pk);
           dv.load();
           if ((ev.f_type == STATUS_DEVICE_R_BROKEN || ev.f_type == STATUS_DEVICE_S_BROKEN) && dv.f_not_active_until_time < mod_time_provider.time())
               RegisterEvent(STATUS_DEVICE_FIXED, 0, mod_time_provider.time(), mod_time_provider.time(), 0, mod_time_provider.time(), 0, dev_pk, 0, 0, 0, 0);
           if ((ev.f_type == STATUS_DEVICE_FIXED || null_select) && dv.f_not_active_until_time > mod_time_provider.time())
           {
               RegisterEvent(STATUS_DEVICE_S_BROKEN, 0, mod_time_provider.time(), mod_time_provider.time(), dv.f_not_active_until_time, mod_time_provider.time(), 0, dev_pk, 0, 0, 0, 0);
           }
       }
       public void broker()
       {
           int[] em_period_devs = new int[devs.Count];
           int[] em_period_ch = new int[chan.Count];
           while (true)
           {
               current_project.db_commit();
               for (int i = 0; i < devs.Count; i++)
               {
                   em_period_devs[i]++;
                   if (em_period_devs[i] > devs[i].f_emergence_period)
                   {
                       if (veroyatnost(devs[i].f_emergence_probability))
                       {
                           lock (broke_critycal_section)
                           {
                               devs[i].f_not_active_until_time = mod_time_provider.time() + r.Next(60);
                              
                               devs[i].db_commit();
                               em_period_devs[i] = 0;
                           }
                       }
                   }
               }
               for (int i = 0; i < chan.Count; i++)
               {
                   em_period_ch[i]++;
                   if (em_period_ch[i] > chan[i].f_emergence_period)
                   {
                       if (veroyatnost(chan[i].f_emergence_probability))
                       {
                           lock (broke_critycal_section)
                           {
                               chan[i].f_not_active_until_time = mod_time_provider.time() + r.Next(60);
                              
                               chan[i].db_commit();
                               //UptdateChannelEvent(chan[i].pk);
                               em_period_ch[i] = 0;
                           }
                       }
                   }
               }
               mod_time_provider.Delay(1, delegate() { });
           }
       }
       public Collection<project> loadProjectList()
       {
           Collection<project> projects = new Collection<project>();
           DataTable table = dbProvider.Instance.select_query("select * from t_project");
           for (int j = 0; j < table.Rows.Count; j++)
           {
               for (int i = 0; i < table.Columns.Count; i++)
               {
                   int local_pk = 0;
                   if (table.Columns[i].Caption.CompareTo("pk") == 0)
                   {
                       local_pk = (int)table.Rows[j].ItemArray[i];
                       projects.Add((project)new project(local_pk).load());
                       break;
                   }
               }
           }
           return projects;
       }
       public process find_proc(int pk)
       {
           for (int j = 0; j < this.proc.Count; j++)
               if (this.proc[j].pk == pk)
                   return this.proc[j];
             return null;                   
       }
       public device find_dev(int pk)
       {
           for (int j = 0; j < this.devs.Count; j++)
               if (this.devs[j].pk == pk)
                   return this.devs[j];
           return null;
       }
       public channel find_chan(int pk)
       {
           for (int j = 0; j < this.chan.Count; j++)
               if (this.chan[j].pk == pk)
                   return this.chan[j];
           return null;
       }
       public binding_proc resolve_binding_proc(int pk)
       {
           for (int i = 0; i < bproc.Count; i++)
               if (bproc[i].pk == pk)
                   return bproc[i];
           return null;
       }
       public void request_dev(process proc)
       {
           for (int i = 0; i < dev_dispatch.Count; i++)
               if (dev_dispatch[i].dev.pk == proc.fk_device)
               {
                   dev_dispatch[i].request_enque(proc);
               }
       }
       public void RegisterPackSended(process proc_send_pk,process proc_recv_pk,device dev_send_pk,device dev_recv_pk,channel chan,data data_pk,int status,int type)
      {
          try { RegisterEvent(type, status, mod_time_provider.time(), data_pk.ideal_time, mod_time_provider.time(), data_pk.ideal_time, chan.pk, dev_send_pk.pk, data_pk.pk, proc_recv_pk.pk, proc_send_pk.pk, dev_recv_pk.pk); }
          catch (Exception exc)
          { }
      } 
       public int request(process proc, int size)
       {
           lock (load_proj_critycal_section)
           {
               DataTable table = dbProvider.Instance.select_query("select * from t_proc_binding where proc_recv_fk=" + proc.pk.ToString());
               int pk = 0;
               for (int i = 0; i < table.Columns.Count; i++)
               {
                   if (table.Columns[i].Caption.CompareTo("pk") == 0)
                       pk = (int)table.Rows[0].ItemArray[i];
               }
               if (pk == 0)
                   return STATUS_ERROR_UNKNOWN;
               binding_proc bp = resolve_binding_proc(pk);
               if (bp == null)
                   return STATUS_ERROR_UNKNOWN;
               channel ch = find_chan(bp.channel_fk);
               UptdateChannelEvent(ch.pk);
               if (ch.f_busy_until_time > mod_time_provider.time())
                   return STATUS_NOT_AVAILABLE;
               if (ch.f_not_active_until_time > mod_time_provider.time())
                   return STATUS_CHANNEL_BROKEN;
               device dv1 = find_dev(bp.fk_dev_recv);
               if (dv1 != null)
               {
                   UptdateDeviceEvent(dv1.pk);
                   if (dv1.f_busy_until_time > mod_time_provider.time())
                       return STATUS_DEV_RECV_NOT_AVAILABLE;
               }
               device dv2 = find_dev(bp.fk_dev_send);
               if (dv2 != null)
               {
                   UptdateDeviceEvent(dv2.pk);
                   if (dv2.f_busy_until_time > mod_time_provider.time())
                       return STATUS_DEV_SEND_NOT_AVAILABLE;
               }
               if (dv1.f_not_active_until_time > mod_time_provider.time())
                   return STATUS_DEVICE_R_BROKEN;

               if (dv2.f_not_active_until_time > mod_time_provider.time())
                   return STATUS_DEVICE_S_BROKEN;
               data[] pack = find_proc(bp.proc_fk).read_data(size);
               long del = (long)(((double)pack.Length * 4) / (double)ch.f_bandwidth);
               ch.f_busy_until_time = mod_time_provider.time() + del;
               ch.db_commit();
               dv1.f_busy_until_time = mod_time_provider.time() + del;
               dv1.db_commit();
               dv2.f_busy_until_time = mod_time_provider.time() + del;
               dv2.db_commit();
               mod_time_provider.Delay(del, delegate()
                   {
                       find_proc(bp.proc_recv_fk).data_recv(pack);
                       for (int i = 0; i < size; i++)
                           RegisterPackSended(find_proc(bp.proc_fk), find_proc(bp.proc_recv_fk), dv2, dv1, ch, pack[i], STATUS_SUCCESS, STATUS_PACK_SENDING);
                       LogMgr.Instance.WriteLog("Процесс  '" + proc.f_name + "' на устройстве '" + find_dev(bp.fk_dev_recv).f_name + "' запросил данные");
                   });
           }
           return STATUS_SUCCESS;
       }
       public _event RegisterEvent(int f_type, int f_status, Int64 f_realtime,
           Int64 f_idealtime, Int64 f_queuetime, Int64 f_creation_time, int fk_target_channel, int fk_device,
           int fk_packet, int fk_target_process, int fk_source_process, int fk_target_device)
       {
           dbProvider.Instance.mysql_command("INSERT INTO `t_event` (`pk`, `f_type`, `f_status`, `f_real_time`, `f_ideal_time`, "
                + " `f_queue_time`, `f_time_for_processing`, `f_creation_time`, `fk_target_channel`,`fk_device`,`fk_packet`,`fk_target_process`,`fk_source_process`,`fk_target_device`) " +
                 "VALUES (NULL, '" + f_type + "', '" + f_status + "', '" + f_realtime + "', '" + f_idealtime + "',  '" + f_queuetime + "','0', '" + f_creation_time + "', '" + fk_target_channel + "', '" + fk_device + "','" + fk_packet + "','" + fk_target_process + "','" + fk_source_process + "','" + fk_target_device + "')");
           DataTable table = dbProvider.Instance.select_query("select * from `t_event`  order by pk desc limit 0,1");
           _event result = new _event(0);
           for (int i = 0; i < table.Columns.Count; i++)
           {
               if (table.Columns[i].Caption.CompareTo("pk") == 0)
                   result.pk = (int)table.Rows[0].ItemArray[i];
               if (table.Columns[i].Caption.CompareTo("f_type") == 0)
                   result.f_type = (int)table.Rows[0].ItemArray[i];
               if (table.Columns[i].Caption.CompareTo("f_status") == 0)
                   result.f_status = (int)table.Rows[0].ItemArray[i];
               if (table.Columns[i].Caption.CompareTo("f_real_time") == 0)
                   result.f_real_time = (Int64)table.Rows[0].ItemArray[i];
               if (table.Columns[i].Caption.CompareTo("f_ideal_time") == 0)
                   result.f_ideal_time = (Int64)table.Rows[0].ItemArray[i];
               if (table.Columns[i].Caption.CompareTo("f_queue_time") == 0)
                   result.f_queue_time = (Int64)table.Rows[0].ItemArray[i];
               if (table.Columns[i].Caption.CompareTo("f_time_for_processing") == 0)
                   result.f_time_for_processing = (int)table.Rows[0].ItemArray[i];
               if (table.Columns[i].Caption.CompareTo("f_creation_time") == 0)
                   result.f_creation_time = (Int64)table.Rows[0].ItemArray[i];
               if (table.Columns[i].Caption.CompareTo("fk_target_channel") == 0)
                   result.fk_target_channel = (int)table.Rows[0].ItemArray[i];
               if (table.Columns[i].Caption.CompareTo("fk_device") == 0)
                   result.fk_device = (int)table.Rows[0].ItemArray[i];
               if (table.Columns[i].Caption.CompareTo("fk_packet") == 0)
                   result.fk_packet = (int)table.Rows[0].ItemArray[i];
               if (table.Columns[i].Caption.CompareTo("fk_target_process") == 0)
                   result.fk_target_process = (int)table.Rows[0].ItemArray[i];
               if (table.Columns[i].Caption.CompareTo("fk_source_process") == 0)
                   result.fk_source_process = (int)table.Rows[0].ItemArray[i];
               if (table.Columns[i].Caption.CompareTo("fk_target_device") == 0)
                   result.fk_target_device = (int)table.Rows[0].ItemArray[i];
           }
           loadProject(current_project.pk);
           return result;
       }
       public data createData(float data,int fk_process)
     {
         dbProvider.Instance.mysql_command("INSERT INTO `t_data` (`pk`, `data`, `fk_process`,`creation_time`) " +
                 "VALUES (NULL, '" + data.ToString().Replace(",",".")+ "', '" + fk_process + "','"+mod_time_provider.time()+"')");
         DataTable table = dbProvider.Instance.select_query("select * from `t_data`  order by pk desc limit 0,1");
         data result = new data(0);
         for (int i = 0; i < table.Columns.Count; i++)
         {
             if (table.Columns[i].Caption.CompareTo("pk") == 0)
                 result.pk = (int)table.Rows[0].ItemArray[i];
             if (table.Columns[i].Caption.CompareTo("data") == 0)
                 result._data = (double)table.Rows[0].ItemArray[i];
             if (table.Columns[i].Caption.CompareTo("fk_process") == 0)
                 result.fk_process = (int)table.Rows[0].ItemArray[i];
             if (table.Columns[i].Caption.CompareTo("creation_time") == 0)
                 result.ideal_time = (Int64)table.Rows[0].ItemArray[i];      
         }
         loadProject(current_project.pk);
         return result;
     }
       public device createDevice(string nazv,string comment,int type, int ver, int period)
       {
           dbProvider.Instance.mysql_command("INSERT INTO `t_device` (`pk`, `fk_project`, `f_name`, `f_comments`, `f_not_active_until_time`, "
              + " `f_type`, `f_busy_until_time`, `f_emergence_probability`, `f_emergence_period`) "+
               "VALUES (NULL, '"+project_pk+"', '"+nazv+"', '"+comment+"', '0',  '"+type+"', '0', '"+ver+"', '"+period+"')");
           DataTable table = dbProvider.Instance.select_query("select * from `t_device`  order by pk desc limit 0,1");
           device result = new device(0);
           for (int i = 0; i < table.Columns.Count; i++)
           {
               if (table.Columns[i].Caption.CompareTo("pk") == 0)
                   result.pk = (int)table.Rows[0].ItemArray[i];
               if (table.Columns[i].Caption.CompareTo("f_name") == 0)
                   result.f_name = (string)table.Rows[0].ItemArray[i];
               if (table.Columns[i].Caption.CompareTo("f_comments") == 0)
                   result.f_comments = (string)table.Rows[0].ItemArray[i];
               if (table.Columns[i].Caption.CompareTo("f_not_active_until_time") == 0)
                   result.f_not_active_until_time = (Int64)table.Rows[0].ItemArray[i];
               if (table.Columns[i].Caption.CompareTo("f_type") == 0)
                   result.f_type = (int)table.Rows[0].ItemArray[i];
               if (table.Columns[i].Caption.CompareTo("f_busy_until_time") == 0)
                   result.f_busy_until_time = (Int64)table.Rows[0].ItemArray[i];
               if (table.Columns[i].Caption.CompareTo("f_emergence_probability") == 0)
                   result.f_emergence_probability = (int)table.Rows[0].ItemArray[i];
               if (table.Columns[i].Caption.CompareTo("f_emergence_period") == 0)
                   result.f_emergence_period = (int)table.Rows[0].ItemArray[i];
           }
           loadProject(current_project.pk);
           return result;           
       }
       public visualisation createVisualisation(int proj,int dev,float xc,float yc)
       {
           dbProvider.Instance.mysql_command("INSERT INTO `visualisation` (`id`, `fk_project`, `fk_device`, `x_coord`, `y_coord`) " +
               "VALUES (NULL, '" +proj + "', '" +dev + "', '" + xc + "',  '" + yc + "')");
           DataTable table = dbProvider.Instance.select_query("select * from `visualisation`  order by id desc limit 0,1");
           visualisation result= new visualisation(proj,dev);
           for (int i = 0; i < table.Columns.Count; i++)
           {
               if (table.Columns[i].Caption.CompareTo("fk_project") == 0)
                   result.fk_project = (int)table.Rows[0].ItemArray[i];

               if (table.Columns[i].Caption.CompareTo("fk_device") == 0)
                   result.fk_device= (int)table.Rows[0].ItemArray[i];

               if (table.Columns[i].Caption.CompareTo("x_coord") == 0)
                   result.x_coords = (float)table.Rows[0].ItemArray[i];
               if (table.Columns[i].Caption.CompareTo("y_coord") == 0)
                   result.y_coords = (float)table.Rows[0].ItemArray[i];
           }
           loadProject(current_project.pk);
           draw_all(false);
           return result;
       }
       public channel createChannel(string nazv, string comment,int bandwidth, int ver, int period, int dev1,int dev2)
       {
           dbProvider.Instance.mysql_command("INSERT INTO `t_channel` (`pk`, `f_name`, `f_comments`, `f_not_active_until_time`, `f_bandwidth`, `f_busy_until_time`, `f_emergence_probability`, `f_emergence_period`, `fk_device1`, `fk_device2`, `fk_project`)"+
               " VALUES (NULL, '"+nazv+"', '"+comment+"', '0', '"+bandwidth+"', '0', '"+ver+"', '"+period+"', '"+dev1+"', '"+dev2+"', '"+project_pk+"')");
           DataTable table = dbProvider.Instance.select_query("select * from `t_channel`  order by pk desc limit 0,1");
           channel result = new channel(0);
           for (int i = 0; i < table.Columns.Count; i++)
           {
               if (table.Columns[i].Caption.CompareTo("pk") == 0)
                   result.pk = (int)table.Rows[0].ItemArray[i];
               if (table.Columns[i].Caption.CompareTo("f_name") == 0)
                   result.f_name = (string)table.Rows[0].ItemArray[i];
               if (table.Columns[i].Caption.CompareTo("f_comments") == 0)
                   result.f_comments = (string)table.Rows[0].ItemArray[i];
               if (table.Columns[i].Caption.CompareTo("f_not_active_until_time") == 0)
                   result.f_not_active_until_time = (Int64)table.Rows[0].ItemArray[i];
               if (table.Columns[i].Caption.CompareTo("f_bandwidth") == 0)
                   result.f_bandwidth = (int)table.Rows[0].ItemArray[i];
               if (table.Columns[i].Caption.CompareTo("f_busy_until_time") == 0)
                   result.f_busy_until_time = (Int64)table.Rows[0].ItemArray[i];
               if (table.Columns[i].Caption.CompareTo("f_emergence_probability") == 0)
                   result.f_emergence_probability = (int)table.Rows[0].ItemArray[i];
               if (table.Columns[i].Caption.CompareTo("f_emergence_period") == 0)
                   result.f_emergence_period = (int)table.Rows[0].ItemArray[i];
               if (table.Columns[i].Caption.CompareTo("fk_device1") == 0)
                   result.fk_device1 = (int)table.Rows[0].ItemArray[i];
               if (table.Columns[i].Caption.CompareTo("fk_device2") == 0)
                   result.fk_device2 = (int)table.Rows[0].ItemArray[i];
               if (table.Columns[i].Caption.CompareTo("fk_project") == 0)
                   result.fk_project = (int)table.Rows[0].ItemArray[i];
           }
           loadProject(current_project.pk);
           return result;
       }
       public process createProcess(string name, string comment, int type, int generation, int request, int dev, int f_request_data_count)
       {
           dbProvider.Instance.mysql_command("INSERT INTO `t_process` (`pk`, `f_name`, `f_comments`, `f_source_type`, `f_current_data`, `f_generate_periodicity`, `f_request_data_periodicity`, `f_has_data`, `fk_device`,	`f_request_data_count` )" +
               " VALUES (NULL, '"+name+"', '"+comment+"', '"+type+"', '0', '"+generation+"', '"+request+"', '0', '"+dev+"','"+f_request_data_count+"')");
           DataTable table = dbProvider.Instance.select_query("select * from `t_process`  order by pk desc limit 0,1");
           process result = new process(0);
           for (int i = 0; i < table.Columns.Count; i++)
           {
               if (table.Columns[i].Caption.CompareTo("pk") == 0)
                   result.pk = (int)table.Rows[0].ItemArray[i];
               if (table.Columns[i].Caption.CompareTo("f_name") == 0)
                   result.f_name = (string)table.Rows[0].ItemArray[i];
               if (table.Columns[i].Caption.CompareTo("f_comments") == 0)
                   result.f_comments = (string)table.Rows[0].ItemArray[i];
               if (table.Columns[i].Caption.CompareTo("f_source_type") == 0)
                   result.f_source_type = (int)table.Rows[0].ItemArray[i];
               if (table.Columns[i].Caption.CompareTo("f_current_data") == 0)
                   result.f_current_data = (float)table.Rows[0].ItemArray[i];
               if (table.Columns[i].Caption.CompareTo("f_generate_periodicity") == 0)
                   result.f_generate_periodicity = (int)table.Rows[0].ItemArray[i];
               if (table.Columns[i].Caption.CompareTo("f_request_data_periodicity") == 0)
                   result.f_request_data_periodicity = (int)table.Rows[0].ItemArray[i];
               if (table.Columns[i].Caption.CompareTo("f_has_data") == 0)
                   result.f_has_data = (int)table.Rows[0].ItemArray[i];
               if (table.Columns[i].Caption.CompareTo("fk_device") == 0)
                   result.fk_device = (int)table.Rows[0].ItemArray[i];
               if (table.Columns[i].Caption.CompareTo("f_request_data_count") == 0)
                   f_request_data_count = (int)table.Rows[0].ItemArray[i];
           }
           loadProject(current_project.pk);
           return result;
       }
       public binding_proc createBindProc(int procRq, int chan, int procRc, int devS, int devRc)
       {
           dbProvider.Instance.mysql_command("INSERT INTO `t_proc_binding` (`pk`, `proc_fk`, `channel_fk`, `proc_recv_fk`, `fk_dev_send`, "
              + " `fk_dev_recv`) " +
               "VALUES (NULL, '" + procRq + "', '" + chan + "', '" + procRc + "',  '" + devS + "', '" + devRc + "')");
           DataTable table = dbProvider.Instance.select_query("select * from `t_proc_binding`  order by pk desc limit 0,1");
           binding_proc result = new binding_proc(0);
           for (int i = 0; i < table.Columns.Count; i++)
           {
               if (table.Columns[i].Caption.CompareTo("pk") == 0)
                   result.pk = (int)table.Rows[0].ItemArray[i];
               if (table.Columns[i].Caption.CompareTo("proc_fk") == 0)
                   result.proc_fk = (int)table.Rows[0].ItemArray[i];
               if (table.Columns[i].Caption.CompareTo("channel_fk") == 0)
                   result.channel_fk= (int)table.Rows[0].ItemArray[i];
               if (table.Columns[i].Caption.CompareTo("proc_recv_fk") == 0)
                   result.proc_recv_fk = (int)table.Rows[0].ItemArray[i];
               if (table.Columns[i].Caption.CompareTo("fk_dev_send") == 0)
                   result.fk_dev_send = (int)table.Rows[0].ItemArray[i];
               if (table.Columns[i].Caption.CompareTo("fk_dev_recv") == 0)
                   result.fk_dev_recv = (int)table.Rows[0].ItemArray[i];
           }
           loadProject(current_project.pk);
           return result;
       }
       public Collection<device> requestAvailableDevices()
       {
           Collection<device> result=new Collection<device>();
           dbProvider.Instance.mysql_command("SELECT * FROM `t_device` WHERE `t_device`.`fk_project`='"+project_pk+"' AND (select count(*) from `t_channel` where `t_channel`.`fk_device1`=`t_device`.`pk` or `t_channel`.`fk_device2`=`t_device`.`pk`)=0");
            DataTable table = dbProvider.Instance.select_query("select * from t_device where fk_project=" + this.project_pk);
            for (int j = 0; j < table.Rows.Count; j++)
            {
                for (int i = 0; i < table.Columns.Count; i++)
                    if (table.Columns[i].Caption.CompareTo("pk") == 0)
                    {
                        result.Add((device)new device((int)table.Rows[j].ItemArray[i]).load());
                        break;
                    }
            }         
           return result;
       }
       public project createProject(string name,int timeScale)
       {
           dbProvider.Instance.mysql_command("INSERT INTO `t_project` (`pk`, `f_name`, `f_creation_date`, `f_time_scale`, `f_model_time`, `f_is_modeling_started`) "
               + "VALUES (NULL, '" + name + "', NOW(), '" + timeScale + "', '0', '0')");
           DataTable table=dbProvider.Instance.select_query("select * from `t_project`  order by pk desc limit 0,1");
           project result = new project(0);
           for (int i = 0; i < table.Columns.Count; i++)
           {
               if (table.Columns[i].Caption.CompareTo("pk") == 0)
                   result.pk = (int)table.Rows[0].ItemArray[i];
               if (table.Columns[i].Caption.CompareTo("f_name") == 0)
                   result.f_name = (string)table.Rows[0].ItemArray[i];
               if (table.Columns[i].Caption.CompareTo("f_creation_date") == 0)
                   result.f_creation_date = (DateTime)table.Rows[0].ItemArray[i];
               if (table.Columns[i].Caption.CompareTo("f_time_scale") == 0)
                   result.f_time_scale = (int)table.Rows[0].ItemArray[i];
               if (table.Columns[i].Caption.CompareTo("f_model_time") == 0)
                   result.f_model_time = (Int64)table.Rows[0].ItemArray[i];
               if (table.Columns[i].Caption.CompareTo("f_is_modeling_started") == 0)
                   result.f_is_modeling_started = (int)table.Rows[0].ItemArray[i];
           }
           project_pk = result.pk;
           project_name = result.f_name;
           f_time_scale = result.f_time_scale;
           f_model_time = result.f_model_time;
           f_is_modeling = result.f_is_modeling_started;
           return result;           
       }
       public void run() {
           dev_dispatch = new Collection<deviceDispatcher>();
           for (int i = 0; i < devs.Count; i++)
               dev_dispatch.Add(new deviceDispatcher(devs[i]));          
           current_project.load();
           mod_time_provider.init(current_project.f_time_scale, current_project.f_model_time);           
           for (int i = 0; i < MainDispatcher.Instance.proc.Count; i++)
               MainDispatcher.Instance.proc[i].run();
           mod_time_provider.start();
           thr= new Thread(broker);
           thr.Start();    
       }
       public void stop()
       {
           for (int i = 0; i < MainDispatcher.Instance.proc.Count; i++)
               MainDispatcher.Instance.proc[i].stop();
           current_project.f_model_time = mod_time_provider.time();
           current_project.db_commit();
           for (int i = 0; i < dev_dispatch.Count; i++)
               dev_dispatch[i].stop();
           thr.Abort();
       }
       private Point find_dev_coords(int fk_dev)
       {
           for(int i=0;i<vis.Count;i++)
           {
               if (vis[i].fk_device == fk_dev)
                   return new Point((int)vis[i].x_coords, (int)vis[i].y_coords);
           }
           return new Point(0, 0);
       }
       public void draw_all(bool need_redraw)
       {
           try
           {
               if (need_redraw)
                   graph.Clear(SystemColors.Control);
               lock (draw_critycal_section)
               {
                   chan_chache.Clear();
                   for (int i = 0; i < chan.Count; i++)
                   {
                       Point p1 = find_dev_coords(chan[i].fk_device1);
                       Point p2 = find_dev_coords(chan[i].fk_device2);
                       Rect r = new Rect();
                       r.p1 = p1;
                       r.p2 = p2;
                       Pen p = new Pen(Color.Black);
                       p.Width = 3;                      
                       graph.DrawLine(p, p1, p2);
                       chan_chache.Add(r);
                       graph.DrawString(chan[i].f_name + "(" + find_dev(chan[i].fk_device1).f_name + " - " + find_dev(chan[i].fk_device2).f_name + ")", SystemFonts.CaptionFont, Brushes.Green, 10, 10 + i * 12);
                   }
               }
               for (int i = 0; i < vis.Count; i++)
               {
                   if (find_dev(vis[i].fk_device).f_type == 0)
                       graph.DrawImage(m, (int)vis[i].x_coords, (int)vis[i].y_coords);
                   else
                       graph.DrawImage(p, (int)vis[i].x_coords, (int)vis[i].y_coords);
                   graph.DrawString(find_dev(vis[i].fk_device).f_name, SystemFonts.CaptionFont, Brushes.Red, (int)vis[i].x_coords, (int)vis[i].y_coords - 20);
               }
           }catch(Exception exc)
           {
           }
           }
       public void loadProject(int pk)
       {
           lock (load_proj_critycal_section)
           {
               devs = new Collection<device>();
               chan = new Collection<channel>();
               proc = new Collection<process>();
               bproc = new Collection<binding_proc>();
               vis = new Collection<visualisation>();
               chan_chache = new Collection<Rect>();
               this.project_pk = pk;
               current_project = new project(pk);
               current_project.load();
               if (mod_time_provider.time() < current_project.f_model_time)
                   mod_time_provider.init(current_project.f_time_scale, current_project.f_model_time);
               if (dbProvider.Instance.select_query("select * from t_project where pk=" + this.project_pk.ToString()).Rows.Count == 1)
               {
                   DataTable table = dbProvider.Instance.select_query("select * from t_device where fk_project=" + this.project_pk);
                   for (int j = 0; j < table.Rows.Count; j++)
                   {
                       for (int i = 0; i < table.Columns.Count; i++)
                           if (table.Columns[i].Caption.CompareTo("pk") == 0)
                           {
                               devs.Add((device)new device((int)table.Rows[j].ItemArray[i]).load());
                               break;
                           }
                   }
                   table = dbProvider.Instance.select_query("select * from t_channel where fk_project=" + this.project_pk);
                   for (int j = 0; j < table.Rows.Count; j++)
                   {
                       for (int i = 0; i < table.Columns.Count; i++)
                           if (table.Columns[i].Caption.CompareTo("pk") == 0)
                           {
                               chan.Add((channel)new channel((int)table.Rows[j].ItemArray[i]).load());
                               break;
                           }
                   }
                   for (int k = 0; k < devs.Count; k++)
                   {
                       table = dbProvider.Instance.select_query("select * from t_process where fk_device=" + devs[k].pk);
                       for (int j = 0; j < table.Rows.Count; j++)
                       {
                           for (int i = 0; i < table.Columns.Count; i++)
                               if (table.Columns[i].Caption.CompareTo("pk") == 0)
                               {
                                   proc.Add((process)new process((int)table.Rows[j].ItemArray[i]).load());
                                   break;
                               }
                       }
                   }
                   for (int k = 0; k < proc.Count; k++)
                   {
                       table = dbProvider.Instance.select_query("select * from t_proc_binding where proc_fk=" + proc[k].pk);
                       for (int j = 0; j < table.Rows.Count; j++)
                       {
                           for (int i = 0; i < table.Columns.Count; i++)
                               if (table.Columns[i].Caption.CompareTo("pk") == 0)
                               {
                                   bproc.Add((binding_proc)new binding_proc((int)table.Rows[j].ItemArray[i]).load());
                                   break;
                               }
                       }
                   }
                   for (int k = 0; k < devs.Count; k++)
                   {
                       table = dbProvider.Instance.select_query("select * from visualisation where fk_project=" + current_project.pk + " and fk_device=" + devs[k].pk);
                       for (int j = 0; j < table.Rows.Count; j++)
                       {
                           int pr = 0, dev = 0;
                           for (int i = 0; i < table.Columns.Count; i++)
                           {
                               if (table.Columns[i].Caption.CompareTo("fk_project") == 0)
                               {
                                   pr = (int)table.Rows[j].ItemArray[i];
                               }
                               if (table.Columns[i].Caption.CompareTo("fk_device") == 0)
                               {
                                   dev = (int)table.Rows[j].ItemArray[i];
                               }
                           }
                           vis.Add((visualisation)new visualisation(pr, dev).load());
                       }
                   }
                   draw_all(false);
               }
           }
       }
       public void delete_dev(int pk)
       {
           dbProvider.Instance.mysql_command("delete from t_device where pk=" + pk);
           loadProject(current_project.pk);
       }
       public void deleteProject(int pk)
       {
           this.project_pk = pk;
           current_project = new project(pk);
           current_project.delete();
       }
       public void updateTimeScale(int newScale, int pk)
       {
           dbProvider.Instance.mysql_command("UPDATE  `t_project` SET  `f_time_scale` =  '"+newScale+"' WHERE  pk=" + pk);
           loadProject(current_project.pk);
       }
    }
}
