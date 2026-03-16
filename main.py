import socket
import json
import threading
import time
import tkinter as tk
from tkinter import ttk, messagebox

# ==========================================
# משתנים גלובליים
# ==========================================
state_lock = threading.Lock()
activeThreats = {}
activeMissiles = {}
explosions = []

# רשימת הסוללות הדינמית
batteriesStatus = {}  # format: "name" : {"status": "IDLE", "type": "Arrow", "x": 0}

battle_stats = {
    "detected": 0,
    "intercepted": 0,
    "impacts": 0
}

# הגדרות מסך, קנבס ושוליים (תוקן!)
CANVAS_W = 750
CANVAS_H = 700
PAD_LEFT = 55
PAD_BOTTOM = 35
PAD_TOP = 25  # שוליים עליונים כדי שה-6000 לא ייחתך
PAD_RIGHT = 25  # שוליים ימניים לסימטריה


def map_x(real_x):
    # מחשבים את הרוחב הזמין לציור אחרי הורדת השוליים
    drawable_w = CANVAS_W - PAD_LEFT - PAD_RIGHT
    ratio = drawable_w / 12000.0
    return int(PAD_LEFT + (real_x + 6000) * ratio)


def map_y(real_y):
    # מחשבים את הגובה הזמין לציור אחרי הורדת השוליים למעלה ולמטה
    drawable_h = CANVAS_H - PAD_BOTTOM - PAD_TOP
    ratio = drawable_h / 6000.0
    return int((CANVAS_H - PAD_BOTTOM) - (real_y * ratio))


# ==========================================
# לוגיקת רשת
# ==========================================
def delayed_delete(threat_id, delay_seconds):
    time.sleep(delay_seconds)
    with state_lock:
        if threat_id in activeThreats:
            del activeThreats[threat_id]


def tcp_listener(host='127.0.0.1', port=5000):
    global global_socket
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
        try:
            print(f"Waiting for C# server on {host}:{port}...")
            s.connect((host, port))
            global_socket = s
            print("Connected! Listening for data...")
            stream_reader = s.makefile('r', encoding='utf-8')

            for line in stream_reader:
                if not line: break
                try:
                    event = json.loads(line.strip())
                    update_state(event)
                except json.JSONDecodeError:
                    pass
        except Exception as e:
            print(f"Network thread fatal error: {e}")


def update_state(event):
    with state_lock:
        e_type = event.get("TYPE") or event.get("Type")
        obj_id = event.get("ID") or event.get("THREAT_ID")

        if not obj_id or not e_type: return

        if e_type in ["PHYSICS", "DETECTED"]:
            x, y = event.get("X", 0), event.get("Y", 0)
            if "Threat" in str(obj_id):
                if obj_id not in activeThreats:
                    activeThreats[obj_id] = {"x": x, "y": y, "status": "ACTIVE"}
                    if e_type == "DETECTED": battle_stats["detected"] += 1
                else:
                    if activeThreats[obj_id]["status"] not in ["DESTROYED", "IMPACT"]:
                        activeThreats[obj_id]["x"] = x
                        activeThreats[obj_id]["y"] = y
            else:
                activeMissiles[obj_id] = {"x": x, "y": y}

        elif e_type == "FIRE_MISSLE":
            if obj_id in activeThreats: activeThreats[obj_id]["status"] = "LOCKED"
            bat_name = event.get("BATTERY_NAME")
            if bat_name and bat_name in batteriesStatus:
                batteriesStatus[bat_name]["status"] = f"🔴 Locked on {obj_id[-3:]}"

        elif e_type == "DESTROYED":
            m_id = event.get("MISSLE_ID")
            if obj_id in activeThreats and activeThreats[obj_id]["status"] != "DESTROYED":
                battle_stats["intercepted"] += 1
                activeThreats[obj_id]["status"] = "DESTROYED"
                explosions.append([activeThreats[obj_id]["x"], activeThreats[obj_id]["y"], 15])
                threading.Thread(target=delayed_delete, args=(obj_id, 2.0), daemon=True).start()
            if m_id in activeMissiles: del activeMissiles[m_id]
            for bat in batteriesStatus.values(): bat["status"] = "🟢 IDLE"

        elif e_type == "IMPACT":
            if obj_id in activeThreats and activeThreats[obj_id]["status"] != "IMPACT":
                battle_stats["impacts"] += 1
                activeThreats[obj_id]["status"] = "IMPACT"
                explosions.append([activeThreats[obj_id]["x"], 0, 30])
                threading.Thread(target=delayed_delete, args=(obj_id, 3.0), daemon=True).start()

        elif e_type == "MISSED":
            m_id = event.get("MISSLE_ID")
            for data in activeThreats.values():
                if data["status"] == "LOCKED": data["status"] = "ACTIVE"
            if m_id in activeMissiles: del activeMissiles[m_id]
            for bat in batteriesStatus.values(): bat["status"] = "🟢 IDLE"


