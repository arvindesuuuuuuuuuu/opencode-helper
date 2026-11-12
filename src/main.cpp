#include <gtk/gtk.h>
#include <sqlite3.h>
#include <string>
#include <vector>
#include <thread>
#include <atomic>
#include <sstream>
#include <iostream>
#include <fstream>
#include <cstdlib>
#include <cstring>
#include <unistd.h>
#include <sys/wait.h>
#include <sys/stat.h>
#include <chrono>
#include <ctime>
#include <algorithm>

const std::string HOME = std::getenv("HOME") ? std::getenv("HOME") : "/home/arvin";
const std::string UNLIMITED_BIN = HOME + "/Infosoft/OpenCode-Unlimited/bin/opencode-unlimited";
const std::string EGRESS_FILE = HOME + "/.config/opencode/unlimited-egress.json";
const std::string OC_LOG_PATH = HOME + "/.local/share/opencode/log/opencode.log";
const std::string DESKTOP_DB = HOME + "/.config/ai.opencode.desktop/drafts.sqlite";
const std::string DB_PATH = HOME + "/.local/share/opencode/opencode.db";

struct AppWidgets {
    GtkWidget *window;
    GtkWidget *lbl_ip;
    GtkWidget *lbl_quota;
    GtkWidget *combo_proj;
    GtkWidget *btn_auto;
    GtkWidget *btn_reset;
    GtkWidget *btn_clear;
    GtkWidget *btn_launch;
    GtkWidget *lbl_status;
    GtkWidget *text_log;
    GtkWidget *scroll_log;
    GtkWidget *btn_log_clear;
};

static AppWidgets app;
static std::string last_known_ip = "";
static bool is_first_check = true;
static std::atomic<bool> check_in_progress(false);

static std::string get_timestamp() {
    auto now = std::chrono::system_clock::now();
    std::time_t now_time = std::chrono::system_clock::to_time_t(now);
    std::tm tm_buf;
    localtime_r(&now_time, &tm_buf);
    char buf[32];
    std::strftime(buf, sizeof(buf), "[%H:%M:%S] ", &tm_buf);
    return std::string(buf);
}

void append_log(const std::string &text) {
    char *msg = g_strdup(text.c_str());
    g_idle_add([](gpointer data) -> gboolean {
        char *str = (char*)data;
        if (app.text_log) {
            GtkTextBuffer *buf = gtk_text_view_get_buffer(GTK_TEXT_VIEW(app.text_log));
            GtkTextIter end;
            gtk_text_buffer_get_end_iter(buf, &end);
            gtk_text_buffer_insert(buf, &end, str, -1);

            // Auto-scroll to end
            GtkTextMark *mark = gtk_text_buffer_create_mark(buf, NULL, &end, FALSE);
            gtk_text_view_scroll_to_mark(GTK_TEXT_VIEW(app.text_log), mark, 0.0, TRUE, 0.0, 1.0);
            gtk_text_buffer_delete_mark(buf, mark);
        }
        g_free(str);
        return FALSE;
    }, msg);
}

void log_line(const std::string &line) {
    append_log(get_timestamp() + line + "\n");
}

std::string exec_cmd(const std::string &cmd) {
    char buffer[256];
    std::string result = "";
    FILE *pipe = popen(cmd.c_str(), "r");
    if (!pipe) return "";
    while (fgets(buffer, sizeof(buffer), pipe) != NULL) {
        result += buffer;
    }
    pclose(pipe);
    return result;
}

std::string exec_cmd_streaming(const std::string &cmd, bool echo_to_log = true) {
    char buffer[512];
    std::string result = "";
    FILE *pipe = popen(cmd.c_str(), "r");
    if (!pipe) {
        if (echo_to_log) log_line("Error: failed to execute " + cmd);
        return "";
    }
    while (fgets(buffer, sizeof(buffer), pipe) != NULL) {
        result += buffer;
        if (echo_to_log) {
            std::string line = buffer;
            while (!line.empty() && (line.back() == '\n' || line.back() == '\r')) line.pop_back();
            if (!line.empty()) append_log("  " + line + "\n");
        }
    }
    pclose(pipe);
    return result;
}

static gboolean on_status_msg(gpointer user_data) {
    char *msg = (char *)user_data;
    if (app.lbl_status) {
        gtk_label_set_text(GTK_LABEL(app.lbl_status), msg);
    }
    free(msg);
    return FALSE;
}

void set_status(const std::string &msg) {
    g_idle_add(on_status_msg, strdup(msg.c_str()));
}

bool dir_exists(const std::string &path) {
    struct stat st;
    return stat(path.c_str(), &st) == 0 && S_ISDIR(st.st_mode);
}

