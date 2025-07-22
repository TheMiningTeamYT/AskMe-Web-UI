using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data.SqlClient;
using System.Web.Configuration;

namespace AskMe_Web_UI {
    public partial class Go : System.Web.UI.Page {
        protected void Page_Load(object sender, EventArgs e) {
            // Can't redirect if the client isn't connected anymore.
            if (!Response.IsClientConnected) {
                Response.End();
            }
            string next = Request.QueryString["next"];
            // If no next url was provided, redirect them to root.
            if (next == null || next == "") {
                Response.Redirect("/", false);
                return;
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
            // Parse the URL from the query string.
            try {
                // Update the database.
                bool success;
                using (SqlCommand cmd = new SqlCommand($"UPDATE Pages SET clicks = clicks + 1 WHERE url = '{next.Replace("'", "''")}';", dbConn)) {
                    success = cmd.ExecuteNonQuery() == 1;
                }
                dbConn.Close();

                // Make sure that we sucessfully updated the database.
                if (!success) {
                    message.InnerText = $"Oops! Something went wrong. Please try again later. URL: {next}";
                    return;
                }

                // Redirect to next page.
                Response.Redirect(next, false);
            } catch (Exception err) {
                message.InnerText = $"Oops! Something went wrong. Please try again later. {err.Message}";
            }
        }
    }
}