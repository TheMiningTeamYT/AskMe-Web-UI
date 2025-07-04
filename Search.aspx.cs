using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;
using System.Web.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace AskMe_Web_UI {
    public partial class Search : System.Web.UI.Page {
        private static Regex cleaner = new Regex(@"[^a-z0-9' ]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        protected void Page_Load(object sender, EventArgs e) {
            DateTime start = DateTime.UtcNow;
            string search = Request.QueryString["q"];
            double time = 0;
            List<PageEntry> resultList;
            int page = 0;
            if (search == null) {
                results.Controls.Add(new HtmlGenericControl("p") {InnerText = "Enter a search query to get started!"});
                next.Visible = false;
                back.Visible = false;
                return;
            }

            // Clean and split the query.
            char[] delimiters = { ' ' };
            string[] words = cleaner.Replace(search, " ").ToLower().Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0) {
                results.Controls.Add(new HtmlGenericControl("p") { InnerText = "Enter a search query to get started!" });
                next.Visible = false;
                back.Visible = false;
                return;
            }

            Regex previewFinder;
            Regex previewBolder;

            if (Session["page"] == null || !IsPostBack) {
                // Connect to the DB.
                SqlConnection dbConn;
                try {
                    dbConn = new SqlConnection($"Server={WebConfigurationManager.AppSettings["sqlServer"]}; Database={WebConfigurationManager.AppSettings["sqlDB"]}; User Id={WebConfigurationManager.AppSettings["sqlUsername"]}; Password={WebConfigurationManager.AppSettings["sqlPassword"]}");
                    dbConn.Open();
                } catch (Exception err) {
                    results.Controls.Add(new HtmlGenericControl("p") { InnerText = "Oops! Something went wrong! Please try again later. Error while connecting to database." });
                    next.Visible = false;
                    back.Visible = false;
                    return;
                }

                // Get all the pages matching the query.
                // pageID, score
                Dictionary<Int64, PageScore> pageScores = new Dictionary<Int64, PageScore>();
                DataSet matchingPages;
                SqlDataAdapter adapter;
                try {
                    for (int i = 0; i < words.Length; i++) {
                        matchingPages = new DataSet();
                        adapter = new SqlDataAdapter {
                            SelectCommand = new SqlCommand($"SELECT word, neighbors, pageID FROM PageIndex WHERE word = '{words[i].Replace("'", "''")}';", dbConn)
                        };
                        adapter.Fill(matchingPages);

                        // Score each page.
                        foreach (DataRow row in matchingPages.Tables[0].Rows) {
                            Int64 pageID = (Int64)row["pageID"];
                            HashSet<string> neighbors = JsonConvert.DeserializeObject<HashSet<string>>((string)row["neighbors"]);
                            if (!pageScores.ContainsKey(pageID)) {
                                pageScores[pageID] = new PageScore {
                                    matches = 0,
                                    score = 0
                                };
                            }
                            pageScores[pageID].matches += 1;
                            if (words.Length == 1) {
                                pageScores[pageID].score += 1;
                            } else {
                                pageScores[pageID].score += words.Length - 1;
                                for (int j = 0; j < words.Length; j++) {
                                    if (j != i && neighbors.Contains(words[j])) {
                                        int distance = Math.Abs(i - j);
                                        if (distance == 1) {
                                            pageScores[pageID].score += words.Length;
                                        } else {
                                            pageScores[pageID].score += words.Length - distance;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // Filter the page score dictionary for pages with at least (words.length / 2) matches
                    pageScores = pageScores.Where(i => i.Value.matches >= (words.Length / 2)).ToDictionary(i => i.Key, i => i.Value);

                    // If we didn't find any results, return.
                    if (pageScores.Keys.Count == 0) {
                        dbConn.Close();
                        results.Controls.Add(new HtmlGenericControl("p") { InnerText = "Sorry, your search didn't return any results." });
                        return;
                    }

                    // Find the actual entries for all of the pages we found and compute their final scores.
                    resultList = new List<PageEntry>();
                    matchingPages = new DataSet();
                    adapter = new SqlDataAdapter {
                        SelectCommand = new SqlCommand($"SELECT ID, url, title, contents, clicks FROM Pages WHERE ID IN {to_sql_list(pageScores.Keys)};", dbConn)
                    };
                    adapter.Fill(matchingPages);
                    
                } catch (Exception err) {
                    dbConn.Close();
                    results.Controls.Add(new HtmlGenericControl("p") { InnerText = $"Oops! Something went wrong! Please try again later. {err.Message}" });
                    return;
                }
                dbConn.Close();
                try {
                    foreach (DataRow row in matchingPages.Tables[0].Rows) {
                        Int64 pageID = (Int64)row["ID"];
                        resultList.Add(new PageEntry { url = (string)row["url"], title = (string)row["title"], contents = (HttpUtility.HtmlDecode((string)row["contents"])).Replace('\r', ' ').Replace('\n', ' '), popularity = ((int)row["clicks"] / float.Parse(WebConfigurationManager.AppSettings["clickWeight"]) + 1.0f) * pageScores[pageID].score });
                    }
                    resultList.Sort();
                    // I think this is safe?
                    previewFinder = new Regex($"\\b\\w(?=.{{0,300}}\\b({String.Join("|", words)})\\b).{{0,1000}}\\b(?<=\\w)", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.Singleline);
                    previewBolder = new Regex($"\\b({String.Join("|", words)})\\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                    time = (DateTime.UtcNow - start).TotalSeconds;
                    // Update the session state.
                    Session["time"] = time;
                    Session["results"] = resultList;
                    Session["page"] = 0;
                    Session["previewFinder"] = previewFinder;
                    Session["previewBolder"] = previewBolder;
                } catch (Exception err) {
                    results.Controls.Add(new HtmlGenericControl("p") { InnerText = $"Oops! Something went wrong! Please try again later. {err.Message}" });
                    return;
                }
            } else {
                // Retrieve the session state.
                try {
                    previewFinder = (Regex)Session["previewFinder"];
                    previewBolder = (Regex)Session["previewBolder"];
                    resultList = (List<PageEntry>)Session["results"];
                    time = (double)Session["time"];
                    page = (int)Session["page"];
                    if (Request.Form[back.UniqueID] == "Back") {
                        if (page > 0) {
                            page--;
                        }
                    } else if (Request.Form[next.UniqueID] == "Next") {
                        if (resultList.Count > (page + 1) * 20) {
                            page++;
                        }
                    }
                    Session["page"] = page;
                } catch (Exception err) {
                    results.Controls.Add(new HtmlGenericControl("p") { InnerText = $"Oops! Something went wrong! Please try again later. {err.Message}" });
                    return;
                }
            }
            try {
                HtmlGenericControl stats = new HtmlGenericControl("p");
                stats.InnerText = $"Found {resultList.Count} results in {time:0.###} seconds.";
                stats.Attributes["class"] = "stats";
                results.Controls.Add(stats);
                title.InnerText = "AskMe: " + search;

                for (int i = page * 20; i < (page + 1) * 20 && i < resultList.Count; i++) {
                    HtmlGenericControl container = new HtmlGenericControl("dl");
                    HtmlGenericControl pageName = new HtmlGenericControl("dt");
                    HtmlGenericControl link = new HtmlGenericControl("a");
                    link.InnerText = resultList[i].title;
                    link.Attributes["href"] = $"/Go.aspx?next={Uri.EscapeDataString(resultList[i].url)}";
                    pageName.Controls.Add(link);
                    container.Controls.Add(pageName);
                    // Find the preview
                    // If we don't find anything, fail silently.
                    // I think this is safe?
                    try {
                        Match previewMatch = previewFinder.Match(resultList[i].contents);
                        if (previewMatch.Captures.Count > 0) {
                            HtmlGenericControl previewElement = new HtmlGenericControl("dd");
                            previewElement.InnerHtml = previewBolder.Replace(HttpUtility.HtmlEncode(previewMatch.Value), "<b>$1</b>");
                            previewElement.Attributes["class"] = "preview";
                            container.Controls.Add(previewElement);
                        }
                    } catch (Exception err) { }
                    HtmlGenericControl pageLoc = new HtmlGenericControl("dd");
                    pageLoc.Controls.Add(new HtmlGenericControl("i") {InnerText = Uri.UnescapeDataString(resultList[i].url)});
                    pageLoc.Attributes["class"] = "location";
                    container.Controls.Add(pageLoc);
                    results.Controls.Add(container);
                }
                HtmlGenericControl pages = new HtmlGenericControl("p");
                pages.InnerText = $"Page {page + 1} of {resultList.Count / 20 + 1}, showing 20 results per page.";
                pages.Attributes["class"] = "stats";
                results.Controls.Add(pages);

                // Update the visibility of the buttons.
                if (resultList.Count > (page + 1) * 20) {
                    next.Visible = true;
                } else {
                    next.Visible = false;
                }

                if (page > 0) {
                    back.Visible = true;
                } else {
                    back.Visible = false;
                }
            } catch (Exception err) {
                results.Controls.Add(new HtmlGenericControl("p") { InnerText = $"Oops! Something went wrong! Please try again later. {err.Message}" });
                return;
            }
        }
        // Thank you Yuriy Faktorovich of StackOverflow! https://stackoverflow.com/questions/1567466/icollection-to-string-in-a-good-format-in-c-sharp
        public static string to_sql_list(ICollection<Int64> l) {
            return $"({String.Join(" , ", l.Select(i => i.ToString()).ToArray())})";
        }
    }
    public class PageScore {
        public int matches;
        public int score;
    }
    public class PageEntry : IComparable<PageEntry> {
        public string url;
        public string title;
        public string contents;
        public float popularity;
        public PageEntry() { }
        public int CompareTo(PageEntry other) {
            if (other.popularity > popularity) {
                return 1;
            }
            if (other.popularity < popularity) {
                return -1;
            }
            return 0;
        }
    }
}