std::vector<std::string> detect_projects(const std::string &home, std::string &active_out) {
    std::vector<std::string> projects;
    active_out = "";

    // 1. drafts.sqlite
    sqlite3 *db = nullptr;
    if (sqlite3_open_v2(DESKTOP_DB.c_str(), &db, SQLITE_OPEN_READONLY, nullptr) == SQLITE_OK) {
        const char *sql = "SELECT key, value FROM state WHERE name LIKE 'opencode.window%.dat' AND key IN ('tabs.recent', 'tabs.info', 'tabs.closed') ORDER BY updated_at DESC;";
        sqlite3_stmt *stmt = nullptr;
        if (sqlite3_prepare_v2(db, sql, -1, &stmt, nullptr) == SQLITE_OK) {
            std::string active_key = "";
            while (sqlite3_step(stmt) == SQLITE_ROW) {
                const char *k = (const char*)sqlite3_column_text(stmt, 0);
                const char *v = (const char*)sqlite3_column_text(stmt, 1);
                if (!k || !v) continue;
                std::string key = k;
                std::string val = v;

                if (key == "tabs.recent" && active_key.empty()) {
                    size_t pos = val.find("\"key\":\"");
                    if (pos == std::string::npos) pos = val.find("\"key\": \"");
                    if (pos != std::string::npos) {
                        size_t start = val.find('"', pos + 6) + 1;
                        size_t end = val.find('"', start);
                        if (end != std::string::npos) active_key = val.substr(start, end - start);
                    }
                }

                // Extract all directories
                size_t pos = 0;
                while ((pos = val.find("\"directory\":", pos)) != std::string::npos) {
                    size_t quote1 = val.find('"', pos + 12);
                    if (quote1 == std::string::npos) break;
                    size_t quote2 = val.find('"', quote1 + 1);
                    if (quote2 == std::string::npos) break;
                    std::string dir = val.substr(quote1 + 1, quote2 - quote1 - 1);
                    pos = quote2 + 1;

                    if (!dir.empty() && dir_exists(dir)) {
                        if (std::find(projects.begin(), projects.end(), dir) == projects.end()) {
                            projects.push_back(dir);
                        }
                    }
                }
            }
            sqlite3_finalize(stmt);
        }
        sqlite3_close(db);
    }

    // 2. opencode.db
    if (sqlite3_open_v2(DB_PATH.c_str(), &db, SQLITE_OPEN_READONLY, nullptr) == SQLITE_OK) {
        const char *sql = "SELECT worktree FROM project ORDER BY time_active DESC;";
        sqlite3_stmt *stmt = nullptr;
        if (sqlite3_prepare_v2(db, sql, -1, &stmt, nullptr) == SQLITE_OK) {
            while (sqlite3_step(stmt) == SQLITE_ROW) {
                const char *wt = (const char*)sqlite3_column_text(stmt, 0);
                if (wt) {
                    std::string dir = wt;
                    if (dir_exists(dir) && std::find(projects.begin(), projects.end(), dir) == projects.end()) {
                        projects.push_back(dir);
                    }
                }
            }
            sqlite3_finalize(stmt);
        }
        sqlite3_close(db);
    }

    // 3. Fallbacks
    std::vector<std::string> fallbacks = {
        home + "/trickcal-launcher",
        home + "/Projects",
        home
    };
    for (const auto &fb : fallbacks) {
        if (dir_exists(fb) && std::find(projects.begin(), projects.end(), fb) == projects.end()) {
            projects.push_back(fb);
        }
    }

    if (!projects.empty()) active_out = projects[0];
    return projects;
}

void load_projects() {
    std::string active_project;
    std::vector<std::string> projects = detect_projects(HOME, active_project);

    gtk_combo_box_text_remove_all(GTK_COMBO_BOX_TEXT(app.combo_proj));
    gtk_combo_box_text_append_text(GTK_COMBO_BOX_TEXT(app.combo_proj), "All Workspaces (All Sessions)");

    int active_idx = -1;
    int idx = 1;
    for (const auto &p : projects) {
        gtk_combo_box_text_append_text(GTK_COMBO_BOX_TEXT(app.combo_proj), p.c_str());
        if (!active_project.empty() && p == active_project) {
            active_idx = idx;
        }
        idx++;
    }

    if (active_idx >= 0) {
        gtk_combo_box_set_active(GTK_COMBO_BOX(app.combo_proj), active_idx);
        size_t slash = active_project.find_last_of('/');
        std::string base = (slash != std::string::npos) ? active_project.substr(slash + 1) : active_project;
        set_status("Auto-detected: " + base);
    } else {
        gtk_combo_box_set_active(GTK_COMBO_BOX(app.combo_proj), 0);
    }
}

void record_spent_ip(const std::string &ip, const std::string &country) {
    if (ip.empty() || ip == "Offline") return;
    time_t now = time(nullptr);
    tm gmtm;
    gmtime_r(&now, &gmtm);
    char day_buf[32], iso_buf[64];
    strftime(day_buf, sizeof(day_buf), "%Y-%m-%d", &gmtm);
    strftime(iso_buf, sizeof(iso_buf), "%Y-%m-%dT%H:%M:%S.000Z", &gmtm);
    std::string today = day_buf;
    std::string iso_now = iso_buf;

    std::string py = "python3 -c \"import json, os; p='" + EGRESS_FILE + "'; os.makedirs(os.path.dirname(p), exist_ok=True); "
                     "d = json.load(open(p)) if os.path.exists(p) and json.load(open(p)).get('day') == '" + today + "' else {'day': '" + today + "', 'addresses': {}}; "
                     "d.setdefault('addresses', {})['" + ip + "/32'] = {'ip': '" + ip + "', 'country': '" + country + "', 'spent': True, 'lastStatus': 429, 'lastSeen': '" + iso_now + "', 'note': 'OpenCode free usage limit reached'}; "
                     "json.dump(d, open(p, 'w'), indent=2)\" 2>/dev/null";
    int rc = system(py.c_str());
    (void)rc;
}

struct StatusData {
    std::string ip;
    std::string country;
    std::string ip_info;
    std::string quota_text;
    bool is_ready;
    bool is_rate_limited;
    bool ip_changed;
    bool silent;
};

static gboolean on_status_checked(gpointer user_data) {
    StatusData *data = (StatusData *)user_data;

    gtk_label_set_text(GTK_LABEL(app.lbl_ip), data->ip_info.c_str());
    gtk_label_set_text(GTK_LABEL(app.lbl_quota), data->quota_text.c_str());

    GtkStyleContext *ctx = gtk_widget_get_style_context(app.lbl_quota);
    gtk_style_context_remove_class(ctx, "quota-ready");
    gtk_style_context_remove_class(ctx, "quota-limited");

    if (data->is_ready) {
        gtk_style_context_add_class(ctx, "quota-ready");
    } else {
        gtk_style_context_add_class(ctx, "quota-limited");
    }

    if (data->ip_changed && !is_first_check) {
        log_line("🔄 Connection change detected: " + data->ip_info);
        if (data->is_rate_limited) {
            set_status("● Awaiting VPN connection...");
            log_line("✖ Quota: Free Limit Reached (Switch VPN to get a new IP)");
            log_line("Awaiting VPN connection to acquire a fresh IP...");
        } else {
            set_status("● Ready");
            log_line("✔ Quota: Ready (Free Tier Available)");
        }
    } else if (data->is_rate_limited) {
        set_status("● Awaiting VPN connection...");
        if (!data->silent) {
            log_line("Network: " + data->ip_info);
            log_line("Quota status: " + data->quota_text);
        }
    } else if (!data->silent) {
        set_status("● Ready");
        log_line("Network: " + data->ip_info);
        log_line("Quota status: " + data->quota_text);
    }

    if (!data->ip.empty() && data->ip != "Offline") {
        last_known_ip = data->ip;
    }
    is_first_check = false;

    delete data;
    return FALSE;
}

