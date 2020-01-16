using System;
using System.Windows.Forms;

public static class Prompt
{
    public static string ShowDialog(string text, string caption)
    {
        Form prompt = new Form();
        prompt.Width = 500;
        prompt.Height = 200;
        prompt.FormBorderStyle = FormBorderStyle.FixedSingle;
        prompt.MaximizeBox = false;
        prompt.MinimizeBox = false;
        prompt.Text = caption;
        Label textLabel = new Label() { Left = 50, Top = 20, Text = text };
        TextBox inputBox = new TextBox() { Left = 50, Top = 50, Width = 400 };
        Button confirmation = new Button() { Text = "Ok", Left = 350, Width = 100, Top = 70 };
        Button cancel = new Button() { Text = "Cancel", Left = 50, Width = 100, Top = 70 };

        confirmation.Click += (sender, e) => { prompt.Close(); };

        cancel.Click += (sender, e) => {
            inputBox.Text = "";
            prompt.Close(); };

        inputBox.KeyDown += (sender, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                prompt.Close();
            }
            else if (e.KeyCode == Keys.Escape)
            {
                inputBox.Text = "";
                prompt.Close();
            }
        };

        try {
            prompt.Icon = new System.Drawing.Icon("gears.ico");
        }catch(Exception e)
        {
        }
        
        prompt.Controls.Add(inputBox);
        prompt.Controls.Add(confirmation);
        prompt.Controls.Add(cancel);
        prompt.Controls.Add(textLabel);
        prompt.ShowDialog();
        return inputBox.Text;
    }
}