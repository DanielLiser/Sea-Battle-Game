# 🏴‍☠️ Pirate Sea Battle - Multiplayer Tactical Game

A multiplayer turn-based strategy game built with a Unity (C#) client and a Python Sockets server. 

## 📖 About the Game
In this game, two players face off head-to-head on a naval grid. Each turn is executed simultaneously (both players lock in their moves, and actions unfold at the same time).
**The goal is to sink the enemy ship before they sink yours!
**
### ✨ Key Features:
* **Simultaneous Turns:** Both players plan their moves at the same time, adding a layer of psychology and prediction to the gameplay.
* **Tactical Abilities:**
  * 🚀 **Missiles:** Launch a missile at a specific cell on the board.
  * 💣 **Mines:** Drop an invisible naval mine in your current cell for defense.
  * 🛡️ **Shield:** Protect your ship from incoming damage for a single turn.
* **Dynamic Animations:** Features ballistic missile arcs, explosion effects, water splashes, and physical bounce-back animations for head-on ship collisions.
* **Immersive UI:** A pirate-themed user interface featuring wooden panels, old parchment screens, and a custom health bar.

## 🛠️ Tech Stack
* **Client (Frontend):** Unity Game Engine (C#)
* **Server (Backend):** Python 3 (Sockets, `threading`, `json`)
* **Networking:** TCP/IP Sockets (Real-time communication)

---

## 🚀 Installation & Setup

To run the project locally, you need to start both the Python server and the Unity client.

### 1. Starting the Server (Backend)
1. Ensure you have Python 3 installed on your machine.
2. Open a terminal in the server directory and run:
   ```bash
   python server.py

**The server will start listening for incoming connections.**

2. Starting the Game (Frontend - Unity)
Open the project in Unity.

Load the Lobby scene.

## ⚠️ CRITICAL NOTE: IP Configuration (Local Area Network)⚠️
Because this game uses local Socket communication at the moment (LAN), you must update the IP address in the Unity code to match your machine's IP!

Currently, the NetworkManager.cs script in Unity tries to connect to a local address (like 127.0.0.1 or the specific IP of the host machine).

How to find your IP? Open Command Prompt (CMD) on the machine running the server and type ipconfig. Look for the IPv4 Address.

How to fix it? Open the NetworkManager.cs script in Unity, locate the connection line (e.g., client.Connect("192.168.x.x", port);), and replace it with the IPv4 address you just found. To play with another computer on the same router, you must use the actual IPv4 address, not Localhost.

Hit Play in Unity! (You can also build the game and run two instances to test it against yourself).


#🎮 **How to Play**
In the Lobby screen, click "Find Game" and wait for an opponent to connect.

During each turn (30 seconds), you must choose:

Where to move: Click on an available adjacent cell (highlighted in blue).

Which ability to use: Select Missile, Mine, or Shield (or select none if you just want to move).

Click ACCEPT to lock in your turn.

Once both players have locked in, the server calculates the results, and the animations will play out (movement, explosions, collisions).

The game ends when one of the players reaches 0 Health.

**This is my first attempt creating a multiplayer game, hope you find it enjoyable**

**Thank you and have fun!**