void check_status_worker(bool silent) {
    StatusData *data = new StatusData();
    data->silent = silent;
    data->is_ready = false;
    data->is_rate_limited = false;

    // 1. Fetch IP & Geo
    std::string ip_raw = exec_cmd("curl -s --max-time 4 https://ipinfo.io/json");
    std::string ip = "Offline", city = "", country = "";
    size_t ip_pos = ip_raw.find("\"ip\": \"");
    if (ip_pos == std::string::npos) ip_pos = ip_raw.find("\"ip\":\"");
    if (ip_pos != std::string::npos) {
        size_t start = ip_raw.find('"', ip_pos + 5) + 1;
        size_t end = ip_raw.find('"', start);
        if (end != std::string::npos) ip = ip_raw.substr(start, end - start);
    }
    size_t city_pos = ip_raw.find("\"city\": \"");
    if (city_pos == std::string::npos) city_pos = ip_raw.find("\"city\":\"");
    if (city_pos != std::string::npos) {
        size_t start = ip_raw.find('"', city_pos + 7) + 1;
        size_t end = ip_raw.find('"', start);
        if (end != std::string::npos) city = ip_raw.substr(start, end - start);
    }
    size_t c_pos = ip_raw.find("\"country\": \"");
    if (c_pos == std::string::npos) c_pos = ip_raw.find("\"country\":\"");
    if (c_pos != std::string::npos) {
        size_t start = ip_raw.find('"', c_pos + 10) + 1;
        size_t end = ip_raw.find('"', start);
        if (end != std::string::npos) country = ip_raw.substr(start, end - start);
    }

    data->ip = ip;
    data->country = country;
    if (ip == "Offline") {
        data->ip_info = "🌐 Offline / Connection Error";
    } else {
        data->ip_info = "🌐 " + ip + (city.empty() ? "" : "  •  " + city + ", " + country);
    }

    data->ip_changed = (!ip.empty() && ip != "Offline" && ip != last_known_ip);

    // 1. Check local spent ledger
    if (ip != "Offline") {
        time_t now = time(nullptr);
        tm gmtm;
        gmtime_r(&now, &gmtm);
        char day_buf[32];
        strftime(day_buf, sizeof(day_buf), "%Y-%m-%d", &gmtm);
        std::string today = day_buf;

        std::ifstream ef(EGRESS_FILE);
        if (ef.is_open()) {
            std::stringstream ss;
            ss << ef.rdbuf();
            std::string content = ss.str();
            ef.close();

            if (content.find("\"day\": \"" + today + "\"") != std::string::npos ||
                content.find("\"day\":\"" + today + "\"") != std::string::npos) {
                std::string block_key = "\"" + ip + "/32\"";
                size_t bpos = content.find(block_key);
                if (bpos != std::string::npos) {
                    size_t next_block = content.find("\"/32\"", bpos + block_key.length());
                    std::string segment = content.substr(bpos, next_block != std::string::npos ? next_block - bpos : 500);
                    if (segment.find("\"spent\": true") != std::string::npos ||
                        segment.find("\"spent\":true") != std::string::npos) {
                        data->is_rate_limited = true;
                        data->quota_text = "● Free Limit Reached (Spent IP)";
                    }
                }
            }
        }
    }

    // 2. Check OpenCode SQLite database for active stored limit errors in last 5 minutes
    if (!data->is_rate_limited && ip != "Offline") {
        sqlite3 *db = nullptr;
        if (sqlite3_open_v2(DB_PATH.c_str(), &db, SQLITE_OPEN_READONLY, nullptr) == SQLITE_OK && db) {
            time_t now = time(nullptr);
            long long threshold_ms = ((long long)now - 300) * 1000; // last 5 minutes
            std::string sql = "SELECT 1 FROM session_message WHERE time_created > ? AND (cast(data as text) LIKE '%provider.free-limit%' OR cast(data as text) LIKE '%Free usage limit reached%') LIMIT 1;";
            sqlite3_stmt *stmt = nullptr;
            if (sqlite3_prepare_v2(db, sql.c_str(), -1, &stmt, nullptr) == SQLITE_OK) {
                sqlite3_bind_int64(stmt, 1, threshold_ms);
                if (sqlite3_step(stmt) == SQLITE_ROW) {
                    data->is_rate_limited = true;
                    data->quota_text = "● Free Limit Reached";
                }
                sqlite3_finalize(stmt);
            }
            sqlite3_close(db);
        }
    }

    if (ip == "Offline") {
        data->quota_text = "● Connection Error";
        data->is_ready = false;
    } else if (!data->is_rate_limited) {
        data->quota_text = "● Ready (Free Tier Available)";
        data->is_ready = true;
    } else {
        data->is_ready = false;
    }

    g_idle_add(on_status_checked, data);
}

void trigger_status_check(bool silent = false) {
    if (check_in_progress.exchange(true)) {
        return;
    }
    if (!silent) {
        log_line("Probing IP & OpenCode gateway status...");
        set_status("● Probing gateway...");
    }
    std::thread([silent]() {
        check_status_worker(silent);
        check_in_progress = false;
    }).detach();
}

void on_reset_clicked(GtkButton *btn, gpointer data) {
    const char *proj_text = gtk_combo_box_text_get_active_text(GTK_COMBO_BOX_TEXT(app.combo_proj));
    std::string proj = proj_text ? proj_text : "";
    bool is_all = (proj == "All Workspaces (All Sessions)" || proj.rfind("All Workspaces", 0) == 0);

    log_line("════════════════════════════════════");
    log_line("🚀 1-Click Clean Reset initiated");
    set_status("● Closing OpenCode & resetting...");

    std::thread([proj, is_all]() {
        log_line("Terminating OpenCode instances (safe)...");
        exec_cmd("pkill -x ai.opencode 2>/dev/null; pkill -f 'ai.opencode' 2>/dev/null; pkill -f 'opencode' 2>/dev/null; pkill -f 'opencode-cli serve' 2>/dev/null");
        usleep(300000);

        log_line("Running opencode-unlimited clear across all sessions...");
        exec_cmd_streaming(UNLIMITED_BIN + " clear 2>&1", true);

        if (is_all) {
            std::string active_dummy;
            std::vector<std::string> targets = detect_projects(HOME, active_dummy);
            if (targets.empty()) targets.push_back(HOME);
            log_line("Resetting session IDs across " + std::to_string(targets.size()) + " workspace(s)...");
            for (const auto &t : targets) {
                log_line("  -> Resetting workspace: " + t);
                exec_cmd_streaming(UNLIMITED_BIN + " reset \"" + t + "\" 2>&1", true);
            }
            log_line("✔ Clean reset finished across all workspaces! All chat history preserved.");
            set_status("● Reset done! Launch OpenCode.");
        } else {
            log_line("Resetting target workspace: " + proj);
            exec_cmd_streaming(UNLIMITED_BIN + " reset \"" + proj + "\" 2>&1", true);
            log_line("✔ Clean reset finished! All chat history preserved.");
            set_status("● Reset done! Launch OpenCode.");
        }
        trigger_status_check(true);
    }).detach();
}

