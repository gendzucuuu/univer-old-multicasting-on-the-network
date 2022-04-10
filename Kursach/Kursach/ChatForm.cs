using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Kursach
{
    public partial class ChatForm : Form
    {
        delegate void TextDelegate(string text);//делегат
        delegate void ComboBoxClearer();//делегат
        String alfavit = "qwertyuiopasdfghjklzxcvbnmйцукенгшщзхъфывапролджэячсмитьбю";//алфавит
        Listener listener;
        string TempNum = String.Empty;
        public ChatForm()
        {
            InitializeComponent();
            listener = Listener.getInstance();
            Thread ListenThread = new Thread(Listen);
            ListenThread.Start();
        }
        /// <summary>
        /// Проверяет, есть ли в нашей строке эти символы
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        private bool LetterCheck(string msg)
        {
            foreach(var ch in alfavit)
            {
                foreach(var chh in msg)
                {
                    if (chh.ToString().ToLower().Equals(ch.ToString().ToLower()))
                        return false;
                }
            }
            return true;
        }
        /// <summary>
        /// Отправить сообщение
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonSend_Click(object sender, EventArgs e)
        {
            if(String.IsNullOrWhiteSpace(textBoxMessage.Text))
            {
                MessageBox.Show($"Введите сообщение", $"Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (comboBoxGroupIds.SelectedItem != null)
                listener.client.Client.Send(Encoding.UTF8.GetBytes($"Command:SendMessage\r\nMessage:{textBoxMessage.Text}\r\nGroupId:{comboBoxGroupIds.SelectedItem.ToString()}"));
            else
                MessageBox.Show($"Вы не подключены ни к одной группе", $"Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        /// <summary>
        /// Сравнивает запросы
        /// </summary>
        public void Listen()
        {
            listener.client.Client.Send(Encoding.UTF8.GetBytes($"Command:GetGroups"));//получаем список групп
            while (true)// тоже что и в ClientObject
            {
                byte[] rawdata = new byte[1024];
                string headerStr = String.Empty;
                listener.client.Client.Receive(rawdata);
                headerStr = Encoding.UTF8.GetString(rawdata, 0, rawdata.Length);
                string[] splitted = headerStr.Split(new string[] { "\r\n" }, StringSplitOptions.None);
                Dictionary<string, string> headers = new Dictionary<string, string>();
                foreach (string s in splitted)
                {
                    if (s.Contains(":"))
                    {
                        headers.Add(s.Substring(0, s.IndexOf(":")), s.Substring(s.IndexOf(":") + 1));
                    }
                }
                if (headers.ContainsKey("Command"))
                {
                    string Command = headers["Command"].Trim('\0');
                    if (Command.Equals("BroadCasting"))
                    {
                        TextBoxText(headers["Message"].Trim('\0'));
                    }
                    if (Command.Equals("Connected"))
                    {
                        TextBoxText("Вы были подключены к чату\r\n");
                    }
                    if (Command.Equals("GroupCreated"))
                    {
                        MessageBox.Show($"Группа успешно создана", $"Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    if (Command.Equals("GroupExists"))
                    {
                        MessageBox.Show($"Такая группа уже существует", $"Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    if (Command.Equals("ConnectedToGroup"))
                    {
                        var grid = headers["GroupId"].Trim('\0');
                        MessageBox.Show($"Вы подключились к группе {grid}", $"Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    if (Command.Equals("Groups"))
                    {
                        ComboBoxClear();
                        var st = headers["List"].Trim('\0');
                        var it = st.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach(var i in it)
                        {
                            ComboBoxAdd(i);
                        }
                    }
                    if (Command.Equals("Refresh"))
                    {
                        listener.client.Client.Send(Encoding.UTF8.GetBytes($"Command:GetGroups"));
                    }
                }
            }
        }

        /// <summary>
        /// Добавляет время и текст в чат
        /// </summary>
        /// <param name="text"></param>
        private void TextBoxText(string text)
        {
            if (InvokeRequired)//нужно ли нам вызывать делегат
            {
                BeginInvoke(new TextDelegate(TextBoxText), new object[] { text });
                return;
            }
            else
            {
                textBoxChat.Text += $"[{DateTime.Now.Hour}:{DateTime.Now.Minute}] {text}\n";
            }
        }

        /// <summary>
        /// Добавляет id группы в список
        /// </summary>
        /// <param name="text"></param>
        private void ComboBoxAdd(string text)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new TextDelegate(ComboBoxAdd), new object[] { text });
                return;
            }
            else
            {
                comboBoxGroupIds.Items.Add(text);
            }
        }

        /// <summary>
        /// Очищает список id
        /// </summary>
        private void ComboBoxClear()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new ComboBoxClearer(ComboBoxClear));
                return;
            }
            else
            {
                comboBoxGroupIds.Items.Clear();
            }
        }

        /// <summary>
        /// Закрывает форму
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChatForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }
       
        /// <summary>
        /// Создает группу
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonNewGroup_Click(object sender, EventArgs e)
        {
            if (!String.IsNullOrWhiteSpace(textBoxID.Text) && LetterCheck(TempNum))
                listener.client.Client.Send(Encoding.UTF8.GetBytes($"Command:CreateGroup\r\nGroupId:{TempNum}"));
            else
            {
                MessageBox.Show($"Введите нормальный номер группы", $"Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                textBoxID.Text = String.Empty;
            }               
        }

        /// <summary>
        /// Вызывается, когда меняются данные в текстбоксе
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBoxID_TextChanged(object sender, EventArgs e)
        {
            TempNum = textBoxID.Text;
        }

        /// <summary>
        /// Как только выбираем группу шлет на сервер
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void comboBoxGroupIds_SelectedIndexChanged(object sender, EventArgs e)
        {
            listener.client.Client.Send(Encoding.UTF8.GetBytes($"Command:JoinGroup\r\nGroupId:{comboBoxGroupIds.SelectedItem.ToString()}"));
        }

    }
}
