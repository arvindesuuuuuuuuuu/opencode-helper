#include <gtk/gtk.h>
#include <string>
#include <vector>
#include <thread>
#include <sstream>
#include <iostream>
#include <cstdlib>
#include <unistd.h>
#include <sys/wait.h>
#include <chrono>
#include <ctime>

const std::string HOME = std::getenv("HOME") ? std::getenv("HOME") : "/home/arvin";
const std::string UNLIMITED_BIN = HOME + "/Infosoft/OpenCode-Unlimited/bin/opencode-unlimited";
const std::string EXPRESSVPN_BIN = "/opt/expressvpn/bin/expressvpnctl";

struct AppWidgets {
    GtkWidget *window;
    GtkWidget *lbl_ip;
    GtkWidget *lbl_quota;
    GtkWidget *combo_vpn;
    GtkWidget *combo_proj;
    GtkWidget *btn_reset;
    GtkWidget *btn_switch;
    GtkWidget *btn_clear;
    GtkWidget *btn_launch;
    GtkWidget *lbl_status;
    GtkWidget *text_log;
    GtkWidget *scroll_log;
    GtkWidget *btn_log_clear;
};

static AppWidgets app;

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

struct StatusData {
    std::string ip_info;
    std::string quota_text;
    bool is_ready;
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

    gtk_label_set_text(GTK_LABEL(app.lbl_status), "● Idle");
    log_line("Network: " + data->ip_info);
    log_line("Quota status: " + data->quota_text);
    delete data;
    return FALSE;
}

void check_status_thread() {
    StatusData *data = new StatusData();
    data->is_ready = false;

    // 1. IP check
    std::string ip_raw = exec_cmd("curl -s --max-time 3 https://ipinfo.io/json");
    std::string ip = "Offline", city = "", country = "";
    size_t ip_pos = ip_raw.find("\"ip\": \"");
    if (ip_pos != std::string::npos) {
        size_t end = ip_raw.find("\"", ip_pos + 7);
        if (end != std::string::npos) ip = ip_raw.substr(ip_pos + 7, end - (ip_pos + 7));
    }
    size_t city_pos = ip_raw.find("\"city\": \"");
    if (city_pos != std::string::npos) {
        size_t end = ip_raw.find("\"", city_pos + 9);
        if (end != std::string::npos) city = ip_raw.substr(city_pos + 9, end - (city_pos + 9));
    }
    size_t c_pos = ip_raw.find("\"country\": \"");
    if (c_pos != std::string::npos) {
        size_t end = ip_raw.find("\"", c_pos + 12);
        if (end != std::string::npos) country = ip_raw.substr(c_pos + 12, end - (c_pos + 12));
    }
    data->ip_info = "🌐 " + ip + (city.empty() ? "" : "  •  " + city + ", " + country);

    // 2. Real Quota check via OpenCode-Unlimited ledger
    if (ip == "Offline") {
        data->quota_text = "● Connection Error";
        data->is_ready = false;
    } else {
        std::string egress_check = exec_cmd("node \"" + HOME + "/Infosoft/OpenCode-Unlimited/lib/egress.js\" check \"" + ip + "\" 2>/dev/null");
        if (egress_check.find("\"spent\": true") != std::string::npos || egress_check.find("\"spent\":true") != std::string::npos) {
            data->quota_text = "● Rate-Limited (Switch Region)";
            data->is_ready = false;
        } else {
            data->quota_text = "● Ready (Free Tier Available)";
            data->is_ready = true;
        }
    }

    g_idle_add(on_status_checked, data);
}

void trigger_status_check() {
    log_line("Probing IP & OpenCode gateway status...");
    gtk_label_set_text(GTK_LABEL(app.lbl_status), "● Probing gateway...");
    std::thread(check_status_thread).detach();
}

static gboolean on_status_msg(gpointer user_data) {
    char *msg = (char *)user_data;
    gtk_label_set_text(GTK_LABEL(app.lbl_status), msg);
    free(msg);
    return FALSE;
}

void set_status(const std::string &msg) {
    g_idle_add(on_status_msg, strdup(msg.c_str()));
}

void on_switch_vpn(GtkButton *btn, gpointer data) {
    const char *reg = gtk_combo_box_text_get_active_text(GTK_COMBO_BOX_TEXT(app.combo_vpn));
    if (!reg) return;
    std::string region = reg;

    log_line("════════════════════════════════════");
    log_line("Switching ExpressVPN to: " + region);
    set_status("● Connecting to " + region + "...");
    std::thread([region]() {
        exec_cmd_streaming(EXPRESSVPN_BIN + " connect \"" + region + "\" 2>&1", true);
        log_line("✔ VPN command finished. Probing network status...");
        set_status("● Connected! Checking IP...");
        check_status_thread();
    }).detach();
}