void on_clear_banners_clicked(GtkButton *btn, gpointer data) {
    log_line("════════════════════════════════════");
    log_line("🧹 Clearing stored error banners across all sessions...");
    set_status("● Stripping banners...");
    std::thread([]() {
        exec_cmd_streaming(UNLIMITED_BIN + " clear 2>&1", true);
        log_line("✔ Banners stripped! Chat sessions untouched.");
        set_status("● Banners stripped (Tabs safe).");
    }).detach();
}

void on_launch_clicked(GtkButton *btn, gpointer data) {
    log_line("════════════════════════════════════");
    log_line("▶ Launching OpenCode desktop (/opt/OpenCode/ai.opencode)...");
    set_status("● Launching OpenCode...");
    std::thread([]() {
        pid_t pid = fork();
        if (pid == 0) {
            setsid(); // Decouple from manager process group
            execl("/opt/OpenCode/ai.opencode", "ai.opencode", NULL);
            execlp("opencode", "opencode", NULL);
            _exit(0);
        } else if (pid > 0) {
            log_line("✔ OpenCode launched (PID: " + std::to_string(pid) + ")");
            set_status("● OpenCode running.");
        } else {
            log_line("✖ Failed to spawn OpenCode process.");
            set_status("● Launch failed.");
        }
    }).detach();
}

