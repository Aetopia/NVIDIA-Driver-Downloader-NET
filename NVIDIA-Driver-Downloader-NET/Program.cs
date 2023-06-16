using System;
using System.Windows.Forms;


class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.Run(new Form());
    }
}