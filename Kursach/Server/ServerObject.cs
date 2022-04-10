using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Server
{
    public class ServerObject
    {
        static TcpListener tcpListener;//чтобы слушать входящие подключения
        List<ClientObject> clients = new List<ClientObject>();//список пользователей
        Grups Groups = new Grups();
        /// <summary>
        /// Контейнер групп
        /// </summary>
        private class Grups
        {
            private List<Group> grups = new List<Group>();
            public void Add(ClientObject cl, int grupid)
            {
                grups.Find(x => x.GroupId == grupid).Add(cl);//прогоняет через все группы и добавляет пользователя
            }
            public void Remove(ClientObject cl, int grupid)
            {
                grups.Find(x => x.GroupId == grupid).Remove(cl);
            }
            public void AddNewGroup(int grupid)
            {
                grups.Add(new Group() { GroupId = grupid, Clients = new List<ClientObject>() });
            }

            /// <summary>
            /// Проверить на существование группы
            /// </summary>
            /// <param name="grupid"></param>
            /// <returns></returns>
            public bool Exists(int grupid)
            {
                foreach(var g in grups)
                {
                    if (g.GroupId.Equals(grupid))
                        return true;
                }
                return false;
            }

            /// <summary>
            /// Получить список всех доступных групп через пробел
            /// </summary>
            /// <returns></returns>
            public string Get()
            {
                string st = String.Empty;
                foreach(var g in grups)
                {
                    st += g.GroupId + " ";
                }
                return st;
            }
        }
        /// <summary>
        /// Группа
        /// </summary>
        private class Group
        {
            public Int32 GroupId;
            public List<ClientObject> Clients;//массив клиентов в группе
            public void Add(ClientObject cl)
            {
                Clients.Add(cl);
            }
            public void Remove(ClientObject cl)
            {
                Clients.Remove(cl);
            }
        }

        /// <summary>
        /// Добавить пользователя в группы
        /// </summary>
        /// <param name="cl"></param>
        /// <param name="grupid"></param>
        protected internal void AddToGroup(ClientObject cl, int grupid)//интернал виден только внутри namespase Server
        {
            if (cl.GroupId == grupid)
                return;
            else
            {
                Groups.Add(cl, grupid);
                cl.GroupId = grupid;
            }
        }

        /// <summary>
        /// Передать список всех групп
        /// </summary>
        /// <param name="id"></param>
        protected internal void GetGroups(string id)
        {
            string header = $"Command:Groups\r\nList:{Groups.Get()}";
            SendMessage(header, id);
        }

        /// <summary>
        /// Создать группу
        /// </summary>
        /// <param name="grupid"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        protected internal bool CreateGroup(int grupid, string id)
        {
            if (!Groups.Exists(grupid))
            {
                Groups.AddNewGroup(grupid);
                SendMessage("Command:GroupCreated", id);
                return true;
            }
            else
                SendMessage("Command:GroupExists", id);
            return false;
        }

        /// <summary>
        /// Добавить соеденение
        /// </summary>
        /// <param name="clientObject">Объект клиента</param>
        protected internal void AddConnection(ClientObject clientObject)
        {
            clients.Add(clientObject);
        }

        /// <summary>
        /// Разорвать соеденение от определенного пользователя
        /// </summary>
        /// <param name="id">ID пользователя</param>
        protected internal void RemoveConnection(string id)
        {
            ClientObject client = clients.FirstOrDefault(x => x.Id == id);
            if (client != null)
                clients.Remove(client);
        }

        /// <summary>
        /// Cлушать сокет
        /// </summary>
        protected internal void Listen()
        {
            try
            {
                tcpListener = new TcpListener(IPAddress.Parse("127.0.0.1"), 8888);//инициализация
                tcpListener.Start();
                Console.WriteLine("Server started. Waiting for connections...");          
                while (true)//начинаем слушать сокет, пока не будет подключения
                {
                    TcpClient tcpClient = tcpListener.AcceptTcpClient();
                    Console.WriteLine($"Incoming connection from {tcpClient.Client.RemoteEndPoint}");//выводит ip пользователя
                    ClientObject clientObject = new ClientObject(tcpClient, this);
                    Thread clientThread = new Thread(new ThreadStart(clientObject.Process));
                    clientThread.Start();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Disconnect();
            }
        }

        /// <summary>
        /// Отправить сообщение всем пользователям указанной группы
        /// </summary>
        /// <param name="message"></param>
        /// <param name="id"></param>
        /// <param name="groupid"></param>
        protected internal void BroadcastMessage(string message, string id, int groupid)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            for (int i = 0; i < clients.Count; i++)
            {
                if(clients[i].GroupId.Equals(groupid))
                    clients[i].client.Client.Send(data);
            }
        }

        /// <summary>
        /// Отправить всем
        /// </summary>
        /// <param name="message"></param>
        protected internal void BroadcastAll(string message)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            for (int i = 0; i < clients.Count; i++)
            {
                clients[i].client.Client.Send(data);
            }
        }

        /// <summary>
        /// Отправить сообщение определенному пользователю
        /// </summary>
        /// <param name="message"></param>
        /// <param name="id"></param>
        protected internal void SendMessage(string message, string id)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            for (int i = 0; i < clients.Count; i++)
            {
                if (clients[i].Id == id)
                {
                    clients[i].client.Client.Send(data);
                    break;
                }
            }
        }

        /// <summary>
        /// Отключить всех пользователей
        /// </summary>
        protected internal void Disconnect()
        {
            tcpListener.Stop(); 

            foreach(var client in clients)
            {
                client.Close(); 
            }
            Environment.Exit(0); 
        }
    }
}