static void apply_modern_css() {
    const char *css =
        "window { background-color: transparent; }\n"
        ".window-surface {\n"
        "    background-color: #0c0d14;\n"
        "    border-radius: 10px;\n"
        "    border: 1px solid rgba(255, 255, 255, 0.09);\n"
        "    box-shadow: 0 12px 32px rgba(0, 0, 0, 0.7);\n"
        "}\n"
        ".titlebar {\n"
        "    background-image: linear-gradient(to bottom, #161826, #12141f);\n"
        "    border-top-left-radius: 9px;\n"
        "    border-top-right-radius: 9px;\n"
        "    border-bottom: 1px solid #1e2235;\n"
        "    padding: 7px 12px;\n"
        "}\n"
        ".app-title {\n"
        "    font-size: 11.5px;\n"
        "    font-weight: 700;\n"
        "    color: #f8fafc;\n"
        "    letter-spacing: 0.2px;\n"
        "}\n"
        ".title-divider {\n"
        "    min-width: 1px;\n"
        "    min-height: 14px;\n"
        "    background-color: #272c44;\n"
        "    margin: 0 6px;\n"
        "}\n"
        ".win-btn {\n"
        "    min-height: 20px;\n"
        "    min-width: 20px;\n"
        "    padding: 0;\n"
        "    font-size: 10px;\n"
        "    font-weight: 700;\n"
        "    border-radius: 10px;\n"
        "    background-color: rgba(255, 255, 255, 0.06);\n"
        "    border: 1px solid rgba(255, 255, 255, 0.1);\n"
        "    color: #94a3b8;\n"
        "    margin-left: 4px;\n"
        "}\n"
        ".win-btn:hover {\n"
        "    background-color: rgba(255, 255, 255, 0.15);\n"
        "    color: #ffffff;\n"
        "}\n"
        ".win-close:hover {\n"
        "    background-color: #ef4444;\n"
        "    border-color: #f87171;\n"
        "    color: #ffffff;\n"
        "}\n"
        ".win-min:hover {\n"
        "    background-color: #f59e0b;\n"
        "    border-color: #fbbf24;\n"
        "    color: #ffffff;\n"
        "}\n"
        ".btn-check {\n"
        "    font-size: 9.5px;\n"
        "    padding: 2px 8px;\n"
        "    min-height: 20px;\n"
        "    border-radius: 4px;\n"
        "    background-color: #1a1e2e;\n"
        "    border: 1px solid #2a304a;\n"
        "    color: #94a3b8;\n"
        "}\n"
        ".btn-check:hover {\n"
        "    background-color: #242940;\n"
        "    color: #ffffff;\n"
        "    border-color: #3b4366;\n"
        "}\n"
        ".content-box { padding: 9px 12px; }\n"
        ".hero-card {\n"
        "    background-image: linear-gradient(180deg, #161826 0%, #11131e 100%);\n"
        "    border-radius: 7px;\n"
        "    border: 1px solid #202436;\n"
        "    padding: 8px 10px;\n"
        "    margin-bottom: 5px;\n"
        "}\n"
        ".guide-card {\n"
        "    background-color: #10121d;\n"
        "    border-radius: 6px;\n"
        "    border: 1px solid #1c2033;\n"
        "    padding: 6px 9px;\n"
        "    margin-bottom: 5px;\n"
        "}\n"
        ".guide-title {\n"
        "    font-size: 9.5px;\n"
        "    font-weight: 700;\n"
        "    color: #93c5fd;\n"
        "    margin-bottom: 2px;\n"
        "}\n"
        ".guide-num {\n"
        "    font-size: 9px;\n"
        "    font-weight: 700;\n"
        "    color: #60a5fa;\n"
        "}\n"
        ".guide-text {\n"
        "    font-size: 9px;\n"
        "    color: #cbd5e1;\n"
        "}\n"
        ".guide-tip-icon {\n"
        "    font-size: 8.5px;\n"
        "    color: #64748b;\n"
        "}\n"
        ".guide-tip-text {\n"
        "    font-size: 8.5px;\n"
        "    color: #64748b;\n"
        "    font-style: italic;\n"
        "}\n"
        ".ip-chip {\n"
        "    font-family: 'JetBrains Mono', 'Consolas', monospace;\n"
        "    font-size: 9.5px;\n"
        "    color: #94a3b8;\n"
        "    letter-spacing: -0.1px;\n"
        "}\n"
        ".quota-ready {\n"
        "    color: #34d399;\n"
        "    background-color: rgba(16, 185, 129, 0.12);\n"
        "    border: 1px solid rgba(16, 185, 129, 0.35);\n"
        "    border-radius: 10px;\n"
        "    padding: 1px 7px;\n"
        "    font-weight: 600;\n"
        "    font-size: 9px;\n"
        "}\n"
        ".quota-limited {\n"
        "    color: #fb7185;\n"
        "    background-color: rgba(244, 63, 94, 0.12);\n"
        "    border: 1px solid rgba(244, 63, 94, 0.35);\n"
        "    border-radius: 10px;\n"
        "    padding: 1px 7px;\n"
        "    font-weight: 600;\n"
        "    font-size: 9px;\n"
        "}\n"
        "button {\n"
        "    min-height: 22px;\n"
        "    min-width: 0;\n"
        "    border-radius: 4px;\n"
        "    padding: 2px 7px;\n"
        "    font-weight: 600;\n"
        "    font-size: 9.5px;\n"
        "    border: 1px solid #262a3d;\n"
        "    background-color: #191c2b;\n"
        "    color: #cbd5e1;\n"
        "    box-shadow: none;\n"
        "    outline: none;\n"
        "    margin: 0;\n"
        "}\n"
        "button:hover {\n"
        "    background-color: #22263b;\n"
        "    color: #ffffff;\n"
        "    border-color: #383e5c;\n"
        "}\n"
        ".btn-action {\n"
        "    background-color: #1b1e2c;\n"
        "    border: 1px solid #2a2f45;\n"
        "    color: #cbd5e1;\n"
        "}\n"
        ".btn-action:hover {\n"
        "    background-color: #252a3f;\n"
        "    border-color: #3d4566;\n"
        "    color: #ffffff;\n"
        "}\n"
        ".btn-auto {\n"
        "    background-color: #1e293b;\n"
        "    border: 1px solid #38bdf8;\n"
        "    color: #38bdf8;\n"
        "    font-weight: 700;\n"
        "    font-size: 9px;\n"
        "    padding: 1px 8px;\n"
        "    min-height: 22px;\n"
        "    border-radius: 4px;\n"
        "}\n"
        ".btn-auto:hover {\n"
        "    background-color: #0284c7;\n"
        "    color: #ffffff;\n"
        "    border-color: #38bdf8;\n"
        "}\n"
        ".hero-btn {\n"
        "    background-image: linear-gradient(to bottom, #2563eb, #1d4ed8);\n"
        "    border: 1px solid #3b82f6;\n"
        "    color: #ffffff;\n"
        "    font-size: 10px;\n"
        "    padding: 5px 10px;\n"
        "    min-height: 27px;\n"
        "    font-weight: 700;\n"
        "    box-shadow: 0 2px 6px rgba(37, 99, 235, 0.28);\n"
        "    border-radius: 5px;\n"
        "}\n"
        ".hero-btn:hover {\n"
        "    background-image: linear-gradient(to bottom, #3b82f6, #2563eb);\n"
        "    border-color: #60a5fa;\n"
        "}\n"
        ".launch-btn {\n"
        "    background-image: linear-gradient(to bottom, #16a34a, #15803d);\n"
        "    border: 1px solid #22c55e;\n"
        "    color: #ffffff;\n"
        "    font-size: 9.5px;\n"
        "    padding: 3px 8px;\n"
        "    min-height: 22px;\n"
        "    font-weight: 700;\n"
        "    border-radius: 4px;\n"
        "    box-shadow: 0 2px 5px rgba(22, 163, 74, 0.22);\n"
        "}\n"
        ".launch-btn:hover {\n"
        "    background-image: linear-gradient(to bottom, #22c55e, #16a34a);\n"
        "}\n"
        "combobox {\n"
        "    min-height: 22px;\n"
        "    background-color: #141622;\n"
        "    color: #f1f5f9;\n"
        "    border: 1px solid #232738;\n"
        "    border-radius: 4px;\n"
        "    font-size: 10px;\n"
        "}\n"
        "combobox:hover { border-color: #353b52; }\n"
        "combobox button {\n"
        "    min-height: 20px;\n"
        "    background-color: transparent;\n"
        "    border: none;\n"
        "    padding: 1px 3px;\n"
        "}\n"
        ".field-label {\n"
        "    font-size: 9.5px;\n"
        "    color: #8a99ad;\n"
        "    font-weight: 600;\n"
        "}\n"
        ".status-bar {\n"
        "    font-size: 9px;\n"
        "    color: #586579;\n"
        "    margin-top: 3px;\n"
        "    font-family: 'JetBrains Mono', monospace;\n"
        "}\n"
        ".log-header {\n"
        "    margin-top: 4px;\n"
        "    margin-bottom: 2px;\n"
        "}\n"
        ".log-title {\n"
        "    font-size: 9.5px;\n"
        "    font-weight: 700;\n"
        "    color: #94a3b8;\n"
        "}\n"
        ".btn-log-clear {\n"
        "    font-size: 8px;\n"
        "    padding: 1px 6px;\n"
        "    min-height: 16px;\n"
        "    background-color: transparent;\n"
        "    border: 1px solid #23283c;\n"
        "    color: #64748b;\n"
        "    border-radius: 3px;\n"
        "}\n"
        ".btn-log-clear:hover {\n"
        "    background-color: #1e2233;\n"
        "    color: #cbd5e1;\n"
        "    border-color: #383e5c;\n"
        "}\n"
        ".log-scroller {\n"
        "    background-color: #07080e;\n"
        "    border-radius: 5px;\n"
        "    border: 1px solid #1c2033;\n"
        "}\n"
        ".log-view {\n"
        "    background-color: #07080e;\n"
        "    color: #94a3b8;\n"
        "    font-family: 'JetBrains Mono', 'Consolas', monospace;\n"
        "    font-size: 8.5px;\n"
        "    padding: 4px 6px;\n"
        "}\n";

    GtkCssProvider *provider = gtk_css_provider_new();
    gtk_css_provider_load_from_data(provider, css, -1, NULL);
    gtk_style_context_add_provider_for_screen(
        gdk_screen_get_default(),
        GTK_STYLE_PROVIDER(provider),
        GTK_STYLE_PROVIDER_PRIORITY_APPLICATION
    );
    g_object_unref(provider);
}

