using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data.SqlClient;
using System.Web.Configuration;

namespace AskMe_Web_UI {
    public partial class Lucky : System.Web.UI.Page {
        protected void Page_Load(object sender, EventArgs e) {
            // Can't redirect if the client isn't connected anymore.
            if (!Response.IsClientConnected) {
                Response.End();
            }
            // Connect to the DB.
            SqlConnection dbConn;
            try {
                dbConn = new SqlConnection($"Server={WebConfigurationManager.AppSettings["sqlServer"]}; Database={WebConfigurationManager.AppSettings["sqlDB"]}; User Id={WebConfigurationManager.AppSettings["sqlUsername"]}; Password={WebConfigurationManager.AppSettings["sqlPassword"]}");
                dbConn.Open();
            } catch (Exception err) {
                message.InnerText = "Oops! Something went wrong! Please try again later. Error while connecting to database.";
                return;
            }
            try {
                // Redirect to a random page.
                string page = null;
                if (Session["lastPages"] == null) {
                    Session["lastPages"] = new HashSet<string>();
                }
                for (int i = 0; i < 1000 && (page == null || ((HashSet<string>)Session["lastPages"]).Contains(page)); i++) {
                    Random rand = new Random();
                    byte[] IDraw = new byte[8];
                    rand.NextBytes(IDraw);
                    Int64 ID = BitConverter.ToInt64(IDraw, 0);
                    using (SqlCommand cmd = new SqlCommand($"SELECT TOP 1 url FROM Pages WHERE ID >= {ID} ORDER BY ID;", dbConn)) {
                        using (SqlDataReader result = cmd.ExecuteReader()) {
                            if (result.HasRows) {
                                result.Read();
                                page = result.GetString(result.GetOrdinal("url"));
                            }
                        }
                    }
                }
                dbConn.Close();
                if (page == null) {
                    message.InnerText = $"Oops! Something went wrong. Please try again later.";
                    return;
                }
                ((HashSet<string>)Session["lastPages"]).Add(page);
                Response.Cache.SetNoStore();
                Response.Redirect(page, false);
            } catch (Exception err) {
                message.InnerText = $"Oops! Something went wrong. Please try again later. {err.Message}";
            }
        }
    }
}