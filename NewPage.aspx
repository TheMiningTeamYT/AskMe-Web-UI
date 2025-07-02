<%@ Page Title="" Language="C#" MasterPageFile="~/MasterPages/AskMe.Master" AutoEventWireup="true" CodeBehind="NewPage.aspx.cs" Inherits="AskMe_Web_UI.NewPage" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <title>Submit page to AskMe.</title>
    <style>
        .header {
            font-size: 60pt;
            text-align: center;
        }
        .welcome {
            font-family: Arial;
            font-size: 40pt;
            text-align: center;
            color: #000000;
            margin: 0;
        }
        .footer {
            text-align: center;
        }
    </style>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="body" runat="server">
    <form id="pageSubmission" runat="server">
        <table align="center">
            <tr><td><p class="welcome">Welcome!</p></td></tr>
            <tr>
                <td>
                    <p style="text-align: center;">
                        Thank you for your interest in submitting a page to AskMe.<br />
                        To proceed, please complete the captcha below, then enter the page you wish to index.
                    </p>
                </td>
            </tr>
            <tr><td><center><p ID="message" runat="server" Visible="false"></p></center></td></tr>
            <tr><td><center><BotDetect:WebFormsCaptcha ID="submissionCaptcha" UserInputID="captchaCode" runat="server" /></center></td></tr>
            <tr><td><center><asp:Label ID="captchaLabel" runat="server" Text="Captcha: "/><asp:TextBox ID="captchaCode" runat="server" Width="200px"></asp:TextBox></center></td></tr>
            <tr><td><center><asp:Label ID="urlLabel" runat="server" Text="URL to submit: "/><asp:TextBox ID="URL" runat="server" Width="200px"></asp:TextBox></center></td></tr>
            <tr><td><center><asp:Button ID="submit" text="Submit" runat="server"/></center></td></tr>
        </table>
    </form>
</asp:Content>