int main(int argc, char *argv[]) {
    gtk_init(&argc, &argv);
    apply_modern_css();

    app.window = gtk_window_new(GTK_WINDOW_TOPLEVEL);
    gtk_window_set_title(GTK_WINDOW(app.window), "OpenCode Helper");
    gtk_window_set_default_size(GTK_WINDOW(app.window), 380, 480);
    gtk_window_set_resizable(GTK_WINDOW(app.window), TRUE);
    gtk_window_set_position(GTK_WINDOW(app.window), GTK_WIN_POS_CENTER);

    // Set application icon if available
    if (access("/usr/share/icons/hicolor/128x128/apps/ai.opencode.png", F_OK) == 0) {
        gtk_window_set_icon_from_file(GTK_WINDOW(app.window), "/usr/share/icons/hicolor/128x128/apps/ai.opencode.png", NULL);
    }

    // Completely frameless window
    gtk_window_set_decorated(GTK_WINDOW(app.window), FALSE);

    // Enable RGBA for soft rounded corners
    GdkScreen *screen = gtk_widget_get_screen(app.window);
    GdkVisual *visual = gdk_screen_get_rgba_visual(screen);
    if (visual && gdk_screen_is_composited(screen)) {
        gtk_widget_set_visual(app.window, visual);
        gtk_widget_set_app_paintable(app.window, TRUE);
    }

    g_signal_connect(app.window, "destroy", G_CALLBACK(gtk_main_quit), NULL);

    // Root Surface Container
    GtkWidget *surface = gtk_box_new(GTK_ORIENTATION_VERTICAL, 0);
    gtk_style_context_add_class(gtk_widget_get_style_context(surface), "window-surface");
    gtk_container_add(GTK_CONTAINER(app.window), surface);

    // ==========================================
    // 1. CUSTOM SLEEK TITLEBAR (Draggable & Centered)
    // ==========================================
    GtkWidget *titlebar_event = gtk_event_box_new();
    GtkWidget *titlebar_box = gtk_box_new(GTK_ORIENTATION_HORIZONTAL, 4);
    gtk_style_context_add_class(gtk_widget_get_style_context(titlebar_box), "titlebar");
    gtk_container_add(GTK_CONTAINER(titlebar_event), titlebar_box);

    // Drag to move window
    g_signal_connect(titlebar_event, "button-press-event", G_CALLBACK(+[](GtkWidget *w, GdkEventButton *e, gpointer win) -> gboolean {
        if (e->type == GDK_BUTTON_PRESS && e->button == GDK_BUTTON_PRIMARY) {
            gtk_window_begin_move_drag(GTK_WINDOW(win), e->button, e->x_root, e->y_root, e->time);
            return TRUE;
        }
        return FALSE;
    }), app.window);

    GtkWidget *app_icon_lbl = gtk_label_new("⚡ OpenCode Helper");
    gtk_widget_set_valign(app_icon_lbl, GTK_ALIGN_CENTER);
    gtk_style_context_add_class(gtk_widget_get_style_context(app_icon_lbl), "app-title");
    gtk_box_pack_start(GTK_BOX(titlebar_box), app_icon_lbl, FALSE, FALSE, 2);

    // Flexible Spacer
    GtkWidget *spacer = gtk_label_new("");
    gtk_widget_set_hexpand(spacer, TRUE);
    gtk_box_pack_start(GTK_BOX(titlebar_box), spacer, TRUE, TRUE, 0);

    // Check button in header
    GtkWidget *btn_check = gtk_button_new_with_label("🔄 Check");
    gtk_widget_set_valign(btn_check, GTK_ALIGN_CENTER);
    gtk_widget_set_size_request(btn_check, -1, 20);
    gtk_style_context_add_class(gtk_widget_get_style_context(btn_check), "btn-check");
    g_signal_connect(btn_check, "clicked", G_CALLBACK(+[](GtkButton *, gpointer) { trigger_status_check(false); }), NULL);
    gtk_box_pack_start(GTK_BOX(titlebar_box), btn_check, FALSE, FALSE, 0);

    // Subtle divider
    GtkWidget *divider = gtk_box_new(GTK_ORIENTATION_HORIZONTAL, 0);
    gtk_widget_set_valign(divider, GTK_ALIGN_CENTER);
    gtk_widget_set_size_request(divider, 1, 14);
    gtk_style_context_add_class(gtk_widget_get_style_context(divider), "title-divider");
    gtk_box_pack_start(GTK_BOX(titlebar_box), divider, FALSE, FALSE, 0);

    // Minimize Button
    GtkWidget *btn_min = gtk_button_new_with_label("─");
    gtk_widget_set_valign(btn_min, GTK_ALIGN_CENTER);
    gtk_widget_set_size_request(btn_min, 20, 20);
    gtk_style_context_add_class(gtk_widget_get_style_context(btn_min), "win-btn");
    gtk_style_context_add_class(gtk_widget_get_style_context(btn_min), "win-min");
    g_signal_connect(btn_min, "clicked", G_CALLBACK(+[](GtkButton *, gpointer win) {
        gtk_window_iconify(GTK_WINDOW(win));
    }), app.window);
    gtk_box_pack_start(GTK_BOX(titlebar_box), btn_min, FALSE, FALSE, 0);

    // Close Button
    GtkWidget *btn_close = gtk_button_new_with_label("✕");
    gtk_widget_set_valign(btn_close, GTK_ALIGN_CENTER);
    gtk_widget_set_size_request(btn_close, 20, 20);
    gtk_style_context_add_class(gtk_widget_get_style_context(btn_close), "win-btn");
    gtk_style_context_add_class(gtk_widget_get_style_context(btn_close), "win-close");
    g_signal_connect(btn_close, "clicked", G_CALLBACK(+[](GtkButton *, gpointer) {
        gtk_main_quit();
    }), NULL);
    gtk_box_pack_start(GTK_BOX(titlebar_box), btn_close, FALSE, FALSE, 0);

    gtk_box_pack_start(GTK_BOX(surface), titlebar_event, FALSE, FALSE, 0);

    // ==========================================
    // 2. CONTENT AREA
    // ==========================================
    GtkWidget *content = gtk_box_new(GTK_ORIENTATION_VERTICAL, 3);
    gtk_style_context_add_class(gtk_widget_get_style_context(content), "content-box");
    gtk_box_pack_start(GTK_BOX(surface), content, TRUE, TRUE, 0);

    // HERO STATUS CARD
    GtkWidget *status_card = gtk_box_new(GTK_ORIENTATION_VERTICAL, 2);
    gtk_style_context_add_class(gtk_widget_get_style_context(status_card), "hero-card");

    app.lbl_ip = gtk_label_new("🌐 Checking network...");
    gtk_widget_set_halign(app.lbl_ip, GTK_ALIGN_START);
    gtk_style_context_add_class(gtk_widget_get_style_context(app.lbl_ip), "ip-chip");
    gtk_box_pack_start(GTK_BOX(status_card), app.lbl_ip, FALSE, FALSE, 0);

    GtkWidget *quota_row = gtk_box_new(GTK_ORIENTATION_HORIZONTAL, 6);
    GtkWidget *q_lbl = gtk_label_new("Quota:");
    gtk_style_context_add_class(gtk_widget_get_style_context(q_lbl), "field-label");
    gtk_box_pack_start(GTK_BOX(quota_row), q_lbl, FALSE, FALSE, 0);

    app.lbl_quota = gtk_label_new("● Checking...");
    gtk_style_context_add_class(gtk_widget_get_style_context(app.lbl_quota), "quota-limited");
    gtk_box_pack_start(GTK_BOX(quota_row), app.lbl_quota, FALSE, FALSE, 0);
    gtk_box_pack_start(GTK_BOX(status_card), quota_row, FALSE, FALSE, 1);

    gtk_box_pack_start(GTK_BOX(content), status_card, FALSE, FALSE, 0);

    // INSTRUCTIONS CARD
    GtkWidget *guide_box = gtk_box_new(GTK_ORIENTATION_VERTICAL, 2);
    gtk_style_context_add_class(gtk_widget_get_style_context(guide_box), "guide-card");

    GtkWidget *guide_title = gtk_label_new("📋 Quick Instructions:");
    gtk_widget_set_halign(guide_title, GTK_ALIGN_START);
    gtk_style_context_add_class(gtk_widget_get_style_context(guide_title), "guide-title");
    gtk_box_pack_start(GTK_BOX(guide_box), guide_title, FALSE, FALSE, 0);

    // Step 1
    GtkWidget *row1 = gtk_box_new(GTK_ORIENTATION_HORIZONTAL, 5);
    GtkWidget *n1 = gtk_label_new("1."); gtk_style_context_add_class(gtk_widget_get_style_context(n1), "guide-num");
    GtkWidget *t1 = gtk_label_new("If Quota is ❌ Free Limit Reached → Connect any VPN");
    gtk_widget_set_halign(t1, GTK_ALIGN_START);
    gtk_style_context_add_class(gtk_widget_get_style_context(t1), "guide-text");
    gtk_box_pack_start(GTK_BOX(row1), n1, FALSE, FALSE, 0);
    gtk_box_pack_start(GTK_BOX(row1), t1, FALSE, FALSE, 0);
    gtk_box_pack_start(GTK_BOX(guide_box), row1, FALSE, FALSE, 0);

    // Step 2
    GtkWidget *row2 = gtk_box_new(GTK_ORIENTATION_HORIZONTAL, 5);
    GtkWidget *n2 = gtk_label_new("2."); gtk_style_context_add_class(gtk_widget_get_style_context(n2), "guide-num");
    GtkWidget *t2 = gtk_label_new("Click '🚀 1-Click Clean Reset' (Preserves all chats)");
    gtk_widget_set_halign(t2, GTK_ALIGN_START);
    gtk_style_context_add_class(gtk_widget_get_style_context(t2), "guide-text");
    gtk_box_pack_start(GTK_BOX(row2), n2, FALSE, FALSE, 0);
    gtk_box_pack_start(GTK_BOX(row2), t2, FALSE, FALSE, 0);
    gtk_box_pack_start(GTK_BOX(guide_box), row2, FALSE, FALSE, 0);

    // Step 3
    GtkWidget *row3 = gtk_box_new(GTK_ORIENTATION_HORIZONTAL, 5);
    GtkWidget *n3 = gtk_label_new("3."); gtk_style_context_add_class(gtk_widget_get_style_context(n3), "guide-num");
    GtkWidget *t3 = gtk_label_new("Click '▶ Launch OpenCode' & reopen chat from list");
    gtk_widget_set_halign(t3, GTK_ALIGN_START);
    gtk_style_context_add_class(gtk_widget_get_style_context(t3), "guide-text");
    gtk_box_pack_start(GTK_BOX(row3), n3, FALSE, FALSE, 0);
    gtk_box_pack_start(GTK_BOX(row3), t3, FALSE, FALSE, 0);
    gtk_box_pack_start(GTK_BOX(guide_box), row3, FALSE, FALSE, 0);

    // Tip Row
    GtkWidget *row4 = gtk_box_new(GTK_ORIENTATION_HORIZONTAL, 5);
    GtkWidget *n4 = gtk_label_new("•"); gtk_style_context_add_class(gtk_widget_get_style_context(n4), "guide-tip-icon");
    GtkWidget *t4 = gtk_label_new("Use '🧹 Clear Banners' if only an error banner is stuck");
    gtk_widget_set_halign(t4, GTK_ALIGN_START);
    gtk_style_context_add_class(gtk_widget_get_style_context(t4), "guide-tip-text");
    gtk_box_pack_start(GTK_BOX(row4), n4, FALSE, FALSE, 0);
    gtk_box_pack_start(GTK_BOX(row4), t4, FALSE, FALSE, 0);
    gtk_box_pack_start(GTK_BOX(guide_box), row4, FALSE, FALSE, 0);

    gtk_box_pack_start(GTK_BOX(content), guide_box, FALSE, FALSE, 0);

    // CONTROLS (Project with Auto button)
    GtkWidget *ctrl_box = gtk_box_new(GTK_ORIENTATION_VERTICAL, 3);

    GtkWidget *proj_row = gtk_box_new(GTK_ORIENTATION_HORIZONTAL, 6);
    GtkWidget *proj_lbl = gtk_label_new("Project:");
    gtk_widget_set_size_request(proj_lbl, 48, -1);
    gtk_widget_set_halign(proj_lbl, GTK_ALIGN_START);
    gtk_style_context_add_class(gtk_widget_get_style_context(proj_lbl), "field-label");
    gtk_box_pack_start(GTK_BOX(proj_row), proj_lbl, FALSE, FALSE, 0);

    app.combo_proj = gtk_combo_box_text_new_with_entry();
    gtk_box_pack_start(GTK_BOX(proj_row), app.combo_proj, TRUE, TRUE, 0);

    app.btn_auto = gtk_button_new_with_label("Auto");
    gtk_style_context_add_class(gtk_widget_get_style_context(app.btn_auto), "btn-auto");
    g_signal_connect(app.btn_auto, "clicked", G_CALLBACK(+[](GtkButton*, gpointer) {
        load_projects();
    }), NULL);
    gtk_box_pack_start(GTK_BOX(proj_row), app.btn_auto, FALSE, FALSE, 0);

    gtk_box_pack_start(GTK_BOX(ctrl_box), proj_row, FALSE, FALSE, 0);
    gtk_box_pack_start(GTK_BOX(content), ctrl_box, FALSE, FALSE, 0);

    load_projects();

    // ACTION BUTTONS (Solid modern buttons)
    GtkWidget *action_box = gtk_box_new(GTK_ORIENTATION_VERTICAL, 3);

    app.btn_reset = gtk_button_new_with_label("🚀 1-Click Clean Reset");
    gtk_style_context_add_class(gtk_widget_get_style_context(app.btn_reset), "hero-btn");
    g_signal_connect(app.btn_reset, "clicked", G_CALLBACK(on_reset_clicked), NULL);
    gtk_box_pack_start(GTK_BOX(action_box), app.btn_reset, FALSE, FALSE, 0);

    GtkWidget *sub_btns = gtk_box_new(GTK_ORIENTATION_HORIZONTAL, 4);
    app.btn_clear = gtk_button_new_with_label("🧹 Clear Banners");
    gtk_style_context_add_class(gtk_widget_get_style_context(app.btn_clear), "btn-action");
    g_signal_connect(app.btn_clear, "clicked", G_CALLBACK(on_clear_banners_clicked), NULL);
    gtk_box_pack_start(GTK_BOX(sub_btns), app.btn_clear, TRUE, TRUE, 0);

    app.btn_launch = gtk_button_new_with_label("▶ Launch OpenCode");
    gtk_style_context_add_class(gtk_widget_get_style_context(app.btn_launch), "launch-btn");
    g_signal_connect(app.btn_launch, "clicked", G_CALLBACK(on_launch_clicked), NULL);
    gtk_box_pack_start(GTK_BOX(sub_btns), app.btn_launch, TRUE, TRUE, 0);

    gtk_box_pack_start(GTK_BOX(action_box), sub_btns, FALSE, FALSE, 0);
    gtk_box_pack_start(GTK_BOX(content), action_box, FALSE, FALSE, 1);

    // LOG CONSOLE SECTION
    GtkWidget *log_header = gtk_box_new(GTK_ORIENTATION_HORIZONTAL, 4);
    gtk_style_context_add_class(gtk_widget_get_style_context(log_header), "log-header");
    GtkWidget *log_title = gtk_label_new("Activity Log:");
    gtk_style_context_add_class(gtk_widget_get_style_context(log_title), "log-title");
    gtk_box_pack_start(GTK_BOX(log_header), log_title, TRUE, TRUE, 0);

    app.btn_log_clear = gtk_button_new_with_label("Clear");
    gtk_style_context_add_class(gtk_widget_get_style_context(app.btn_log_clear), "btn-log-clear");
    g_signal_connect(app.btn_log_clear, "clicked", G_CALLBACK(+[](GtkButton*, gpointer) {
        if (app.text_log) {
            GtkTextBuffer *buf = gtk_text_view_get_buffer(GTK_TEXT_VIEW(app.text_log));
            gtk_text_buffer_set_text(buf, "", 0);
        }
    }), NULL);
    gtk_box_pack_end(GTK_BOX(log_header), app.btn_log_clear, FALSE, FALSE, 0);
    gtk_box_pack_start(GTK_BOX(content), log_header, FALSE, FALSE, 0);

    app.scroll_log = gtk_scrolled_window_new(NULL, NULL);
    gtk_scrolled_window_set_policy(GTK_SCROLLED_WINDOW(app.scroll_log), GTK_POLICY_AUTOMATIC, GTK_POLICY_AUTOMATIC);
    gtk_widget_set_size_request(app.scroll_log, -1, 105);
    gtk_style_context_add_class(gtk_widget_get_style_context(app.scroll_log), "log-scroller");

    app.text_log = gtk_text_view_new();
    gtk_text_view_set_editable(GTK_TEXT_VIEW(app.text_log), FALSE);
    gtk_text_view_set_cursor_visible(GTK_TEXT_VIEW(app.text_log), FALSE);
    gtk_text_view_set_wrap_mode(GTK_TEXT_VIEW(app.text_log), GTK_WRAP_WORD_CHAR);
    gtk_style_context_add_class(gtk_widget_get_style_context(app.text_log), "log-view");
    gtk_container_add(GTK_CONTAINER(app.scroll_log), app.text_log);
    gtk_box_pack_start(GTK_BOX(content), app.scroll_log, TRUE, TRUE, 2);

    // STATUS FOOTER
    app.lbl_status = gtk_label_new("● Ready");
    gtk_widget_set_halign(app.lbl_status, GTK_ALIGN_START);
    gtk_style_context_add_class(gtk_widget_get_style_context(app.lbl_status), "status-bar");
    gtk_box_pack_end(GTK_BOX(content), app.lbl_status, FALSE, FALSE, 0);

    gtk_widget_show_all(app.window);
    log_line("OpenCode Helper initialized. Ready.");
    trigger_status_check(false);

    // 4-Second VPN / Network Watcher
    g_timeout_add_seconds(4, [](gpointer) -> gboolean {
        trigger_status_check(true);
        return TRUE;
    }, NULL);

    gtk_main();
    return 0;
}
