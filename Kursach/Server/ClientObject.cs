using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Server
{
    public class ClientObject
    {
        protected internal string Id { get; private set; }
        protected internal int GroupId = -1;
        protected internal TcpClient client;
        ServerObject server;
        Thread CommandThread;

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="tcpClient"></param>
        /// <param name="serverObject"></param>
        public ClientObject(TcpClient tcpClient, ServerObject serverObject)
        {
            Id = Guid.NewGuid().ToString();
            client = tcpClient;
            server = serverObject;
            serverObject.AddConnection(this);
        }

        /// <summary>
        /// Создать новый поток и отправить сообщение
        /// </summary>
        public void Process()
        {
            try
            {
                CommandThread = new Thread(RecieveCommand);
                CommandThread.Start();
                string header = "Command:" + "Connected";
                server.SendMessage(header, Id);
                Console.WriteLine(Id + " connected.");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// Получение текущей команды от клиента
        /// </summary>
        private void RecieveCommand()
        {
            while(true)
            {
                byte[] rawdata = new byte[1024];//буфер на 1 кб
                string headerStr = String.Empty;//страничка нашего хедера, будет висеть пока ничего не подключится
                client.Client.Receive(rawdata);
                headerStr = Encoding.UTF8.GetString(rawdata, 0, rawdata.Length);//расшифровываем буфер
                string[] splitted = headerStr.Split(new string[] { "\r\n" }, StringSplitOptions.None);//разделитель
                Dictionary<string, string> headers = new Dictionary<string, string>();//словарь для хедеров
                foreach (string s in splitted)
                {
                    if (s.Contains(":"))
                    {
                        headers.Add(s.Substring(0, s.IndexOf(":")), s.Substring(s.IndexOf(":") + 1));
                    }
                }
                if (headers.ContainsKey("Command"))//если в хедере комманда, прогоняем
                {
                    string Command = headers["Command"].Trim('\0');//т.к. буфер на 1024 байта, а может слаться меньше, мы удаляем /0
                    if (Command.Equals("JoinGroup"))
                    {
                        GroupId = int.Parse(headers["GroupId"].Trim('\0'));//преобразует в int
                        Console.WriteLine(Id + " tried to join group " + GroupId);
                        server.AddToGroup(this, GroupId);//добавляем
                        string header = $"Command:ConnectedToGroup\r\nGroupId:{GroupId}";
                        server.SendMessage(header, Id);//отправляем ему
                    }
                    if (Command.Equals("GetGroups"))
                    {
                        Console.WriteLine(Id + " requested groups list");
                        server.GetGroups(Id);
                        Console.WriteLine("List of availiable groups was sent to " + Id);
                    }
                    if (Command.Equals("CreateGroup"))
                    {
                        int grid = int.Parse(headers["GroupId"].Trim('\0'));
                        Console.WriteLine(Id + " tried to create group " + grid);
                        if(server.CreateGroup(grid, Id))
                        {
                            Console.WriteLine("Group " + grid + " was created");
                            server.BroadcastAll("Command:Refresh");//отправляет инф. что надо перезагрузить список групп
                        }
                        else
                            Console.WriteLine("Group " + grid + " already exists");
                    }
                    if (Command.Equals("SendMessage"))
                    {
                        int grid = int.Parse(headers["GroupId"].Trim('\0'));
                        string msg = headers["Message"].Trim('\0');
                        string header =  $"Command:BroadCasting\r\nMessage:{msg}";//говорим что это просто сообщение в чат
                        Console.WriteLine(Id + $" tried send message \"{msg}\" to group " + grid);//отправляем
                        server.BroadcastMessage(header, Id, grid);
                        Console.WriteLine("Message sent");
                    }
                    if (Command.Equals("Disconnect"))
                    {
                        string Message = String.Empty;
                        if (!String.IsNullOrWhiteSpace(Id))//проверяет пустая строка или нет
                        {
                            string header = "Command:" + "Disconnected";
                            server.SendMessage(header, Id);
                            Console.WriteLine(Id + " disconnected.");
                            GroupId = -1;
                        }
                        server.RemoveConnection(Id);
                        Close();
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Разорвать соединение
        /// </summary>
        protected internal void Close()
        {
            if (client != null)
                client.Close();
        }
    }
}