void on_reset_clicked(GtkButton *btn, gpointer data) {
    const char *proj_text = gtk_combo_box_text_get_active_text(GTK_COMBO_BOX_TEXT(app.combo_proj));
    if (!proj_text) return;
    std::string proj = proj_text;

    log_line("════════════════════════════════════");
    log_line("🚀 1-Click Clean Reset initiated");
    log_line("Target directory: " + proj);
    set_status("● Closing OpenCode & resetting...");

    std::thread([proj]() {
        log_line("Terminating OpenCode instances (safe)...");
        exec_cmd("pkill -x ai.opencode 2>/dev/null; pkill -f 'ai.opencode' 2>/dev/null; pkill -f 'opencode-cli serve' 2>/dev/null");
        usleep(300000);

        log_line("Running opencode-unlimited clear...");
        exec_cmd_streaming(UNLIMITED_BIN + " clear 2>&1", true);

        log_line("Running opencode-unlimited reset \"" + proj + "\"...");
        exec_cmd_streaming(UNLIMITED_BIN + " reset \"" + proj + "\" 2>&1", true);

        log_line("✔ Clean reset finished! All chat history preserved.");
        set_status("● Reset done! Launch OpenCode.");
        check_status_thread();
    }).detach();
}

void on_clear_banners_clicked(GtkButton *btn, gpointer data) {
    log_line("════════════════════════════════════");
    log_line("🧹 Clearing stored error banners...");
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
        "    margin-bottom: 4px;\n"
        "}\n"
        ".stepper-card {\n"
        "    background-color: #10121a;\n"
        "    border-radius: 5px;\n"
        "    border: 1px solid #1a1d2a;\n"
        "    padding: 4px 8px;\n"
        "    margin-bottom: 4px;\n"
        "}\n"
        ".guide-card {\n"
        "    background-color: #10121d;\n"
        "    border-radius: 6px;\n"
        "    border: 1px solid #1c2033;\n"
        "    padding: 6px 9px;\n"
        "    margin-bottom: 4px;\n"
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
        ".btn-switch {\n"
        "    background-image: linear-gradient(to bottom, #2563eb, #1d4ed8);\n"
        "    border: 1px solid #3b82f6;\n"
        "    color: #ffffff;\n"
        "    font-weight: 700;\n"
        "    min-height: 22px;\n"
        "    padding: 1px 10px;\n"
        "    border-radius: 4px;\n"
        "}\n"
        ".btn-switch:hover {\n"
        "    background-image: linear-gradient(to bottom, #3b82f6, #2563eb);\n"
        "    border-color: #60a5fa;\n"
        "}\n"
        ".hero-btn {\n"
        "    background-image: linear-gradient(to bottom, #2563eb, #1d4ed8);\n"
        "    border: 1px solid #3b82f6;\n"
        "    color: #ffffff;\n"
        "    font-size: 10px;\n"
        "    padding: 4px 10px;\n"
        "    min-height: 26px;\n"
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
    // Resizable window with live log console
    gtk_window_set_default_size(GTK_WINDOW(app.window), 380, 480);
    gtk_window_set_resizable(GTK_WINDOW(app.window), TRUE);
    gtk_window_set_position(GTK_WINDOW(app.window), GTK_WIN_POS_CENTER);

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
    // 1. CUSTOM SLEEK TITLEBAR (Draggable & Vertically Centered)
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

    // Check button in header (properly vertically centered)
    GtkWidget *btn_check = gtk_button_new_with_label("🔄 Check");
    gtk_widget_set_valign(btn_check, GTK_ALIGN_CENTER);
    gtk_widget_set_size_request(btn_check, -1, 20);
    gtk_style_context_add_class(gtk_widget_get_style_context(btn_check), "btn-check");
    g_signal_connect(btn_check, "clicked", G_CALLBACK(+[](GtkButton *, gpointer) { trigger_status_check(); }), NULL);
    gtk_box_pack_start(GTK_BOX(titlebar_box), btn_check, FALSE, FALSE, 0);

    // Subtle divider (vertically centered)
    GtkWidget *divider = gtk_box_new(GTK_ORIENTATION_HORIZONTAL, 0);
    gtk_widget_set_valign(divider, GTK_ALIGN_CENTER);
    gtk_widget_set_size_request(divider, 1, 14);
    gtk_style_context_add_class(gtk_widget_get_style_context(divider), "title-divider");
    gtk_box_pack_start(GTK_BOX(titlebar_box), divider, FALSE, FALSE, 0);

    // Circular Minimize Button (perfect 20x20 circle, vertically centered)
    GtkWidget *btn_min = gtk_button_new_with_label("─");
    gtk_widget_set_valign(btn_min, GTK_ALIGN_CENTER);
    gtk_widget_set_size_request(btn_min, 20, 20);
    gtk_style_context_add_class(gtk_widget_get_style_context(btn_min), "win-btn");
    gtk_style_context_add_class(gtk_widget_get_style_context(btn_min), "win-min");
    g_signal_connect(btn_min, "clicked", G_CALLBACK(+[](GtkButton *, gpointer win) {
        gtk_window_iconify(GTK_WINDOW(win));
    }), app.window);
    gtk_box_pack_start(GTK_BOX(titlebar_box), btn_min, FALSE, FALSE, 0);

    // Circular Close Button (perfect 20x20 circle, vertically centered)
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
    GtkWidget *t1 = gtk_label_new("If Quota is Limited → Select VPN & click 'Switch'");
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

    // CONTROLS (VPN + Project)
    GtkWidget *ctrl_box = gtk_box_new(GTK_ORIENTATION_VERTICAL, 3);

    // VPN Row
    GtkWidget *vpn_row = gtk_box_new(GTK_ORIENTATION_HORIZONTAL, 6);
    GtkWidget *vpn_lbl = gtk_label_new("VPN:");
    gtk_widget_set_size_request(vpn_lbl, 48, -1);
    gtk_widget_set_halign(vpn_lbl, GTK_ALIGN_START);
    gtk_style_context_add_class(gtk_widget_get_style_context(vpn_lbl), "field-label");
    gtk_box_pack_start(GTK_BOX(vpn_row), vpn_lbl, FALSE, FALSE, 0);

    app.combo_vpn = gtk_combo_box_text_new();
    const char *regions[] = {
        "singapore-jurong", "singapore-cbd", "japan-tokyo",
        "japan-shibuya", "usa-new-york", "usa-los-angeles", "taiwan-3", "smart"
    };
    for (const char *r : regions) gtk_combo_box_text_append_text(GTK_COMBO_BOX_TEXT(app.combo_vpn), r);
    gtk_combo_box_set_active(GTK_COMBO_BOX(app.combo_vpn), 0);
    gtk_box_pack_start(GTK_BOX(vpn_row), app.combo_vpn, TRUE, TRUE, 0);

    app.btn_switch = gtk_button_new_with_label("Switch");
    gtk_style_context_add_class(gtk_widget_get_style_context(app.btn_switch), "btn-switch");
    g_signal_connect(app.btn_switch, "clicked", G_CALLBACK(on_switch_vpn), NULL);
    gtk_box_pack_start(GTK_BOX(vpn_row), app.btn_switch, FALSE, FALSE, 0);

    gtk_box_pack_start(GTK_BOX(ctrl_box), vpn_row, FALSE, FALSE, 0);

    // Project Row
    GtkWidget *proj_row = gtk_box_new(GTK_ORIENTATION_HORIZONTAL, 6);
    GtkWidget *proj_lbl = gtk_label_new("Project:");
    gtk_widget_set_size_request(proj_lbl, 48, -1);
    gtk_widget_set_halign(proj_lbl, GTK_ALIGN_START);
    gtk_style_context_add_class(gtk_widget_get_style_context(proj_lbl), "field-label");
    gtk_box_pack_start(GTK_BOX(proj_row), proj_lbl, FALSE, FALSE, 0);

    app.combo_proj = gtk_combo_box_text_new_with_entry();
    gtk_combo_box_text_append_text(GTK_COMBO_BOX_TEXT(app.combo_proj), HOME.c_str());
    gtk_combo_box_text_append_text(GTK_COMBO_BOX_TEXT(app.combo_proj), (HOME + "/trickcal-launcher").c_str());
    gtk_combo_box_text_append_text(GTK_COMBO_BOX_TEXT(app.combo_proj), (HOME + "/Infosoft/surigao-hris-web").c_str());
    gtk_combo_box_set_active(GTK_COMBO_BOX(app.combo_proj), 0);
    gtk_box_pack_start(GTK_BOX(proj_row), app.combo_proj, TRUE, TRUE, 0);

    gtk_box_pack_start(GTK_BOX(ctrl_box), proj_row, FALSE, FALSE, 0);
    gtk_box_pack_start(GTK_BOX(content), ctrl_box, FALSE, FALSE, 0);

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
    trigger_status_check();

    gtk_main();
    return 0;
}
