using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Configuration;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data.SqlClient;

namespace AskMe_Web_UI {
    public partial class NewPage : System.Web.UI.Page {
        protected void Page_Load(object sender, EventArgs e) {
            if (IsPostBack) {
                string input = captchaCode.Text;
                message.Visible = true;
                captchaCode.Text = null;

                // Check that a URL was actually provided.
                if (URL.Text == null || URL.Text == "") {
                    message.InnerText = "Please enter a URL to submit!";
                    return;
                }

                // Check that the URL is valid (or valid enough to be made into a URI) before submitting it.
                try {
                    Uri testURI = new Uri(URL.Text);
                    if (testURI.Scheme != Uri.UriSchemeHttp && testURI.Scheme != Uri.UriSchemeHttps) {
                        URL.Text = null;
                        message.InnerText = "The URL you provided is not valid. Please try again. (Only HTTP & HTTPS URLs are accepted).";
                        return;
                    }
                } catch (UriFormatException err) {
                    URL.Text = null;
                    message.InnerText = "The URL you provided is not valid. Please try again. (Only HTTP & HTTPS URLs are accepted).";
                    return;
                }
                try {
                    if (submissionCaptcha.Validate(input)) {
                        // Connect to the DB.
                        SqlConnection dbConn;
                        try {
                            dbConn = new SqlConnection($"Server={WebConfigurationManager.AppSettings["sqlServer"]}; Database={WebConfigurationManager.AppSettings["sqlDB"]}; User Id={WebConfigurationManager.AppSettings["sqlUsername"]}; Password={WebConfigurationManager.AppSettings["sqlPassword"]}");
                            dbConn.Open();
                        } catch (Exception err) {
                            message.InnerText = "Oops! Something went wrong! Please try again later. Error while connecting to database.";
                            return;
                        }
                        // Add the submitted page to the quarantine.
                        try {
                            string safeURL = URL.Text.Replace("'", "''");
                            using (SqlCommand cmd = new SqlCommand($"IF NOT EXISTS (SELECT * FROM Quarantine WHERE url = '{safeURL}') INSERT INTO Quarantine (url) VALUES ('{safeURL}');", dbConn)) {
                                cmd.ExecuteNonQuery();
                            }
                            URL.Text = null;
                            message.InnerText = "Page submitted successfully! Your submission will be reviewed by an admin. You may now return to AskMe by clicking on the logo at the top, or submit another URL.";
                        } catch (Exception err) {
                            message.InnerText = $"Oops! Something went wrong! Please try again later. {err.Message}";
                        }
                        dbConn.Close();
                    } else {
                        message.InnerText = "Unable to verify your captcha. Please try again.";
                    }
                } catch (Exception err) {
                    message.InnerText = $"Oops! Something went wrong! Please try again later. {err.Message}";
                    return;
                }
            } else {
                captchaCode.Text = null;
                URL.Text = null;
                message.Visible = false;
            }
        }
    }
}