# ==========================================
# ממשק המשתמש (GUI)
# ==========================================
class CombinedDashboard:
    def __init__(self, root):
        self.root = root
        self.root.title("🛡️ Hagana Air Defense - Command & Control")
        self.root.geometry("1450x850")
        self.root.configure(bg="#1e1e1e")
        self.battery_counter = 1

        style = ttk.Style()
        style.theme_use("clam")
        style.configure("TFrame", background="#1e1e1e")
        style.configure("TLabelFrame", background="#1e1e1e", foreground="#00ff00", font=("Consolas", 12, "bold"))
        style.configure("TLabel", background="#1e1e1e", foreground="white", font=("Consolas", 12))
        style.configure("Treeview", background="#2d2d2d", foreground="white", fieldbackground="#2d2d2d",
                        font=("Consolas", 11))
        style.configure("Treeview.Heading", font=("Consolas", 12, "bold"), background="#444444", foreground="white")

        self.left_frame = tk.Frame(self.root, bg="black", width=CANVAS_W, height=CANVAS_H)
        self.left_frame.pack(side="left", padx=10, pady=10, fill="both", expand=True)

        self.right_frame = ttk.Frame(self.root)
        self.right_frame.pack(side="right", padx=10, pady=10, fill="y", expand=False)

        self.canvas = tk.Canvas(self.left_frame, width=CANVAS_W, height=CANVAS_H, bg='#0a1526', highlightthickness=1,
                                highlightbackground="#00ccff")
        self.canvas.pack(fill="both", expand=True)

        self.deploy_frame = ttk.LabelFrame(self.right_frame, text=" [ DEPLOY & COMMAND ] ", padding=15)
        self.deploy_frame.pack(fill="x", pady=5)

        ttk.Label(self.deploy_frame, text="System:").grid(row=0, column=0, padx=5, pady=5)
        self.type_var = tk.StringVar()
        self.type_cb = ttk.Combobox(self.deploy_frame, textvariable=self.type_var, values=["IronDome", "Arrow"],
                                    state="readonly", width=12)
        self.type_cb.current(0)
        self.type_cb.grid(row=0, column=1, padx=5, pady=5)

        ttk.Label(self.deploy_frame, text="X Coor:").grid(row=1, column=0, padx=5, pady=5)
        self.x_entry = ttk.Entry(self.deploy_frame, width=14)
        self.x_entry.insert(0, "0")
        self.x_entry.grid(row=1, column=1, padx=5, pady=5)

        self.btn_deploy = tk.Button(self.deploy_frame, text="🛠️ DEPLOY", bg="#006600", fg="white",
                                    font=("Consolas", 10, "bold"), command=self.deploy_launcher)
        self.btn_deploy.grid(row=0, column=2, rowspan=2, padx=10, sticky="nsew")

        self.btn_start = tk.Button(self.deploy_frame, text="🔥 START BATTLE", bg="#cc0000", fg="white",
                                   font=("Consolas", 10, "bold"), command=self.start_battle)
        self.btn_start.grid(row=0, column=3, rowspan=2, padx=10, sticky="nsew")

        self.launchers_frame = ttk.LabelFrame(self.right_frame, text=" [ LAUNCHERS STATUS ] ", padding=15)
        self.launchers_frame.pack(fill="x", pady=5)
        self.battery_labels = {}

        self.threats_frame = ttk.LabelFrame(self.right_frame, text=" [ ACTIVE THREATS IN AIR ] ", padding=15)
        self.threats_frame.pack(fill="both", expand=True, pady=5)
        self.tree = ttk.Treeview(self.threats_frame, columns=("ID", "Altitude"), show="headings", height=12)
        self.tree.heading("ID", text="🚀 Threat ID")
        self.tree.heading("Altitude", text="Altitude (m)")
        self.tree.column("ID", width=180, anchor="center")
        self.tree.column("Altitude", width=150, anchor="center")
        self.tree.tag_configure("ACTIVE", foreground="white")
        self.tree.tag_configure("LOCKED", foreground="yellow")
        self.tree.tag_configure("DESTROYED", foreground="#00ff00")
        self.tree.tag_configure("IMPACT", foreground="#ff4444")
        self.tree.pack(fill="both", expand=True)

        self.stats_frame = ttk.LabelFrame(self.right_frame, text=" [ BATTLE STATISTICS ] ", padding=15)
        self.stats_frame.pack(fill="x", side="bottom", pady=5)
        self.lbl_detected = ttk.Label(self.stats_frame, text="📡 Detected: 0", font=("Consolas", 13, "bold"),
                                      foreground="#00ccff")
        self.lbl_detected.pack(anchor="w", pady=2)
        self.lbl_intercepted = ttk.Label(self.stats_frame, text="✅ Intercepted: 0", font=("Consolas", 13, "bold"),
                                         foreground="#00ff00")
        self.lbl_intercepted.pack(anchor="w", pady=2)
        self.lbl_impacts = ttk.Label(self.stats_frame, text="💥 Impacts: 0", font=("Consolas", 13, "bold"),
                                     foreground="#ff4444")
        self.lbl_impacts.pack(anchor="w", pady=2)
        self.lbl_rate = ttk.Label(self.stats_frame, text="📊 Success Rate: 0%", font=("Consolas", 13, "bold"),
                                  foreground="white")
        self.lbl_rate.pack(anchor="w", pady=2)

        self.update_gui()

    def deploy_launcher(self):
        global global_socket
        try:
            x_val = int(self.x_entry.get())
            if x_val < -6000 or x_val > 6000:
                messagebox.showerror("Error", "X must be between -6000 and +6000")
                return

            b_type = self.type_var.get()
            prefix = "iron" if b_type == "IronDome" else "arrow"
            b_name = f"{prefix}{self.battery_counter}"
            self.battery_counter += 1

            if global_socket:
                command = {"COMMAND": "ADD_BATTERY", "BAT_TYPE": b_type, "NAME": b_name, "X": x_val}
                msg_str = json.dumps(command) + "\n"
                global_socket.sendall(msg_str.encode('utf-8'))

                with state_lock:
                    batteriesStatus[b_name] = {"status": "🟢 IDLE", "type": b_type, "x": x_val}
                    lbl = ttk.Label(self.launchers_frame, text=f"{b_name:<15} | 🟢 IDLE")
                    lbl.pack(anchor="w", pady=2)
                    self.battery_labels[b_name] = lbl
            else:
                messagebox.showwarning("Connection", "Not connected to C# server yet!")
        except ValueError:
            messagebox.showerror("Error", "Please enter a valid integer for X")

    def start_battle(self):
        global global_socket
        if global_socket:
            if len(batteriesStatus) == 0:
                if not messagebox.askyesno("Warning", "You have NO batteries deployed! Start anyway?"):
                    return
            msg_str = json.dumps({"COMMAND": "START"}) + "\n"
            global_socket.sendall(msg_str.encode('utf-8'))
            self.btn_start.config(state="disabled", text="⚔️ RUNNING...", bg="gray")
        else:
            messagebox.showwarning("Connection", "Not connected to C# server yet!")

    def update_gui(self):
        global explosions
        with state_lock:
            self.canvas.delete("all")

            # צירים מעודכנים עם השוליים החדשים!
            self.canvas.create_line(PAD_LEFT, PAD_TOP, PAD_LEFT, CANVAS_H - PAD_BOTTOM, fill="#00ccff", width=2)
            self.canvas.create_line(PAD_LEFT, CANVAS_H - PAD_BOTTOM, CANVAS_W - PAD_RIGHT, CANVAS_H - PAD_BOTTOM,
                                    fill="#00ccff", width=2)

            for h in range(0, 6001, 1000):
                y_pos = map_y(h)
                self.canvas.create_line(PAD_LEFT - 5, y_pos, PAD_LEFT, y_pos, fill="#00ccff", width=2)
                # ה-e באנכור אומר שהטקסט צמוד לימין, כך שהוא לא יחרוג שמאלה מדי
                self.canvas.create_text(PAD_LEFT - 10, y_pos, text=str(h), fill="#00ccff", font=("Consolas", 9),
                                        anchor="e")
                self.canvas.create_line(PAD_LEFT, y_pos, CANVAS_W - PAD_RIGHT, y_pos, fill="#1a3355", dash=(2, 4))

            for r in range(-6000, 6001, 2000):
                x_pos = map_x(r)
                self.canvas.create_line(x_pos, CANVAS_H - PAD_BOTTOM, x_pos, CANVAS_H - PAD_BOTTOM + 5, fill="#00ccff",
                                        width=2)
                self.canvas.create_text(x_pos, CANVAS_H - PAD_BOTTOM + 15, text=str(r), fill="#00ccff",
                                        font=("Consolas", 9))
                self.canvas.create_line(x_pos, PAD_TOP, x_pos, CANVAS_H - PAD_BOTTOM, fill="#1a3355", dash=(2, 4))

            # גבול גזרה
            split_y = map_y(3000)
            self.canvas.create_line(PAD_LEFT, split_y, CANVAS_W - PAD_RIGHT, split_y, fill="yellow", dash=(6, 4),
                                    width=1)
            self.canvas.create_text(PAD_LEFT + 5, split_y - 10, text="ARROW ZONE (>3000m)", fill="yellow", anchor="w",
                                    font=("Consolas", 8, "bold"))
            self.canvas.create_text(PAD_LEFT + 5, split_y + 10, text="IRON DOME ZONE (<3000m)", fill="yellow",
                                    anchor="w", font=("Consolas", 8, "bold"))

            # ציור משגרי הסוללות
            for b_name, b_data in batteriesStatus.items():
                bx = map_x(b_data["x"])
                by = map_y(0)
                b_color = "cyan" if b_data["type"] == "IronDome" else "#00ff00"
                self.canvas.create_polygon(bx, by - 15, bx - 10, by, bx + 10, by, fill=b_color, outline="white")
                self.canvas.create_text(bx, by - 25, text=b_name, fill=b_color, font=("Arial", 8))

            # ציור איומים ומיירטים
            for t_id, data in activeThreats.items():
                if data["status"] not in ["DESTROYED", "IMPACT"]:
                    cx, cy = map_x(data["x"]), map_y(data["y"])
                    color = "red" if data["status"] == "ACTIVE" else "yellow"
                    self.canvas.create_oval(cx - 4, cy - 4, cx + 4, cy + 4, fill=color, outline="white")
                    self.canvas.create_text(cx + 10, cy - 10, text=t_id[-3:], fill=color, font=("Consolas", 8, "bold"),
                                            anchor="w")

            for m_id, data in activeMissiles.items():
                cx, cy = map_x(data["x"]), map_y(data["y"])
                self.canvas.create_oval(cx - 3, cy - 3, cx + 3, cy + 3, fill="#00ff00")
                self.canvas.create_line(cx, cy, cx, cy + 15, fill="#00ff00")
                sys_type = "Arrow" if "arrow" in m_id.lower() else "IronDome"
                self.canvas.create_text(cx + 5, cy + 10, text=sys_type, fill="#00ff00", font=("Consolas", 7),
                                        anchor="w")

            # עדכון GUI (טבלאות ולייבלים)
            for bat_name, b_data in batteriesStatus.items():
                color = "#ff4444" if "Locked" in b_data["status"] else "#00cc00"
                if bat_name in self.battery_labels:
                    self.battery_labels[bat_name].config(text=f"{bat_name:<10} | {b_data['status']}", foreground=color)

            for item in self.tree.get_children(): self.tree.delete(item)
            for t_id, data in sorted(activeThreats.items(), key=lambda x: x[1]["y"], reverse=True):
                self.tree.insert("", "end", values=(t_id, f"{data['y']:,.0f}"), tags=(data["status"],))

            detected = battle_stats["detected"]
            hits = battle_stats["intercepted"]
            misses = battle_stats["impacts"]
            total = hits + misses
            rate = (hits / total * 100) if total > 0 else 0

            self.lbl_detected.config(text=f"📡 Detected: {detected}")
            self.lbl_intercepted.config(text=f"✅ Intercepted: {hits}")
            self.lbl_impacts.config(text=f"💥 Impacts: {misses}")

            if total == 0:
                rate_color = "white"
            elif rate >= 70:
                rate_color = "#00ff00"
            elif rate >= 40:
                rate_color = "yellow"
            else:
                rate_color = "#ff4444"
            self.lbl_rate.config(text=f"📊 Success Rate: {rate:.1f}%", foreground=rate_color)

            active_explosions = []
            for exp in explosions:
                ex, ey, timer = exp
                cx, cy = map_x(ex), map_y(ey)
                size = (35 - timer) * 1.5
                self.canvas.create_oval(cx - size, cy - size, cx + size, cy + size, fill="orange", outline="yellow",
                                        width=2)
                exp[2] -= 1
                if exp[2] > 0: active_explosions.append(exp)
            explosions = active_explosions

        self.root.after(30, self.update_gui)


if __name__ == "__main__":
    listener_thread = threading.Thread(target=tcp_listener, daemon=True)
    listener_thread.start()

    root = tk.Tk()
    app = CombinedDashboard(root)
    root.mainloop()