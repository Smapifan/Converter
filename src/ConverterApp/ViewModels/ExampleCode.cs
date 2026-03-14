namespace ConverterApp.ViewModels;

/// <summary>
/// Built-in example code snippets for quick-testing all conversion paths.
/// </summary>
internal static class ExampleCode
{
    public const string HtmlExample = """
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8" />
  <title>Sample Form</title>
</head>
<body>
  <div id="app" class="container">
    <h1>Login</h1>
    <form id="loginForm" onsubmit="handleLogin()">
      <label for="username">Username</label>
      <input type="text" id="username" name="username" placeholder="Enter username" />

      <label for="password">Password</label>
      <input type="password" id="password" name="password" placeholder="Enter password" />

      <input type="checkbox" id="remember" />
      <label for="remember">Remember me</label>

      <button type="submit" onclick="handleLogin()">Login</button>
    </form>
    <p id="message"></p>
  </div>
</body>
</html>
""";

    public const string CssExample = """
.container {
  padding: 24px;
  max-width: 400px;
  background-color: #ffffff;
}

h1 {
  font-size: 28px;
  font-weight: bold;
  color: #333333;
}

label {
  font-size: 14px;
  color: #555555;
}

input[type="text"],
input[type="password"] {
  width: 100%;
  height: 40px;
  border: 1px solid #cccccc;
  border-radius: 4px;
  padding: 8px;
  margin: 4px 0 16px 0;
}

button {
  background-color: #512bd4;
  color: #ffffff;
  height: 44px;
  width: 100%;
  border-radius: 8px;
  font-size: 16px;
  font-weight: bold;
}
""";

    public const string JsExample = """
function handleLogin() {
  var username = document.getElementById('username').value;
  var password = document.getElementById('password').value;
  if (username && password) {
    document.getElementById('message').textContent = 'Logging in…';
  }
}

document.getElementById('username').addEventListener('focus', onUsernameFocus);
document.getElementById('password').addEventListener('keydown', onPasswordKeydown);
""";

    public const string MauiXamlExample = """
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="MyApp.MainPage"
             Title="Login">
    <ScrollView>
        <VerticalStackLayout Padding="24" Spacing="8">
            <Label Text="Login"
                   FontSize="Title"
                   FontAttributes="Bold"
                   TextColor="#333333" />

            <Label Text="Username"
                   FontSize="14"
                   TextColor="#555555" />
            <Entry x:Name="username"
                   Placeholder="Enter username"
                   WidthRequest="360"
                   HeightRequest="40" />

            <Label Text="Password"
                   FontSize="14"
                   TextColor="#555555" />
            <Entry x:Name="password"
                   IsPassword="True"
                   Placeholder="Enter password"
                   WidthRequest="360"
                   HeightRequest="40" />

            <CheckBox x:Name="remember" IsChecked="False" />
            <Label Text="Remember me" />

            <Button Text="Login"
                    BackgroundColor="#512bd4"
                    TextColor="White"
                    CornerRadius="8"
                    HeightRequest="44"
                    Clicked="OnLogin_Clicked" />

            <Label x:Name="message" />
        </VerticalStackLayout>
    </ScrollView>
</ContentPage>
""";

    public const string MauiCsExample = """
using Microsoft.Maui.Controls;

namespace MyApp;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }

    private void OnLogin_Clicked(object? sender, EventArgs e)
    {
        if (!string.IsNullOrEmpty(username.Text) && !string.IsNullOrEmpty(password.Text))
        {
            message.Text = "Logging in…";
        }
    }
}
""";

    public const string WinFormsExample = """
namespace MyWinFormsApp
{
    partial class Form1
    {
        private System.Windows.Forms.Button btnLogin;
        private System.Windows.Forms.TextBox txtUsername;
        private System.Windows.Forms.TextBox txtPassword;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblUsername;
        private System.Windows.Forms.Label lblPassword;
        private System.Windows.Forms.CheckBox chkRemember;

        private void InitializeComponent()
        {
            this.Text = "Login";
            this.ClientSize = new System.Drawing.Size(400, 320);

            this.lblTitle.Text = "Login";
            this.lblTitle.Location = new System.Drawing.Point(16, 16);
            this.lblTitle.Size = new System.Drawing.Size(368, 36);
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 18F, System.Drawing.FontStyle.Bold);

            this.lblUsername.Text = "Username";
            this.lblUsername.Location = new System.Drawing.Point(16, 64);
            this.lblUsername.Size = new System.Drawing.Size(368, 20);

            this.txtUsername.Text = "";
            this.txtUsername.Location = new System.Drawing.Point(16, 88);
            this.txtUsername.Size = new System.Drawing.Size(368, 30);

            this.lblPassword.Text = "Password";
            this.lblPassword.Location = new System.Drawing.Point(16, 128);
            this.lblPassword.Size = new System.Drawing.Size(368, 20);

            this.txtPassword.Text = "";
            this.txtPassword.Location = new System.Drawing.Point(16, 152);
            this.txtPassword.Size = new System.Drawing.Size(368, 30);

            this.chkRemember.Text = "Remember me";
            this.chkRemember.Location = new System.Drawing.Point(16, 196);
            this.chkRemember.Size = new System.Drawing.Size(200, 24);

            this.btnLogin.Text = "Login";
            this.btnLogin.Location = new System.Drawing.Point(16, 232);
            this.btnLogin.Size = new System.Drawing.Size(368, 44);
            this.btnLogin.BackColor = System.Drawing.Color.FromArgb(81, 43, 212);
            this.btnLogin.ForeColor = System.Drawing.Color.White;

            this.btnLogin.Click += new System.EventHandler(btnLogin_Click);
        }

        private void btnLogin_Click(object sender, System.EventArgs e)
        {
            MessageBox.Show("Logging in...");
        }
    }
}
""";
}
