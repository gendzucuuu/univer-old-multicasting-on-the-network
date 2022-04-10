using System;
using System.Windows.Forms;

namespace Kursach
{
    static class Program
    {
        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        [STAThread]//означает что приложение однопоточное
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ChatForm());
        }
    }
}
