<%@ Page Title="" Language="C#" MasterPageFile="~/MasterPages/AskMe.Master" AutoEventWireup="true" CodeBehind="Search.aspx.cs" Inherits="AskMe_Web_UI.Search" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <title id="title" runat="server">AskMe Search Page</title>
    <style>
        .stats {
            font-size: 12px;
        }
        .location {
            font-size: 12px;
        }
        .preview {
            font-size: 12px;
        }
    </style>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="body" runat="server">
    <form id="query" action="/Search.aspx" method="get">
        <input class="searchBar" type="text" name="q" id="search"/>
        <input type="submit" value="Search!" />
    </form>
    <asp:Panel ID="results" runat="server"></asp:Panel>
    <form id="posControls" runat="server">
        <asp:Button ID="back" runat="server" Visible="False" Text="Back"/>
        <asp:Button ID="next" runat="server" Visible="False" Text="Next"/>
    </form>
</asp:Content>
